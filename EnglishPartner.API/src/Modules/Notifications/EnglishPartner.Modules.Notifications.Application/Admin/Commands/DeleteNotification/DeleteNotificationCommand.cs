using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand;
