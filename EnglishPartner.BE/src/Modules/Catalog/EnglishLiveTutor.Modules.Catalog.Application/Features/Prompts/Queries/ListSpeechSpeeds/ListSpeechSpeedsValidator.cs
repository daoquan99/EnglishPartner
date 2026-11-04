using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds;

public sealed class ListSpeechSpeedsValidator : AbstractValidator<ListSpeechSpeedsQuery>
{
    public ListSpeechSpeedsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue).WithMessage("Page phải lớn hơn 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue).WithMessage("PageSize phải lớn hơn 0.");
    }
}
