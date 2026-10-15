using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

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
            ILivePronunciationTtsSynthesizer synthesizer,
            CancellationToken ct) =>
        {
            try
            {
                var result = await synthesizer.SynthesizeSpeechAsync(req, ct);
                return Results.Ok(result);
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
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var query = db.ScoringRubrics
                .AsNoTracking()
                .Include(r => r.Criteria.OrderBy(c => c.DisplayOrder))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    r.Code.ToLower().Contains(term) ||
                    r.Name.ToLower().Contains(term) ||
                    r.Description.ToLower().Contains(term));
            }

            var ordered = query
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.Code);

            return await ordered.ToPaginatedOrListResultAsync(httpContext, page, pageSize, paged, ct);
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
                string code = string.IsNullOrWhiteSpace(req.Name)
                    ? $"RUBRIC_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
                    : req.Name.Trim().ToUpperInvariant().Replace(' ', '_');
                var created = await handler.HandleAsync(new UpdateScoringRubricCommand(code, req), ct);
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
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rubric = await db.ScoringRubrics.FirstOrDefaultAsync(r => r.Code == code, ct);
            if (rubric is null) return Results.NotFound();
            rubric.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
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
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var query = db.AiModelCatalog.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLowerInvariant();
                query = query.Where(m =>
                    m.DisplayName.ToLower().Contains(term) ||
                    m.ModelCode.ToLower().Contains(term) ||
                    (m.CapabilityNote != null && m.CapabilityNote.ToLower().Contains(term)));
            }

            var ordered = query
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.DisplayName);

            return await ordered.ToPaginatedOrListResultAsync(httpContext, page, pageSize, paged, ct);
        });

        modelsGroup.MapPost("/", async (
            UpsertAiModelConfigRequest req,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.ModelCode))
                return Results.BadRequest(new { error = "Mã Model (ModelCode) không được để trống." });

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var entity = new AiModelConfigEntity(
                displayName: string.IsNullOrWhiteSpace(req.DisplayName) ? req.ModelCode : req.DisplayName.Trim(),
                modelCode: req.ModelCode.Trim(),
                routedModelCode: string.IsNullOrWhiteSpace(req.RoutedModelCode) ? null : req.RoutedModelCode.Trim(),
                supportsTextGeneration: req.SupportsTextGeneration,
                supportsStructuredOutput: req.SupportsStructuredOutput,
                supportsAudioInput: req.SupportsAudioInput,
                supportsPronunciationAssessment: req.SupportsPronunciationAssessment,
                supportsLiveAudio: req.SupportsLiveAudio,
                supportsTranscription: req.SupportsTranscription,
                supportsTranslation: req.SupportsTranslation,
                supportsStreaming: req.SupportsStreaming,
                isAvailable: req.IsAvailable,
                isDeprecated: req.IsDeprecated,
                isEnabled: req.IsEnabled,
                capabilityNote: req.CapabilityNote ?? string.Empty,
                displayOrder: req.DisplayOrder);

            db.AiModelCatalog.Add(entity);
            await db.SaveChangesAsync(ct);
            return Results.Ok(entity);
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpsertAiModelConfigRequest req,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.AiModelCatalog.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (existing is null) return Results.NotFound(new { error = "Không tìm thấy AI Model." });

            db.AiModelCatalog.Remove(existing);
            await db.SaveChangesAsync(ct);

            var updated = new AiModelConfigEntity(
                displayName: string.IsNullOrWhiteSpace(req.DisplayName) ? req.ModelCode : req.DisplayName.Trim(),
                modelCode: req.ModelCode.Trim(),
                routedModelCode: string.IsNullOrWhiteSpace(req.RoutedModelCode) ? null : req.RoutedModelCode.Trim(),
                supportsTextGeneration: req.SupportsTextGeneration,
                supportsStructuredOutput: req.SupportsStructuredOutput,
                supportsAudioInput: req.SupportsAudioInput,
                supportsPronunciationAssessment: req.SupportsPronunciationAssessment,
                supportsLiveAudio: req.SupportsLiveAudio,
                supportsTranscription: req.SupportsTranscription,
                supportsTranslation: req.SupportsTranslation,
                supportsStreaming: req.SupportsStreaming,
                isAvailable: req.IsAvailable,
                isDeprecated: req.IsDeprecated,
                isEnabled: req.IsEnabled,
                capabilityNote: req.CapabilityNote ?? string.Empty,
                displayOrder: req.DisplayOrder,
                id: id);

            db.AiModelCatalog.Add(updated);
            await db.SaveChangesAsync(ct);
            return Results.Ok(updated);
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.AiModelCatalog.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (existing is null) return Results.NotFound();
            existing.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequirePermission("rubrics.manage");

        return app;
    }
}
