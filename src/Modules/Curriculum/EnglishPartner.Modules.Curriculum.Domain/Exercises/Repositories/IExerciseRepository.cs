namespace EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByLanguageAndLevelAsync(string languageCode, string? level, int count, CancellationToken cancellationToken = default);
    void Add(Exercise exercise);
    void Update(Exercise exercise);
    void Remove(Exercise exercise, Guid? deletedBy = null, DateTime? deletedAt = null);
}
