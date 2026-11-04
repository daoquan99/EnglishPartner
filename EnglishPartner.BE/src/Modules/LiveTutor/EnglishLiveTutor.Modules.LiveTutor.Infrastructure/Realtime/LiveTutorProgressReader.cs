using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Features.Conversations.Contracts;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Realtime;

public sealed class LiveTutorProgressReader : ILiveTutorProgressReader
{
    private readonly ILiveTutorDbContext _db;

    public LiveTutorProgressReader(ILiveTutorDbContext db)
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

        var grouped = await _db.Conversations
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

    public async Task<LiveTutorProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default)
    {
        var stats = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Sessions = g.Count(),
                SessionsSinceFrom = g.Count(c => c.CreatedAtUtc >= fromUtc)
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new LiveTutorProgressSummary(0, 0)
            : new LiveTutorProgressSummary(stats.Sessions, stats.SessionsSinceFrom);
    }
}
