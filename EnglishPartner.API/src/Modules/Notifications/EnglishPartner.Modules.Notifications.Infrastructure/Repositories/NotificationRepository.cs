using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Notifications.Infrastructure.Repositories;

internal sealed class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<int> CountUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> BulkDeleteOldNotificationsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .Where(x => x.CreatedAt <= cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Add(Notification notification)
    {
        dbContext.Notifications.Add(notification);
    }

    // ── Admin methods ────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetAllPagedAsync(
        Guid? userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking().AsQueryable();

        if (userId.HasValue)    query = query.Where(x => x.UserId == userId.Value);
        if (isRead.HasValue)    query = query.Where(x => x.IsRead == isRead.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<int> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Notifications
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<int> MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

    public async Task<int> DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Notifications
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
}
