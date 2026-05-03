using EnglishPartner.Modules.Curriculum.Application.Topics.Commands.CreateTopic;
using EnglishPartner.Modules.Curriculum.Application.Topics.Commands.DeleteTopic;
using EnglishPartner.Modules.Curriculum.Application.Topics.Commands.UpdateTopic;
using EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopicById;
using EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopics;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class TopicEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/topics")
            .WithTags("Topics")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("api/v1/admin/curriculum/topics")
            .WithTags("Admin: Topics")
            .RequireAuthorization("Admin");

        // ── User Endpoints ──
        group.MapGet("", async (string languageCode, string? searchTerm, ISender sender) =>
        {
            var result = await sender.Send(new GetTopicsQuery(languageCode, searchTerm));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetTopics")
        .WithSummary("Get all topics for a language. Supports optional keyword search.");

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTopicByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetTopicById")
        .WithSummary("Get a topic by ID including its vocabulary and conversation list.");

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateTopicCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/curriculum/topics/{result.Value}", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("CreateTopic")
        .WithSummary("Create a new curriculum topic.")
        .RequireAuthorization("curriculum.topics.write");

        adminGroup.MapPut("{id:guid}", async (Guid id, UpdateTopicCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("Route ID does not match body ID.");
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateTopic")
        .WithSummary("Update an existing topic's title, description, or level.")
        .RequireAuthorization("curriculum.topics.write");

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteTopicCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteTopic")
        .WithSummary("Soft-delete a topic.")
        .RequireAuthorization("curriculum.topics.write");
    }
}
