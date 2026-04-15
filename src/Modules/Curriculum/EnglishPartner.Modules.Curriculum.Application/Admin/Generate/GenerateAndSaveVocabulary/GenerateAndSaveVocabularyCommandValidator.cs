using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveVocabulary;

public sealed class GenerateAndSaveVocabularyCommandValidator : AbstractValidator<GenerateAndSaveVocabularyCommand>
{
    public GenerateAndSaveVocabularyCommandValidator()
    {
        RuleFor(x => x.WordHint)
            .NotEmpty().WithMessage("WordHint is required.")
            .MaximumLength(200);

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 20).WithMessage("Count must be between 1 and 20.");

        RuleFor(x => x.Level)
            .MaximumLength(20)
            .Matches("^(A1|A2|B1|B2|C1|C2)$").When(x => x.Level is not null)
            .WithMessage("Level must be one of: A1, A2, B1, B2, C1, C2.");

        RuleFor(x => x.ExampleCount)
            .InclusiveBetween(1, 10).When(x => x.AutoGenerateExamples)
            .WithMessage("ExampleCount must be between 1 and 10.");

        RuleForEach(x => x.ExampleTargetLanguageCodes)
            .NotEmpty().MaximumLength(10)
            .When(x => x.ExampleTargetLanguageCodes is { Count: > 0 });
    }
}
