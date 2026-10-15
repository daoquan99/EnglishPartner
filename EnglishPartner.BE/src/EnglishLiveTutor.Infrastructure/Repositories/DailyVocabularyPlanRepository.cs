using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class DailyVocabularyPlanRepository : IDailyVocabularyPlanRepository
{
    private readonly AppDbContext _db;

    public DailyVocabularyPlanRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DailyVocabularyPlanEntity?> GetByUserAndDateWithItemsAsync(string userId, string learningDate, CancellationToken ct = default)
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

    public async Task<DailyVocabularyPlanEntity?> GetByItemIdWithItemsAsync(Guid itemId, CancellationToken ct = default)
    {
        return await _db.DailyVocabularyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Items.Any(i => i.Id == itemId), ct);
    }

    public async Task<DailyVocabularyPlanEntity?> GetByUserAndSenseIdWithItemsAsync(string userId, Guid senseId, CancellationToken ct = default)
    {
        return await _db.DailyVocabularyPlans
            .Include(p => p.Items)
            .OrderByDescending(p => p.LearningDate)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Items.Any(i => i.SenseId == senseId), ct);
    }

    public Task AddAsync(DailyVocabularyPlanEntity plan, CancellationToken ct = default)
    {
        _db.DailyVocabularyPlans.Add(plan);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
