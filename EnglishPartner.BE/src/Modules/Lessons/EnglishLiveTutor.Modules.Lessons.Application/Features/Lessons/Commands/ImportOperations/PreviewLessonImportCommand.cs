using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;

public sealed record PreviewLessonImportCommand(
    string RawJson
) : ICommand<LessonCandidateValidationResultDto>;

public sealed class PreviewLessonImportCommandHandler : ICommandHandler<PreviewLessonImportCommand, LessonCandidateValidationResultDto>
{
    private readonly ILessonCandidateValidator _validator;

    public PreviewLessonImportCommandHandler(ILessonCandidateValidator validator)
    {
        _validator = validator;
    }

    public Task<LessonCandidateValidationResultDto> HandleAsync(PreviewLessonImportCommand command, CancellationToken ct = default)
    {
        JsonNode? rootNode;
        try
        {
            rootNode = JsonNode.Parse(command.RawJson);
        }
        catch (Exception ex)
        {
            var issues = new List<ValidationIssueDto>
            {
                new ValidationIssueDto("$", "Error", $"Malformed JSON syntax: {ex.Message}")
            };
            return Task.FromResult(new LessonCandidateValidationResultDto(false, issues, null));
        }

        return _validator.ValidateCandidateAsync(rootNode, ct);
    }
}
