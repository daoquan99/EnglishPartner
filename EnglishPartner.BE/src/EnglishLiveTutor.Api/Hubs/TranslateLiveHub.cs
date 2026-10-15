using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace EnglishLiveTutor.Api.Hubs;

/// <summary>
/// Dedicated Hub #2: Connects Frontend <-> Backend <-> Gemini 3.5 Live Translate (models/gemini-3.5-live-translate-preview).
/// Receives 16kHz PCM audio directly from Frontend via StreamAudioForTranslation and returns ONLY Vietnamese text (Line 2).
/// </summary>
public sealed class TranslateLiveHub : Hub
{
    private readonly GeminiLiveConnectionManager _manager;

    public TranslateLiveHub(GeminiLiveConnectionManager manager) => _manager = manager;

    public async Task InitializeTranslateSession(StartSubHubSessionRequest request)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, request.ConversationId);
        await _manager.StartTranslateHubSessionAsync(request);
        await Clients.Caller.SendAsync("TranslateHubReady", new
        {
            request.ConversationId,
            Model = "gemini-3.5-live-translate-preview",
            TargetLanguage = "vi",
            TextOnly = true
        });
    }

    public async Task StreamAudioForTranslation(AudioChunkPayload payload)
    {
        await _manager.ForwardTranslateHubAudioAsync(payload.ConversationId, payload.Base64Pcm16k);
    }

    public async Task EndTranslateAudioTurn(string conversationId)
    {
        await _manager.EndTranslateSpeechAsync(conversationId);
    }
}
