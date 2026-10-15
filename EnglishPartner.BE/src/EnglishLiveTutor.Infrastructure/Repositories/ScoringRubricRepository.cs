using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

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

    public Task AddAsync(ScoringRubricEntity rubric, CancellationToken ct = default)
    {
        _db.ScoringRubrics.Add(rubric);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
