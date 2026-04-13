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
            .Where(x => x.UserId == userId && x.NextReviewDate <= onOrBefore.Date)
            .OrderBy(x => x.NextReviewDate)
            .ToListAsync(cancellationToken);

    public void Add(ItemMastery itemMastery) => dbContext.ItemMasteries.Add(itemMastery);
    public void Update(ItemMastery itemMastery) => dbContext.ItemMasteries.Update(itemMastery);
}
