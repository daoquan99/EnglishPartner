namespace EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

public sealed record FallingWordPoolCountDto(
    int TotalValidCount,
    string Source,
    string? CefrLevel,
    string PlayFilter,
    int AllCount,
    int UnplayedCount,
    int PlayedCount,
    int CorrectCount,
    int FailedCount);
