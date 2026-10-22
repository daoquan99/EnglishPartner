using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public class FallingWordGameSessionTests
{
    private readonly DateTimeOffset _baseTime = new(2026, 10, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateSession_ValidArguments_InitializesCorrectly()
    {
        var session = FallingWordGameSessionEntity.Create(
            userId: "user-123",
            source: FallingWordGameSource.VocabularyCatalog,
            cefrLevel: "b1",
            playFilter: FallingWordPlayFilter.Unplayed,
            timeLimitSeconds: 5,
            requestedCount: 10);

        Assert.Equal("user-123", session.UserId);
        Assert.Equal(FallingWordGameSource.VocabularyCatalog, session.Source);
        Assert.Equal("B1", session.CefrLevel);
        Assert.Equal(FallingWordPlayFilter.Unplayed, session.PlayFilter);
        Assert.Equal(5, session.TimeLimitSeconds);
        Assert.Equal(10, session.RequestedCount);
        Assert.Equal(0, session.ActualCount);
        Assert.Equal(FallingWordGameStatus.Ready, session.Status);
        Assert.Null(session.Accuracy);
    }

    [Fact]
    public void AddRound_OnlyWhenReady_CalculatesActualCount()
    {
        var session = FallingWordGameSessionEntity.Create("user-123", FallingWordGameSource.VocabularyCatalog, null, FallingWordPlayFilter.All, 5, 2);
        var round1 = new FallingWordGameRoundEntity(session.Id, 1, Guid.NewGuid(), Guid.NewGuid(), "schedule", "lịch trình", "B1");
        var round2 = new FallingWordGameRoundEntity(session.Id, 2, Guid.NewGuid(), Guid.NewGuid(), "calendar", "lịch", "A2");

        session.AddRound(round1);
        session.AddRound(round2);

        Assert.Equal(2, session.ActualCount);
        Assert.Equal(2, session.Rounds.Count);
    }

    [Fact]
    public void StartNextRound_ActivatesFirstQueuedRound_SetsServerAuthoritativeDeadline()
    {
        var session = CreateSessionWithRounds("apple", "banana");

        var activeRound = session.StartNextRound(_baseTime);

        Assert.NotNull(activeRound);
        Assert.Equal(1, activeRound.RoundOrder);
        Assert.Equal(FallingWordRoundStatus.Active, activeRound.Status);
        Assert.Equal(_baseTime, activeRound.StartedAtUtc);
        Assert.Equal(_baseTime.AddSeconds(5), activeRound.ExpiresAtUtc);
        Assert.Equal(FallingWordGameStatus.InProgress, session.Status);
    }

    [Fact]
    public void StartNextRound_WhenAlreadyActiveAndNotExpired_ReturnsSameActiveRoundWithoutResettingTimer()
    {
        var session = CreateSessionWithRounds("apple", "banana");
        var firstCall = session.StartNextRound(_baseTime);

        // Call again 2 seconds later
        var secondCall = session.StartNextRound(_baseTime.AddSeconds(2));

        Assert.NotNull(secondCall);
        Assert.Equal(firstCall!.Id, secondCall.Id);
        Assert.Equal(_baseTime, secondCall.StartedAtUtc);
        Assert.Equal(_baseTime.AddSeconds(5), secondCall.ExpiresAtUtc); // Deadline was NOT reset!
    }

    [Fact]
    public void StartNextRound_WhenAlreadyActiveAndOverdue_FinalizesTimeoutAndAdvances()
    {
        var session = CreateSessionWithRounds("apple", "banana");
        session.StartNextRound(_baseTime);

        // Learner never submitted, 6 seconds elapsed (deadline was 5s)
        var nextRound = session.StartNextRound(_baseTime.AddSeconds(6));

        Assert.NotNull(nextRound);
        Assert.Equal(2, nextRound.RoundOrder);
        Assert.Equal(1, session.TimedOutCount);
        Assert.Equal(0, session.CurrentStreak);
    }

    [Fact]
    public void SubmitAnswer_CorrectSpelling_IncrementsStreakAndCorrectCount()
    {
        var session = CreateSessionWithRounds("schedule", "calendar");
        var round = session.StartNextRound(_baseTime)!;

        // Submitting case-insensitive with whitespace
        var result = session.SubmitAnswer(round.Id, "  Schedule  ", _baseTime.AddSeconds(3));

        Assert.Equal(FallingWordRoundStatus.Correct, result.Status);
        Assert.Equal(1, session.CorrectCount);
        Assert.Equal(0, session.IncorrectCount);
        Assert.Equal(1, session.CurrentStreak);
        Assert.Equal(1, session.BestStreak);
        Assert.Equal(100m, session.Accuracy);
    }

    [Fact]
    public void SubmitAnswer_IncorrectSpelling_ResetsStreakAndIncrementsIncorrectCount()
    {
        var session = CreateSessionWithRounds("schedule", "calendar");
        var round = session.StartNextRound(_baseTime)!;

        var result = session.SubmitAnswer(round.Id, "Scheduel", _baseTime.AddSeconds(2));

        Assert.Equal(FallingWordRoundStatus.Incorrect, result.Status);
        Assert.Equal(0, session.CorrectCount);
        Assert.Equal(1, session.IncorrectCount);
        Assert.Equal(0, session.CurrentStreak);
        Assert.Equal(0m, session.Accuracy);
        Assert.Equal("IncorrectSpelling", result.FailureReason);
    }

    [Fact]
    public void SubmitAnswer_AfterDeadline_MarkedAsTimedOutWithAuthoritativeFailureReason()
    {
        var session = CreateSessionWithRounds("schedule", "calendar");
        var round = session.StartNextRound(_baseTime)!;

        // Submitted at second 5.1 (deadline was at second 5.0)
        var result = session.SubmitAnswer(round.Id, "schedule", _baseTime.AddSeconds(5.1));

        Assert.Equal(FallingWordRoundStatus.TimedOut, result.Status);
        Assert.Equal(0, session.CorrectCount);
        Assert.Equal(1, session.TimedOutCount);
        Assert.Equal("ExpiredBeforeSubmit", result.FailureReason);
    }

    [Fact]
    public void SubmitAnswer_DoubleSubmit_ReturnsExistingTerminalOutcomeIdempotently()
    {
        var session = CreateSessionWithRounds("schedule", "calendar");
        var round = session.StartNextRound(_baseTime)!;

        var first = session.SubmitAnswer(round.Id, "schedule", _baseTime.AddSeconds(2));
        var second = session.SubmitAnswer(round.Id, "wrong", _baseTime.AddSeconds(3));

        Assert.Equal(FallingWordRoundStatus.Correct, second.Status);
        Assert.Equal(1, session.CorrectCount);
        Assert.Equal(0, session.IncorrectCount); // Not incremented again!
    }

    [Fact]
    public void FinalizeTimeout_BeforeDeadline_ThrowsInvalidOperationException()
    {
        var session = CreateSessionWithRounds("schedule");
        var round = session.StartNextRound(_baseTime)!;

        // Calling timeout at second 4 when deadline is at second 5
        Assert.Throws<InvalidOperationException>(() =>
            session.FinalizeTimeout(round.Id, _baseTime.AddSeconds(4)));
    }

    [Fact]
    public void FinalizeTimeout_AfterDeadline_SetsTimedOutStatus()
    {
        var session = CreateSessionWithRounds("schedule");
        var round = session.StartNextRound(_baseTime)!;

        var result = session.FinalizeTimeout(round.Id, _baseTime.AddSeconds(5.5));

        Assert.Equal(FallingWordRoundStatus.TimedOut, result.Status);
        Assert.Equal(1, session.TimedOutCount);
        Assert.Equal(FallingWordGameStatus.Completed, session.Status);
    }

    [Fact]
    public void Abandon_WhenActiveRoundOverdue_FinalizesTimedOutTruthfully()
    {
        var session = CreateSessionWithRounds("apple", "banana");
        session.StartNextRound(_baseTime);

        // Abandon at second 6 (expired at 5)
        session.Abandon(_baseTime.AddSeconds(6));

        Assert.Equal(FallingWordGameStatus.Abandoned, session.Status);
        Assert.Equal(1, session.TimedOutCount);
        Assert.Equal(1, session.AbandonedCount); // The queued 2nd round is abandoned
    }

    [Fact]
    public void Abandon_WhenActiveRoundNotExpired_MarksAbandonedAndPreservesStats()
    {
        var session = CreateSessionWithRounds("apple", "banana");
        session.StartNextRound(_baseTime);

        // Abandon at second 2
        session.Abandon(_baseTime.AddSeconds(2));

        Assert.Equal(FallingWordGameStatus.Abandoned, session.Status);
        Assert.Equal(0, session.TimedOutCount);
        Assert.Equal(2, session.AbandonedCount); // Active and Queued rounds
    }

    [Fact]
    public void Accuracy_ComputedCorrectly_WithoutCountingAbandonedOrQueued()
    {
        var session = CreateSessionWithRounds("w1", "w2", "w3", "w4");
        // w1 correct
        var r1 = session.StartNextRound(_baseTime)!;
        session.SubmitAnswer(r1.Id, "w1", _baseTime.AddSeconds(1));

        // w2 incorrect
        var r2 = session.StartNextRound(_baseTime.AddSeconds(2))!;
        session.SubmitAnswer(r2.Id, "wrong", _baseTime.AddSeconds(3));

        // w3 timed out
        var r3 = session.StartNextRound(_baseTime.AddSeconds(4))!;
        session.FinalizeTimeout(r3.Id, _baseTime.AddSeconds(10));

        // w4 abandoned
        session.Abandon(_baseTime.AddSeconds(11));

        // Evaluated rounds: 1 correct, 1 incorrect, 1 timed out = 3 evaluated
        // Abandoned round does not count in denominator
        // Accuracy = 1 / 3 = 33.33%
        Assert.Equal(33.33m, session.Accuracy);
    }

    [Fact]
    public void SkipRound_WhenActive_MarksRoundIncorrectWithSkippedByLearner_AndResetsStreak()
    {
        var session = CreateSessionWithRounds("schedule", "calendar");
        var round = session.StartNextRound(_baseTime)!;

        // Skip the active round
        var skipped = session.SkipRound(round.Id, _baseTime.AddSeconds(2));

        Assert.Equal(FallingWordRoundStatus.Incorrect, skipped.Status);
        Assert.Equal("SkippedByLearner", skipped.FailureReason);
        Assert.Equal("[Bỏ qua]", skipped.SubmittedText);
        Assert.Equal(1, session.IncorrectCount);
        Assert.Equal(0, session.CurrentStreak);
        Assert.Equal(2000, skipped.ElapsedMs);
    }

    [Fact]
    public void MaskedPattern_And_LetterCount_GeneratedAccurately()
    {
        var round = new FallingWordGameRoundEntity(
            Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(),
            "ice cream", "kem", "A1");

        Assert.Equal("_ _ _   _ _ _ _ _", round.MaskedPattern);
        Assert.Equal(8, round.LetterCount); // 8 letters, space preserved
    }

    private static FallingWordGameSessionEntity CreateSessionWithRounds(params string[] words)
    {
        var session = FallingWordGameSessionEntity.Create(
            "test-user",
            FallingWordGameSource.VocabularyCatalog,
            null,
            FallingWordPlayFilter.All,
            timeLimitSeconds: 5,
            requestedCount: words.Length);

        for (int i = 0; i < words.Length; i++)
        {
            var round = new FallingWordGameRoundEntity(
                session.Id,
                i + 1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                words[i],
                $"Nghĩa của {words[i]}",
                "A1");
            session.AddRound(round);
        }

        return session;
    }
}
