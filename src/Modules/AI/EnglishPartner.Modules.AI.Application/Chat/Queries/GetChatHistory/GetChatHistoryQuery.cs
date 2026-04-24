using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Queries.GetChatHistory;

/// <summary>
/// Returns all messages in a chat session, ordered by timestamp.
/// </summary>
public sealed record GetChatHistoryQuery(
    Guid SessionId) : IQuery<ChatHistoryResponse>;

public sealed record ChatHistoryResponse(
    Guid SessionId,
    string Title,
    IReadOnlyList<ChatMessageDto> Messages);

public sealed record ChatMessageDto(
    Guid Id,
    string Role,
    string Text,
    DateTime Timestamp);
