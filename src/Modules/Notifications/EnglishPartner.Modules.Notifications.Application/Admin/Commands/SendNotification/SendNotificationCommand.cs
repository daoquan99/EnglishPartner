using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.SendNotification;

public sealed record SendNotificationCommand(
    Guid           UserId,
    string         Title,
    string         Message,
    NotificationType Type) : ICommand<Guid>;
