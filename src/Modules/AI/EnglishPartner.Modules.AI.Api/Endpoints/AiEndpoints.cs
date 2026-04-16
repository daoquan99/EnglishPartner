using System.Security.Claims;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Application.Admin.Queries.GetAiRequestLogs;
using EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;
using EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;
using EnglishPartner.Modules.AI.Application.Chat.Queries.GetChatHistory;
using EnglishPartner.Modules.AI.Application.Chat.Queries.GetMySessions;
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
            .WithTags("AI");
            //.RequireAuthorization();

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
        .WithSummary("Evaluate user text input using AI. Returns SM-2 quality score (0-5).")
        .RequireRateLimiting("ai-scoring");

        group.MapPost("/evaluate-audio", async (
            IFormFile audioFile,
            [Microsoft.AspNetCore.Mvc.FromForm] Guid? itemId,
            [Microsoft.AspNetCore.Mvc.FromForm] CurriculumItemType? itemType,
            [Microsoft.AspNetCore.Mvc.FromForm] string? itemContext,
            ISender sender) =>
        {
            if (audioFile is null || audioFile.Length == 0)
                return Results.BadRequest("Audio file is required.");

            const long MaxAudioBytes = 10 * 1024 * 1024; // 10 MB
            if (audioFile.Length > MaxAudioBytes)
                return Results.BadRequest("Audio file is too large. Maximum size is 10 MB.");

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
        .DisableAntiforgery()
        .RequireRateLimiting("ai-scoring");

        // ── Text-to-Speech ──
        group.MapGet("/tts/voices", () =>
        {
            var voices = TtsVoiceCatalog.All.Select(v => new
            {
                v.Id,
                v.DisplayName,
                v.Accent,
                v.Gender,
                v.LanguageCode
            });
            return Results.Ok(voices);
        })
        .WithName("GetTtsVoices")
        .WithSummary("List all available Neural2 TTS voices with friendly names.");

        group.MapPost("/tts", async (TtsRequest request, ISpeechService speechService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest("text is required.");

            if (request.Text.Length > 5000)
                return Results.BadRequest("text must not exceed 5000 characters.");

            // Validate voice against catalog — fallback to default if not found
            var resolvedVoice = request.Voice is not null && TtsVoiceCatalog.FindById(request.Voice) is not null
                ? request.Voice
                : null; // null → SpeechService uses config default (en-US-Neural2-C)

            try
            {
                var audioBytes = await speechService.SynthesizeSpeechAsync(
                    request.Text,
                    resolvedVoice,
                    request.SpeakingRate,
                    ct);

                return Results.File(audioBytes, "audio/mpeg", "speech.mp3");
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 502);
            }
        })
        .WithName("TextToSpeech")
        .WithSummary("Convert text to speech audio (MP3). Returns binary audio/mpeg file.")
        .RequireRateLimiting("ai-chat");

        // ── Gemini TTS ──
        group.MapPost("/gemini-tts", async (GeminiTtsRequest request, IGeminiTtsService geminiTtsService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest("text is required.");

            if (request.Text.Length > 5000)
                return Results.BadRequest("text must not exceed 5000 characters.");

            try
            {
                var audioBytes = await geminiTtsService.SynthesizeSpeechAsync(
                    request.Text,
                    request.StylePrompt,
                    request.Voice ?? "zephyr",
                    request.LanguageCode,
                    ct);

                return Results.File(audioBytes, "audio/mpeg", "gemini-speech.mp3");
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 502);
            }
        })
        .WithName("GeminiTextToSpeech")
        .WithSummary("Convert text to highly expressive speech audio (MP3) using Gemini. Support stylePrompt.")
        .RequireRateLimiting("ai-chat");

        // ── Speech-to-Text ──
        group.MapPost("/stt", async (
            IFormFile audioFile,
            ISpeechService speechService,
            // Pass comma-separated codes for auto-detect: "en-US,vi-VN"
            // Or a single code: "en-US"
            // Or leave empty for server default
            [Microsoft.AspNetCore.Mvc.FromForm] string? languages,
            CancellationToken ct) =>
        {
            if (audioFile is null || audioFile.Length == 0)
                return Results.BadRequest("Audio file is required.");

            const long MaxBytes = 10 * 1024 * 1024; // 10 MB
            if (audioFile.Length > MaxBytes)
                return Results.BadRequest("Audio file too large. Maximum size is 10 MB.");

            using var stream = audioFile.OpenReadStream();
            var buffer = new byte[audioFile.Length];
            _ = await stream.ReadAsync(buffer, ct);

            // Parse comma-separated language codes: "en-US,vi-VN" → ["en-US", "vi-VN"]
            var languageCodes = string.IsNullOrWhiteSpace(languages)
                ? null
                : languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            try
            {
                var result = await speechService.RecognizeSpeechAsync(
                    buffer,
                    audioFile.ContentType,
                    languageCodes,
                    ct);

                return Results.Ok(new
                {
                    result.Transcript,
                    result.Confidence,
                    result.DetectedLanguage
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 502);
            }
        })
        .WithName("SpeechToText")
        .WithSummary("Transcribe an audio file to text using Google Cloud Speech-to-Text.")
        .DisableAntiforgery()
        .RequireRateLimiting("ai-scoring");

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
        .WithSummary("Send a message to the AI Tutor and get a reply.")
        .RequireRateLimiting("ai-chat");

        // ── Admin group ──
        var admin = app.MapGroup("api/v1/admin/ai")
            .WithTags("AI Admin")
            .RequireAuthorization("Admin");

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
        .WithSummary("Get a paginated list of AI request logs.")
        .RequireAuthorization("ai.logs.read");
    }

    private static bool TryGetUserId(HttpContext ctx, out Guid userId)
        => ctx.TryGetUserId(out userId);
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

/// <param name="Text">Text to synthesize (max 5000 chars).</param>
/// <param name="Voice">Voice name override (e.g. "en-US-Neural2-F" for female). Null = server default.</param>
/// <param name="SpeakingRate">Speed multiplier 0.25–4.0. Null = 1.0 (normal).</param>
public sealed record TtsRequest(
    string Text,
    string? Voice = null,
    double? SpeakingRate = null);

/// <param name="Text">Text to synthesize.</param>
/// <param name="Voice">Voice ID from LiveVoiceCatalog (e.g. "zephyr"). Null = default.</param>
/// <param name="StylePrompt">Override for style instruction. Null = config default.</param>
/// <param name="LanguageCode">Target language code. Null = config default.</param>
public sealed record GeminiTtsRequest(
    string Text,
    string? Voice = null,
    string? StylePrompt = null,
    string? LanguageCode = null);
