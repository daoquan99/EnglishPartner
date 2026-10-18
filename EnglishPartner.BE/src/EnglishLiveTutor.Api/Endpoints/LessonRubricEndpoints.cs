using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Features.Lessons.Commands.RubricCommands;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.RubricQueries;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LessonRubricEndpoints
{
    public static IEndpointRouteBuilder MapLessonRubricEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lesson-rubrics").RequireAuthorization();

        // 1. List Lesson Rubrics: GET /api/lesson-rubrics
        group.MapGet("/", async (
            HttpContext httpContext,
            string? exerciseType,
            string? cefrLevel,
            bool? onlyActive,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedRubrics = await sender.Send(new ListLessonRubricsQuery(
                ExerciseType: exerciseType,
                CefrLevel: cefrLevel,
                OnlyActive: onlyActive,
                Page: page,
                PageSize: pageSize), ct);

            return pagedRubrics.ToHttpResult(httpContext, paged);
        }).RequirePermission("lessons.read");

        // 2. Get Rubric Detail: GET /api/lesson-rubrics/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var rubric = await sender.Send(new GetLessonRubricByIdQuery(id), ct);
            return rubric is null ? Results.NotFound() : Results.Ok(rubric);
        }).RequirePermission("lessons.read");

        // 3. Create Rubric Template: POST /api/lesson-rubrics
        group.MapPost("/", async (
            CreateLessonRubricRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var created = await sender.Send(new CreateLessonRubricCommand(req), ct);
                return Results.Created($"/api/lesson-rubrics/{created.Id}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lesson-rubrics.manage");

        // 4. Activate Rubric: POST /api/lesson-rubrics/{id}/activate
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new ActivateLessonRubricCommand(id), ct);
                return Results.Ok(new { message = "Rubric activated successfully." });
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
        }).RequirePermission("lesson-rubrics.manage");

        // 5. Deactivate Rubric: POST /api/lesson-rubrics/{id}/deactivate
        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new DeactivateLessonRubricCommand(id), ct);
                return Results.Ok(new { message = "Rubric deactivated successfully." });
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
        }).RequirePermission("lesson-rubrics.manage");

        return app;
    }
}
