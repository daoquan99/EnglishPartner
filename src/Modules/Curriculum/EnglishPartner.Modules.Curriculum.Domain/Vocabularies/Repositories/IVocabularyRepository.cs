namespace EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;

public interface IVocabularyRepository
{
    Task<Vocabulary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> GetByLanguageCodeAsync(string languageCode, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> SearchAsync(string query, string languageCode, CancellationToken cancellationToken = default);
    void Add(Vocabulary vocabulary);
    void Update(Vocabulary vocabulary);
    void Remove(Vocabulary vocabulary);
}
