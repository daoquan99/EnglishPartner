namespace EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

public sealed record ActiveFallingWordRoundDto(
    Guid RoundId,
    Guid SessionId,
    int RoundOrder,
    int TotalRounds,
    Guid VocabularyId,
    Guid SenseId,
    string MeaningVi,
    string? CefrLevel,
    string MaskedPattern,
    int LetterCount,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset ServerNowUtc,
    int TimeLimitSeconds,
    double RemainingSeconds);

public sealed record TerminalFallingWordRoundDto(
    Guid RoundId,
    int RoundOrder,
    Guid VocabularyId,
    Guid SenseId,
    string MeaningVi,
    string? CefrLevel,
    string Status,
    string ExpectedWord,
    string? LearnerAnswer,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    long? ElapsedMs,
    string? FailureReason);

public sealed record SubmitFallingWordAnswerRequest(
    string? AnswerText);

public sealed record SubmitFallingWordAnswerResponse(
    TerminalFallingWordRoundDto RoundResult,
    bool IsSessionCompleted,
    int CorrectCount,
    int IncorrectCount,
    int TimedOutCount,
    int CurrentStreak,
    int BestStreak,
    decimal? Accuracy);
