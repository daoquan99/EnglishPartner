using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.DeleteAllUserNotifications;

public sealed record DeleteAllUserNotificationsCommand(Guid UserId) : ICommand;
