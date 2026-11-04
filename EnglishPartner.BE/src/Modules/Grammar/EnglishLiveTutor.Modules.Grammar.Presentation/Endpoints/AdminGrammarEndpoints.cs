using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.ApproveGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.DraftGrammarTheoryWithAi;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.GenerateOfficialGrammarExercises;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.PromoteGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.RejectGrammarExercise;
using EnglishLiveTutor.Application.Features.Grammar.Commands.ImportGrammarExercisesJson;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Queries.Admin;
using EnglishLiveTutor.Application.Features.Grammar.Queries.Admin.ListGrammarModerationQueue;
using EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt;
using EnglishLiveTutor.Domain.Grammar.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLiveTutor.Api.Endpoints;

public static class AdminGrammarEndpoints
{
    public static IEndpointRouteBuilder MapAdminGrammarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/grammar").RequireAuthorization();

        // 1. List Steps: GET /api/admin/grammar/steps?level=&page=&pageSize=&paged=
        group.MapGet("/steps", async (
            [FromQuery] string? level,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] bool? paged,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListGrammarStepsAdminQuery(level, page, pageSize, paged), ct);
            return result.ToHttpResult(httpContext, paged);
        }).RequirePermission("grammar.manage");

        // 2. Get Step Detail: GET /api/admin/grammar/steps/{code}
        group.MapGet("/steps/{code}", async (string code, ISender sender, CancellationToken ct) =>
        {
            var step = await sender.Send(new GetGrammarStepAdminQuery(code), ct);
            return Results.Ok(step);
        }).RequirePermission("grammar.manage");

        // 3. Update Step: PUT /api/admin/grammar/steps/{code}
        group.MapPut("/steps/{code}", async (
            string code,
            [FromBody] UpdateGrammarStepRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateGrammarStepCommand(
                Code: code,
                TitleVi: request.TitleVi,
                TitleEn: request.TitleEn,
                SummaryVi: request.SummaryVi,
                IsPublished: request.IsPublished), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.manage");

        // 4. Update Theory: PUT /api/admin/grammar/steps/{code}/theory
        group.MapPut("/steps/{code}/theory", async (
            string code,
            [FromBody] UpdateGrammarStepTheoryRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new UpdateGrammarStepTheoryCommand(code, request.Theory), ct);
            return Results.NoContent();
        }).RequirePermission("grammar.manage");

        // 5. Create Official Exercise: POST /api/admin/grammar/steps/{code}/exercises
        group.MapPost("/steps/{code}/exercises", async (
            string code,
            [FromBody] CreateOfficialGrammarExerciseRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var exercise = await sender.Send(new CreateOfficialGrammarExerciseCommand(
                Code: code,
                Key: request.Key,
                Type: request.Type,
                Difficulty: request.Difficulty,
                Title: request.Title,
                Instructions: request.Instructions,
                MaxPoints: request.MaxPoints,
                GradingMode: request.GradingMode,
                Content: request.Content,
                Grading: request.Grading,
                IsCore: request.IsCore,
                Focus: request.Focus), ct);
            return Results.Created($"/api/admin/grammar/exercises/{exercise.Id}", exercise);
        }).RequirePermission("grammar.manage");

        // 6. Update Official Exercise: PUT /api/admin/grammar/exercises/{id} (also supported via /steps/{code}/exercises/{id})
        async Task<IResult> UpdateExerciseHandler(
            Guid id,
            [FromBody] UpdateOfficialGrammarExerciseRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var exercise = await sender.Send(new UpdateOfficialGrammarExerciseCommand(
                Id: id,
                Type: request.Type,
                Difficulty: request.Difficulty,
                Title: request.Title,
                Instructions: request.Instructions,
                MaxPoints: request.MaxPoints,
                GradingMode: request.GradingMode,
                Content: request.Content,
                Grading: request.Grading,
                IsCore: request.IsCore,
                Focus: request.Focus), ct);
            return Results.Ok(exercise);
        }

        group.MapPut("/exercises/{id:guid}", UpdateExerciseHandler).RequirePermission("grammar.manage");
        group.MapPut("/steps/{code}/exercises/{id:guid}", (string code, Guid id, [FromBody] UpdateOfficialGrammarExerciseRequest req, ISender sender, CancellationToken ct) =>
            UpdateExerciseHandler(id, req, sender, ct)).RequirePermission("grammar.manage");

        // 7. Delete Official Exercise: DELETE /api/admin/grammar/exercises/{id} (also supported via /steps/{code}/exercises/{id})
        async Task<IResult> DeleteExerciseHandler(
            Guid id,
            ISender sender,
            CancellationToken ct)
        {
            await sender.Send(new DeleteOfficialGrammarExerciseCommand(id), ct);
            return Results.NoContent();
        }

        group.MapDelete("/exercises/{id:guid}", DeleteExerciseHandler).RequirePermission("grammar.manage");
        group.MapDelete("/steps/{code}/exercises/{id:guid}", (string code, Guid id, ISender sender, CancellationToken ct) =>
            DeleteExerciseHandler(id, sender, ct)).RequirePermission("grammar.manage");

        // 8. Import Content Pack: POST /api/admin/grammar/content-pack/import
        group.MapPost("/content-pack/import", async (
            [FromBody] ImportGrammarContentPackRequest? request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ImportGrammarContentPackCommand(), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.manage");

        // --- G3 Admin: Moderation & AI ---

        // 9. List Moderation Queue: GET /api/admin/grammar/moderation
        group.MapGet("/moderation", async (
            [FromQuery] GrammarModerationStatus? status,
            [FromQuery] string? level,
            [FromQuery] string? stepCode,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] bool? paged,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListGrammarModerationQueueQuery(
                Status: status ?? GrammarModerationStatus.Pending,
                Level: level,
                StepCode: stepCode,
                Page: page,
                PageSize: pageSize,
                Paged: paged), ct);
            return result.ToHttpResult(httpContext, paged);
        }).RequirePermission("grammar.moderate");

        // 10. Approve Exercise: POST /api/admin/grammar/exercises/{id}/approve
        group.MapPost("/exercises/{id:guid}/approve", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ApproveGrammarExerciseCommand(id), ct);
            return Results.NoContent();
        }).RequirePermission("grammar.moderate");

        // 11. Reject Exercise: POST /api/admin/grammar/exercises/{id}/reject
        group.MapPost("/exercises/{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectGrammarExerciseRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new RejectGrammarExerciseCommand(id, request), ct);
            return Results.NoContent();
        }).RequirePermission("grammar.moderate");

        // 12. Promote Exercise: POST /api/admin/grammar/exercises/{id}/promote
        group.MapPost("/exercises/{id:guid}/promote", async (
            Guid id,
            [FromBody] PromoteGrammarExerciseRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new PromoteGrammarExerciseCommand(id, request), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.manage");

        // 13. Admin Generate Official Exercises: POST /api/admin/grammar/steps/{code}/exercises/generate
        group.MapPost("/steps/{code}/exercises/generate", async (
            string code,
            [FromBody] GenerateOfficialGrammarExercisesRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateOfficialGrammarExercisesCommand(code, request), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai").RequirePermission("grammar.manage");

        // 14. Admin Prompt: POST /api/admin/grammar/steps/{code}/exercises/prompt
        group.MapPost("/steps/{code}/exercises/prompt", async (
            string code,
            [FromBody] BuildGrammarExercisePromptRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new BuildGrammarExercisePromptQuery(code, request, ForAdmin: true), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.manage");

        // 15. Admin Import: POST /api/admin/grammar/steps/{code}/exercises/import
        group.MapPost("/steps/{code}/exercises/import", async (
            string code,
            [FromBody] ImportOfficialGrammarExercisesJsonRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateOfficialGrammarExercisesCommand(
                code,
                new GenerateOfficialGrammarExercisesRequest(request.ExpectedItems ?? [], request.IsCore),
                ImportJsonText: request.JsonText), ct);
            return Results.Ok(result);
        }).RequirePermission("grammar.manage");

        // 16. Draft Theory with AI: POST /api/admin/grammar/steps/{code}/theory/ai-draft
        group.MapPost("/steps/{code}/theory/ai-draft", async (
            string code,
            [FromBody] DraftGrammarTheoryWithAiRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DraftGrammarTheoryWithAiCommand(code, request), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai").RequirePermission("grammar.manage");

        return app;
    }
}
