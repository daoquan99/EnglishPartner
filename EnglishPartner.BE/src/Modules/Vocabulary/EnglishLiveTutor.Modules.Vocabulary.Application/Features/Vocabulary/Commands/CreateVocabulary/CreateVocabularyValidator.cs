using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;

public sealed class CreateVocabularyValidator : AbstractValidator<CreateVocabularyCommand>
{
    public CreateVocabularyValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
