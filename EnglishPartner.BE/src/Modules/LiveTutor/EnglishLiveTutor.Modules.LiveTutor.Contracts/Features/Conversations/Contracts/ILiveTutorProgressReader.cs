using EnglishLiveTutor.Application.Common.Progress;

namespace EnglishLiveTutor.Application.Features.Conversations.Contracts;

public sealed record LiveTutorProgressSummary(
    int Sessions,
    int SessionsSinceFrom);

public interface ILiveTutorProgressReader
{
    Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(
        string userId,
        DateOnly fromLocal,
        DateOnly toLocal,
        int utcOffsetMinutes,
        CancellationToken ct = default);

    Task<LiveTutorProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default);
}
