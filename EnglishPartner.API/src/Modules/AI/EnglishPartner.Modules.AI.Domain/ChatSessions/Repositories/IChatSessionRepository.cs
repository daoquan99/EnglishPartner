namespace EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<ChatSessionSummary>> GetUserSessionSummariesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatHistorySummary?> GetLatestSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default);
    void Add(ChatSession session);
    void Update(ChatSession session);
}

/// <summary>Lightweight projection — avoids loading all messages just for a list view.</summary>
public sealed record ChatSessionSummary(
    Guid Id,
    string Title,
    string? SystemPrompt,
    string? Topic,
    SessionType SessionType,
    string? VoiceName,
    int MessageCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
