using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Domain.VocabularyGames;

public sealed class FallingWordGameSessionEntity : AggregateRoot<Guid>
{
    private readonly List<FallingWordGameRoundEntity> _rounds = new();

    public string UserId { get; private set; } = string.Empty;
    public FallingWordGameSource Source { get; private set; } = FallingWordGameSource.VocabularyCatalog;
    public string? CefrLevel { get; private set; }
    public FallingWordPlayFilter PlayFilter { get; private set; } = FallingWordPlayFilter.All;
    public Guid? DailyPlanId { get; private set; }
    public string? LearningDate { get; private set; }
    public string? TimeZoneId { get; private set; }

    public int TimeLimitSeconds { get; private set; } = 5;
    public int RequestedCount { get; private set; } = 10;
    public int ActualCount { get; private set; } = 0;

    public FallingWordGameStatus Status { get; private set; } = FallingWordGameStatus.Ready;
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public int CorrectCount { get; private set; }
    public int IncorrectCount { get; private set; }
    public int TimedOutCount { get; private set; }
    public int AbandonedCount { get; private set; }

    public int BestStreak { get; private set; }
    public int CurrentStreak { get; private set; }

    public byte[]? RowVersion { get; private set; }

    public IReadOnlyCollection<FallingWordGameRoundEntity> Rounds => _rounds.AsReadOnly();

    public int EvaluatedRoundsCount => CorrectCount + IncorrectCount + TimedOutCount;
    public int FailedCount => IncorrectCount + TimedOutCount;

    public decimal? Accuracy => EvaluatedRoundsCount > 0
        ? Math.Round(((decimal)CorrectCount / EvaluatedRoundsCount) * 100m, 2, MidpointRounding.AwayFromZero)
        : null;

    private FallingWordGameSessionEntity()
    {
    }

    public FallingWordGameSessionEntity(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        FallingWordPlayFilter playFilter,
        int timeLimitSeconds,
        int requestedCount,
        Guid? dailyPlanId = null,
        string? learningDate = null,
        string? timeZoneId = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (timeLimitSeconds < 1 || timeLimitSeconds > 120)
            throw new ArgumentOutOfRangeException(nameof(timeLimitSeconds), "TimeLimitSeconds must be between 1 and 120.");
        if (requestedCount < 1 || requestedCount > 100)
            throw new ArgumentOutOfRangeException(nameof(requestedCount), "RequestedCount must be between 1 and 100.");

        UserId = userId.Trim();
        Source = source;
        CefrLevel = string.IsNullOrWhiteSpace(cefrLevel) ? null : cefrLevel.Trim().ToUpperInvariant();
        PlayFilter = playFilter;
        TimeLimitSeconds = timeLimitSeconds;
        RequestedCount = requestedCount;

        DailyPlanId = dailyPlanId;
        LearningDate = string.IsNullOrWhiteSpace(learningDate) ? null : learningDate.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();

        Status = FallingWordGameStatus.Ready;
    }

    public static FallingWordGameSessionEntity Create(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        FallingWordPlayFilter playFilter,
        int timeLimitSeconds,
        int requestedCount,
        Guid? dailyPlanId = null,
        string? learningDate = null,
        string? timeZoneId = null,
        Guid? id = null)
    {
        return new FallingWordGameSessionEntity(
            userId,
            source,
            cefrLevel,
            playFilter,
            timeLimitSeconds,
            requestedCount,
            dailyPlanId,
            learningDate,
            timeZoneId,
            id);
    }

    public void AddRound(FallingWordGameRoundEntity round)
    {
        if (Status != FallingWordGameStatus.Ready)
        {
            throw new InvalidOperationException($"Cannot add rounds to session in status '{Status}'. Only Ready sessions accept rounds.");
        }

        if (round.SessionId != Id)
        {
            throw new ArgumentException("Round SessionId does not match this session.", nameof(round));
        }

        _rounds.Add(round);
        ActualCount = _rounds.Count;
    }

    public FallingWordGameRoundEntity? StartNextRound(DateTimeOffset now)
    {
        if (Status == FallingWordGameStatus.Completed || Status == FallingWordGameStatus.Abandoned)
        {
            throw new InvalidOperationException($"Cannot start rounds in '{Status}' session.");
        }

        // 1. If there is already an Active round:
        var currentActive = _rounds.FirstOrDefault(r => r.Status == FallingWordRoundStatus.Active);
        if (currentActive != null)
        {
            // If overdue, finalize timeout first
            if (currentActive.ExpiresAtUtc.HasValue && now >= currentActive.ExpiresAtUtc.Value)
            {
                currentActive.FinalizeTimeout(now);
                TimedOutCount++;
                CurrentStreak = 0;
            }
            else
            {
                // Return same active round (idempotent, does not reset timer!)
                return currentActive;
            }
        }

        // 2. Find next Queued round by order
        var nextQueued = _rounds
            .Where(r => r.Status == FallingWordRoundStatus.Queued)
            .OrderBy(r => r.RoundOrder)
            .FirstOrDefault();

        if (nextQueued == null)
        {
            // All rounds finalized
            Status = FallingWordGameStatus.Completed;
            CompletedAtUtc = now;
            return null;
        }

        Status = FallingWordGameStatus.InProgress;
        StartedAtUtc ??= now;

        nextQueued.Activate(now, TimeSpan.FromSeconds(TimeLimitSeconds));
        return nextQueued;
    }

