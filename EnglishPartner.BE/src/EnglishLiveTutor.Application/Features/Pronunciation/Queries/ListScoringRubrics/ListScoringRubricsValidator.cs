using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics;

public sealed class ListScoringRubricsValidator : AbstractValidator<ListScoringRubricsQuery>
{
    public ListScoringRubricsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue).WithMessage("Page pháº£i lá»›n hÆ¡n 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue).WithMessage("PageSize pháº£i lá»›n hÆ¡n 0.");
    }
}
