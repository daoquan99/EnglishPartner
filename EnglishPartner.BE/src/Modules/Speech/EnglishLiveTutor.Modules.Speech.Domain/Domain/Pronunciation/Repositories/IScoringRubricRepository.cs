namespace EnglishLiveTutor.Domain.Pronunciation.Repositories;

public interface IScoringRubricRepository
{
    Task<ScoringRubricEntity?> GetActivePronunciationRubricAsync(CancellationToken ct = default);
    Task<ScoringRubricEntity?> GetByIdWithCriteriaAsync(Guid rubricId, CancellationToken ct = default);
    Task<ScoringRubricEntity?> GetByCodeWithCriteriaAsync(string code, CancellationToken ct = default);
    Task AddAsync(ScoringRubricEntity rubric, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
