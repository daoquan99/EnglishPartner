using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAsRead;

internal sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository repository) : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null || notification.UserId != request.UserId)
            return Result.Failure(CommonErrors.NotFound("Notification", request.NotificationId));

        notification.MarkAsRead();

        // SaveChangesAsync is handled by NotificationUnitOfWorkBehavior in the pipeline.
        return Result.Success();
    }
}
