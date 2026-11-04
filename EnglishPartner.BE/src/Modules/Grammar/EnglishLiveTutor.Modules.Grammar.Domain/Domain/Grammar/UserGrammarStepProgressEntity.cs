using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Grammar.Enums;

namespace EnglishLiveTutor.Domain.Grammar;

public sealed class UserGrammarStepProgressEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = string.Empty;
    public Guid StepId { get; private set; }
    public GrammarStepProgressStatus Status { get; private set; } = GrammarStepProgressStatus.Unlocked;
    public decimal BestScore100 { get; private set; }
    public GrammarStepPassedBy? PassedBy { get; private set; }
    public DateTimeOffset UnlockedAtUtc { get; private set; }
    public DateTimeOffset? PassedAtUtc { get; private set; }

    private UserGrammarStepProgressEntity()
    {
    }

    public UserGrammarStepProgressEntity(
        string userId,
        Guid stepId,
        DateTimeOffset unlockedAtUtc,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (stepId == Guid.Empty)
        {
            throw new ArgumentException("StepId cannot be empty.", nameof(stepId));
        }

        UserId = userId.Trim();
        StepId = stepId;
        Status = GrammarStepProgressStatus.Unlocked;
        BestScore100 = 0m;
        PassedBy = null;
        UnlockedAtUtc = unlockedAtUtc;
        PassedAtUtc = null;

        CreatedAtUtc = unlockedAtUtc;
        UpdatedAtUtc = unlockedAtUtc;
    }

    public static UserGrammarStepProgressEntity Create(
        string userId,
        Guid stepId,
        DateTimeOffset unlockedAtUtc,
        Guid? id = null) =>
        new(userId, stepId, unlockedAtUtc, id);

    public void RecordScore(decimal score100, decimal passThreshold, DateTimeOffset now)
    {
        if (score100 < 0 || score100 > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(score100), "Score must be between 0 and 100.");
        }

        if (score100 > BestScore100)
        {
            BestScore100 = score100;
        }

        if (score100 >= passThreshold && Status != GrammarStepProgressStatus.Passed)
        {
            Status = GrammarStepProgressStatus.Passed;
            PassedBy = GrammarStepPassedBy.Steps;
            PassedAtUtc = now;
        }

        UpdatedAtUtc = now;
    }

    public void MarkPassedByLevelTest(DateTimeOffset now)
    {
        if (Status == GrammarStepProgressStatus.Passed)
        {
            return;
        }

        Status = GrammarStepProgressStatus.Passed;
        PassedBy = GrammarStepPassedBy.LevelTest;
        PassedAtUtc = now;
        UpdatedAtUtc = now;
    }
}
