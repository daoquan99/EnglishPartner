using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;

internal sealed class ItemMasteryRepository(LearningDbContext dbContext) : IItemMasteryRepository
{
    public async Task<ItemMastery?> GetAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
        => await dbContext.ItemMasteries.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId, cancellationToken);

    public async Task<List<ItemMastery>> GetDueReviewsAsync(Guid userId, DateTime onOrBefore, CancellationToken cancellationToken)
        => await dbContext.ItemMasteries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.NextReviewDate <= onOrBefore.Date)
            .OrderBy(x => x.NextReviewDate)
            .Take(200)  // Cap: prevents unbounded payload for power users
            .ToListAsync(cancellationToken);


    public void Add(ItemMastery itemMastery) => dbContext.ItemMasteries.Add(itemMastery);
    public void Update(ItemMastery itemMastery) => dbContext.ItemMasteries.Update(itemMastery);

    public async Task<(List<ItemMastery> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.ItemMasteries.Where(x => x.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.NextReviewDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<int> DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.ItemMasteries
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<int> DeleteByItemIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
        => await dbContext.ItemMasteries
            .Where(x => x.UserId == userId && x.ItemId == itemId)
            .ExecuteDeleteAsync(cancellationToken);
}
