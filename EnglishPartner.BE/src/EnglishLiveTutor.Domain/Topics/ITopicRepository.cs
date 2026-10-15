namespace EnglishLiveTutor.Domain.Topics;

public interface ITopicRepository
{
    Task<TopicEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TopicEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<TopicEntity?> GetByIdOrCodeIncludingDeletedAsync(Guid? id, string code, CancellationToken ct = default);
    Task AddAsync(TopicEntity topic, CancellationToken ct = default);
}
