using EnglishPartner.Modules.AI.Domain;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Queries.GetChatHistory;

internal sealed class GetChatHistoryQueryHandler(
    IChatSessionRepository repository,
    ICurrentUserService currentUser) : IQueryHandler<GetChatHistoryQuery, ChatHistoryResponse>
{
    public async Task<Result<ChatHistoryResponse>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var session = await repository.GetByIdWithMessagesAsync(request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure<ChatHistoryResponse>(AiErrors.ChatSession.NotFound);

        if (session.UserId != currentUser.UserId!.Value)
            return Result.Failure<ChatHistoryResponse>(AiErrors.ChatSession.AccessDenied);

        var messages = session.Messages
            .OrderBy(m => m.Order)
            .Select(m => new ChatMessageDto(m.Id, m.Role.ToString().ToLowerInvariant(), m.Text, m.CreatedAt, m.Order, m.AudioUrl, m.Translation))
            .ToList();

        return new ChatHistoryResponse(session.Id, session.Title, session.SystemPrompt, session.Topic, messages);
    }
}
