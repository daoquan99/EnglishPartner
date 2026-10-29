using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.ImportGrammarExercisesJson;

public sealed class ImportGrammarExercisesJsonValidator : AbstractValidator<ImportGrammarExercisesJsonCommand>
{
    public ImportGrammarExercisesJsonValidator()
    {
        RuleFor(x => x.StepCode)
            .NotEmpty().WithMessage("StepCode is required.")
            .MaximumLength(32).WithMessage("StepCode must not exceed 32 characters.");

        RuleFor(x => x.Request.JsonText)
            .NotEmpty().WithMessage("JSON text is required.")
            .MaximumLength(200_000).WithMessage("JSON text must not exceed 200,000 characters.");
    }
}
