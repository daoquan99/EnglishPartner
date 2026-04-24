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
            .WithTags("AI")
            .RequireAuthorization(); // all AI endpoints require login

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

        // ── Gemini Live Voices ──
        group.MapGet("/live-voices", () =>
        {
            var voices = LiveVoiceCatalog.All.Select(v => new
            {
                v.Id,
                v.DisplayName,
                v.Gender
            });
            return Results.Ok(voices);
        })
        .WithName("GetLiveVoices")
        .WithSummary("List all 30 available Gemini Native Audio voices (polyglot) for Live sessions and Gemini TTS.");

        group.MapPost("/tts", async (TtsRequest request, ISpeechService speechService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest("text is required.");

            if (request.Text.Length > 5000)
                return Results.BadRequest("text must not exceed 5000 characters.");

            var resolvedVoice = request.Voice is not null && TtsVoiceCatalog.FindById(request.Voice) is not null
                ? request.Voice
                : null;

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
        .RequireRateLimiting("ai-scoring");

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
        .RequireRateLimiting("ai-scoring");

        // ── Speech-to-Text ──
        group.MapPost("/stt", async (
            IFormFile audioFile,
            ISpeechService speechService,
            [Microsoft.AspNetCore.Mvc.FromForm] string? languages,
            CancellationToken ct) =>
        {
            if (audioFile is null || audioFile.Length == 0)
                return Results.BadRequest("Audio file is required.");

            const long MaxBytes = 10 * 1024 * 1024;
            if (audioFile.Length > MaxBytes)
                return Results.BadRequest("Audio file too large. Maximum size is 10 MB.");

            using var stream = audioFile.OpenReadStream();
            var buffer = new byte[audioFile.Length];
            _ = await stream.ReadAsync(buffer, ct);

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

        // ── Chat (UserId resolved by handler via ICurrentUserService) ──
        group.MapGet("/chat/sessions", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMySessionsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetMyChatSessions")
        .WithSummary("List all AI Tutor chat sessions for the current user.");

        group.MapPost("/chat/sessions", async (StartChatRequest request, ISender sender) =>
        {
            var command = new StartChatCommand(request.Title, request.SystemPrompt);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/ai/chat/sessions/{result.Value}", new { SessionId = result.Value })
                : result.ToProblemDetails();
        })
        .WithName("StartChat")
        .WithSummary("Start a new AI Tutor chat session.");

        group.MapGet("/chat/sessions/{sessionId:guid}/messages", async (Guid sessionId, ISender sender) =>
        {
            var result = await sender.Send(new GetChatHistoryQuery(sessionId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetChatHistory")
        .WithSummary("Get all messages in a chat session.");

        group.MapPost("/chat/sessions/{sessionId:guid}/messages", async (Guid sessionId, SendMessageRequest request, ISender sender) =>
        {
            var command = new SendMessageCommand(sessionId, request.Text);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("SendChatMessage")
        .WithSummary("Send a message to the AI Tutor and get a reply.")
        .RequireRateLimiting("ai-chat");

        // ── Admin group ──
        var admin = app.MapGroup("api/v1/admin/ai")
            .WithTags("AI Admin")
            .RequireAuthorization("ai.logs.read");

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
}

// ── Request DTOs ──
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

public sealed record TtsRequest(
    string Text,
    string? Voice = null,
    double? SpeakingRate = null);

public sealed record GeminiTtsRequest(
    string Text,
    string? Voice = null,
    string? StylePrompt = null,
    string? LanguageCode = null);
