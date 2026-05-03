namespace EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;

public interface IGrammarRepository
{
    Task<Grammar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Grammar>> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<(List<Grammar> Items, int TotalCount)> GetPagedLookupAsync(string languageCode, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Grammar>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    void Add(Grammar grammar);
    void Update(Grammar grammar);
    void Remove(Grammar grammar, Guid? deletedBy = null, DateTime? deletedAt = null);
}
