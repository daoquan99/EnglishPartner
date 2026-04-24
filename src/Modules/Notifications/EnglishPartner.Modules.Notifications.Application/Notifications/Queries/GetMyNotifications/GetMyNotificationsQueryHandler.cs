using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Queries.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler(
    INotificationRepository repository,
    ICurrentUserService currentUser)
    : IQueryHandler<GetMyNotificationsQuery, GetMyNotificationsResponse>
{
    public async Task<Result<GetMyNotificationsResponse>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var totalCountTask  = repository.CountByUserIdAsync(userId, cancellationToken);
        var unreadCountTask = repository.CountUnreadByUserIdAsync(userId, cancellationToken);
        var itemsTask       = repository.GetByUserIdPagedAsync(userId, request.Page, request.PageSize, cancellationToken);

        await Task.WhenAll(totalCountTask, unreadCountTask, itemsTask);

        var dtos = itemsTask.Result.Select(x =>
            new NotificationDto(x.Id, x.Title, x.Message, x.Type, x.IsRead, x.CreatedAt)).ToList();

        return new GetMyNotificationsResponse(dtos, totalCountTask.Result, unreadCountTask.Result, request.Page, request.PageSize);
    }
}
