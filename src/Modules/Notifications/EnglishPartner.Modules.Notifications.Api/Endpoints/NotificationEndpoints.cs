using EnglishPartner.Modules.Notifications.Application.Notifications.Commands.MarkAsRead;
using EnglishPartner.Modules.Notifications.Application.Notifications.Queries.GetMyNotifications;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace EnglishPartner.Modules.Notifications.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        // GET /api/v1/notifications
        group.MapGet("/", async (
            ISender sender,
            HttpContext context,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await sender.Send(new GetMyNotificationsQuery(userId, page, pageSize), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // PATCH /api/v1/notifications/{id}/read
        group.MapPatch("/{id:guid}/read", async (
            Guid id,
            ISender sender,
            HttpContext context,
            CancellationToken cancellationToken = default) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await sender.Send(new MarkNotificationAsReadCommand(id, userId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }
}
