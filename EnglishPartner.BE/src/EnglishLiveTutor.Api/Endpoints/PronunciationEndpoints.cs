using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;
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

namespace EnglishLiveTutor.Api.Endpoints;

public static class PronunciationEndpoints
{
    public static IEndpointRouteBuilder MapPronunciationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pronunciation");

        group.MapGet("/models", async (
            IQueryHandler<GetPronunciationModelsQuery, List<AiModelConfigEntity>> handler,
            CancellationToken ct) =>
        {
            var models = await handler.HandleAsync(new GetPronunciationModelsQuery(), ct);
            return Results.Ok(models);
        });

        // Synthesize Spoken Pronunciation Audio using Gemini 3.8 Live / Extended Thinking + 30 Teacher Voices + US/UK Accent
        group.MapPost("/synthesize", async (
            SynthesizePronunciationAudioRequest req,
            ICommandHandler<SynthesizePronunciationAudioCommand, SynthesizePronunciationAudioResponseDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new SynthesizePronunciationAudioCommand(req), ct);
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
            IQueryHandler<GetPronunciationAudioQuery, SynthesizePronunciationAudioResponseDto?> handler,
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
                var result = await handler.HandleAsync(
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
            IQueryHandler<ListScoringRubricsQuery, PagedResultDto<ScoringRubricEntity>> handler,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await handler.HandleAsync(new ListScoringRubricsQuery(search, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/rubrics/active", async (
            IQueryHandler<GetActiveScoringRubricQuery, ScoringRubricEntity?> handler,
            CancellationToken ct) =>
        {
            var rubric = await handler.HandleAsync(new GetActiveScoringRubricQuery(), ct);
            return rubric is null ? Results.NotFound() : Results.Ok(rubric);
        });

        group.MapPost("/rubrics", async (
            UpdateScoringRubricRequest req,
            ICommandHandler<UpdateScoringRubricCommand, ScoringRubricEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var created = await handler.HandleAsync(new UpdateScoringRubricCommand(req), ct);
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
            ICommandHandler<UpdateScoringRubricCommand, ScoringRubricEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new UpdateScoringRubricCommand(code, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        group.MapDelete("/rubrics/{code}", async (
            string code,
            ICommandHandler<DeleteScoringRubricCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteScoringRubricCommand(code), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("rubrics.manage");

        group.MapPost("/assessments", async (
            SubmitPronunciationAssessmentRequest req,
            ICurrentUser currentUser,
            ICommandHandler<AssessPronunciationCommand, PronunciationAssessmentResultDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                string userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "default-learner" : currentUser.UserId!;
                var result = await handler.HandleAsync(new AssessPronunciationCommand(userId, req), ct);
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
            IQueryHandler<GetPronunciationHistoryQuery, List<PronunciationAssessmentResultDto>> handler,
            CancellationToken ct) =>
        {
            var history = await handler.HandleAsync(
                new GetPronunciationHistoryQuery(userId ?? "default-learner", vocabularyId, take ?? 20),
                ct);
            return Results.Ok(history);
        });

        // --- Admin AI Models Catalog CRUD (/api/ai-models) ---
        var modelsGroup = app.MapGroup("/api/ai-models");

        modelsGroup.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListAiModelConfigsQuery, PagedResultDto<AiModelConfigEntity>> handler,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await handler.HandleAsync(new ListAiModelConfigsQuery(search, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        modelsGroup.MapPost("/", async (
            UpsertAiModelConfigRequest req,
            ICommandHandler<CreateAiModelConfigCommand, AiModelConfigEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new CreateAiModelConfigCommand(req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpsertAiModelConfigRequest req,
            ICommandHandler<UpdateAiModelConfigCommand, AiModelConfigEntity?> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new UpdateAiModelConfigCommand(id, req), ct);
                return result is null ? Results.NotFound(new { error = "Không tìm thấy AI Model." }) : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteAiModelConfigCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteAiModelConfigCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("rubrics.manage");

        return app;
    }
}
