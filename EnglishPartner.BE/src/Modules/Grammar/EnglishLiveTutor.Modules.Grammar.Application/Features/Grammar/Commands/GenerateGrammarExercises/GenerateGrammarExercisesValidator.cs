using EnglishLiveTutor.Application.Features.Grammar.Rules;
using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.GenerateGrammarExercises;

public sealed class GenerateGrammarExercisesValidator : AbstractValidator<GenerateGrammarExercisesCommand>
{
    public GenerateGrammarExercisesValidator()
    {
        RuleFor(x => x.StepCode)
            .NotEmpty().WithMessage("StepCode is required.")
            .MaximumLength(32).WithMessage("StepCode must not exceed 32 characters.");

        RuleFor(x => x.Request.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one exercise item must be requested.")
            .Must(items => items.Sum(i => i.Count) is >= 1 and <= GrammarRules.MaxItemsPerRequest)
            .WithMessage("Total requested exercises must be between 1 and 10.");

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
