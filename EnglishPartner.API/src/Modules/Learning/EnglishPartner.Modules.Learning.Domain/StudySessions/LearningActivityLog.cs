using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.StudySessions;

/// <summary>
/// Tracks a user's specific interaction with a single curriculum item.
/// Used to log what they answered, how long it took, and if it was correct.
/// Immutable log record — extends Entity (not AggregateRoot) since it's never soft-deleted or updated.
/// </summary>
public sealed class LearningActivityLog : Entity
{
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid ItemId { get; private set; }
    public string UserAnswer { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public int ScoreGranted { get; private set; } // The SM-2 Quality Score (0-5)
    public int? RawScore { get; private set; }    // The AI evaluation score (0-100), if applicable
    public string? AiFeedback { get; private set; } // The AI explanation or pronunciation feedback, if applicable
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
        int timeSpentSeconds,
        DateTime utcNow,
        int? rawScore = null,
        string? aiFeedback = null)
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
            RawScore = rawScore,
            AiFeedback = aiFeedback,
            TimeSpentSeconds = timeSpentSeconds,
            Timestamp = utcNow
        };
    }
}
