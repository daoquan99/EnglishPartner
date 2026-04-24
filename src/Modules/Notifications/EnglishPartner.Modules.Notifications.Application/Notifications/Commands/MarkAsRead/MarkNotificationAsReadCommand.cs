using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
