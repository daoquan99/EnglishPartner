using EnglishLiveTutor.Application.Common.Progress;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Contracts;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Domain.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Grammar;

public sealed class GrammarProgressReader : IGrammarProgressReader
{
    private readonly IGrammarDbContext _db;

    public GrammarProgressReader(IGrammarDbContext db)
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

        var grouped = await _db.GrammarAttempts
            .AsNoTracking()
            .Where(e => e.UserId == userId &&
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

    public async Task<GrammarProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default)
    {
        var allPublishedSteps = await _db.GrammarSteps
            .AsNoTracking()
            .Where(s => s.IsPublished && !s.IsDeleted)
            .Select(s => GrammarStepEntity.Create(
                s.Code,
                s.CefrLevel,
                s.Order,
                s.TitleVi,
                s.TitleEn,
                s.SummaryVi,
                "{}", // TheoryJson is not needed for progress counts
                s.TheoryVersion,
                s.IsLevelReview,
                s.IsPublished,
                s.Id))
            .ToListAsync(ct);

        var userProgresses = await _db.UserGrammarStepProgress
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        var roadmap = GrammarRoadmapCalculator.ComputeRoadmap(allPublishedSteps, userProgresses);

        var levels = roadmap.Levels
            .Where(l => l.Steps.Count > 0)
            .Select(l => new GrammarLevelProgress(
                l.Level,
                l.Steps.Count(s => s.Status == "Passed"),
                l.Steps.Count))
            .ToList();

        var currentStepCode = roadmap.Levels
            .SelectMany(l => l.Steps)
            .FirstOrDefault(s => s.Status == "Unlocked")?.Code;

        return new GrammarProgressSummary(levels, currentStepCode);
    }
}
