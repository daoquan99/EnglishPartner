namespace EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(ChatSession session);
    void Update(ChatSession session);
}
