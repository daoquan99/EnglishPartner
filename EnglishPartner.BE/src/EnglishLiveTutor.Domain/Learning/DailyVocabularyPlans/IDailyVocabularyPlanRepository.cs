using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans.Entities;

namespace EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;

public interface IDailyVocabularyPlanRepository
{
    Task<DailyVocabularyPlanEntity?> GetByUserAndDateWithItemsAsync(string userId, string learningDate, CancellationToken ct = default);
    Task<DailyVocabularyPlanEntity?> GetByIdWithItemsAsync(Guid planId, CancellationToken ct = default);
    Task<DailyVocabularyPlanEntity?> GetByItemIdWithItemsAsync(Guid dailyItemId, CancellationToken ct = default);
    Task AddAsync(DailyVocabularyPlanEntity plan, CancellationToken ct = default);
    Task AddItemAsync(DailyVocabularyItemEntity item, CancellationToken ct = default);
}
