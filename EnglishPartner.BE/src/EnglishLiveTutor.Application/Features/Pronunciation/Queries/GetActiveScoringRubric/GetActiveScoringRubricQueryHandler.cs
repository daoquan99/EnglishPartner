using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;

public sealed class GetActiveScoringRubricQueryHandler : IQueryHandler<GetActiveScoringRubricQuery, ScoringRubricEntity?>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetActiveScoringRubricQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ScoringRubricEntity?> HandleAsync(GetActiveScoringRubricQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria.OrderBy(c => c.DisplayOrder))
            .Where(r => r.IsActive && r.AssessmentType == "Pronunciation")
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(ct);
    }
}
