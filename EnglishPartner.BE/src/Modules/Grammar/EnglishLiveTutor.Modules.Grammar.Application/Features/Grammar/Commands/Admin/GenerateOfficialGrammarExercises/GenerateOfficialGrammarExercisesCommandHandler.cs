using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.GenerateOfficialGrammarExercises;

public sealed class GenerateOfficialGrammarExercisesCommandHandler
    : ICommandHandler<GenerateOfficialGrammarExercisesCommand, GenerateOfficialGrammarExercisesResponse>
{
    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _timeProvider;
    private readonly IGrammarExercisePromptContextLoader _contextLoader;
    private readonly IGrammarExercisePromptBuilder _promptBuilder;
    private readonly IGrammarExerciseAiGenerator _aiGenerator;
    private readonly IGrammarExerciseOutputProcessor _processor;

    public GenerateOfficialGrammarExercisesCommandHandler(
        IGrammarDbContext db,
        IUnitOfWork uow,
        TimeProvider timeProvider,
        IGrammarExercisePromptContextLoader contextLoader,
        IGrammarExercisePromptBuilder promptBuilder,
        IGrammarExerciseAiGenerator aiGenerator,
        IGrammarExerciseOutputProcessor processor)
    {
        _db = db;
        _uow = uow;
        _timeProvider = timeProvider;
        _contextLoader = contextLoader;
        _promptBuilder = promptBuilder;
        _aiGenerator = aiGenerator;
        _processor = processor;
    }

    public async Task<GenerateOfficialGrammarExercisesResponse> HandleAsync(
        GenerateOfficialGrammarExercisesCommand command,
        CancellationToken ct = default)
    {
        string normalizedCode = command.StepCode.Trim().ToUpperInvariant();
        var step = await _db.GrammarSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Step '{command.StepCode}' không tồn tại.");
        }

        bool isImport = command.ImportJsonText is not null;
        string rawResponse;
        if (isImport)
        {
            // Manual mode: the admin pasted JSON produced from the same prompt elsewhere.
            rawResponse = command.ImportJsonText!;
        }
        else
        {
            // 1. Build prompt
            var promptInput = await _contextLoader.LoadContextAsync(
                command.StepCode,
                command.Request.Items,
                learnerNote: null,
                ct);

            string promptText = _promptBuilder.Build(promptInput);

            // 2. Call AI generator outside transaction
            rawResponse = await _aiGenerator.GenerateRawAsync(
                promptText,
                command.Request.AiConfig ?? new GrammarAiExecutionConfig(),
                ct);
        }

        // 3. Load existing hashes for deduplication
        var existingHashes = (await _db.GrammarExercises.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.StepId == step.Id && !e.IsDeleted &&
                        (e.Source == GrammarExerciseSource.Official ||
                         e.Moderation == GrammarModerationStatus.Approved))
            .Select(e => e.ContentHash)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 4. Process AI output
        var (valid, rejected) = _processor.Process(
            rawResponse,
            isImport && command.Request.Items.Count == 0 ? null : command.Request.Items,
            existingHashes);

        if (valid.Count > GrammarRules.MaxAdminItemsPerRequest)
        {
            valid = valid.Take(GrammarRules.MaxAdminItemsPerRequest).ToList();
        }

        if (valid.Count == 0)
        {
            throw new UnprocessableEntityException(
                "grammar_generation_no_valid_exercises",
                new Dictionary<string, object?> { ["rejected"] = rejected });
        }

        // 5. Generate official keys
        var existingKeys = (await _db.GrammarExercises.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.StepId == step.Id && e.Key != null)
            .Select(e => e.Key!)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int keyCounter = 1;
        var now = _timeProvider.GetUtcNow();
        var createdDtos = new List<GrammarExerciseAdminDto>(valid.Count);

        foreach (var v in valid)
        {
            string key;
            do
            {
                key = $"{step.Code}-E{keyCounter:D2}";
                keyCounter++;
            } while (existingKeys.Contains(key));

            existingKeys.Add(key);

            var exercise = GrammarExerciseEntity.CreateOfficial(
                stepId: step.Id,
                key: key,
                type: v.Type,
                difficulty: v.Difficulty,
                title: v.Title,
                instructions: v.Instructions,
                contentJson: v.ContentJson,
                gradingDefinitionJson: v.GradingDefinitionJson,
                maxPoints: v.MaxPoints,
                gradingMode: v.GradingMode,
                isCore: command.Request.IsCore,
                contentHash: v.ContentHash,
                now: now,
                focus: v.Focus);

            _db.GrammarExercises.Add(exercise);

            JsonElement contentElem;
            using (var doc = JsonDocument.Parse(exercise.ContentJson))
            {
                contentElem = doc.RootElement.Clone();
            }

            JsonElement? gradingElem = null;
            if (!string.IsNullOrWhiteSpace(exercise.GradingDefinitionJson))
            {
                using var doc = JsonDocument.Parse(exercise.GradingDefinitionJson);
                gradingElem = doc.RootElement.Clone();
            }

            createdDtos.Add(new GrammarExerciseAdminDto(
                Id: exercise.Id,
                StepId: exercise.StepId,
                Key: exercise.Key,
                Type: exercise.Type,
                Difficulty: exercise.Difficulty,
                Title: exercise.Title,
                Instructions: exercise.Instructions,
                Focus: exercise.Focus,
                Content: contentElem,
                Grading: gradingElem,
                MaxPoints: exercise.MaxPoints,
                GradingMode: exercise.GradingMode,
                Source: exercise.Source,
                OwnerUserId: exercise.OwnerUserId,
                AuthorUserId: exercise.AuthorUserId,
                Visibility: exercise.Visibility,
                Moderation: exercise.Moderation,
                RejectReason: exercise.RejectReason,
                ReviewedBy: exercise.ReviewedBy,
                ReviewedAtUtc: exercise.ReviewedAtUtc,
                IsCore: exercise.IsCore,
                ContentHash: exercise.ContentHash,
                CreatedAtUtc: exercise.CreatedAtUtc,
                UpdatedAtUtc: exercise.UpdatedAtUtc
            ));
        }

        await _uow.CommitChangesAsync(ct);

        return new GenerateOfficialGrammarExercisesResponse(createdDtos, rejected);
    }
}
