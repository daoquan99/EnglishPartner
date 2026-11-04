namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public interface IPronunciationScoreReader
{
    Task<decimal?> GetLatestScoreAsync(string userId, Guid senseId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, decimal?>> GetLatestScoresAsync(
        string userId,
        IReadOnlyCollection<Guid> senseIds,
        CancellationToken ct = default);

    Task<PronunciationScoreSummary> GetSummaryAsync(string userId, CancellationToken ct = default);
}
