using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Queries.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler(INotificationRepository repository)
    : IQueryHandler<GetMyNotificationsQuery, GetMyNotificationsResponse>
{
    public async Task<Result<GetMyNotificationsResponse>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await repository.CountByUserIdAsync(request.UserId, cancellationToken);

        var items = await repository.GetByUserIdPagedAsync(request.UserId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(x => new NotificationDto(x.Id, x.Title, x.Message, x.Type, x.IsRead, x.CreatedAt)).ToList();

        return new GetMyNotificationsResponse(dtos, totalCount, request.Page, request.PageSize);
    }
}
