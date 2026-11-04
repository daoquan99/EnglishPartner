using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.RejectGrammarExercise;

public sealed class RejectGrammarExerciseValidator : AbstractValidator<RejectGrammarExerciseCommand>
{
    public RejectGrammarExerciseValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Exercise Id is required.");

        RuleFor(x => x.Request.Reason)
            .NotEmpty().WithMessage("Reject reason is required.")
            .MaximumLength(500).WithMessage("Reject reason must not exceed 500 characters.");
    }
}
