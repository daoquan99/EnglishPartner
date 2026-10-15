namespace EnglishLiveTutor.Domain.Topics.Repositories;

public interface ITopicRepository
{
    Task<List<TopicEntity>> ListAsync(
        bool onlyActive = true,
        bool? supportsConversation = null,
        bool? supportsVocabulary = null,
        string? modeCode = null,
        string? category = null,
        CancellationToken ct = default);

    Task<TopicEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TopicEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<TopicEntity> UpsertAsync(TopicEntity topic, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
