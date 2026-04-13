namespace EnglishPartner.Modules.Notifications.Application.Abstractions;

public interface INotificationUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
