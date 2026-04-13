using EnglishPartner.Modules.Notifications.Domain.Notifications;

namespace EnglishPartner.Modules.Notifications.Application.Abstractions;

public interface INotificationPusher
{
    Task PushAsync(Notification notification, CancellationToken cancellationToken = default);
}
