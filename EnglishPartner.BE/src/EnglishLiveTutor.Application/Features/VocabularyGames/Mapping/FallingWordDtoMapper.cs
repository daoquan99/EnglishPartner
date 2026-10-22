using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;

public static class FallingWordDtoMapper
{
    public static FallingWordSessionDto ToSessionDto(FallingWordGameSessionEntity session, DateTimeOffset serverNow)
    {
        var rounds = session.Rounds.OrderBy(r => r.RoundOrder).ToList();
        var activeEntity = rounds.FirstOrDefault(r => r.Status == FallingWordRoundStatus.Active);

        ActiveFallingWordRoundDto? activeRoundDto = null;
        if (activeEntity != null)
        {
            activeRoundDto = ToActiveRoundDto(session, activeEntity, serverNow);
        }

        var completedRounds = rounds
            .Where(r => r.Status == FallingWordRoundStatus.Correct ||
                        r.Status == FallingWordRoundStatus.Incorrect ||
                        r.Status == FallingWordRoundStatus.TimedOut ||
                        r.Status == FallingWordRoundStatus.Abandoned)
            .Select(ToTerminalRoundDto)
            .ToList();

        int? currentOrder = activeEntity?.RoundOrder ??
            (session.Status == FallingWordGameStatus.Completed || session.Status == FallingWordGameStatus.Abandoned
                ? (int?)null
                : rounds.FirstOrDefault(r => r.Status == FallingWordRoundStatus.Queued)?.RoundOrder);

        return new FallingWordSessionDto(
            Id: session.Id,
            Source: session.Source.ToString(),
            CefrLevel: session.CefrLevel,
            PlayFilter: session.PlayFilter.ToString(),
            TimeLimitSeconds: session.TimeLimitSeconds,
            RequestedCount: session.RequestedCount,
            ActualCount: session.ActualCount,
            Shortfall: Math.Max(0, session.RequestedCount - session.ActualCount),
            Status: session.Status.ToString(),
            StartedAtUtc: session.StartedAtUtc,
            CompletedAtUtc: session.CompletedAtUtc,
            CorrectCount: session.CorrectCount,
            IncorrectCount: session.IncorrectCount,
            TimedOutCount: session.TimedOutCount,
            AbandonedCount: session.AbandonedCount,
            BestStreak: session.BestStreak,
            CurrentStreak: session.CurrentStreak,
            Accuracy: session.Accuracy,
            CurrentRoundOrder: currentOrder,
            TotalRounds: session.ActualCount,
            ActiveRound: activeRoundDto,
            CompletedRounds: completedRounds);
    }

    public static ActiveFallingWordRoundDto ToActiveRoundDto(
        FallingWordGameSessionEntity session,
        FallingWordGameRoundEntity round,
        DateTimeOffset serverNow)
    {
        double remainingSeconds = round.ExpiresAtUtc.HasValue
            ? Math.Max(0, (round.ExpiresAtUtc.Value - serverNow).TotalSeconds)
            : 0;

        return new ActiveFallingWordRoundDto(
            RoundId: round.Id,
            SessionId: session.Id,
            RoundOrder: round.RoundOrder,
            TotalRounds: session.ActualCount,
            VocabularyId: round.VocabularyId,
            SenseId: round.SenseId,
            MeaningVi: round.MeaningViSnapshot,
            CefrLevel: round.CefrLevelSnapshot,
            MaskedPattern: round.MaskedPattern,
            LetterCount: round.LetterCount,
            Status: round.Status.ToString(),
            StartedAtUtc: round.StartedAtUtc,
            ExpiresAtUtc: round.ExpiresAtUtc,
            ServerNowUtc: serverNow,
            TimeLimitSeconds: session.TimeLimitSeconds,
            RemainingSeconds: Math.Round(remainingSeconds, 2));
    }

    public static TerminalFallingWordRoundDto ToTerminalRoundDto(FallingWordGameRoundEntity round)
    {
        return new TerminalFallingWordRoundDto(
            RoundId: round.Id,
            RoundOrder: round.RoundOrder,
            VocabularyId: round.VocabularyId,
            SenseId: round.SenseId,
            MeaningVi: round.MeaningViSnapshot,
            CefrLevel: round.CefrLevelSnapshot,
            Status: round.Status.ToString(),
            ExpectedWord: round.TargetWordSnapshot, // Revealed only for terminal outcome
            LearnerAnswer: round.SubmittedText,
            StartedAtUtc: round.StartedAtUtc,
            ExpiresAtUtc: round.ExpiresAtUtc,
            SubmittedAtUtc: round.SubmittedAtUtc,
            ElapsedMs: round.ElapsedMs,
            FailureReason: round.FailureReason);
    }

    public static FallingWordSessionSummaryDto ToSummaryDto(FallingWordGameSessionEntity session)
    {
        return new FallingWordSessionSummaryDto(
            Id: session.Id,
            Source: session.Source.ToString(),
            CefrLevel: session.CefrLevel,
            PlayFilter: session.PlayFilter.ToString(),
            TimeLimitSeconds: session.TimeLimitSeconds,
            ActualCount: session.ActualCount,
            CorrectCount: session.CorrectCount,
            IncorrectCount: session.IncorrectCount,
            TimedOutCount: session.TimedOutCount,
            AbandonedCount: session.AbandonedCount,
            BestStreak: session.BestStreak,
            Accuracy: session.Accuracy,
            Status: session.Status.ToString(),
            CreatedAtUtc: session.CreatedAtUtc,
            StartedAtUtc: session.StartedAtUtc,
            CompletedAtUtc: session.CompletedAtUtc);
    }

    public static WordFallingHistoryItemDto ToHistoryItemDto(
        FallingWordGameRoundEntity round,
        string source,
        int timeLimitSeconds)
    {
        return new WordFallingHistoryItemDto(
            RoundId: round.Id,
            SessionId: round.SessionId,
            StartedAtUtc: round.StartedAtUtc ?? round.CreatedAtUtc,
            Outcome: round.Status.ToString(),
            LearnerAnswer: round.SubmittedText,
            ExpectedWord: round.TargetWordSnapshot,
            ElapsedMs: round.ElapsedMs,
            TimeLimitSeconds: timeLimitSeconds,
            Source: source);
    }
}
