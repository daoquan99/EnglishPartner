using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Grammar.Entities;

public sealed class GrammarAttemptAnswerEntity : Entity<Guid>
{
    public static readonly string[] AllowedStatuses =
    [
        "Completed",
        "PendingGrading",
        "FailedGrading",
        "NotAssessable",
        "SelfAssessed"
    ];

    public Guid AttemptId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public string AnswerPayloadJson { get; private set; } = "{}";
    public string Status { get; private set; } = "Completed";
    public decimal? Score100 { get; private set; }
    public decimal? PointsEarned { get; private set; }
    public decimal MaxPoints { get; private set; }
    public string? FeedbackJson { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    private GrammarAttemptAnswerEntity()
    {
    }

    public GrammarAttemptAnswerEntity(
        Guid attemptId,
        Guid exerciseId,
        string answerPayloadJson,
        string status,
        decimal? score100,
        decimal? pointsEarned,
        decimal maxPoints,
        string? feedbackJson,
        string contentHash,
        DateTimeOffset submittedAtUtc,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("AttemptId cannot be empty.", nameof(attemptId));
        }

        if (exerciseId == Guid.Empty)
        {
            throw new ArgumentException("ExerciseId cannot be empty.", nameof(exerciseId));
        }

        AttemptId = attemptId;
        ExerciseId = exerciseId;

        Update(answerPayloadJson, status, score100, pointsEarned, maxPoints, feedbackJson, contentHash, submittedAtUtc);
    }

    public void Update(
        string answerPayloadJson,
        string status,
        decimal? score100,
        decimal? pointsEarned,
        decimal maxPoints,
        string? feedbackJson,
        string contentHash,
        DateTimeOffset submittedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(answerPayloadJson))
        {
            throw new ArgumentException("AnswerPayloadJson is required.", nameof(answerPayloadJson));
        }

        if (answerPayloadJson.Length > 20000)
        {
            throw new ArgumentException("AnswerPayloadJson must not exceed 20000 characters.", nameof(answerPayloadJson));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException($"Status '{status}' is not an allowed status.", nameof(status));
        }

        if (maxPoints < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPoints), "MaxPoints cannot be negative.");
        }

        if (score100.HasValue && (score100.Value < 0 || score100.Value > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(score100), "Score100 must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new ArgumentException("ContentHash is required.", nameof(contentHash));
        }

        AnswerPayloadJson = answerPayloadJson.Trim();
        Status = status.Trim();
        Score100 = score100;
        PointsEarned = pointsEarned;
        MaxPoints = maxPoints;
        FeedbackJson = string.IsNullOrWhiteSpace(feedbackJson) ? null : feedbackJson.Trim();
        ContentHash = contentHash.Trim().ToLowerInvariant();
        SubmittedAtUtc = submittedAtUtc;

        UpdatedAtUtc = submittedAtUtc;
    }
}
