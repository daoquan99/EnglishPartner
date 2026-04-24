using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.AI.Application.Chat.Queries.GetMySessions;

/// <summary>
/// Returns a list of chat sessions belonging to the authenticated user.
/// </summary>
public sealed record GetMySessionsQuery() : IQuery<List<ChatSessionSummaryDto>>;

public sealed record ChatSessionSummaryDto(
    Guid Id,
    string Title,
    int MessageCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
