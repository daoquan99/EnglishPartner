using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.StudySessions;

/// <summary>
/// Tracks a user's specific interaction with a single curriculum item.
/// Used to log what they answered, how long it took, and if it was correct.
/// </summary>
public sealed class LearningActivityLog : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid ItemId { get; private set; }
    public string UserAnswer { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public int ScoreGranted { get; private set; }
    public int TimeSpentSeconds { get; private set; }
    public DateTime Timestamp { get; private set; }

    private LearningActivityLog() { }

    public static LearningActivityLog Record(
        Guid userId, 
        Guid sessionId, 
        Guid itemId, 
        string userAnswer, 
        bool isCorrect, 
        int scoreGranted, 
        int timeSpentSeconds)
    {
        return new LearningActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            ItemId = itemId,
            UserAnswer = userAnswer ?? string.Empty,
            IsCorrect = isCorrect,
            ScoreGranted = scoreGranted,
            TimeSpentSeconds = timeSpentSeconds,
            Timestamp = DateTime.UtcNow
        };
    }
}
