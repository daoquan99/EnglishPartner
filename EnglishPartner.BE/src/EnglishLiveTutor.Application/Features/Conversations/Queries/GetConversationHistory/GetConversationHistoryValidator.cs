using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;

public sealed class GetConversationHistoryValidator : AbstractValidator<GetConversationHistoryQuery>
{
    public GetConversationHistoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
