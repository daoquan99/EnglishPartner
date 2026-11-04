using EnglishLiveTutor.Application.Common.Progress;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed record SpeechProgressSummary(
    int Assessments,
    decimal? AverageScore,
    decimal? AverageScoreSinceFrom);

public interface ISpeechProgressReader
{
    Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(
        string userId,
        DateOnly fromLocal,
        DateOnly toLocal,
        int utcOffsetMinutes,
        CancellationToken ct = default);

    Task<SpeechProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default);
}
