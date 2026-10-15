using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;

namespace EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;

public interface IScoringRubricRepository
{
    Task<ScoringRubricEntity?> GetActivePronunciationRubricAsync(CancellationToken ct = default);
    Task<ScoringRubricEntity?> GetByIdWithCriteriaAsync(Guid rubricId, CancellationToken ct = default);
    Task<ScoringRubricEntity?> GetByCodeWithCriteriaAsync(string code, CancellationToken ct = default);
    void ReplaceCriteria(ScoringRubricEntity rubric, IEnumerable<ScoringRubricCriterionEntity> previousCriteria);
    Task AddAsync(ScoringRubricEntity rubric, CancellationToken ct = default);
}
