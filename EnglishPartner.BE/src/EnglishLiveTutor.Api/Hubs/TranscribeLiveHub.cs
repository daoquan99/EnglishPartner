using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Conversations.Validation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Api.Hubs;

/// <summary>
/// Dedicated Hub #1: Connects Frontend <-> Backend <-> Gemini 3.5 Transcribe Live (models/gemini-3.5-transcribe-live).
/// Receives 16kHz PCM audio directly from Frontend and delivers Line 1 real-time English transcripts.
/// </summary>
public sealed class TranscribeLiveHub : Hub
{
    private readonly ILiveSessionService _manager;
    private readonly ILogger<TranscribeLiveHub>? _logger;

    public TranscribeLiveHub(
        ILiveSessionService manager,
        ILogger<TranscribeLiveHub>? logger = null)
    {
        _manager = manager;
        _logger = logger;
    }

    private string GetUserId()
    {
        var user = Context.User;
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Unauthorized: user identity not found.");
        }
        return userId;
    }

    public async Task InitializeTranscribeSession(StartSubHubSessionRequest request)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(request.ConversationId);
            var normalizedReq = request with { ConversationId = normalizedConvId };

            await _manager.StartTranscribeHubSessionAsync(userId, Context.ConnectionId, normalizedReq);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}:{normalizedConvId}");
            await Clients.Caller.SendAsync("TranscribeHubReady", new
            {
                ConversationId = normalizedConvId,
                Model = "gemini-3.5-transcribe-live"
            });
        }, _logger, nameof(InitializeTranscribeSession));
    }

    public async Task StreamAudioForTranscription(AudioChunkPayload payload)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(payload.ConversationId);
            await _manager.ForwardTranscribeHubAudioAsync(userId, Context.ConnectionId, normalizedConvId, payload.Base64Pcm16k);
        }, _logger, nameof(StreamAudioForTranscription));
    }

    public async Task EndAudioTurn(string conversationId)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(conversationId);
            await _manager.EndUserSpeechAsync(userId, Context.ConnectionId, normalizedConvId);
        }, _logger, nameof(EndAudioTurn));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _manager.OnClientDisconnectedAsync(Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Lỗi khi xử lý OnDisconnectedAsync trong TranscribeLiveHub: {ExceptionType}", ex.GetType().Name);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
