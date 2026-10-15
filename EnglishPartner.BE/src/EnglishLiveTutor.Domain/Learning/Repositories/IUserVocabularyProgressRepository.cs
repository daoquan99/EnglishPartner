namespace EnglishLiveTutor.Domain.Learning.Repositories;

public interface IUserVocabularyProgressRepository
{
    Task<UserVocabularyProgressEntity?> GetByUserAndSenseAsync(string userId, Guid senseId, CancellationToken ct = default);
    Task AddAsync(UserVocabularyProgressEntity progress, CancellationToken ct = default);
    Task AddReviewAttemptAsync(VocabularyReviewAttemptEntity attempt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
