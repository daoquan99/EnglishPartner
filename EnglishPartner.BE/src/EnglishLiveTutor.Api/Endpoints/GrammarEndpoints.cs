using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Features.Grammar.Commands;
using EnglishLiveTutor.Application.Features.Grammar.Commands.ChangeMyGrammarExerciseVisibility;
using EnglishLiveTutor.Application.Features.Grammar.Commands.DeleteMyGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.GenerateGrammarExercises;
using EnglishLiveTutor.Application.Features.Grammar.Commands.ImportGrammarExercisesJson;
using EnglishLiveTutor.Application.Features.Grammar.Commands.UpdateMyGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Queries;
using EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt;
using EnglishLiveTutor.Application.Features.Grammar.Queries.ListMyGrammarExercises;
using EnglishLiveTutor.Domain.Grammar.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLiveTutor.Api.Endpoints;

public static class GrammarEndpoints
{
    public static IEndpointRouteBuilder MapGrammarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/grammar").RequireAuthorization();

        // 1. Get Roadmap: GET /api/grammar/roadmap
        group.MapGet("/roadmap", async (ISender sender, CancellationToken ct) =>
        {
            var roadmap = await sender.Send(new GetGrammarRoadmapQuery(), ct);
            return Results.Ok(roadmap);
        }).RequirePermission("grammar.read");

        // 2. Get Step Detail: GET /api/grammar/steps/{code}
        group.MapGet("/steps/{code}", async (string code, ISender sender, CancellationToken ct) =>
        {
            var step = await sender.Send(new GetGrammarStepQuery(code), ct);
            return Results.Ok(step);
        }).RequirePermission("grammar.read");

        // 3. List Step Exercises: GET /api/grammar/steps/{code}/exercises?source=&page=&pageSize=
        group.MapGet("/steps/{code}/exercises", async (
            string code,
            [FromQuery] string? source,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] bool? paged,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var exercises = await sender.Send(new ListGrammarExercisesQuery(code, source, page, pageSize, paged), ct);
            return exercises.ToHttpResult(httpContext, paged);
        }).RequirePermission("grammar.read");

        // 4. Start Attempt: POST /api/grammar/attempts
        group.MapPost("/attempts", async (
            [FromBody] StartGrammarAttemptRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var attempt = await sender.Send(new StartGrammarAttemptCommand(request.Kind, request.StepCode, request.CefrLevel, request.ForceNew), ct);
            return Results.Ok(attempt);
        }).RequireRateLimiting("ai").RequirePermission("grammar.attempt");

        // 5. Get Attempt: GET /api/grammar/attempts/{id}
        group.MapGet("/attempts/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var attempt = await sender.Send(new GetGrammarAttemptQuery(id), ct);
            return Results.Ok(attempt);
        }).RequirePermission("grammar.attempt");

        // 6. Submit Answer: POST /api/grammar/attempts/{id}/answers
        group.MapPost("/attempts/{id:guid}/answers", async (
            Guid id,
            [FromBody] SubmitGrammarAnswerRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitGrammarAnswerCommand(id, request.ExerciseId, request.AnswerPayload, request.AiConfig), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai").RequirePermission("grammar.attempt");

        // 7. Complete Attempt: POST /api/grammar/attempts/{id}/complete
        group.MapPost("/attempts/{id:guid}/complete", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CompleteGrammarAttemptCommand(id), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai").RequirePermission("grammar.attempt");

        // --- G3: Learner Exercise Generation & My Exercises ---

        // 8. Generate Exercises: POST /api/grammar/steps/{code}/exercises/generate
        group.MapPost("/steps/{code}/exercises/generate", async (
            string code,
            [FromBody] GenerateGrammarExercisesRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateGrammarExercisesCommand(code, request), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai").RequirePermission("grammar.generate");

        // 9. Build Generation Prompt: POST /api/grammar/steps/{code}/exercises/prompt
        group.MapPost("/steps/{code}/exercises/prompt", async (
            string code,
            [FromBody] BuildGrammarExercisePromptRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new BuildGrammarExercisePromptQuery(code, request), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.generate");

        // 10. Import Exercises JSON: POST /api/grammar/steps/{code}/exercises/import
        group.MapPost("/steps/{code}/exercises/import", async (
            string code,
            [FromBody] ImportGrammarExercisesJsonRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ImportGrammarExercisesJsonCommand(code, request), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.generate");

        // 11. List My Exercises: GET /api/grammar/my-exercises
        group.MapGet("/my-exercises", async (
            [FromQuery] string? stepCode,
            [FromQuery] GrammarModerationStatus? moderation,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] bool? paged,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListMyGrammarExercisesQuery(stepCode, moderation, page, pageSize, paged), ct);
            return result.ToHttpResult(httpContext, paged);
        }).RequirePermission("grammar.generate");

        // 12. Update My Exercise: PUT /api/grammar/my-exercises/{id}
        group.MapPut("/my-exercises/{id:guid}", async (
            Guid id,
            [FromBody] UpdateMyGrammarExerciseRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateMyGrammarExerciseCommand(id, request), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.generate");

        // 13. Change Visibility: POST /api/grammar/my-exercises/{id}/visibility
        group.MapPost("/my-exercises/{id:guid}/visibility", async (
            Guid id,
            [FromBody] ChangeMyGrammarExerciseVisibilityRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangeMyGrammarExerciseVisibilityCommand(id, request), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.generate");

        // 14. Delete My Exercise: DELETE /api/grammar/my-exercises/{id}
        group.MapDelete("/my-exercises/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteMyGrammarExerciseCommand(id), ct);
            return Results.NoContent();
        }).RequirePermission("grammar.generate");

        return app;
    }
}
