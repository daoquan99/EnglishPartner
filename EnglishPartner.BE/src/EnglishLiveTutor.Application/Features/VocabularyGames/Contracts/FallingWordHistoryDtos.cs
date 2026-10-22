namespace EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

public sealed record WordFallingHistoryItemDto(
    Guid RoundId,
    Guid SessionId,
    DateTimeOffset StartedAtUtc,
    string Outcome,
    string? LearnerAnswer,
    string ExpectedWord,
    long? ElapsedMs,
    int TimeLimitSeconds,
    string Source);
