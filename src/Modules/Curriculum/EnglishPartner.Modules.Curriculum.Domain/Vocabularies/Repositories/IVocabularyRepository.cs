namespace EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;

public interface IVocabularyRepository
{
    Task<Vocabulary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> GetByLanguageCodeAsync(string languageCode, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken = default);
    /// <summary>Filtered + paginated list. Pass null to skip a filter.</summary>
    Task<(List<Vocabulary> Items, int TotalCount)> GetPagedAsync(
        string languageCode, int page, int pageSize,
        string? searchTerm, string? level, Guid? topicId,
        CancellationToken cancellationToken = default);
    Task<(List<Vocabulary> Items, int TotalCount)> GetPagedLookupAsync(
        string languageCode, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Vocabulary>> SearchAsync(string query, string languageCode, CancellationToken cancellationToken = default);
    void Add(Vocabulary vocabulary);
    void Update(Vocabulary vocabulary);
    void Remove(Vocabulary vocabulary, Guid? deletedBy = null, DateTime? deletedAt = null);
}
