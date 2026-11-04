using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;

public sealed record UpdateScoringRubricCommand(
    string CodeOrId,
    UpdateScoringRubricRequest Request
) : ICommand<ScoringRubricEntity>
{
    public UpdateScoringRubricCommand(UpdateScoringRubricRequest request)
        : this(string.IsNullOrWhiteSpace(request.Name)
            ? $"RUBRIC_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
            : request.Name.Trim().ToUpperInvariant().Replace(' ', '_'), request)
    {
    }

    public UpdateScoringRubricCommand(Guid rubricId, UpdateScoringRubricRequest request)
        : this(rubricId.ToString(), request)
    {
    }
}
