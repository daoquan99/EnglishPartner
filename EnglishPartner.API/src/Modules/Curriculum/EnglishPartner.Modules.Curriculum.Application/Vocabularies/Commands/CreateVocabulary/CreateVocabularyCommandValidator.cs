using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.CreateVocabulary;

public sealed class CreateVocabularyCommandValidator : AbstractValidator<CreateVocabularyCommand>
{
    public CreateVocabularyCommandValidator()
    {
        RuleFor(x => x.Word).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Definition).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.PartOfSpeech).MaximumLength(50);
        RuleFor(x => x.Level).MaximumLength(20);
    }
}
