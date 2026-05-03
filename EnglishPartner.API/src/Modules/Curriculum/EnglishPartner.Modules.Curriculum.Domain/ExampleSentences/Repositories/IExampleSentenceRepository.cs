namespace EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;

public interface IExampleSentenceRepository
{
    Task<ExampleSentence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ExampleSentence>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken = default);
    Task<List<ExampleSentence>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken = default);
    Task<List<ExampleSentence>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<(List<ExampleSentence> Items, int TotalCount)> GetPagedAsync(
        string languageCode, int page, int pageSize,
        string? searchTerm, Guid? vocabularyId, Guid? grammarId, Guid? topicId,
        CancellationToken cancellationToken = default);
    void Add(ExampleSentence exampleSentence);
    void Update(ExampleSentence exampleSentence);
    void Remove(ExampleSentence exampleSentence, Guid? deletedBy = null, DateTime? deletedAt = null);
}
