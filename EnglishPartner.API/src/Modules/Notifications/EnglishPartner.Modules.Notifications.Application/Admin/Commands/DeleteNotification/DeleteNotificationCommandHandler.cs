using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.DeleteNotification;

internal sealed class DeleteNotificationCommandHandler(
    INotificationRepository notificationRepository) : ICommandHandler<DeleteNotificationCommand>
{
    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var deleted = await notificationRepository.DeleteByIdAsync(request.NotificationId, cancellationToken);
        if (deleted == 0)
            return Result.Failure(NotificationErrors.NotFound);

        return Result.Success();
    }
}
