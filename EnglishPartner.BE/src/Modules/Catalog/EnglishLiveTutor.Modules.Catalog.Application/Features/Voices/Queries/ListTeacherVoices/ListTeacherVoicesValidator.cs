using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Queries.ListTeacherVoices;

public sealed class ListTeacherVoicesValidator : AbstractValidator<ListTeacherVoicesQuery>
{
    public ListTeacherVoicesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue).WithMessage("Page phải lớn hơn 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue).WithMessage("PageSize phải lớn hơn 0.");
    }
}
