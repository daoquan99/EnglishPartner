using EnglishLiveTutor.Application.Common.Progress;

namespace EnglishLiveTutor.Application.Features.Grammar.Contracts;

public sealed record GrammarLevelProgress(
    string Level,
    int PassedSteps,
    int TotalSteps);

public sealed record GrammarProgressSummary(
    IReadOnlyList<GrammarLevelProgress> Levels,
    string? CurrentStepCode);

public interface IGrammarProgressReader
{
    Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(
        string userId,
        DateOnly fromLocal,
        DateOnly toLocal,
        int utcOffsetMinutes,
        CancellationToken ct = default);

    Task<GrammarProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default);
}
