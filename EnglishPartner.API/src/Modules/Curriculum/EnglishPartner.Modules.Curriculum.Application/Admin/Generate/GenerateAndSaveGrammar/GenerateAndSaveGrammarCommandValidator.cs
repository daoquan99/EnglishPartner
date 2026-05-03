using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveGrammar;

public sealed class GenerateAndSaveGrammarCommandValidator : AbstractValidator<GenerateAndSaveGrammarCommand>
{
    public GenerateAndSaveGrammarCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300);

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 10).WithMessage("Count must be between 1 and 10.");

        RuleFor(x => x.ExampleCount)
            .InclusiveBetween(1, 10).WithMessage("ExampleCount must be between 1 and 10.");

        RuleFor(x => x.Level)
            .MaximumLength(20)
            .Matches("^(A1|A2|B1|B2|C1|C2)$").When(x => x.Level is not null)
            .WithMessage("Level must be one of: A1, A2, B1, B2, C1, C2.");

        RuleForEach(x => x.TargetLanguageCodes)
            .NotEmpty().MaximumLength(10)
            .When(x => x.TargetLanguageCodes is { Count: > 0 });
    }
}
