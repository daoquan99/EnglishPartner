using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Admin.Generate.GenerateAndSaveExampleSentences;

public sealed class GenerateAndSaveExampleSentencesCommandValidator : AbstractValidator<GenerateAndSaveExampleSentencesCommand>
{
    public GenerateAndSaveExampleSentencesCommandValidator()
    {
        RuleFor(x => x.Context)
            .NotEmpty().WithMessage("Context is required.")
            .MaximumLength(500);

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 20).WithMessage("Count must be between 1 and 20.");

        RuleFor(x => x.Level)
            .MaximumLength(20)
            .Matches("^(A1|A2|B1|B2|C1|C2)$").When(x => x.Level is not null)
            .WithMessage("Level must be one of: A1, A2, B1, B2, C1, C2.");

        RuleForEach(x => x.TargetLanguageCodes)
            .NotEmpty().MaximumLength(10)
            .When(x => x.TargetLanguageCodes is { Count: > 0 });
    }
}
