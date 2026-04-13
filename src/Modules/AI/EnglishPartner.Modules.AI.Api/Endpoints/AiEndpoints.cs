using System.Security.Claims;
using EnglishPartner.Modules.AI.Application.Admin.Queries.GetAiRequestLogs;
using EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;
using EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;
using EnglishPartner.Modules.AI.Application.Chat.Queries.GetChatHistory;
using EnglishPartner.Modules.AI.Application.Chat.Queries.GetMySessions;
using EnglishPartner.Modules.AI.Application.ContentGeneration.Commands.GenerateConversation;
using EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateInput;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class AiEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        // ── Authenticated user group ──
        var group = app.MapGroup("api/v1/ai")
            .WithTags("AI")
            .RequireAuthorization();

        // ── Evaluation ──
        group.MapPost("/evaluate", async (EvaluateInputRequest request, ISender sender) =>
        {
            var command = new EvaluateInputCommand(
                request.UserInput,
                request.ItemContext,
                request.ItemId,
                request.ItemType);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("EvaluateInput")
        .WithSummary("Evaluate user text input using AI. Returns SM-2 quality score (0-5).");

        group.MapPost("/evaluate-audio", async (
            IFormFile audioFile,
            [Microsoft.AspNetCore.Mvc.FromForm] Guid? itemId,
            [Microsoft.AspNetCore.Mvc.FromForm] CurriculumItemType? itemType,
            [Microsoft.AspNetCore.Mvc.FromForm] string? itemContext,
            ISender sender) =>
        {
            if (audioFile is null || audioFile.Length == 0)
                return Results.BadRequest("Audio file is required.");

            using var stream = audioFile.OpenReadStream();
            var command = new EnglishPartner.Modules.AI.Application.Evaluation.Commands.EvaluateAudio.EvaluateAudioCommand(
                stream,
                audioFile.FileName,
                audioFile.ContentType,
                itemContext,
                itemId,
                itemType);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("EvaluateAudio")
        .WithSummary("Upload and evaluate an audio file using AI. Returns Transcription and SM-2 quality score (0-5).")
        .DisableAntiforgery(); // Minimal API + IFormFile sometimes requires this if antiforgery is globally enforced.

        // ── Chat — Sessions ──
        group.MapGet("/chat/sessions", async (ISender sender, HttpContext ctx) =>
        {
            if (!TryGetUserId(ctx, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetMySessionsQuery(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetMyChatSessions")
        .WithSummary("List all AI Tutor chat sessions for the current user.");

        group.MapPost("/chat/sessions", async (StartChatRequest request, ISender sender, HttpContext ctx) =>
        {
            if (!TryGetUserId(ctx, out var userId)) return Results.Unauthorized();
            var command = new StartChatCommand(userId, request.Title, request.SystemPrompt);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/ai/chat/sessions/{result.Value}", new { SessionId = result.Value })
                : result.ToProblemDetails();
        })
        .WithName("StartChat")
        .WithSummary("Start a new AI Tutor chat session.");

        group.MapGet("/chat/sessions/{sessionId:guid}/messages", async (Guid sessionId, ISender sender, HttpContext ctx) =>
        {
            if (!TryGetUserId(ctx, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetChatHistoryQuery(sessionId, userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetChatHistory")
        .WithSummary("Get all messages in a chat session.");

        group.MapPost("/chat/sessions/{sessionId:guid}/messages", async (Guid sessionId, SendMessageRequest request, ISender sender, HttpContext ctx) =>
        {
            if (!TryGetUserId(ctx, out var userId)) return Results.Unauthorized();
            var command = new SendMessageCommand(sessionId, userId, request.Text);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("SendChatMessage")
        .WithSummary("Send a message to the AI Tutor and get a reply.");

        // ── Admin group ──
        var admin = app.MapGroup("api/v1/admin/ai")
            .WithTags("AI Admin")
            .RequireAuthorization("Admin");

        admin.MapPost("/generate-conversation", async (GenerateConversationCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GenerateConversation")
        .WithSummary("Generate an AI conversation script for a curriculum topic.");

        admin.MapGet("/request-logs", async (
            [AsParameters] GetAiRequestLogsRequest request,
            ISender sender) =>
        {
            var query = new GetAiRequestLogsQuery(
                request.Page,
                request.PageSize,
                request.RequestType,
                request.IsSuccess,
                request.From,
                request.To);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetAiRequestLogs")
        .WithSummary("Get a paginated list of AI request logs.");
    }

    private static bool TryGetUserId(HttpContext ctx, out Guid userId)
    {
        var value = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}

// ── Request DTOs (only fields from body — UserId comes from JWT) ──
/// <param name="ItemId">Optional. Curriculum item ID — server fetches context automatically.</param>
/// <param name="ItemType">Required when ItemId is provided.</param>
/// <param name="ItemContext">Optional. Provide directly if not using ItemId (standalone mode).</param>
public sealed record EvaluateInputRequest(
    string UserInput,
    Guid? ItemId = null,
    CurriculumItemType? ItemType = null,
    string? ItemContext = null);
public sealed record StartChatRequest(string Title, string? SystemPrompt = null);
public sealed record SendMessageRequest(string Text);
public sealed record GetAiRequestLogsRequest(
    int Page = 1,
    int PageSize = 20,
    string? RequestType = null,
    bool? IsSuccess = null,
    DateTime? From = null,
    DateTime? To = null);
