using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Pronunciation;

public sealed class ScoringRubricRepository : IScoringRubricRepository
{
    private readonly AppDbContext _db;

    public ScoringRubricRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ScoringRubricEntity?> GetActivePronunciationRubricAsync(CancellationToken ct = default)
    {
        return await _db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria.OrderBy(c => c.DisplayOrder))
            .Where(r => r.IsActive && r.AssessmentType == "Pronunciation")
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ScoringRubricEntity?> GetByIdWithCriteriaAsync(Guid rubricId, CancellationToken ct = default)
    {
        return await _db.ScoringRubrics
            .Include(r => r.Criteria)
            .FirstOrDefaultAsync(r => r.Id == rubricId, ct);
    }

    public async Task<ScoringRubricEntity?> GetByCodeWithCriteriaAsync(string code, CancellationToken ct = default)
    {
        string norm = code.Trim();
        return await _db.ScoringRubrics
            .Include(r => r.Criteria)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(r => r.Code == norm, ct);
    }

    public void ReplaceCriteria(ScoringRubricEntity rubric, IEnumerable<ScoringRubricCriterionEntity> previousCriteria)
    {
        _db.ScoringRubricCriteria.RemoveRange(previousCriteria);
        foreach (var criterion in rubric.Criteria)
        {
            if (_db.Entry(criterion).State == EntityState.Detached)
            {
                _db.ScoringRubricCriteria.Add(criterion);
            }
        }
    }

    public Task AddAsync(ScoringRubricEntity rubric, CancellationToken ct = default)
    {
        _db.ScoringRubrics.Add(rubric);
        return Task.CompletedTask;
    }
}
