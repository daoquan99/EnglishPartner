using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Notifications.Application.Admin.Queries.GetAllNotifications;

public sealed record GetAllNotificationsQuery(
    Guid? UserId,
    bool? IsRead,
    int Page,
    int PageSize) : IQuery<GetAllNotificationsResponse>;

public sealed record GetAllNotificationsResponse(
    IReadOnlyList<AdminNotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminNotificationDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt);
