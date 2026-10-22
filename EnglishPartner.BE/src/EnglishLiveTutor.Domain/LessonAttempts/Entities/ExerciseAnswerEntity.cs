using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Domain.LessonAttempts.Entities;

/// <summary>
/// Represents an answer submitted by a learner for a specific exercise or sub-question within an attempt.
/// </summary>
public sealed class ExerciseAnswerEntity : Entity<Guid>
{
    public Guid AttemptId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public Guid? SubQuestionId { get; private set; }
    public GradingMode GradingMode { get; private set; }
    public decimal MaxPoints { get; private set; }
    public SubmissionGradingStatus Status { get; private set; } = SubmissionGradingStatus.Pending;
    public decimal? Score100 { get; private set; }
    public decimal? PointsEarned { get; private set; }
    public string AnswerPayloadJson { get; private set; } = "{}";
    public string? GradingResultJson { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? GradedAtUtc { get; private set; }

    private ExerciseAnswerEntity()
    {
    }

    public ExerciseAnswerEntity(
        Guid attemptId,
        Guid exerciseId,
        Guid? subQuestionId,
        GradingMode gradingMode,
        decimal maxPoints,
        string answerPayloadJson,
        SubmissionGradingStatus status = SubmissionGradingStatus.Pending,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (attemptId == Guid.Empty)
            throw new ArgumentException("AttemptId cannot be empty.", nameof(attemptId));
        if (exerciseId == Guid.Empty)
            throw new ArgumentException("ExerciseId cannot be empty.", nameof(exerciseId));
        if (maxPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(maxPoints), "MaxPoints cannot be negative.");
        if (string.IsNullOrWhiteSpace(answerPayloadJson))
            throw new ArgumentException("AnswerPayloadJson is required.", nameof(answerPayloadJson));

        AttemptId = attemptId;
        ExerciseId = exerciseId;
        SubQuestionId = subQuestionId;
        GradingMode = gradingMode;
        MaxPoints = maxPoints;
        AnswerPayloadJson = answerPayloadJson.Trim();
        Status = status;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateSubmission(string answerPayloadJson, SubmissionGradingStatus status = SubmissionGradingStatus.Pending)
    {
        if (string.IsNullOrWhiteSpace(answerPayloadJson))
            throw new ArgumentException("AnswerPayloadJson is required.", nameof(answerPayloadJson));

        AnswerPayloadJson = answerPayloadJson.Trim();
        Status = status;
        Score100 = null;
        PointsEarned = null;
        GradingResultJson = null;
        LeaseExpiresAtUtc = null;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
        GradedAtUtc = null;
    }

    public void AcquireGradingLease(DateTimeOffset leaseExpiresAtUtc)
    {
        Status = SubmissionGradingStatus.Grading;
        LeaseExpiresAtUtc = leaseExpiresAtUtc;
    }

    public void ReleaseGradingLease()
    {
        LeaseExpiresAtUtc = null;
        if (Status == SubmissionGradingStatus.Grading)
        {
            Status = SubmissionGradingStatus.Pending;
        }
    }

    public void CompleteGrading(
        decimal? score100,
        decimal? pointsEarned,
        string? gradingResultJson,
        DateTimeOffset gradedAtUtc)
    {
        Status = SubmissionGradingStatus.Completed;
        Score100 = score100;
        PointsEarned = pointsEarned;
        GradingResultJson = gradingResultJson?.Trim();
        LeaseExpiresAtUtc = null;
        GradedAtUtc = gradedAtUtc;
    }

    public void AttachAiReview(string updatedGradingResultJson)
    {
        GradingResultJson = updatedGradingResultJson?.Trim();
    }

    public void MarkNotAssessable(string? gradingResultJson, DateTimeOffset evaluatedAtUtc)
    {
        Status = SubmissionGradingStatus.NotAssessable;
        Score100 = null;
        PointsEarned = null;
        GradingResultJson = gradingResultJson?.Trim();
        LeaseExpiresAtUtc = null;
        GradedAtUtc = evaluatedAtUtc;
    }

    public void MarkFailed(string? failureReasonJson, DateTimeOffset failedAtUtc)
    {
        Status = SubmissionGradingStatus.Failed;
        Score100 = null;
        PointsEarned = null;
        GradingResultJson = failureReasonJson?.Trim();
        LeaseExpiresAtUtc = null;
        GradedAtUtc = failedAtUtc;
    }
}
