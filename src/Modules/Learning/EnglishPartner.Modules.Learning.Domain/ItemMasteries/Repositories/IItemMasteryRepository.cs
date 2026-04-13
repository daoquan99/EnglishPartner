namespace EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;

public interface IItemMasteryRepository
{
    Task<ItemMastery?> GetAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<List<ItemMastery>> GetDueReviewsAsync(Guid userId, DateTime onOrBefore, CancellationToken cancellationToken = default);
    void Add(ItemMastery itemMastery);
    void Update(ItemMastery itemMastery);
}
