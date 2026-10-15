using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;
using EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;
using EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;
using EnglishLiveTutor.Domain.Conversations;

namespace EnglishLiveTutor.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conversations");

        group.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListConversationsQuery, PagedResultDto<object>> handler,
            CancellationToken ct) =>
        {
            var pagedConversations = await handler.HandleAsync(new ListConversationsQuery(search, page, pageSize), ct);
            return pagedConversations.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{id}", async (
            string id,
            IQueryHandler<GetConversationHistoryQuery, object?> handler,
            CancellationToken ct) =>
        {
            var conv = await handler.HandleAsync(new GetConversationHistoryQuery(id), ct);
            return conv is null ? Results.NotFound() : Results.Ok(conv);
        });

        group.MapPost("/", async (
            CreateConversationRequest req,
            ICommandHandler<CreateConversationCommand, ConversationEntity> handler,
            CancellationToken ct) =>
        {
            var conv = await handler.HandleAsync(new CreateConversationCommand(req), ct);
            return Results.Ok(conv);
        });

        group.MapPut("/{id}/settings", async (
            string id,
            UpdateConversationSettingsRequest req,
            ICommandHandler<UpdateConversationSettingsCommand, ConversationEntity> handler,
            CancellationToken ct) =>
        {
            var conv = await handler.HandleAsync(new UpdateConversationSettingsCommand(id, req), ct);
            return Results.Ok(conv);
        });

        group.MapDelete("/{id}", async (
            string id,
            ICommandHandler<DeleteConversationCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteConversationCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
