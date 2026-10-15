using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class UserVocabularyProgressRepository : IUserVocabularyProgressRepository
{
    private readonly AppDbContext _db;

    public UserVocabularyProgressRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserVocabularyProgressEntity?> GetByUserAndSenseAsync(string userId, Guid senseId, CancellationToken ct = default)
    {
        return await _db.UserVocabularyProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SenseId == senseId, ct);
    }

    public Task AddAsync(UserVocabularyProgressEntity progress, CancellationToken ct = default)
    {
        _db.UserVocabularyProgresses.Add(progress);
        return Task.CompletedTask;
    }

    public Task AddReviewAttemptAsync(VocabularyReviewAttemptEntity attempt, CancellationToken ct = default)
    {
        _db.VocabularyReviewAttempts.Add(attempt);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
