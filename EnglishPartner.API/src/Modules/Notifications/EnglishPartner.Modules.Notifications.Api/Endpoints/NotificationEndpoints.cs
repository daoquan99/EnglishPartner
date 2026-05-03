using EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAsRead;
using EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAllMyAsRead;
using EnglishPartner.Modules.Notifications.Application.Notifications.Queries.GetMyNotifications;
using EnglishPartner.Modules.Notifications.Api.Hubs;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Notifications.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHub<NotificationHub>("/hubs/notifications");

        var group = app.MapGroup("api/v1/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        // GET /api/v1/notifications
        group.MapGet("/", async (
            ISender sender,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(new GetMyNotificationsQuery(page, pageSize), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }).WithName("GetMyNotifications").WithSummary("Get paginated notifications for the current user.");

        // PATCH /api/v1/notifications/{id}/read
        group.MapPatch("/{id:guid}/read", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        }).WithName("MarkNotificationAsRead").WithSummary("Mark a single notification as read.");

        // PATCH /api/v1/notifications/read-all
        group.MapPatch("/read-all", async (
            ISender sender,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(new MarkAllMyNotificationsAsReadCommand(), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        }).WithName("MarkAllMyNotificationsAsRead").WithSummary("Mark all notifications as read for the current user.");
    }
}
