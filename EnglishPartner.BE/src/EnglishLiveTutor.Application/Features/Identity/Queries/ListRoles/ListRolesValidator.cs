using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles;

public sealed class ListRolesValidator : AbstractValidator<ListRolesQuery>
{
    public ListRolesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue).WithMessage("Page phải lớn hơn 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue).WithMessage("PageSize phải lớn hơn 0.");
    }
}
