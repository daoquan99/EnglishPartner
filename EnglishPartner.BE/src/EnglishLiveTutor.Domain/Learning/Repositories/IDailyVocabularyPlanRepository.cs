namespace EnglishLiveTutor.Domain.Learning.Repositories;

public interface IDailyVocabularyPlanRepository
{
    Task<DailyVocabularyPlanEntity?> GetByUserAndDateWithItemsAsync(string userId, string learningDate, CancellationToken ct = default);
    Task<DailyVocabularyPlanEntity?> GetByIdWithItemsAsync(Guid planId, CancellationToken ct = default);
    Task<DailyVocabularyPlanEntity?> GetByItemIdWithItemsAsync(Guid itemId, CancellationToken ct = default);
    Task<DailyVocabularyPlanEntity?> GetByUserAndSenseIdWithItemsAsync(string userId, Guid senseId, CancellationToken ct = default);
    Task AddAsync(DailyVocabularyPlanEntity plan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
