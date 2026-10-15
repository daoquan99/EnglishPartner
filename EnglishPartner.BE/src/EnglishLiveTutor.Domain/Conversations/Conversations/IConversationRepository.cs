namespace EnglishLiveTutor.Domain.Conversations.Conversations;

/// <summary>
/// Aggregate write repository for ConversationEntity.
/// Read projections and paged list queries live in Application read handlers,
/// while high-frequency streaming turn writes go through IConversationRealtimeWriter.
/// </summary>
public interface IConversationRepository
{
    Task<ConversationEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ConversationEntity?> GetByIdWithHistoryAsync(string id, CancellationToken ct = default);
    Task AddAsync(ConversationEntity conversation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
