using EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.CreateExercise;
using EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExerciseById;
using EnglishPartner.Modules.Curriculum.Application.Exercises.Queries.GetExercises;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class ExerciseEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/exercises")
            .WithTags("Exercises")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("api/v1/admin/curriculum/exercises")
            .WithTags("Admin: Exercises")
            .RequireAuthorization("Admin");

        // ── User Endpoints ──
        group.MapGet("", async (string languageCode, Guid? topicId, Guid? vocabularyId, Guid? grammarId, ISender sender) =>
        {
            var result = await sender.Send(new GetExercisesQuery(languageCode, topicId, vocabularyId, grammarId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetExerciseByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateExerciseCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/exercises/{result.Value}", result.Value) : result.ToProblemDetails();
        });

        adminGroup.MapPut("{id:guid}", async (Guid id, EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.UpdateExercise.UpdateExerciseCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.DeleteExercise.DeleteExerciseCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapPost("{id:guid}/options", async (Guid id, EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.AddExerciseOption.AddExerciseOptionCommand command, ISender sender) =>
        {
            if (id != command.ExerciseId) return Results.BadRequest();
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }
}
