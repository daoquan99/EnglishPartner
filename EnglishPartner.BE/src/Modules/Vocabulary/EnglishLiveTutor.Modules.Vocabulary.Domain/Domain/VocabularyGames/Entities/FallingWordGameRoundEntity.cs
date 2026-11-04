using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Domain.VocabularyGames.Entities;

public sealed class FallingWordGameRoundEntity : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public int RoundOrder { get; private set; }
    public Guid VocabularyId { get; private set; }
    public Guid SenseId { get; private set; }
    public Guid? DailyItemId { get; private set; }

    /// <summary>
    /// Private target snapshots stored securely on the server.
    /// Not exposed to the learner in active challenge DTOs until the round reaches a terminal outcome.
    /// </summary>
    public string TargetWordSnapshot { get; private set; } = string.Empty;
    public string NormalizedWordSnapshot { get; private set; } = string.Empty;

    public string MeaningViSnapshot { get; private set; } = string.Empty;
    public string? CefrLevelSnapshot { get; private set; }
    public string MaskedPattern { get; private set; } = string.Empty;
    public int LetterCount { get; private set; }

    public FallingWordRoundStatus Status { get; private set; } = FallingWordRoundStatus.Queued;
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public string? SubmittedText { get; private set; }
    public long? ElapsedMs { get; private set; }
    public string? FailureReason { get; private set; }

    private FallingWordGameRoundEntity()
    {
    }

    public FallingWordGameRoundEntity(
        Guid sessionId,
        int roundOrder,
        Guid vocabularyId,
        Guid senseId,
        string targetWord,
        string meaningVi,
        string? cefrLevel,
        Guid? dailyItemId = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (roundOrder < 1)
            throw new ArgumentOutOfRangeException(nameof(roundOrder), "RoundOrder must be at least 1.");
        if (vocabularyId == Guid.Empty)
            throw new ArgumentException("VocabularyId is required.", nameof(vocabularyId));
        if (senseId == Guid.Empty)
            throw new ArgumentException("SenseId is required.", nameof(senseId));
        if (string.IsNullOrWhiteSpace(targetWord))
            throw new ArgumentException("TargetWord is required.", nameof(targetWord));
        if (string.IsNullOrWhiteSpace(meaningVi))
            throw new ArgumentException("MeaningVi is required.", nameof(meaningVi));

        SessionId = sessionId;
        RoundOrder = roundOrder;
        VocabularyId = vocabularyId;
        SenseId = senseId;
        DailyItemId = dailyItemId;

        TargetWordSnapshot = targetWord.Trim();
        NormalizedWordSnapshot = NormalizeWord(targetWord);
        MeaningViSnapshot = meaningVi.Trim();
        CefrLevelSnapshot = string.IsNullOrWhiteSpace(cefrLevel) ? null : cefrLevel.Trim().ToUpperInvariant();

        MaskedPattern = GenerateMaskedPattern(TargetWordSnapshot);
        LetterCount = CountMeaningfulLetters(TargetWordSnapshot);

        Status = FallingWordRoundStatus.Queued;
    }

    public void Activate(DateTimeOffset now, TimeSpan timeLimit)
    {
        if (Status != FallingWordRoundStatus.Queued)
        {
            throw new InvalidOperationException($"Cannot activate round in status '{Status}'. Only Queued rounds can be activated.");
        }

        Status = FallingWordRoundStatus.Active;
        StartedAtUtc = now;
        ExpiresAtUtc = now.Add(timeLimit);
    }

    public bool SubmitAnswer(string answerText, DateTimeOffset now)
    {
        if (Status != FallingWordRoundStatus.Active)
        {
            throw new InvalidOperationException($"Cannot submit answer for round in status '{Status}'.");
        }

        // Authoritative server deadline check
        if (ExpiresAtUtc.HasValue && now >= ExpiresAtUtc.Value)
        {
            Status = FallingWordRoundStatus.TimedOut;
            SubmittedText = answerText?.Trim();
            SubmittedAtUtc = now;
            ElapsedMs = StartedAtUtc.HasValue ? (long)Math.Max(0, (now - StartedAtUtc.Value).TotalMilliseconds) : null;
            FailureReason = "ExpiredBeforeSubmit";
            return false;
        }

        string normalizedAnswer = NormalizeWord(answerText);
        bool isCorrect = string.Equals(normalizedAnswer, NormalizedWordSnapshot, StringComparison.OrdinalIgnoreCase);

        Status = isCorrect ? FallingWordRoundStatus.Correct : FallingWordRoundStatus.Incorrect;
        SubmittedText = answerText?.Trim();
        SubmittedAtUtc = now;
        ElapsedMs = StartedAtUtc.HasValue ? (long)Math.Max(0, (now - StartedAtUtc.Value).TotalMilliseconds) : null;
        FailureReason = isCorrect ? null : "IncorrectSpelling";

        return isCorrect;
    }

    public void FinalizeTimeout(DateTimeOffset now)
    {
        if (Status == FallingWordRoundStatus.TimedOut)
        {
            return; // Idempotent
        }

        if (Status != FallingWordRoundStatus.Active)
        {
            throw new InvalidOperationException($"Cannot finalize timeout for round in status '{Status}'.");
        }

        if (ExpiresAtUtc.HasValue && now < ExpiresAtUtc.Value)
        {
            throw new InvalidOperationException("Cannot timeout round before server expiration deadline.");
        }

        Status = FallingWordRoundStatus.TimedOut;
        SubmittedAtUtc ??= now;
        ElapsedMs = StartedAtUtc.HasValue ? (long)Math.Max(0, (now - StartedAtUtc.Value).TotalMilliseconds) : null;
        FailureReason = "ExpiredBeforeSubmit";
    }

    public void Skip(DateTimeOffset now)
    {
        if (Status != FallingWordRoundStatus.Active)
        {
            throw new InvalidOperationException($"Cannot skip round in status '{Status}'. Only Active rounds can be skipped.");
        }

        Status = FallingWordRoundStatus.Incorrect;
        SubmittedText = "[Bỏ qua]";
        SubmittedAtUtc = now;
        ElapsedMs = StartedAtUtc.HasValue ? (long)Math.Max(0, (now - StartedAtUtc.Value).TotalMilliseconds) : null;
        FailureReason = "SkippedByLearner";
    }

    public void Abandon(DateTimeOffset now)
    {
        if (Status == FallingWordRoundStatus.Correct ||
            Status == FallingWordRoundStatus.Incorrect ||
            Status == FallingWordRoundStatus.TimedOut ||
            Status == FallingWordRoundStatus.Abandoned)
        {
            return; // Terminal states are immutable
        }

        if (Status == FallingWordRoundStatus.Active)
        {
            // If already overdue at abandonment time, record truthful TimedOut
            if (ExpiresAtUtc.HasValue && now >= ExpiresAtUtc.Value)
            {
                FinalizeTimeout(now);
                return;
            }

            Status = FallingWordRoundStatus.Abandoned;
            SubmittedAtUtc = now;
            ElapsedMs = StartedAtUtc.HasValue ? (long)Math.Max(0, (now - StartedAtUtc.Value).TotalMilliseconds) : null;
            FailureReason = "AbandonedByLearner";
            return;
        }

        if (Status == FallingWordRoundStatus.Queued)
        {
            Status = FallingWordRoundStatus.Abandoned;
            FailureReason = "SessionAbandoned";
        }
    }

    public static string NormalizeWord(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim().ToLowerInvariant();
    }

    public static string GenerateMaskedPattern(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        var tokens = new List<string>();
        foreach (char c in word.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                tokens.Add("_");
            }
            else
            {
                tokens.Add(c.ToString());
            }
        }

        return string.Join(" ", tokens);
    }

    public static int CountMeaningfulLetters(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return 0;

        return word.Count(char.IsLetterOrDigit);
    }
}
