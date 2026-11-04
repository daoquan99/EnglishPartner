using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;

public sealed class ListConversationsValidator : AbstractValidator<ListConversationsQuery>
{
    public ListConversationsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue).WithMessage("Page phải lớn hơn 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue).WithMessage("PageSize phải lớn hơn 0.");
    }
}
