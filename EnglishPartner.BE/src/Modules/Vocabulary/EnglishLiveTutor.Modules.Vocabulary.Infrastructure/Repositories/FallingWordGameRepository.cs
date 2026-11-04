using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class FallingWordGameRepository : IFallingWordGameRepository
{
    private readonly IVocabularyDbContext _db;

    public FallingWordGameRepository(IVocabularyDbContext db)
    {
        _db = db;
    }

    public Task<FallingWordGameSessionEntity?> GetByIdWithRoundsAsync(Guid sessionId, CancellationToken ct = default)
    {
        return _db.FallingWordGameSessions
            .Include(s => s.Rounds)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public Task<FallingWordGameSessionEntity?> GetByIdWithRoundsForUserAsync(Guid sessionId, string userId, CancellationToken ct = default)
    {
        return _db.FallingWordGameSessions
            .Include(s => s.Rounds)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
    }

    public async Task AddAsync(FallingWordGameSessionEntity session, CancellationToken ct = default)
    {
        await _db.FallingWordGameSessions.AddAsync(session, ct);
    }

    public Task<List<FallingWordGameRoundEntity>> GetOverdueActiveRoundsAsync(
        DateTimeOffset now,
        int batchSize = 50,
        CancellationToken ct = default)
    {
        return _db.FallingWordGameRounds
            .Where(r => r.Status == FallingWordRoundStatus.Active && r.ExpiresAtUtc.HasValue && r.ExpiresAtUtc.Value <= now)
            .OrderBy(r => r.ExpiresAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);
    }
}
