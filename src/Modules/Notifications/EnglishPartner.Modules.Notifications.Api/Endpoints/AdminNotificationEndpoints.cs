using EnglishPartner.Modules.Notifications.Application.Admin.Commands.DeleteAllUserNotifications;
using EnglishPartner.Modules.Notifications.Application.Admin.Commands.DeleteNotification;
using EnglishPartner.Modules.Notifications.Application.Admin.Commands.MarkAllAsRead;
using EnglishPartner.Modules.Notifications.Application.Admin.Commands.SendNotification;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Notifications.Api.Endpoints;

/// <summary>
/// Admin endpoints for managing notifications.
/// All require Admin role + notifications.admin permission.
/// </summary>
public static class AdminNotificationEndpoints
{
    public static void MapAdminNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/notifications")
            .RequireAuthorization("notifications.admin")
            .WithTags("Admin — Notifications");

        // GET /api/v1/admin/notifications
        group.MapGet("/", async (
            INotificationRepository repo,
            Guid?  userId   = null,
            bool?  isRead   = null,
            int    page     = 1,
            int    pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var (items, total) = await repo.GetAllPagedAsync(userId, isRead, page, pageSize, cancellationToken);
            return Results.Ok(new { items, totalCount = total, page, pageSize });
        });

        // GET /api/v1/admin/notifications/users/{userId}
        group.MapGet("/users/{userId:guid}", async (
            Guid userId,
            INotificationRepository repo,
            bool? isRead   = null,
            int   page     = 1,
            int   pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var (items, total) = await repo.GetAllPagedAsync(userId, isRead, page, pageSize, cancellationToken);
            return Results.Ok(new { items, totalCount = total, page, pageSize });
        });

        // POST /api/v1/admin/notifications/send
        group.MapPost("/send", async (
            SendRequest body,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<NotificationType>(body.Type, ignoreCase: true, out var type))
                return Results.BadRequest("Invalid notification type.");

            var result = await sender.Send(
                new SendNotificationCommand(body.UserId, body.Title, body.Message, type), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/notifications/{result.Value}", new { id = result.Value })
                : result.ToProblemDetails();
        });

        // DELETE /api/v1/admin/notifications/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteNotificationCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        // POST /api/v1/admin/notifications/users/{userId}/read-all
        group.MapPost("/users/{userId:guid}/read-all", async (
            Guid userId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new MarkAllAsReadCommand(userId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        // DELETE /api/v1/admin/notifications/users/{userId}/all
        group.MapDelete("/users/{userId:guid}/all", async (
            Guid userId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteAllUserNotificationsCommand(userId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }

    private sealed record SendRequest(Guid UserId, string Title, string Message, string Type);
}
