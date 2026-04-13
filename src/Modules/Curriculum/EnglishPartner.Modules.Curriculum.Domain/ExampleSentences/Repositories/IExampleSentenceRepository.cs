namespace EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;

public interface IExampleSentenceRepository
{
    Task<ExampleSentence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ExampleSentence>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken = default);
    Task<List<ExampleSentence>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken = default);
    Task<List<ExampleSentence>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    void Add(ExampleSentence exampleSentence);
    void Update(ExampleSentence exampleSentence);
    void Remove(ExampleSentence exampleSentence);
}
