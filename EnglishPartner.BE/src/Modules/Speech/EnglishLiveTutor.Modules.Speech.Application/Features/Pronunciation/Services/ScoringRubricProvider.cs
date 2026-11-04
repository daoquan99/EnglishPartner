using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Services;

public sealed class ScoringRubricProvider : IScoringRubricProvider
{
    private readonly ISpeechDbContext _db;

    public ScoringRubricProvider(ISpeechDbContext db)
    {
        _db = db;
    }

    public async Task<ScoringRubricSnapshot?> GetByCodeAndVersionAsync(
        string code,
        int version,
        CancellationToken ct = default)
    {
        var rubric = await _db.ScoringRubrics
            .AsNoTracking()
            .Include(r => r.Criteria)
            .FirstOrDefaultAsync(r => r.Code == code && r.Version == version, ct);

        return rubric is null ? null : Map(rubric);
    }

    public async Task<IReadOnlyList<string>> ListExistingCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
    {
        return await _db.ScoringRubrics
            .AsNoTracking()
            .Where(r => codes.Contains(r.Code) && !r.IsDeleted)
            .Select(r => r.Code)
            .ToListAsync(ct);
    }

    private static ScoringRubricSnapshot Map(ScoringRubricEntity rubric)
    {
        var criteria = rubric.Criteria
            .Select(c => new ScoringRubricCriterionSnapshot(
                c.Code,
                c.Name,
                c.Description,
                c.Weight,
                c.DisplayOrder,
                c.IsEnabled,
                c.DescriptorLevel0,
                c.DescriptorLevel1,
                c.DescriptorLevel2,
                c.DescriptorLevel3,
                c.DescriptorLevel4))
            .ToList();

        return new ScoringRubricSnapshot(rubric.Code, rubric.Version, criteria);
    }
}
