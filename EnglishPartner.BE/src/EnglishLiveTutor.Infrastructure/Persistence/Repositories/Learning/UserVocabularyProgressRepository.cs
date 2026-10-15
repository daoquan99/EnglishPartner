using EnglishLiveTutor.Domain.Learning.ReviewAttempts;
using EnglishLiveTutor.Domain.Learning.UserVocabularyProgress;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Learning;

public sealed class UserVocabularyProgressRepository : IUserVocabularyProgressRepository
{
    private readonly AppDbContext _db;

    public UserVocabularyProgressRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserVocabularyProgressEntity?> GetByUserAndSenseAsync(
        string userId,
        Guid senseId,
        CancellationToken ct = default)
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
}
