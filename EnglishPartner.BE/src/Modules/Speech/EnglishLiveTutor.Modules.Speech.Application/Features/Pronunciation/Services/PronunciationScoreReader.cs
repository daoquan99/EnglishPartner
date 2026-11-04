using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Speech;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Services;

public sealed class PronunciationScoreReader(ISpeechDbContext db) : IPronunciationScoreReader
{
    public Task<decimal?> GetLatestScoreAsync(
        string userId,
        Guid senseId,
        CancellationToken ct = default)
    {
        return db.PronunciationAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.SenseId == senseId && a.OverallScore != null)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => a.OverallScore)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestScoresAsync(
        string userId,
        IReadOnlyCollection<Guid> senseIds,
        CancellationToken ct = default)
    {
        var scores = await db.PronunciationAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId && senseIds.Contains(a.SenseId) && a.OverallScore != null)
            .GroupBy(a => a.SenseId)
            .Select(g => new
            {
                SenseId = g.Key,
                Score = g.OrderByDescending(x => x.CreatedAtUtc).Select(x => x.OverallScore).FirstOrDefault()
            })
            .ToListAsync(ct);

        return scores.ToDictionary(x => x.SenseId, x => x.Score);
    }

    public async Task<PronunciationScoreSummary> GetSummaryAsync(
        string userId,
        CancellationToken ct = default)
    {
        int totalCount = await db.PronunciationAssessments.CountAsync(a => a.UserId == userId, ct);
        decimal? averageScore = await db.PronunciationAssessments
            .Where(a => a.UserId == userId && a.OverallScore != null)
            .AverageAsync(a => a.OverallScore, ct);

        return new PronunciationScoreSummary(totalCount, averageScore);
    }
}
