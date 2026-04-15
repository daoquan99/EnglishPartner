namespace EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;

public interface IItemMasteryRepository
{
    Task<ItemMastery?> GetAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<List<ItemMastery>> GetDueReviewsAsync(Guid userId, DateTime onOrBefore, CancellationToken cancellationToken = default);
    void Add(ItemMastery itemMastery);
    void Update(ItemMastery itemMastery);

    // Admin operations
    Task<(List<ItemMastery> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> DeleteByItemIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
}
