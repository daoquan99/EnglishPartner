using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Queries.GetAllNotifications;

internal sealed class GetAllNotificationsQueryHandler(
    INotificationRepository repository)
    : IQueryHandler<GetAllNotificationsQuery, GetAllNotificationsResponse>
{
    public async Task<Result<GetAllNotificationsResponse>> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetAllPagedAsync(
            request.UserId, request.IsRead, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(x => new AdminNotificationDto(
            x.Id, x.UserId, x.Title, x.Message, x.Type, x.IsRead, x.CreatedAt)).ToList();

        return new GetAllNotificationsResponse(dtos, total, request.Page, request.PageSize);
    }
}
