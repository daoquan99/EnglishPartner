using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;

public sealed class DeleteVocabularyValidator : AbstractValidator<DeleteVocabularyCommand>
{
    public DeleteVocabularyValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
