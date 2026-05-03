using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class SpeechEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
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

        group.MapGet("/stt/stream", async (
            HttpContext context,
            ISpeechService speechService,
            string? languages = null,
            string mimeType = "audio/webm",
            CancellationToken ct = default) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return Results.BadRequest("Expected a WebSocket request.");
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var languageCodes = string.IsNullOrWhiteSpace(languages)
                ? null
                : languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Audio stream from client
            async IAsyncEnumerable<byte[]> ReadAudioStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var buffer = new byte[1024 * 8]; // 8KB chunks
                while (!webSocket.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        break;
                    }
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                    {
                        yield return buffer.Take(result.Count).ToArray();
                    }
                }
            }

            // Callback when Google STT returns interim/final result
            async Task OnResultAsync(SttStreamingResult result)
            {
                if (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        System.Net.WebSockets.WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }

            try
            {
                await speechService.StreamingRecognizeAsync(
                    ReadAudioStreamAsync(ct),
                    mimeType,
                    OnResultAsync,
                    languageCodes,
                    ct);
            }
            catch (OperationCanceledException)
            {
                // Normal disconnect
            }
            catch (Exception ex)
            {
                if (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var error = System.Text.Encoding.UTF8.GetBytes($"{{\"error\": \"{ex.Message}\"}}");
                    await webSocket.SendAsync(new ArraySegment<byte>(error), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                    await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, "Error processing audio stream", CancellationToken.None);
                }
            }

            if (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            }

            return Results.Empty;
        })
        .WithName("SpeechToTextStream")
        .WithSummary("Transcribe audio to text in real-time via WebSockets.")
        .RequireRateLimiting("ai-scoring");
    }
}

public sealed record TtsRequest(
    string Text,
    string? Voice = null,
    double? SpeakingRate = null);

public sealed record GeminiTtsRequest(
    string Text,
    string? Voice = null,
    string? StylePrompt = null,
    string? LanguageCode = null);
