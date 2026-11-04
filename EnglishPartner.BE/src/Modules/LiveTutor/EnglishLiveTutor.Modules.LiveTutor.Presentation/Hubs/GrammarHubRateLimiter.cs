using System.Threading.RateLimiting;

namespace EnglishLiveTutor.Api.Hubs;

public interface IGrammarHubRateLimiter
{
    bool TryAcquireAnalyze(string userId);
    bool TryAcquireInitialize(string userId);
    bool TryAcquireFeedbackAudio(string userId);
    bool TryAcquire(string userId) => TryAcquireAnalyze(userId);
}

public sealed class GrammarHubRateLimiter : IGrammarHubRateLimiter, IDisposable
{
    private readonly PartitionedRateLimiter<string> _analyzeLimiter;
    private readonly PartitionedRateLimiter<string> _initLimiter;
    private readonly PartitionedRateLimiter<string> _feedbackAudioLimiter;

    public GrammarHubRateLimiter(
        int analyzeLimit = 20,
        int initLimit = 10,
        int feedbackAudioLimit = 20,
        TimeSpan? window = null,
        int limit = 0)
    {
        var win = window ?? TimeSpan.FromMinutes(1);
        int effectiveAnalyzeLimit = limit > 0 ? limit : analyzeLimit;
        int effectiveInitLimit = limit > 0 ? limit : initLimit;
        int effectiveFeedbackLimit = limit > 0 ? limit : feedbackAudioLimit;

        _analyzeLimiter = PartitionedRateLimiter.Create<string, string>(userId =>
            RateLimitPartition.GetSlidingWindowLimiter(
                userId,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = effectiveAnalyzeLimit,
                    Window = win,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        _initLimiter = PartitionedRateLimiter.Create<string, string>(userId =>
            RateLimitPartition.GetSlidingWindowLimiter(
                userId,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = effectiveInitLimit,
                    Window = win,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        _feedbackAudioLimiter = PartitionedRateLimiter.Create<string, string>(userId =>
            RateLimitPartition.GetSlidingWindowLimiter(
                userId,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = effectiveFeedbackLimit,
                    Window = win,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    public bool TryAcquireAnalyze(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        using var lease = _analyzeLimiter.AttemptAcquire(userId);
        return lease.IsAcquired;
    }

    public bool TryAcquireInitialize(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        using var lease = _initLimiter.AttemptAcquire(userId);
        return lease.IsAcquired;
    }

    public bool TryAcquireFeedbackAudio(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        using var lease = _feedbackAudioLimiter.AttemptAcquire(userId);
        return lease.IsAcquired;
    }

    public void Dispose()
    {
        _analyzeLimiter.Dispose();
        _initLimiter.Dispose();
        _feedbackAudioLimiter.Dispose();
    }
}
