using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertCefrLevelPrompt;
using EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.ListConversationModePrompts;
using EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;
using EnglishLiveTutor.Domain.Prompts;

namespace EnglishLiveTutor.Api.Endpoints;

public static class PromptEndpoints
{
    private static readonly IReadOnlyList<SpeechSpeedOptionDto> SpeechSpeedCatalog = new List<SpeechSpeedOptionDto>
    {
        new(0.75, "0.75x — Chậm & Rõ từng từ", "Phù hợp cho người mới bắt đầu hoặc nghe từ mới khó", "A1 - A2", false),
        new(0.90, "0.90x — Vừa phải, dễ bắt kịp", "Nhịp độ sư phạm rõ ràng, dễ nghe trọng âm và nối âm", "A2 - B1", false),
        new(1.00, "1.00x — Tự nhiên chuẩn bản xứ", "Tốc độ hội thoại tiêu chuẩn hằng ngày và phỏng vấn", "B1 - B2", true),
        new(1.15, "1.15x — Nhanh & Năng động", "Luyện phản xạ nghe nhanh trong họp Scrum hoặc tranh luận", "B2 - C1", false),
        new(1.25, "1.25x — Thử thách tốc độ cao", "Mô phỏng áp lực cao và người bản xứ nói nhanh", "C1 - C2", false)
    };

    public static IEndpointRouteBuilder MapPromptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/prompts");

        group.MapGet("/speech-speeds", () => Results.Ok(SpeechSpeedCatalog));

        group.MapGet("/cefr-levels", async (
            IQueryHandler<ListCefrLevelPromptsQuery, List<CefrLevelPromptEntity>> handler,
            CancellationToken ct) =>
        {
            var list = await handler.HandleAsync(new ListCefrLevelPromptsQuery(), ct);
            return Results.Ok(list);
        });

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

        group.MapGet("/modes", async (
            string? category,
            IQueryHandler<ListConversationModePromptsQuery, List<ConversationModePromptEntity>> handler,
            CancellationToken ct) =>
        {
            var modes = await handler.HandleAsync(new ListConversationModePromptsQuery(category), ct);
            return Results.Ok(modes);
        });

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
