using EnglishLiveTutor.Application.Features.Grammar.Rules;
using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt;

public sealed class BuildGrammarExercisePromptValidator : AbstractValidator<BuildGrammarExercisePromptQuery>
{
    public BuildGrammarExercisePromptValidator()
    {
        RuleFor(x => x.StepCode)
            .NotEmpty().WithMessage("StepCode is required.")
            .MaximumLength(32).WithMessage("StepCode must not exceed 32 characters.");

        RuleFor(x => x.Request.Items)
            .NotEmpty().WithMessage("At least one exercise item must be requested.");

        RuleFor(x => x.Request.Items)
            .Must((query, items) => items.Sum(i => i.Count) >= 1 &&
                items.Sum(i => i.Count) <= (query.ForAdmin ? GrammarRules.MaxAdminItemsPerRequest : GrammarRules.MaxItemsPerRequest))
            .When(x => x.Request.Items != null)
            .WithMessage("Total requested exercises exceeds the allowed limit.");

        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Type)
                .Must(t => GrammarExerciseRules.AllowedTypes.Contains(t))
                .WithMessage("Exercise type is not supported in Grammar Roadmap.");

            item.RuleFor(i => i.Count)
                .GreaterThan(0).WithMessage("Count must be greater than 0.");

            item.RuleFor(i => i.Difficulty)
                .InclusiveBetween(1, 3).WithMessage("Difficulty must be between 1 and 3.");
        });
    }
}
