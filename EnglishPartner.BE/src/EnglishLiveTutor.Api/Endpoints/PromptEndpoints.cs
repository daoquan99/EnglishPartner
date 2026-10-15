using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListSpeechSpeeds;
using EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;
using EnglishLiveTutor.Domain.Prompts;

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
            IQueryHandler<ListSpeechSpeedsQuery, PagedResultDto<SpeechSpeedOptionDto>> handler,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await handler.HandleAsync(new ListSpeechSpeedsQuery(onlyActive, search, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        group.MapPost("/speech-speeds", async (
            UpsertSpeechSpeedRequest req,
            ICommandHandler<UpsertSpeechSpeedCommand, SpeechSpeedOptionDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new UpsertSpeechSpeedCommand(req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("prompts.manage");

        group.MapPut("/speech-speeds/{id:guid}", async (
            Guid id,
            UpsertSpeechSpeedRequest req,
            ICommandHandler<UpdateSpeechSpeedCommand, SpeechSpeedOptionDto?> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new UpdateSpeechSpeedCommand(id, req), ct);
                return result is null ? Results.NotFound(new { error = "Không tìm thấy cấu hình tốc độ nói." }) : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("prompts.manage");

        group.MapDelete("/speech-speeds/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteSpeechSpeedCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteSpeechSpeedCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
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
                var created = await handler.HandleAsync(new UpsertCefrLevelPromptCommand(req), ct);
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
            ICommandHandler<DeleteCefrLevelPromptCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteCefrLevelPromptCommand(cefrLevel), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
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
            ICommandHandler<DeleteConversationModePromptCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteConversationModePromptCommand(modeCode), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
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
