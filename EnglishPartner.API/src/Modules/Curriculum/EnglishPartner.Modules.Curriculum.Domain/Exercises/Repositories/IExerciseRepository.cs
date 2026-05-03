namespace EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken = default);
    Task<(List<Exercise> Items, int TotalCount)> GetPagedAsync(
        string languageCode,
        int page,
        int pageSize,
        Guid? topicId,
        Guid? vocabularyId,
        Guid? grammarId,
        string? level,
        ExerciseType? exerciseType = null,
        CancellationToken cancellationToken = default);
    Task<List<Exercise>> GetWithoutOptionsAsync(CancellationToken cancellationToken = default);
    void Add(Exercise exercise);
    void Update(Exercise exercise);
    void Remove(Exercise exercise, Guid? deletedBy = null, DateTime? deletedAt = null);
}
