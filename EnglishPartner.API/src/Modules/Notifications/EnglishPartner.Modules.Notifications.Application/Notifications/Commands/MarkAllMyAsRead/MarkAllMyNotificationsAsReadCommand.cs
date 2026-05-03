using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.Modules.Notifications.Application.Abstractions;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAllMyAsRead;

public sealed record MarkAllMyNotificationsAsReadCommand : ICommand;

internal sealed class MarkAllMyNotificationsAsReadCommandHandler(
    INotificationRepository repository,
    ICurrentUserService currentUser) : ICommandHandler<MarkAllMyNotificationsAsReadCommand>
{
    public async Task<Result> Handle(MarkAllMyNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        await repository.MarkAllAsReadByUserIdAsync(userId, cancellationToken);
        return Result.Success();
    }
}
