using EnglishLiveTutor.Domain.VocabularyGames.Entities;

namespace EnglishLiveTutor.Domain.VocabularyGames.Repositories;

public interface IFallingWordGameRepository
{
    Task<FallingWordGameSessionEntity?> GetByIdWithRoundsAsync(Guid sessionId, CancellationToken ct = default);
    Task AddAsync(FallingWordGameSessionEntity session, CancellationToken ct = default);
    Task<List<FallingWordGameRoundEntity>> GetOverdueActiveRoundsAsync(DateTimeOffset now, int batchSize = 50, CancellationToken ct = default);
}
