using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;
using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Learning;

public sealed class DailyVocabularyPlanRepository : IDailyVocabularyPlanRepository
{
    private readonly AppDbContext _db;

    public DailyVocabularyPlanRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DailyVocabularyPlanEntity?> GetByUserAndDateWithItemsAsync(
        string userId,
        string learningDate,
        CancellationToken ct = default)
    {
        return await _db.DailyVocabularyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == learningDate, ct);
    }

    public async Task<DailyVocabularyPlanEntity?> GetByIdWithItemsAsync(Guid planId, CancellationToken ct = default)
    {
        return await _db.DailyVocabularyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);
    }

    public async Task<DailyVocabularyPlanEntity?> GetByItemIdWithItemsAsync(Guid dailyItemId, CancellationToken ct = default)
    {
        var item = await _db.DailyVocabularyItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == dailyItemId && !i.IsDeleted, ct);
        if (item is null)
        {
            return null;
        }

        return await _db.DailyVocabularyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == item.PlanId, ct);
    }

    public Task AddAsync(DailyVocabularyPlanEntity plan, CancellationToken ct = default)
    {
        _db.DailyVocabularyPlans.Add(plan);
        return Task.CompletedTask;
    }

    public Task AddItemAsync(DailyVocabularyItemEntity item, CancellationToken ct = default)
    {
        if (_db.Entry(item).State == EntityState.Detached)
        {
            _db.DailyVocabularyItems.Add(item);
        }
        return Task.CompletedTask;
    }
}
