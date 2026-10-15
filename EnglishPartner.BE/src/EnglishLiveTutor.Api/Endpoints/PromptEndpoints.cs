using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Api.Endpoints;

public static class PromptEndpoints
{
    public static IEndpointRouteBuilder MapPromptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/prompts");

        // --- 1. Speech Speeds CRUD (Database-Backed with IQueryable Pagination) ---
        group.MapGet("/speech-speeds", async (
            HttpContext httpContext,
            bool? onlyActive,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var query = db.SpeechSpeeds.AsNoTracking().AsQueryable();
            if (onlyActive ?? true)
            {
                query = query.Where(s => s.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    s.Code.ToLower().Contains(term) ||
                    s.Label.ToLower().Contains(term) ||
                    s.DescriptionVi.ToLower().Contains(term) ||
                    s.RecommendedCefrRange.ToLower().Contains(term));
            }

            var projected = query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.SpeedValue)
                .Select(s => new SpeechSpeedOptionDto(
                    s.Id,
                    s.Code,
                    s.SpeedValue,
                    s.Label,
                    s.DescriptionVi,
                    s.RecommendedCefrRange,
                    s.IsDefault,
                    s.IsActive,
                    s.DisplayOrder));

            var pagedResult = await PagedResultDto<SpeechSpeedOptionDto>.CreateAsync(projected, page, pageSize, ct);
            if (pagedResult.TotalCount == 0 && string.IsNullOrWhiteSpace(search))
            {
                var defaults = DatabaseSeeder.BuildDefaultSpeechSpeeds()
                    .Select(s => new SpeechSpeedOptionDto(
                        s.Id,
                        s.Code,
                        s.SpeedValue,
                        s.Label,
                        s.DescriptionVi,
                        s.RecommendedCefrRange,
                        s.IsDefault,
                        s.IsActive,
                        s.DisplayOrder))
                    .ToList();
                pagedResult = PagedResultDto<SpeechSpeedOptionDto>.FromPage(defaults, defaults.Count, page, pageSize);
            }

            return pagedResult.ToHttpResult(httpContext, paged);
        });

        group.MapPost("/speech-speeds", async (
            UpsertSpeechSpeedRequest req,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            if (req.IsDefault)
            {
                var currentDefaults = await db.SpeechSpeeds.Where(x => x.IsDefault).ToListAsync(ct);
                foreach (var d in currentDefaults)
                {
                    d.UpdateDetails(d.Code, d.SpeedValue, d.Label, d.DescriptionVi, d.RecommendedCefrRange, false, d.IsActive, d.DisplayOrder);
                }
            }

            var existing = await db.SpeechSpeeds
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == req.Code.Trim(), ct);

            if (existing is not null)
            {
                existing.Restore();
                existing.UpdateDetails(
                    req.Code,
                    req.Value,
                    req.Label,
                    req.DescriptionVi,
                    req.RecommendedCefrRange,
                    req.IsDefault,
                    req.IsActive,
                    req.DisplayOrder);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new SpeechSpeedOptionDto(
                    existing.Id, existing.Code, existing.SpeedValue, existing.Label,
                    existing.DescriptionVi, existing.RecommendedCefrRange, existing.IsDefault,
                    existing.IsActive, existing.DisplayOrder));
            }

            var entity = new SpeechSpeedEntity(
                req.Code,
                req.Value,
                req.Label,
                req.DescriptionVi,
                req.RecommendedCefrRange,
                req.IsDefault,
                req.IsActive,
                req.DisplayOrder);

            db.SpeechSpeeds.Add(entity);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new SpeechSpeedOptionDto(
                entity.Id, entity.Code, entity.SpeedValue, entity.Label,
                entity.DescriptionVi, entity.RecommendedCefrRange, entity.IsDefault,
                entity.IsActive, entity.DisplayOrder));
        }).RequirePermission("prompts.manage");

        group.MapPut("/speech-speeds/{id:guid}", async (
            Guid id,
            UpsertSpeechSpeedRequest req,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var entity = await db.SpeechSpeeds.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return Results.NotFound(new { error = "Không tìm thấy cấu hình tốc độ nói." });

            if (req.IsDefault)
            {
                var others = await db.SpeechSpeeds.Where(x => x.Id != id && x.IsDefault).ToListAsync(ct);
                foreach (var o in others)
                {
                    o.UpdateDetails(o.Code, o.SpeedValue, o.Label, o.DescriptionVi, o.RecommendedCefrRange, false, o.IsActive, o.DisplayOrder);
                }
            }

            entity.UpdateDetails(
                req.Code,
                req.Value,
                req.Label,
                req.DescriptionVi,
                req.RecommendedCefrRange,
                req.IsDefault,
                req.IsActive,
                req.DisplayOrder);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new SpeechSpeedOptionDto(
                entity.Id, entity.Code, entity.SpeedValue, entity.Label,
                entity.DescriptionVi, entity.RecommendedCefrRange, entity.IsDefault,
                entity.IsActive, entity.DisplayOrder));
        }).RequirePermission("prompts.manage");

        group.MapDelete("/speech-speeds/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var entity = await db.SpeechSpeeds.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return Results.NotFound();
            entity.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequirePermission("prompts.manage");

        // --- 2. CEFR Level Prompts CRUD ---
        group.MapGet("/cefr-levels", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListCefrLevelPromptsQuery, PagedResultDto<CefrLevelPromptEntity>> handler,
            CancellationToken ct) =>
        {
            var pagedResult = await handler.HandleAsync(new ListCefrLevelPromptsQuery(search, page, pageSize), ct);
            return pagedResult.ToHttpResult(httpContext, paged);
        });

        group.MapPost("/cefr-levels", async (
            UpsertCefrLevelPromptRequest req,
            ICommandHandler<UpsertCefrLevelPromptCommand, CefrLevelPromptEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                string levelCode = !string.IsNullOrWhiteSpace(req.DisplayName) && req.DisplayName.Length <= 4
                    ? req.DisplayName
                    : "B1";
                var created = await handler.HandleAsync(new UpsertCefrLevelPromptCommand(levelCode, req), ct);
                return Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("prompts.manage");

        group.MapPut("/cefr-levels/{cefrLevel}", async (
            string cefrLevel,
            UpsertCefrLevelPromptRequest req,
            ICommandHandler<UpsertCefrLevelPromptCommand, CefrLevelPromptEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new UpsertCefrLevelPromptCommand(cefrLevel, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("prompts.manage");

        group.MapDelete("/cefr-levels/{cefrLevel}", async (
            string cefrLevel,
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            string norm = cefrLevel.Trim().ToUpperInvariant();
            var entity = await db.CefrLevelPrompts.FirstOrDefaultAsync(x => x.CefrLevel == norm, ct);
            if (entity is null) return Results.NotFound();
            entity.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequirePermission("prompts.manage");

        // --- 3. Conversation Mode Prompts CRUD ---
        group.MapGet("/modes", async (
            HttpContext httpContext,
            string? category,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListConversationModePromptsQuery, PagedResultDto<ConversationModePromptEntity>> handler,
            CancellationToken ct) =>
        {
            var pagedModes = await handler.HandleAsync(new ListConversationModePromptsQuery(category, search, page, pageSize), ct);
            return pagedModes.ToHttpResult(httpContext, paged);
        });

        group.MapPost("/modes", async (
            UpsertConversationModePromptRequest req,
            ICommandHandler<UpsertConversationModePromptCommand, ConversationModePromptEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var created = await handler.HandleAsync(new UpsertConversationModePromptCommand(req), ct);
                return Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("prompts.manage");

        group.MapPut("/modes", async (
            UpsertConversationModePromptRequest req,
            ICommandHandler<UpsertConversationModePromptCommand, ConversationModePromptEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new UpsertConversationModePromptCommand(req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("prompts.manage");

        group.MapDelete("/modes/{modeCode}", async (
            string modeCode,
            ICurrentUser currentUser,
            IAppDbContextFactory dbFactory,
            CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            string norm = modeCode.Trim().ToLowerInvariant();
            var entity = await db.ConversationModePrompts.FirstOrDefaultAsync(x => x.ModeCode == norm, ct);
            if (entity is null) return Results.NotFound();
            entity.SoftDelete(DateTimeOffset.UtcNow, currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequirePermission("prompts.manage");

        // --- 4. System Instruction Preview ---
        group.MapPost("/preview", async (
            BuildSystemPromptPreviewRequest req,
            IQueryHandler<PreviewSystemPromptQuery, string> handler,
            CancellationToken ct) =>
        {
            string systemInstruction = await handler.HandleAsync(new PreviewSystemPromptQuery(req), ct);
            return Results.Ok(new
            {
                req.CefrLevel,
                req.ConversationMode,
                req.VoiceName,
                req.SpeechSpeed,
                req.ExtendedThinking,
                systemInstruction
            });
        });

        return app;
    }
}
