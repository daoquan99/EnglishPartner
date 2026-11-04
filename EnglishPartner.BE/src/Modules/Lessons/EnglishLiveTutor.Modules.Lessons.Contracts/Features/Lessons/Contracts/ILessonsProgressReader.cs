using EnglishLiveTutor.Application.Common.Progress;

namespace EnglishLiveTutor.Application.Features.Lessons.Contracts;

public sealed record LessonProgressSummary(
    int CompletedAttempts,
    decimal? AverageScore100);

public interface ILessonsProgressReader
{
    Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(
        string userId,
        DateOnly fromLocal,
        DateOnly toLocal,
        int utcOffsetMinutes,
        CancellationToken ct = default);

    Task<LessonProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default);
}
