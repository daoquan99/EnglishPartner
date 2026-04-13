using EnglishPartner.Modules.Curriculum.Application.Topics.Commands.CreateTopic;
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
        });

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTopicByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateTopicCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/topics/{result.Value}", result.Value) : result.ToProblemDetails();
        });

        adminGroup.MapPut("{id:guid}", async (Guid id, UpdateTopicCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("ID in path does not match ID in body.");
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new EnglishPartner.Modules.Curriculum.Application.Topics.Commands.DeleteTopic.DeleteTopicCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }
}
