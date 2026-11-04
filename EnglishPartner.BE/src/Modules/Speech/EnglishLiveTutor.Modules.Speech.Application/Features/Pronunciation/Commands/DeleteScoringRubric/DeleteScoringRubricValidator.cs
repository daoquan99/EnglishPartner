using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric;

public sealed class DeleteScoringRubricValidator : AbstractValidator<DeleteScoringRubricCommand>
{
    public DeleteScoringRubricValidator()
    {
        // Validation rules can be configured here
    }
}
