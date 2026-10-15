using EnglishLiveTutor.Domain.Learning.ReviewAttempts;

namespace EnglishLiveTutor.Domain.Learning.UserVocabularyProgress;

public interface IUserVocabularyProgressRepository
{
    Task<UserVocabularyProgressEntity?> GetByUserAndSenseAsync(string userId, Guid senseId, CancellationToken ct = default);
    Task AddAsync(UserVocabularyProgressEntity progress, CancellationToken ct = default);
    Task AddReviewAttemptAsync(VocabularyReviewAttemptEntity attempt, CancellationToken ct = default);
}
