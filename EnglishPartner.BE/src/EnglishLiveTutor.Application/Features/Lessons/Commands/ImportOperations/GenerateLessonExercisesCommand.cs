using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;

public sealed record GenerateLessonExercisesCommand(
    string? ApiKey,
    string? ModelCode,
    Guid? TargetLessonId,
    string Topic,
    string CefrLevel,
    string? LessonTitle = null,
    List<string>? PreferredExerciseTypes = null,
    int TargetExerciseCount = 5,
    string? CustomInstructions = null,
    string CommitMode = "PreviewOnly",
    string? OperationKey = null
) : ICommand<LessonCandidateValidationResultDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class GenerateLessonExercisesCommandHandler : ICommandHandler<GenerateLessonExercisesCommand, LessonCandidateValidationResultDto>
{
    private const int MaxRepairRetries = 1;

    private readonly IAppDbContext _db;
    private readonly ILessonPromptBuilder _promptBuilder;
    private readonly ILessonAiGenerator _aiGenerator;
    private readonly ILessonCandidateValidator _validator;
    private readonly ISender _sender;

    public GenerateLessonExercisesCommandHandler(
        IAppDbContext db,
        ILessonPromptBuilder promptBuilder,
        ILessonAiGenerator aiGenerator,
        ILessonCandidateValidator validator,
        ISender sender)
    {
        _db = db;
        _promptBuilder = promptBuilder;
        _aiGenerator = aiGenerator;
        _validator = validator;
        _sender = sender;
    }

    public async Task<LessonCandidateValidationResultDto> HandleAsync(GenerateLessonExercisesCommand command, CancellationToken ct = default)
    {
        string topic = command.Topic;
        string cefr = command.CefrLevel;
        string title = !string.IsNullOrWhiteSpace(command.LessonTitle) ? command.LessonTitle : $"Lesson: {topic}";

        if (command.TargetLessonId.HasValue && command.TargetLessonId.Value != Guid.Empty)
        {
            var targetLesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == command.TargetLessonId.Value, ct);
            if (targetLesson == null)
            {
                throw new NotFoundException($"Target lesson with ID '{command.TargetLessonId.Value}' not found.");
            }

            topic = targetLesson.Topic;
            cefr = targetLesson.CefrLevel;
            title = targetLesson.Title;
        }

        string requestedModel = string.IsNullOrWhiteSpace(command.ModelCode) ? "gemini-3.8-flash" : command.ModelCode.Trim();
        var modelEntity = await _db.AiModelCatalog.AsNoTracking().FirstOrDefaultAsync(m => m.ModelCode == requestedModel, ct);
        string effectiveModel = modelEntity != null ? modelEntity.GetEffectiveModelCode() : requestedModel;

        var promptInput = new LessonPromptInput(
            Topic: topic,
            CefrLevel: cefr,
            LessonTitle: title,
            PreferredExerciseTypes: command.PreferredExerciseTypes,
            TargetExerciseCount: command.TargetExerciseCount,
            CustomInstructions: command.CustomInstructions);

        var promptResult = await _promptBuilder.BuildPromptAsync(promptInput, ct);

        // 1. Initial generation
        string generatedJson = await _aiGenerator.GenerateLessonExercisesJsonAsync(
            promptResult.PromptText,
            command.ApiKey,
            effectiveModel,
            ct);

        JsonNode? candidateNode = ParseCandidateJson(generatedJson);
        var validationResult = await _validator.ValidateCandidateAsync(candidateNode, ct);

        // 2. Bounded repair retry if output has validation issues
        if (!validationResult.IsValid && candidateNode != null && MaxRepairRetries > 0)
        {
            string issuesList = string.Join("\n", validationResult.Issues
                .Where(i => i.Severity == "Error")
                .Take(5)
                .Select(i => $"- [{i.Path}]: {i.Message}"));

            string repairPrompt = $@"{promptResult.PromptText}

CRITICAL REPAIR INSTRUCTIONS:
The previously generated output failed schema validation with these errors:
{issuesList}

Regenerate the complete lesson exercises adhering strictly to the JSON schema without these errors.";

            try
            {
                string repairedJson = await _aiGenerator.GenerateLessonExercisesJsonAsync(
                    repairPrompt,
                    command.ApiKey,
                    effectiveModel,
                    ct);

                var repairedNode = ParseCandidateJson(repairedJson);
                var revalidated = await _validator.ValidateCandidateAsync(repairedNode, ct);
                if (revalidated.IsValid || revalidated.Issues.Count < validationResult.Issues.Count)
                {
                    validationResult = revalidated;
                    candidateNode = repairedNode;
                }
            }
            catch
            {
                // Preserve initial validation issues if repair call fails
            }
        }

        // 3. Commit branch if requested, valid, and target provided
        if (command.CommitMode is "Append" or "Replace" && command.TargetLessonId.HasValue && validationResult.IsValid && candidateNode != null)
        {
            var commitReq = new CommitLessonImportRequest(
                TargetLessonId: command.TargetLessonId.Value,
                TargetRevision: null,
                CommitMode: command.CommitMode,
                Candidate: candidateNode);

            // Delegate to shared commit use case via MediatR pipeline
            await _sender.Send(new CommitLessonImportCommand(commitReq, command.OperationKey), ct);
        }

        return validationResult;
    }

    private static JsonNode? ParseCandidateJson(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }
}
