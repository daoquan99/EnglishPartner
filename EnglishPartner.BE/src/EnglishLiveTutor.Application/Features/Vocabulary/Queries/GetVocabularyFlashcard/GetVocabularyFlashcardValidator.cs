using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;

public sealed class GetVocabularyFlashcardValidator : AbstractValidator<GetVocabularyFlashcardQuery>
{
    public GetVocabularyFlashcardValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
    }
}
