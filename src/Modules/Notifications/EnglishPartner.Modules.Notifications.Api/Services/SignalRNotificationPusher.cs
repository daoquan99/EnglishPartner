using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EnglishPartner.Modules.Notifications.Api.Services;

internal sealed class SignalRNotificationPusher(IHubContext<NotificationHub> hubContext) : INotificationPusher
{
    public async Task PushAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.IsRead,
            notification.CreatedAt
        };

        await hubContext.Clients.Group(notification.UserId.ToString())
            .SendAsync("ReceiveNotification", payload, cancellationToken);
    }
}
