using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.DeleteAllUserNotifications;

internal sealed class DeleteAllUserNotificationsCommandHandler(
    INotificationRepository notificationRepository) : ICommandHandler<DeleteAllUserNotificationsCommand>
{
    public async Task<Result> Handle(DeleteAllUserNotificationsCommand request, CancellationToken cancellationToken)
    {
        await notificationRepository.DeleteAllByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success();
    }
}
