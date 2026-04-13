using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(Guid UserId, int Page, int PageSize) : IQuery<GetMyNotificationsResponse>;

public sealed record GetMyNotificationsResponse(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt);