    public FallingWordGameRoundEntity SubmitAnswer(Guid roundId, string answerText, DateTimeOffset now)
    {
        if (Status == FallingWordGameStatus.Completed || Status == FallingWordGameStatus.Abandoned)
        {
            throw new InvalidOperationException($"Cannot submit answers to a session that is already '{Status}'.");
        }

        var round = _rounds.FirstOrDefault(r => r.Id == roundId)
            ?? throw new KeyNotFoundException($"Round with ID '{roundId}' was not found in session.");

        // If round is already terminal, return existing outcome (idempotent)
        if (round.Status == FallingWordRoundStatus.Correct ||
            round.Status == FallingWordRoundStatus.Incorrect ||
            round.Status == FallingWordRoundStatus.TimedOut)
        {
            return round;
        }

        bool isCorrect = round.SubmitAnswer(answerText, now);

        if (round.Status == FallingWordRoundStatus.Correct)
        {
            CorrectCount++;
            CurrentStreak++;
            if (CurrentStreak > BestStreak)
            {
                BestStreak = CurrentStreak;
            }
        }
        else if (round.Status == FallingWordRoundStatus.Incorrect)
        {
            IncorrectCount++;
            CurrentStreak = 0;
        }
        else if (round.Status == FallingWordRoundStatus.TimedOut)
        {
            TimedOutCount++;
            CurrentStreak = 0;
        }

        CheckSessionCompletion(now);
        return round;
    }

    public FallingWordGameRoundEntity FinalizeTimeout(Guid roundId, DateTimeOffset now)
    {
        var round = _rounds.FirstOrDefault(r => r.Id == roundId)
            ?? throw new KeyNotFoundException($"Round with ID '{roundId}' was not found in session.");

        // If already terminal, return idempotently
        if (round.Status == FallingWordRoundStatus.TimedOut)
        {
            return round;
        }

        if (round.Status == FallingWordRoundStatus.Correct || round.Status == FallingWordRoundStatus.Incorrect)
        {
            return round;
        }

        if (Status == FallingWordGameStatus.Completed || Status == FallingWordGameStatus.Abandoned)
        {
            throw new InvalidOperationException($"Cannot finalize timeout on session in status '{Status}'.");
        }

        round.FinalizeTimeout(now);
        TimedOutCount++;
        CurrentStreak = 0;

        CheckSessionCompletion(now);
        return round;
    }

    public FallingWordGameRoundEntity SkipRound(Guid roundId, DateTimeOffset now)
    {
        if (Status == FallingWordGameStatus.Completed || Status == FallingWordGameStatus.Abandoned)
        {
            throw new InvalidOperationException($"Cannot skip round on session in status '{Status}'.");
        }

        var round = _rounds.FirstOrDefault(r => r.Id == roundId)
            ?? throw new KeyNotFoundException($"Round with ID '{roundId}' was not found in session.");

        if (round.Status == FallingWordRoundStatus.Correct ||
            round.Status == FallingWordRoundStatus.Incorrect ||
            round.Status == FallingWordRoundStatus.TimedOut)
        {
            return round;
        }

        round.Skip(now);
        IncorrectCount++;
        CurrentStreak = 0;

        CheckSessionCompletion(now);
        return round;
    }

    public void Abandon(DateTimeOffset now)
    {
        if (Status == FallingWordGameStatus.Completed || Status == FallingWordGameStatus.Abandoned)
        {
            return; // Idempotent
        }

        foreach (var round in _rounds)
        {
            if (round.Status == FallingWordRoundStatus.Active)
            {
                if (round.ExpiresAtUtc.HasValue && now >= round.ExpiresAtUtc.Value)
                {
                    round.FinalizeTimeout(now);
                    TimedOutCount++;
                    CurrentStreak = 0;
                }
                else
                {
                    round.Abandon(now);
                    AbandonedCount++;
                    CurrentStreak = 0;
                }
            }
            else if (round.Status == FallingWordRoundStatus.Queued)
            {
                round.Abandon(now);
                AbandonedCount++;
            }
        }

        Status = FallingWordGameStatus.Abandoned;
        CompletedAtUtc = now;
    }

    private void CheckSessionCompletion(DateTimeOffset now)
    {
        bool hasPending = _rounds.Any(r =>
            r.Status == FallingWordRoundStatus.Queued ||
            r.Status == FallingWordRoundStatus.Active);

        if (!hasPending && _rounds.Count > 0)
        {
            Status = FallingWordGameStatus.Completed;
            CompletedAtUtc = now;
        }
    }
}
