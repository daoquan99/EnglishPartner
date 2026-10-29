using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.DraftGrammarTheoryWithAi;

public sealed class DraftGrammarTheoryWithAiValidator : AbstractValidator<DraftGrammarTheoryWithAiCommand>
{
    public DraftGrammarTheoryWithAiValidator()
    {
        RuleFor(x => x.StepCode)
            .NotEmpty().WithMessage("StepCode is required.")
            .MaximumLength(32).WithMessage("StepCode must not exceed 32 characters.");
    }
}
