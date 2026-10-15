namespace EnglishLiveTutor.Domain.Conversations;

public interface IConversationRepository
{
    Task<ConversationEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ConversationEntity?> GetWithHistoryAsync(string id, CancellationToken ct = default);
    Task AddAsync(ConversationEntity conversation, CancellationToken ct = default);
}
