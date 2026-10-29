using System.Text.Json;
using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Grammar.Entities;
using EnglishLiveTutor.Domain.Grammar.Enums;

namespace EnglishLiveTutor.Domain.Grammar;

public sealed class GrammarAttemptEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = string.Empty;
    public GrammarAttemptKind Kind { get; private set; }
    public Guid? StepId { get; private set; }
    public string CefrLevel { get; private set; } = string.Empty;
    public string ExerciseIdsJson { get; private set; } = "[]";
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public decimal? Score100 { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private readonly List<GrammarAttemptAnswerEntity> _answers = new();
    public IReadOnlyCollection<GrammarAttemptAnswerEntity> Answers => _answers.AsReadOnly();

    private IReadOnlyList<Guid>? _cachedExerciseIds;

    public IReadOnlyList<Guid> ExerciseIds
    {
        get
        {
            if (_cachedExerciseIds != null)
            {
                return _cachedExerciseIds;
            }

            if (string.IsNullOrWhiteSpace(ExerciseIdsJson) || ExerciseIdsJson == "[]")
            {
                _cachedExerciseIds = Array.Empty<Guid>();
                return _cachedExerciseIds;
            }

            try
            {
                _cachedExerciseIds = JsonSerializer.Deserialize<List<Guid>>(ExerciseIdsJson) ?? new List<Guid>();
            }
            catch
            {
                _cachedExerciseIds = Array.Empty<Guid>();
            }

            return _cachedExerciseIds;
        }
    }

    private GrammarAttemptEntity()
    {
    }

    public GrammarAttemptEntity(
        string userId,
        GrammarAttemptKind kind,
        Guid? stepId,
        string cefrLevel,
        IReadOnlyList<Guid> exerciseIds,
        DateTimeOffset startedAtUtc,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(cefrLevel))
        {
            throw new ArgumentException("CefrLevel is required.", nameof(cefrLevel));
        }

        string normalizedCefr = cefrLevel.Trim().ToUpperInvariant();
        if (!GrammarStepEntity.Levels.Contains(normalizedCefr))
        {
            throw new ArgumentException($"CefrLevel must be one of {string.Join(", ", GrammarStepEntity.Levels)}.", nameof(cefrLevel));
        }

        if (exerciseIds == null || exerciseIds.Count < 1 || exerciseIds.Count > 40)
        {
            throw new ArgumentException("ExerciseIds must contain between 1 and 40 items.", nameof(exerciseIds));
        }

        if (exerciseIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("ExerciseIds cannot contain Guid.Empty.", nameof(exerciseIds));
        }

        if (exerciseIds.Distinct().Count() != exerciseIds.Count)
        {
            throw new ArgumentException("ExerciseIds cannot contain duplicate IDs.", nameof(exerciseIds));
        }

        if (kind is GrammarAttemptKind.StepCore or GrammarAttemptKind.Practice)
        {
            if (!stepId.HasValue || stepId.Value == Guid.Empty)
            {
                throw new ArgumentException("StepId is required for StepCore and Practice attempts.", nameof(stepId));
            }
        }

        UserId = userId.Trim();
        Kind = kind;
        StepId = kind == GrammarAttemptKind.LevelTest ? null : stepId;
        CefrLevel = normalizedCefr;
        ExerciseIdsJson = JsonSerializer.Serialize(exerciseIds);
        _cachedExerciseIds = exerciseIds.ToList();
        StartedAtUtc = startedAtUtc;

        CreatedAtUtc = startedAtUtc;
        UpdatedAtUtc = startedAtUtc;
    }

    public static GrammarAttemptEntity Start(
        string userId,
        GrammarAttemptKind kind,
        Guid? stepId,
        string cefrLevel,
        IReadOnlyList<Guid> exerciseIds,
        DateTimeOffset startedAtUtc,
        Guid? id = null) =>
        new(userId, kind, stepId, cefrLevel, exerciseIds, startedAtUtc, id);

    public GrammarAttemptAnswerEntity SubmitAnswer(
        Guid exerciseId,
        string answerPayloadJson,
        string status,
        decimal? score100,
        decimal? pointsEarned,
        decimal maxPoints,
        string? feedbackJson,
        string contentHash,
        DateTimeOffset submittedAtUtc)
    {
        if (CompletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Cannot submit answer to a completed attempt.");
        }

        if (!ExerciseIds.Contains(exerciseId))
        {
            throw new InvalidOperationException($"Exercise '{exerciseId}' does not belong to this attempt.");
        }

        if (!GrammarAttemptAnswerEntity.AllowedStatuses.Contains(status))
        {
            throw new ArgumentException($"Status '{status}' is not an allowed status.", nameof(status));
        }

        var existing = _answers.FirstOrDefault(a => a.ExerciseId == exerciseId);
        if (existing != null)
        {
            if (Kind is GrammarAttemptKind.StepCore or GrammarAttemptKind.LevelTest &&
                !string.Equals(existing.Status, "FailedGrading", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("grammar_answer_locked");
            }

            existing.Update(answerPayloadJson, status, score100, pointsEarned, maxPoints, feedbackJson, contentHash, submittedAtUtc);
            UpdatedAtUtc = submittedAtUtc;
            return existing;
        }

        var newAnswer = new GrammarAttemptAnswerEntity(
            attemptId: Id,
            exerciseId: exerciseId,
            answerPayloadJson: answerPayloadJson,
            status: status,
            score100: score100,
            pointsEarned: pointsEarned,
            maxPoints: maxPoints,
            feedbackJson: feedbackJson,
            contentHash: contentHash,
            submittedAtUtc: submittedAtUtc);

        _answers.Add(newAnswer);
        UpdatedAtUtc = submittedAtUtc;
        return newAnswer;
    }

    public void Complete(decimal score100, DateTimeOffset completedAtUtc)
    {
        if (CompletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Attempt has already been completed.");
        }

        if (score100 < 0 || score100 > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(score100), "Score must be between 0 and 100.");
        }

        Score100 = score100;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
    }
}
