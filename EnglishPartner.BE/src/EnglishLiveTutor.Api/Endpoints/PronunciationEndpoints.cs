using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationAudio;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Pronunciation;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class PronunciationEndpoints
{
    public static IEndpointRouteBuilder MapPronunciationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pronunciation").RequireAuthorization();

        group.MapGet("/models", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var models = await sender.Send(new GetPronunciationModelsQuery(), ct);
            return Results.Ok(models);
        });

        // Synthesize Spoken Pronunciation Audio using Gemini 3.8 Live / Extended Thinking + 30 Teacher Voices + US/UK Accent
        group.MapPost("/synthesize", async (
            SynthesizePronunciationAudioRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new SynthesizePronunciationAudioCommand(req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Query Persisted Pronunciation Audio from Cache
        group.MapGet("/audio", async (
            string targetType,
            string? targetId,
            string? voiceName,
            double? speechSpeed,
            string? accent,
            string? modelCode,
            string? vocabularyId,
            string? senseId,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(targetId) || !Guid.TryParse(targetId, out var parsedTargetId) || parsedTargetId == Guid.Empty)
            {
                return Results.NotFound(new { error = "Không tìm thấy âm thanh phát âm đã lưu trong bộ nhớ đệm." });
            }

            Guid? parsedVocabId = Guid.TryParse(vocabularyId, out var vid) ? vid : null;
            Guid? parsedSenseId = Guid.TryParse(senseId, out var sid) ? sid : null;

            try
            {
                var result = await sender.Send(
                    new GetPronunciationAudioQuery(
                        targetType,
                        parsedTargetId,
                        voiceName,
                        speechSpeed,
                        accent,
                        modelCode,
                        parsedVocabId,
                        parsedSenseId),
                    ct);

                return result is null
                    ? Results.NotFound(new { error = "Không tìm thấy âm thanh phát âm đã lưu trong bộ nhớ đệm." })
                    : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // List all Scoring Rubrics for Admin CRUD (Database-level IQueryable Pagination)
        group.MapGet("/rubrics", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await sender.Send(new ListScoringRubricsQuery(search, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/rubrics/active", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var rubric = await sender.Send(new GetActiveScoringRubricQuery(), ct);
            return rubric is null ? Results.NotFound() : Results.Ok(rubric);
        });

        group.MapPost("/rubrics", async (
            UpdateScoringRubricRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var created = await sender.Send(new UpdateScoringRubricCommand(req), ct);
                return Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        group.MapPut("/rubrics/{code}", async (
            string code,
            UpdateScoringRubricRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await sender.Send(new UpdateScoringRubricCommand(code, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        group.MapDelete("/rubrics/{code}", async (
            string code,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteScoringRubricCommand(code), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("rubrics.manage");

        group.MapPost("/assessments", async (
            SubmitPronunciationAssessmentRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                string userId = currentUser.UserId!;
                var result = await sender.Send(new AssessPronunciationCommand(userId, req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/assessments/history", async (
            string? userId,
            Guid? vocabularyId,
            int? take,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            string effectiveUserId = !string.IsNullOrWhiteSpace(userId)
                ? userId.Trim()
                : (currentUser.UserId ?? "");

            if (string.IsNullOrWhiteSpace(effectiveUserId))
            {
                return Results.Unauthorized();
            }

            var history = await sender.Send(
                new GetPronunciationHistoryQuery(effectiveUserId, vocabularyId, take ?? 20),
                ct);
            return Results.Ok(history);
        });

        return app;
    }
}
