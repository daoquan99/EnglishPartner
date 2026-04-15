using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.SendNotification;

internal sealed class SendNotificationCommandHandler(
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher) : ICommandHandler<SendNotificationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            request.UserId,
            request.Title,
            request.Message,
            request.Type);

        notificationRepository.Add(notification);

        // Push real-time if user is online
        await notificationPusher.PushAsync(notification, cancellationToken);

        return notification.Id;
    }
}
