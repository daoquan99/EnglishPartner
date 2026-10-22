using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

public sealed record CreateFallingWordSessionRequest(
    string Source = "VocabularyCatalog",
    string? CefrLevel = null,
    string PlayFilter = "All",
    int TimeLimitSeconds = 5,
    int RequestedCount = 10,
    string? TimeZoneId = null);

public sealed record FallingWordSessionDto(
    Guid Id,
    string Source,
    string? CefrLevel,
    string PlayFilter,
    int TimeLimitSeconds,
    int RequestedCount,
    int ActualCount,
    int Shortfall,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int CorrectCount,
    int IncorrectCount,
    int TimedOutCount,
    int AbandonedCount,
    int BestStreak,
    int CurrentStreak,
    decimal? Accuracy,
    int? CurrentRoundOrder,
    int TotalRounds,
    ActiveFallingWordRoundDto? ActiveRound,
    List<TerminalFallingWordRoundDto> CompletedRounds);

public sealed record FallingWordSessionSummaryDto(
    Guid Id,
    string Source,
    string? CefrLevel,
    string PlayFilter,
    int TimeLimitSeconds,
    int ActualCount,
    int CorrectCount,
    int IncorrectCount,
    int TimedOutCount,
    int AbandonedCount,
    int BestStreak,
    decimal? Accuracy,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
