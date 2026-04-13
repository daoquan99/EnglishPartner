using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAsRead;

internal sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository repository,
    INotificationUnitOfWork unitOfWork) : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null || notification.UserId != request.UserId)
        {
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."));
        }

        notification.MarkAsRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
