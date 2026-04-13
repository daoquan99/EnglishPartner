namespace EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken = default);
    void Add(Conversation conversation);
    void Update(Conversation conversation);
    void Remove(Conversation conversation);
}
