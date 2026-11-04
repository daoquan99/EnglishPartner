using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Speech;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Pronunciation;

public sealed class SpeechProgressReader : ISpeechProgressReader
{
    private readonly ISpeechDbContext _db;

    public SpeechProgressReader(ISpeechDbContext db)
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

        var grouped = await _db.PronunciationAssessments
            .AsNoTracking()
            .Where(e => e.UserId == userId &&
                        e.CreatedAtUtc >= fromUtc &&
                        e.CreatedAtUtc < toUtc)
            .GroupBy(e => e.CreatedAtUtc.AddMinutes(utcOffsetMinutes).Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped
            .Select(x => new DailyActivityCount(DateOnly.FromDateTime(x.Date), x.Count))
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<SpeechProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default)
    {
        var stats = await _db.PronunciationAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                AverageScore = g.Average(a => a.OverallScore),
                AverageScoreSinceFrom = g.Average(a => a.CreatedAtUtc >= fromUtc ? a.OverallScore : null)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
        {
            return new SpeechProgressSummary(0, null, null);
        }

        return new SpeechProgressSummary(
            stats.TotalCount,
            stats.AverageScore.HasValue ? Math.Round(stats.AverageScore.Value, 1, MidpointRounding.AwayFromZero) : null,
            stats.AverageScoreSinceFrom.HasValue ? Math.Round(stats.AverageScoreSinceFrom.Value, 1, MidpointRounding.AwayFromZero) : null);
    }
}
