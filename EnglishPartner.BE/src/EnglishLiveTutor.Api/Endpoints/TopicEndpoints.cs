using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;
using EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;
using EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;

namespace EnglishLiveTutor.Api.Endpoints;

public static class TopicEndpoints
{
    public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics");

        group.MapGet("/", async (
            bool? onlyActive,
            bool? supportsConversation,
            bool? supportsVocabulary,
            string? modeCode,
            string? category,
            IQueryHandler<ListTopicsQuery, List<TopicDto>> handler,
            CancellationToken ct) =>
        {
            var list = await handler.HandleAsync(
                new ListTopicsQuery(
                    OnlyActive: onlyActive ?? true,
                    SupportsConversation: supportsConversation,
                    SupportsVocabulary: supportsVocabulary,
                    ModeCode: modeCode,
                    Category: category),
                ct);
            return Results.Ok(list);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetTopicByIdQuery, TopicDto?> handler,
            CancellationToken ct) =>
        {
            var topic = await handler.HandleAsync(new GetTopicByIdQuery(id), ct);
            return topic is null ? Results.NotFound() : Results.Ok(topic);
        });

        group.MapPost("/", async (
            UpsertTopicRequest req,
            ICommandHandler<UpsertTopicCommand, TopicDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var saved = await handler.HandleAsync(new UpsertTopicCommand(null, req), ct);
                return Results.Ok(saved);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("topics.manage");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpsertTopicRequest req,
            ICommandHandler<UpsertTopicCommand, TopicDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var saved = await handler.HandleAsync(new UpsertTopicCommand(id, req), ct);
                return Results.Ok(saved);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("topics.manage");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteTopicCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteTopicCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("topics.manage");

        return app;
    }
}
