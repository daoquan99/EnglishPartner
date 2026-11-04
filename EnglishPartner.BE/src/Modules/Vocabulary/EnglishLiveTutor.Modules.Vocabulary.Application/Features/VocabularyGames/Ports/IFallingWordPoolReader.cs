using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Ports;

public sealed record CandidatePoolItem(
    Guid VocabularyId,
    Guid SenseId,
    Guid? DailyItemId,
    string Word,
    string NormalizedWord,
    string MeaningVi,
    string? CefrLevel,
    FallingWordPlayFilter HistoryOutcome);

public interface IFallingWordPoolReader
{
    Task<FallingWordPoolCountDto> GetPoolCountAsync(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        FallingWordPlayFilter playFilter,
        string? timeZoneId,
        CancellationToken ct = default);

    Task<(List<CandidatePoolItem> Selected, int TotalPoolCount, Guid? DailyPlanId, string? LearningDate)> SelectPoolForSessionAsync(
        string userId,
        FallingWordGameSource source,
        string? cefrLevel,
        FallingWordPlayFilter playFilter,
        int requestedCount,
        string? timeZoneId,
        CancellationToken ct = default);
}
