using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.UpdateVocabulary;

internal sealed class UpdateVocabularyCommandValidator : AbstractValidator<UpdateVocabularyCommand>
{
    public UpdateVocabularyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Word)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Word is required and must be ≤ 200 characters.");

        RuleFor(x => x.Definition)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage("Definition is required and must be ≤ 1000 characters.");

        RuleFor(x => x.Level)
            .MaximumLength(10)
            .When(x => x.Level is not null);

        RuleFor(x => x.Pronunciation)
            .MaximumLength(200)
            .When(x => x.Pronunciation is not null);

        RuleFor(x => x.Synonyms)
            .Must(s => s!.Count <= 20 && s.All(x => x.Length <= 100))
            .When(x => x.Synonyms is not null)
            .WithMessage("Max 20 synonyms, each ≤ 100 characters.");
    }
}
