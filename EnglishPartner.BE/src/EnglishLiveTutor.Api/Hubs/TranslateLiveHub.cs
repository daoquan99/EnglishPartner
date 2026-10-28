using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Conversations.Validation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Api.Hubs;

/// <summary>
/// Dedicated Hub #2: Connects Frontend <-> Backend <-> Gemini 3.5 Live Translate (models/gemini-3.5-live-translate-preview).
/// Receives 16kHz PCM audio directly from Frontend via StreamAudioForTranslation and returns ONLY Vietnamese text (Line 2).
/// </summary>
public sealed class TranslateLiveHub : Hub
{
    private readonly ILiveSessionService _manager;
    private readonly ILogger<TranslateLiveHub>? _logger;

    public TranslateLiveHub(
        ILiveSessionService manager,
        ILogger<TranslateLiveHub>? logger = null)
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

    public async Task InitializeTranslateSession(StartSubHubSessionRequest request)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(request.ConversationId);
            var normalizedReq = request with { ConversationId = normalizedConvId };

            await _manager.StartTranslateHubSessionAsync(userId, Context.ConnectionId, normalizedReq);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}:{normalizedConvId}");
            await Clients.Caller.SendAsync("TranslateHubReady", new
            {
                ConversationId = normalizedConvId,
                Model = "gemini-3.5-live-translate-preview",
                TargetLanguage = "vi",
                TextOnly = true
            });
        }, _logger, nameof(InitializeTranslateSession));
    }

    public async Task StreamAudioForTranslation(AudioChunkPayload payload)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(payload.ConversationId);
            await _manager.ForwardTranslateHubAudioAsync(userId, Context.ConnectionId, normalizedConvId, payload.Base64Pcm16k);
        }, _logger, nameof(StreamAudioForTranslation));
    }

    public async Task EndTranslateAudioTurn(string conversationId)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(conversationId);
            await _manager.EndTranslateSpeechAsync(userId, Context.ConnectionId, normalizedConvId);
        }, _logger, nameof(EndTranslateAudioTurn));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _manager.OnClientDisconnectedAsync(Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Lỗi khi xử lý OnDisconnectedAsync trong TranslateLiveHub: {ExceptionType}", ex.GetType().Name);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
