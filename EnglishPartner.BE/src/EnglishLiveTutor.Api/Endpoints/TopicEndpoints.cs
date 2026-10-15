using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Topics.Commands.DeleteTopic;
using EnglishLiveTutor.Application.Features.Topics.Commands.UpsertTopic;
using EnglishLiveTutor.Application.Features.Topics.Queries.GetTopicById;
using EnglishLiveTutor.Application.Features.Topics.Queries.ListTopics;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class TopicEndpoints
{
    public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        group.MapGet("/", async (
            HttpContext httpContext,
            bool? onlyActive,
            bool? supportsConversation,
            bool? supportsVocabulary,
            string? modeCode,
            string? category,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedTopics = await sender.Send(
                new ListTopicsQuery(
                    OnlyActive: onlyActive ?? true,
                    SupportsConversation: supportsConversation,
                    SupportsVocabulary: supportsVocabulary,
                    ModeCode: modeCode,
                    Category: category,
                    Search: search,
                    Page: page,
                    PageSize: pageSize),
                ct);
            return pagedTopics.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var topic = await sender.Send(new GetTopicByIdQuery(id), ct);
            return topic is null ? Results.NotFound() : Results.Ok(topic);
        });

        group.MapPost("/", async (
            UpsertTopicRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var saved = await sender.Send(new UpsertTopicCommand(null, req), ct);
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
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var saved = await sender.Send(new UpsertTopicCommand(id, req), ct);
                return Results.Ok(saved);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("topics.manage");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteTopicCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("topics.manage");

        return app;
    }
}
