using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.MarkAllAsRead;

public sealed record MarkAllAsReadCommand(Guid UserId) : ICommand;
