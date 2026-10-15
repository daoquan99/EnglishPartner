using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;

public sealed class GetVocabularyFlashcardValidator : AbstractValidator<GetVocabularyFlashcardQuery>
{
    public GetVocabularyFlashcardValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
