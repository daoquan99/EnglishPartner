using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Api.Endpoints.Hubs;

/// <summary>
/// SignalR Hub for handling Real-time Native Audio Dialog with Gemini Live API.
/// </summary>
public sealed class AiLiveAvatarHub : Hub
{
    private readonly LiveAudioSessionManager _sessionManager;
    private readonly ILogger<AiLiveAvatarHub> _logger;

    public AiLiveAvatarHub(LiveAudioSessionManager sessionManager, ILogger<AiLiveAvatarHub> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Client {ConnectionId} connected to AiLiveAvatarHub.", connectionId);

        var session = _sessionManager.CreateSession(connectionId);

        // Bind incoming events from AI to the Hub Client
        session.OnAudioReceived = async (pcmData) =>
        {
            try
            {
                await Clients.Client(connectionId).SendAsync("AudioChunkReceived", pcmData);
            }
            catch { /* Ignore if disconnected */ }
        };

        session.OnTextReceived = async (text) =>
        {
            try
            {
                await Clients.Client(connectionId).SendAsync("TextChunkReceived", text);
            }
            catch { /* Ignore if disconnected */ }
        };

        // Initialize connection to Gemini immediately with a default System Prompt
        var systemPrompt = "You are an English companion tutor. Talk directly and naturally. Keep sentences short and conversational.";
        await session.ConnectAsync(systemPrompt, Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected.", Context.ConnectionId);
        await _sessionManager.RemoveSessionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called by the frontend to stream mic audio chunks (16-bit PCM at 16kHz).
    /// </summary>
    public async Task SendAudioChunk(byte[] audioChunk)
    {
        try
        {
            var session = _sessionManager.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await session.SendAudioAsync(audioChunk, Context.ConnectionAborted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send audio chunk to Gemini.");
        }
    }

    /// <summary>
    /// Can be used by frontend to send raw text (e.g., button clicks or interruptions).
    /// </summary>
    public async Task SendTextMessage(string text)
    {
        try
        {
            var session = _sessionManager.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await session.SendTextAsync(text, Context.ConnectionAborted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send text to Gemini.");
        }
    }
}
