using EnglishLiveTutor.Application.Common.Progress;

namespace EnglishLiveTutor.Application.Features.Learning.Contracts;

public sealed record VocabularyProgressSummary(
    int Studied,
    int Mastered,
    int DueForReviewNow,
    double AverageMastery);

public interface IVocabularyProgressReader
{
    Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(
        string userId,
        DateOnly fromLocal,
        DateOnly toLocal,
        int utcOffsetMinutes,
        CancellationToken ct = default);

    Task<VocabularyProgressSummary> GetSummaryAsync(
        string userId,
        DateTimeOffset fromUtc,
        CancellationToken ct = default);
}
