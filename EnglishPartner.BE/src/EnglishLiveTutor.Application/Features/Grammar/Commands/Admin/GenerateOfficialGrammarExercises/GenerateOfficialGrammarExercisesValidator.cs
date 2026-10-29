using EnglishLiveTutor.Application.Features.Grammar.Rules;
using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.GenerateOfficialGrammarExercises;

public sealed class GenerateOfficialGrammarExercisesValidator : AbstractValidator<GenerateOfficialGrammarExercisesCommand>
{
    public GenerateOfficialGrammarExercisesValidator()
    {
        RuleFor(x => x.StepCode)
            .NotEmpty().WithMessage("StepCode is required.")
            .MaximumLength(32).WithMessage("StepCode must not exceed 32 characters.");

        RuleFor(x => x.Request.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one exercise item must be requested.")
            .Must(items => items.Sum(i => i.Count) is >= 1 and <= GrammarRules.MaxAdminItemsPerRequest)
            .WithMessage("Total requested exercises must be between 1 and 20.")
            .When(x => x.ImportJsonText is null);

        RuleFor(x => x.Request.Items)
            .Must(items => items.Sum(i => i.Count) <= GrammarRules.MaxAdminItemsPerRequest)
            .When(x => x.ImportJsonText is not null && x.Request.Items is not null)
            .WithMessage("Total expected exercises must not exceed 20.");

        RuleFor(x => x.ImportJsonText)
            .NotEmpty().MaximumLength(200_000)
            .When(x => x.ImportJsonText is not null);

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
