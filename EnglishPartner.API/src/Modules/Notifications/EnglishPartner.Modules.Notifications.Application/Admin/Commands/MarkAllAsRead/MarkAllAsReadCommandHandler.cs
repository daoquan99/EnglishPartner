using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Commands.MarkAllAsRead;

internal sealed class MarkAllAsReadCommandHandler(
    INotificationRepository notificationRepository) : ICommandHandler<MarkAllAsReadCommand>
{
    public async Task<Result> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        await notificationRepository.MarkAllAsReadByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success();
    }
}
