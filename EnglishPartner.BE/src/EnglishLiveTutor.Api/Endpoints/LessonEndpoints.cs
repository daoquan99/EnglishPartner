using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Features.Lessons.Commands.DraftOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.ExerciseOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.ImportOperations;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonById;
using EnglishLiveTutor.Application.Features.Lessons.Queries.GetLessonExerciseTypes;
using EnglishLiveTutor.Application.Features.Lessons.Queries.ListLessons;
using EnglishLiveTutor.Domain.Lessons.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Exercise Types Catalog (16 canonical types)
        app.MapGet("/api/lesson-exercise-types", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var catalog = await sender.Send(new GetLessonExerciseTypesQuery(), ct);
            return Results.Ok(catalog);
        }).RequireAuthorization().RequirePermission("lessons.read");

        var group = app.MapGroup("/api/lessons").RequireAuthorization();

        // 2. List Lessons (PagedResultDto, filtering)
        group.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            string? cefrLevel,
            string? topic,
            string? status,
            bool? isLearner,
            int? page,
            int? pageNumber,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            bool isAuthor = IsLessonAuthorOrAdmin(httpContext.User);
            bool effectiveIsLearner = isAuthor ? (isLearner ?? false) : true;

            LessonStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LessonStatus>(status, ignoreCase: true, out var st))
                parsedStatus = st;

            var pagedLessons = await sender.Send(new ListLessonsQuery(
                Search: search,
                CefrLevel: cefrLevel,
                Topic: topic,
                Status: parsedStatus,
                IsLearner: effectiveIsLearner,
                Page: page ?? pageNumber,
                PageSize: pageSize), ct);

            return pagedLessons.ToHttpResult(httpContext, paged);
        }).RequirePermission("lessons.read");

        // 3. Create Draft Lesson
        group.MapPost("/", async (
            CreateLessonDraftRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var created = await sender.Send(new CreateLessonDraftCommand(req), ct);
                return Results.Created($"/api/lessons/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.manage");

        // 4. Get Lesson By ID (Author DTO for Admin/Teacher; sanitized Learner DTO for Learner)
        group.MapGet("/{id:guid}", async (
            Guid id,
            bool? isLearner,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            bool isAuthor = IsLessonAuthorOrAdmin(httpContext.User);
            bool effectiveIsLearner = isAuthor ? (isLearner ?? false) : true;
            var lesson = await sender.Send(new GetLessonByIdQuery(id, IsLearner: effectiveIsLearner), ct);
            return lesson is null ? Results.NotFound() : Results.Ok(lesson);
        }).RequirePermission("lessons.read");

        // 5. Update Draft Lesson
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            UpdateLessonDraftRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await sender.Send(new UpdateLessonDraftCommand(id, req, ifMatch), ct);
                return Results.Ok(updated);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.manage");

        // 6. Delete Draft Lesson
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new DeleteLessonDraftCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.manage");

        // 7. Create Draft Revision from Lesson (supports both /revision and /revisions)
        group.MapPost("/{id:guid}/revision", HandleCreateRevision).RequirePermission("lessons.manage");
        group.MapPost("/{id:guid}/revisions", HandleCreateRevision).RequirePermission("lessons.manage");

        static async Task<IResult> HandleCreateRevision(
            Guid id,
            ISender sender,
            CancellationToken ct)
        {
            try
            {
                var revision = await sender.Send(new CreateDraftRevisionCommand(id), ct);
                return Results.Created($"/api/lessons/{revision.DraftLessonId}", revision);
            }
            catch (NotFoundException)
            {
                return Results.NotFound(new { error = $"Lesson with ID '{id}' was not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        // 8. Publish Lesson
        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            [FromBody] PublishLessonRequest? bodyReq,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var req = bodyReq ?? new PublishLessonRequest(null);
                var published = await sender.Send(new PublishLessonCommand(id, req, ifMatch), ct);
                return Results.Ok(published);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.publish");

        // 9. Archive Lesson
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new ArchiveLessonCommand(id), ct);
                return Results.Ok(new { message = "Lesson archived successfully." });
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
        }).RequirePermission("lessons.publish");

        // 9b. Revert to Draft (Unpublish)
        async Task<IResult> RevertToDraftHandler(
            Guid id,
            ISender sender,
            CancellationToken ct)
        {
            try
            {
                var reverted = await sender.Send(new RevertLessonToDraftCommand(id), ct);
                return Results.Ok(reverted);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        group.MapPost("/{id:guid}/unpublish", RevertToDraftHandler).RequirePermission("lessons.publish");
        group.MapPost("/{id:guid}/revert-to-draft", RevertToDraftHandler).RequirePermission("lessons.publish");

        // 10. Exercise Sub-routes
        group.MapPost("/{id:guid}/exercises", async (
            Guid id,
            AddExerciseRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var lesson = await sender.Send(new AddExerciseToDraftCommand(id, req), ct);
                return Results.Ok(lesson);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.manage");

        group.MapPut("/{id:guid}/exercises/{exerciseId:guid}", async (
            Guid id,
            Guid exerciseId,
            UpdateExerciseRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var lesson = await sender.Send(new UpdateDraftExerciseCommand(id, exerciseId, req), ct);
                return Results.Ok(lesson);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.manage");

        group.MapDelete("/{id:guid}/exercises/{exerciseId:guid}", async (
            Guid id,
            Guid exerciseId,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var lesson = await sender.Send(new RemoveDraftExerciseCommand(id, exerciseId), ct);
                return Results.Ok(lesson);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.manage");

        async Task<IResult> ReorderExercisesHandler(
            Guid id,
            ReorderExercisesRequest req,
            ISender sender,
            CancellationToken ct)
        {
            try
            {
                var lesson = await sender.Send(new ReorderDraftExercisesCommand(id, req), ct);
                return Results.Ok(lesson);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        group.MapPut("/{id:guid}/exercises/order", ReorderExercisesHandler).RequirePermission("lessons.manage");
        group.MapPut("/{id:guid}/exercises/reorder", ReorderExercisesHandler).RequirePermission("lessons.manage");

        // 11. Generation & Import Sub-routes
        group.MapPost("/generation/prompt", async (
            BuildLessonPromptCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var prompt = await sender.Send(command, ct);
            return Results.Ok(prompt);
        }).RequirePermission("lessons.generate");

        group.MapPost("/generation/generate", async (
            GenerateLessonExercisesCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(command, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("lessons.generate");

        async Task<IResult> ValidateCandidateHandler(
            HttpRequest request,
            ISender sender,
            CancellationToken ct)
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(body))
            {
                return Results.BadRequest(new { error = "Request body cannot be empty." });
            }

            string jsonToValidate = body;
            try
            {
                var parsed = JsonNode.Parse(body);
                if (parsed is JsonObject obj)
                {
                    if (obj.TryGetPropertyValue("rawJson", out var rawNode) && rawNode?.GetValueKind() == JsonValueKind.String)
                    {
                        jsonToValidate = rawNode.GetValue<string>() ?? body;
                    }
                    else if (obj.TryGetPropertyValue("candidate", out var candNode) && candNode is JsonObject)
                    {
                        jsonToValidate = candNode.ToJsonString();
                    }
                }
            }
            catch
            {
                // Let PreviewLessonImportCommand return syntax error if invalid
            }

            var preview = await sender.Send(new PreviewLessonImportCommand(jsonToValidate), ct);
            return Results.Ok(preview);
        }

        group.MapPost("/import/preview", ValidateCandidateHandler).RequirePermission("lessons.manage");
        group.MapPost("/validate-candidate", ValidateCandidateHandler).RequirePermission("lessons.manage");

        async Task<IResult> CommitImportHandler(
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            CommitLessonImportRequest req,
            ISender sender,
            CancellationToken ct)
        {
            try
            {
                var committed = await sender.Send(new CommitLessonImportCommand(req, operationKey), ct);
                return Results.Ok(committed);
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        group.MapPost("/import/commit", CommitImportHandler).RequirePermission("lessons.manage");
        group.MapPost("/commit-import", CommitImportHandler).RequirePermission("lessons.manage");

        return app;
    }

    private static bool IsLessonAuthorOrAdmin(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (user.IsInRole("ADMIN") || user.IsInRole("Admin") ||
            user.IsInRole("TEACHER") || user.IsInRole("Teacher"))
        {
            return true;
        }

        bool hasRole = user.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
            (string.Equals(c.Value, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "TEACHER", StringComparison.OrdinalIgnoreCase)));

        if (hasRole)
            return true;

        return user.Claims.Any(c =>
            c.Type == "permission" &&
            string.Equals(c.Value, "lessons.manage", StringComparison.OrdinalIgnoreCase));
    }
}
