using EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.CreateExampleSentence;
using EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.UpdateExampleSentence;
using EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.DeleteExampleSentence;
using EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Queries.GetExampleSentences;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class ExampleSentenceEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/example-sentences")
            .WithTags("ExampleSentences")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("api/v1/admin/curriculum/example-sentences")
            .WithTags("Admin: ExampleSentences")
            .RequireAuthorization("Admin");

        // ── User Endpoints ──
        group.MapGet("", async (Guid? vocabularyId, Guid? grammarId, Guid? topicId, ISender sender) =>
        {
            var result = await sender.Send(new GetExampleSentencesQuery(vocabularyId, grammarId, topicId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateExampleSentenceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/example-sentences/{result.Value}", result.Value) : result.ToProblemDetails();
        });

        adminGroup.MapPut("{id:guid}", async (Guid id, UpdateExampleSentenceCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteExampleSentenceCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }
}
