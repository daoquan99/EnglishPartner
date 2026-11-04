using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LessonAttemptEndpoints
{
    public static IEndpointRouteBuilder MapLessonAttemptEndpoints(this IEndpointRouteBuilder app)
    {
        // Start Attempt on a Lesson: POST /api/lessons/{id}/attempts
        app.MapPost("/api/lessons/{id:guid}/attempts", async (
            Guid id,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            var attempt = await sender.Send(new StartLessonAttemptCommand(id, operationKey), ct);
            return Results.Created($"/api/lesson-attempts/{attempt.AttemptId}", attempt);
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        var group = app.MapGroup("/api/lesson-attempts").RequireAuthorization();

        group.MapPost("/{id:guid}/exercises/{exerciseId:guid}/synthesize", async (
            Guid id, Guid exerciseId, SynthesizeExerciseAudioRequest request, ISender sender, CancellationToken ct) =>
        {
            return Results.Ok(await sender.Send(new SynthesizeExerciseAudioCommand(id, exerciseId, request), ct));
        }).RequireRateLimiting("ai").RequirePermission("lesson-exercises.attempt");

        // List Learner's Attempts: GET /api/lesson-attempts
        group.MapGet("/", async (
            HttpContext httpContext,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedAttempts = await sender.Send(new ListLessonAttemptsQuery(
                Page: page,
                PageSize: pageSize), ct);

            return pagedAttempts.ToHttpResult(httpContext, paged);
        }).RequirePermission("lesson-exercises.attempt");

        // Resume / Get Attempt Details: GET /api/lesson-attempts/{id} and /api/lesson-attempts/{id}/resume
        group.MapGet("/{id:guid}", ResumeAttemptHandler).RequirePermission("lesson-exercises.attempt");
        group.MapGet("/{id:guid}/resume", ResumeAttemptHandler).RequirePermission("lesson-exercises.attempt");

        static async Task<IResult> ResumeAttemptHandler(
            Guid id,
            ISender sender,
            CancellationToken ct)
        {
            var attempt = await sender.Send(new ResumeLessonAttemptQuery(id), ct);
            return attempt is null
                ? ApiProblem.NotFound($"Lesson attempt with ID '{id}' was not found.")
                : Results.Ok(attempt);
        }

        // Submit Exercise Answer: POST /api/lesson-attempts/{id}/answers
        group.MapPost("/{id:guid}/answers", async (
            Guid id,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            SubmitExerciseAnswerRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new SubmitExerciseAnswerCommand(id, req, operationKey), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Concurrency or lifecycle conflict
                return ApiProblem.Conflict(ex.Message);
            }
        }).RequireRateLimiting("ai").RequirePermission("lesson-exercises.attempt");

        // Retry Answer Grading: POST /api/lesson-attempts/{id}/answers/{answerId}/retry-grading and /retry
        async Task<IResult> RetryGradingHandler(
            Guid id,
            Guid answerId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            [FromBody] RetryGradingRequest? bodyReq,
            ISender sender,
            CancellationToken ct)
        {
            int revision = bodyReq?.AttemptRevision ?? 1;
            var result = await sender.Send(new RetryAnswerGradingCommand(id, answerId, revision, operationKey, bodyReq?.AiConfig), ct);
            return Results.Ok(result);
        }

        group.MapPost("/{id:guid}/answers/{answerId:guid}/retry-grading", RetryGradingHandler).RequireRateLimiting("ai").RequirePermission("lesson-exercises.attempt");
        group.MapPost("/{id:guid}/answers/{answerId:guid}/retry", RetryGradingHandler).RequireRateLimiting("ai").RequirePermission("lesson-exercises.attempt");

        // Request Dictation AI Review: POST /api/lesson-attempts/{id}/answers/{answerId}/ai-review
        group.MapPost("/{id:guid}/answers/{answerId:guid}/ai-review", async (
            Guid id,
            Guid answerId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            [FromBody] RequestDictationAiReviewRequest? req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RequestDictationAiReviewCommand(id, answerId, req ?? new RequestDictationAiReviewRequest(), operationKey), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai").RequirePermission("lesson-exercises.attempt");

        // Complete Attempt: POST /api/lesson-attempts/{id}/complete
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            var completed = await sender.Send(new CompleteLessonAttemptCommand(id, operationKey), ct);
            return Results.Ok(completed);
        }).RequirePermission("lesson-exercises.attempt");

        return app;
    }
}
