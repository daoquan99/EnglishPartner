using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Features.Conversations.Validation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Api.Hubs;

public sealed class TutorConversationHub : Hub
{
    private readonly ILiveSessionService _manager;
    private readonly ILogger<TutorConversationHub>? _logger;

    public TutorConversationHub(
        ILiveSessionService manager,
        ILogger<TutorConversationHub>? logger = null)
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

    public async Task InitializeTutorSession(StartTutorSessionRequest request)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(request.ConversationId);
            var normalizedReq = request with { ConversationId = normalizedConvId };

            await _manager.StartTutorSessionAsync(userId, Context.ConnectionId, normalizedReq);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}:{normalizedConvId}");
            await Clients.Caller.SendAsync("TutorSessionReady", new
            {
                ConversationId = normalizedConvId,
                Model = normalizedReq.ExtendedThinking ? "gemini-3.8-live-extended-thinking" : "gemini-3.8-live",
                normalizedReq.CefrLevel,
                normalizedReq.ExtendedThinking,
                normalizedReq.VoiceName,
                normalizedReq.SpeechSpeed
            });
        }, _logger, nameof(InitializeTutorSession));
    }

    public async Task StreamUserAudioToTutor(AudioChunkPayload payload)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(payload.ConversationId);
            await _manager.ForwardTutorHubAudioAsync(userId, Context.ConnectionId, normalizedConvId, payload.Base64Pcm16k);
        }, _logger, nameof(StreamUserAudioToTutor));
    }

    public async Task NotifyUserSpeechEnd(string conversationId)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(conversationId);
            await _manager.EndUserSpeechAsync(userId, Context.ConnectionId, normalizedConvId);
        }, _logger, nameof(NotifyUserSpeechEnd));
    }

    public async Task StopSession(string conversationId)
    {
        await HubExceptionHelper.ExecuteAsync(async () =>
        {
            var userId = GetUserId();
            var normalizedConvId = ConversationIdValidator.NormalizeAndValidate(conversationId);
            await _manager.StopAllForConversationAsync(userId, Context.ConnectionId, normalizedConvId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{userId}:{normalizedConvId}");
        }, _logger, nameof(StopSession));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _manager.OnClientDisconnectedAsync(Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Lỗi khi xử lý OnDisconnectedAsync trong TutorConversationHub: {ExceptionType}", ex.GetType().Name);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
