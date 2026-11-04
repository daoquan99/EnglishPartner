using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Features.Lessons.Contracts;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Lessons;

public sealed class LessonsProgressReader : ILessonsProgressReader
{
    private readonly ILessonsDbContext _db;

    public LessonsProgressReader(ILessonsDbContext db)
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

        var grouped = await _db.LessonAttempts
            .AsNoTracking()
            .Where(e => e.UserId == userId &&
                        e.Status == LessonAttemptStatus.Completed &&
                        e.CompletedAtUtc != null &&
                        e.CompletedAtUtc >= fromUtc &&
                        e.CompletedAtUtc < toUtc)
            .GroupBy(e => e.CompletedAtUtc!.Value.AddMinutes(utcOffsetMinutes).Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped
            .Select(x => new DailyActivityCount(DateOnly.FromDateTime(x.Date), x.Count))
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<LessonProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default)
    {
        var stats = await _db.LessonAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.Status == LessonAttemptStatus.Completed)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                CompletedAttempts = g.Count(),
                AverageScore100 = (decimal?)g.Average(a => a.FinalScore100)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
        {
            return new LessonProgressSummary(0, null);
        }

        return new LessonProgressSummary(
            stats.CompletedAttempts,
            stats.AverageScore100.HasValue ? Math.Round(stats.AverageScore100.Value, 1, MidpointRounding.AwayFromZero) : null);
    }
}
