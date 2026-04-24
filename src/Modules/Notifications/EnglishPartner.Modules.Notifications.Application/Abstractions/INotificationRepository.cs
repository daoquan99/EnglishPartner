using EnglishPartner.Modules.Notifications.Domain.Notifications;

namespace EnglishPartner.Modules.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> BulkDeleteOldNotificationsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    void Add(Notification notification);

    // Admin operations
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetAllPagedAsync(
        Guid? userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
