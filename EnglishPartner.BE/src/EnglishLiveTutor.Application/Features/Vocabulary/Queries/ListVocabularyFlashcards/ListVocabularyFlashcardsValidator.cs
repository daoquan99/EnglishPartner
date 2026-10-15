using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;

public sealed class ListVocabularyFlashcardsValidator : AbstractValidator<ListVocabularyFlashcardsQuery>
{
    public ListVocabularyFlashcardsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue).WithMessage("Page pháº£i lá»›n hÆ¡n 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue).WithMessage("PageSize pháº£i lá»›n hÆ¡n 0.");
    }
}
