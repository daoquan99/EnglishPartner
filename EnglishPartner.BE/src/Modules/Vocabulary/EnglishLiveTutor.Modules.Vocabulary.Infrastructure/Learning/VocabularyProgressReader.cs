using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Features.Learning.Contracts;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Learning;

public sealed class VocabularyProgressReader : IVocabularyProgressReader
{
    private readonly IVocabularyDbContext _db;

    public VocabularyProgressReader(IVocabularyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(
        string userId,
        DateOnly fromLocal,
        DateOnly toLocal,
        int utcOffsetMinutes,
        CancellationToken ct = default)
    {
        var fromUtc = new DateTimeOffset(fromLocal.Year, fromLocal.Month, fromLocal.Day, 0, 0, 0, TimeSpan.FromMinutes(utcOffsetMinutes)).ToUniversalTime();
        var toUtc = new DateTimeOffset(toLocal.AddDays(1).Year, toLocal.AddDays(1).Month, toLocal.AddDays(1).Day, 0, 0, 0, TimeSpan.FromMinutes(utcOffsetMinutes)).ToUniversalTime();

        var grouped = await _db.VocabularyReviewAttempts
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.ReviewedAt >= fromUtc && e.ReviewedAt < toUtc)
            .GroupBy(e => e.ReviewedAt.AddMinutes(utcOffsetMinutes).Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped
            .Select(x => new DailyActivityCount(DateOnly.FromDateTime(x.Date), x.Count))
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<VocabularyProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var stats = await _db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Studied = g.Count(),
                Mastered = g.Count(p => p.LearningStatus == "Mastered"),
                DueForReviewNow = g.Count(p => p.NextReviewAt != null && p.NextReviewAt <= now),
                AverageMastery = (double?)g.Average(p => (double)p.MasteryLevel)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
        {
            return new VocabularyProgressSummary(0, 0, 0, 0.0);
        }

        return new VocabularyProgressSummary(
            stats.Studied,
            stats.Mastered,
            stats.DueForReviewNow,
            stats.AverageMastery.HasValue ? Math.Round(stats.AverageMastery.Value, 1, MidpointRounding.AwayFromZero) : 0.0);
    }
}
