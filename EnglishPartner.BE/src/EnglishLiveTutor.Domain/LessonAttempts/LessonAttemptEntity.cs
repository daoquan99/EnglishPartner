using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.LessonAttempts.Entities;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Domain.LessonAttempts;

/// <summary>
/// Aggregate root representing a learner's attempt at a lesson.
/// Tracks attempt revision for optimistic concurrency, answer submissions, and scoring.
/// </summary>
public sealed class LessonAttemptEntity : AggregateRoot<Guid>
{
    public Guid LessonId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int PinnedLessonVersion { get; private set; } = 1;
    public int AttemptRevision { get; private set; } = 1;
    public LessonAttemptStatus Status { get; private set; } = LessonAttemptStatus.InProgress;
    public DateTimeOffset StartedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public decimal CurrentSubtotalPoints { get; private set; }
    public decimal TotalMaxPoints { get; private set; }
    public decimal? FinalScore100 { get; private set; }

    private readonly List<ExerciseAnswerEntity> _answers = new();
    public IReadOnlyCollection<ExerciseAnswerEntity> Answers => _answers.AsReadOnly();

    private LessonAttemptEntity()
    {
    }

    public LessonAttemptEntity(
        Guid lessonId,
        string userId,
        int pinnedLessonVersion,
        decimal totalMaxPoints,
        DateTimeOffset? startedAtUtc = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (lessonId == Guid.Empty)
            throw new ArgumentException("Lesson ID cannot be empty.", nameof(lessonId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (pinnedLessonVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(pinnedLessonVersion), "PinnedLessonVersion must be at least 1.");
        if (totalMaxPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(totalMaxPoints), "TotalMaxPoints cannot be negative.");

        LessonId = lessonId;
        UserId = userId.Trim();
        PinnedLessonVersion = pinnedLessonVersion;
        TotalMaxPoints = totalMaxPoints;
        AttemptRevision = 1;
        Status = LessonAttemptStatus.InProgress;
        StartedAtUtc = startedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public static LessonAttemptEntity Start(
        Guid lessonId,
        string userId,
        int pinnedLessonVersion,
        decimal totalMaxPoints,
        Guid? id = null) =>
        new(lessonId, userId, pinnedLessonVersion, totalMaxPoints, DateTimeOffset.UtcNow, id);

    public ExerciseAnswerEntity SubmitAnswer(
        Guid exerciseId,
        Guid? subQuestionId,
        int expectedRevision,
        GradingMode gradingMode,
        decimal maxPoints,
        string answerPayloadJson,
        SubmissionGradingStatus initialStatus = SubmissionGradingStatus.Pending)
    {
        EnsureInProgress();

        if (expectedRevision != AttemptRevision)
        {
            throw new InvalidOperationException(
                $"Attempt revision mismatch. Expected {AttemptRevision}, provided {expectedRevision}.");
        }

        var existing = _answers.FirstOrDefault(a =>
            a.ExerciseId == exerciseId &&
            a.SubQuestionId == subQuestionId);

        ExerciseAnswerEntity answer;
        if (existing != null)
        {
            existing.UpdateSubmission(answerPayloadJson, initialStatus);
            answer = existing;
        }
        else
        {
            answer = new ExerciseAnswerEntity(
                Id,
                exerciseId,
                subQuestionId,
                gradingMode,
                maxPoints,
                answerPayloadJson,
                initialStatus);
            _answers.Add(answer);
        }

        AttemptRevision++;
        RecalculatePoints();
        return answer;
    }

    public void ApplyGradingResult(
        Guid answerId,
        SubmissionGradingStatus status,
        decimal? score100,
        decimal? pointsEarned,
        string? gradingResultJson,
        DateTimeOffset gradedAtUtc)
    {
        var answer = _answers.FirstOrDefault(a => a.Id == answerId)
            ?? throw new KeyNotFoundException($"Answer with ID '{answerId}' was not found in attempt.");

        switch (status)
        {
            case SubmissionGradingStatus.Completed:
                answer.CompleteGrading(score100, pointsEarned, gradingResultJson, gradedAtUtc);
                break;
            case SubmissionGradingStatus.NotAssessable:
                answer.MarkNotAssessable(gradingResultJson, gradedAtUtc);
                break;
            case SubmissionGradingStatus.Failed:
                answer.MarkFailed(gradingResultJson, gradedAtUtc);
                break;
            default:
                throw new ArgumentException($"Cannot apply non-terminal grading status '{status}'.", nameof(status));
        }

        RecalculatePoints();
    }

    public void RetryGrading(Guid answerId, int expectedRevision)
    {
        EnsureInProgress();

        if (expectedRevision != AttemptRevision)
        {
            throw new InvalidOperationException(
                $"Attempt revision mismatch. Expected {AttemptRevision}, provided {expectedRevision}.");
        }

        var answer = _answers.FirstOrDefault(a => a.Id == answerId)
            ?? throw new KeyNotFoundException($"Answer with ID '{answerId}' was not found in attempt.");

        if (answer.Status != SubmissionGradingStatus.Failed && answer.Status != SubmissionGradingStatus.NotAssessable)
        {
            throw new InvalidOperationException($"Cannot retry grading for answer with status '{answer.Status}'. Must be Failed or NotAssessable.");
        }

        answer.UpdateSubmission(answer.AnswerPayloadJson, SubmissionGradingStatus.Pending);
        AttemptRevision++;
        RecalculatePoints();
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        EnsureInProgress();

        Status = LessonAttemptStatus.Completed;
        CompletedAtUtc = completedAtUtc;

        RecalculatePoints();

        if (TotalMaxPoints > 0)
        {
            FinalScore100 = Math.Round((CurrentSubtotalPoints / TotalMaxPoints) * 100m, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            FinalScore100 = 100m;
        }
    }

    public void Abandon(DateTimeOffset abandonedAtUtc)
    {
        if (Status == LessonAttemptStatus.Abandoned)
            return;

        Status = LessonAttemptStatus.Abandoned;
        CompletedAtUtc = abandonedAtUtc;
    }

    private void EnsureInProgress()
    {
        if (Status != LessonAttemptStatus.InProgress)
            throw new InvalidOperationException($"Cannot perform action when attempt is in '{Status}' status.");
    }

    private void RecalculatePoints()
    {
        CurrentSubtotalPoints = _answers
            .Where(a => a.Status == SubmissionGradingStatus.Completed && a.PointsEarned.HasValue)
            .Sum(a => a.PointsEarned!.Value);
    }
}
