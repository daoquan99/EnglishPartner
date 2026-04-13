namespace EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;

public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Topic>> GetByLanguageCodeAsync(string languageCode, string? searchTerm = null, CancellationToken cancellationToken = default);
    void Add(Topic topic);
    void Update(Topic topic);
    void Remove(Topic topic);
}
