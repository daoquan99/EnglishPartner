using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;
using EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;
using EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;
using EnglishLiveTutor.Domain.Conversations;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conversations").RequireAuthorization();

        group.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedConversations = await sender.Send(new ListConversationsQuery(search, page, pageSize), ct);
            return pagedConversations.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            var conv = await sender.Send(new GetConversationHistoryQuery(id), ct);
            return conv is null ? Results.NotFound() : Results.Ok(conv);
        });

        group.MapPost("/", async (
            CreateConversationRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var conv = await sender.Send(new CreateConversationCommand(req), ct);
            return Results.Ok(conv);
        });

        group.MapPut("/{id}/settings", async (
            string id,
            UpdateConversationSettingsRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var conv = await sender.Send(new UpdateConversationSettingsCommand(id, req), ct);
            return Results.Ok(conv);
        });

        group.MapDelete("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteConversationCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
