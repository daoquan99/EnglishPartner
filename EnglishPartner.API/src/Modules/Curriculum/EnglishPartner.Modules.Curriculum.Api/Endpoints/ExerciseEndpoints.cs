using EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.AddExerciseOption;
using EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.CreateExercise;
using EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.DeleteExercise;
using EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.SubmitExercise;
using EnglishPartner.Modules.Curriculum.Application.Exercises.Commands.UpdateExercise;
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
        group.MapGet("", async (ISender sender, string languageCode = "en", int page = 1, int pageSize = 20, Guid? topicId = null, Guid? vocabularyId = null, Guid? grammarId = null, string? level = null, int? exerciseType = null) =>
        {
            var result = await sender.Send(new GetExercisesQuery(languageCode, page, pageSize, topicId, vocabularyId, grammarId, level, exerciseType));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetExercises")
        .WithSummary("Get exercises filtered by language, topic, vocabulary, grammar, or type.");

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetExerciseByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetExerciseById")
        .WithSummary("Get a single exercise by ID including all answer options.");

        group.MapPost("{id:guid}/submit", async (Guid id, SubmitStandaloneExerciseRequest request, ISender sender) =>
        {
            var command = new SubmitExerciseCommand(id, request.IsCorrect);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("SubmitStandaloneExercise")
        .WithSummary("Submit a standalone exercise attempt (grants XP if correct).");

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateExerciseCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/curriculum/exercises/{result.Value}", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("CreateExercise")
        .WithSummary("Create a new exercise.")
        .RequireAuthorization("curriculum.exercise.write");

        adminGroup.MapPut("{id:guid}", async (Guid id, UpdateExerciseCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("Route ID does not match body ID.");
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateExercise")
        .WithSummary("Update an existing exercise.")
        .RequireAuthorization("curriculum.exercise.write");

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteExerciseCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteExercise")
        .WithSummary("Soft-delete an exercise.")
        .RequireAuthorization("curriculum.exercise.write");

        // BUG-3 pattern: ExerciseId from route, not body
        adminGroup.MapPost("{exerciseId:guid}/options",
            async (Guid exerciseId, AddExerciseOptionRequest request, ISender sender) =>
            {
                var command = new AddExerciseOptionCommand(
                    exerciseId,
                    request.Text,
                    request.IsCorrect);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
            })
        .WithName("AddExerciseOption")
        .WithSummary("Add an answer option to an exercise.")
        .RequireAuthorization("curriculum.exercise.write");
    }
}

/// <summary>Request body for adding an exercise option. ExerciseId comes from the route.</summary>
public sealed record AddExerciseOptionRequest(
    string Text,
    bool IsCorrect);

public sealed record SubmitStandaloneExerciseRequest(bool IsCorrect);
