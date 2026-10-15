using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;

public sealed class DeleteVocabularyValidator : AbstractValidator<DeleteVocabularyCommand>
{
    public DeleteVocabularyValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
