using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace EnglishLiveTutor.Api.Hubs;

/// <summary>
/// Dedicated Hub #1: Connects Frontend <-> Backend <-> Gemini 3.5 Transcribe Live (models/gemini-3.5-transcribe-live).
/// Receives 16kHz PCM audio directly from Frontend and delivers Line 1 real-time English transcripts.
/// </summary>
public sealed class TranscribeLiveHub : Hub
{
    private readonly GeminiLiveConnectionManager _manager;

    public TranscribeLiveHub(GeminiLiveConnectionManager manager) => _manager = manager;

    public async Task InitializeTranscribeSession(StartSubHubSessionRequest request)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, request.ConversationId);
        await _manager.StartTranscribeHubSessionAsync(request);
        await Clients.Caller.SendAsync("TranscribeHubReady", new
        {
            request.ConversationId,
            Model = "gemini-3.5-transcribe-live"
        });
    }

    public async Task StreamAudioForTranscription(AudioChunkPayload payload)
    {
        await _manager.ForwardTranscribeHubAudioAsync(payload.ConversationId, payload.Base64Pcm16k);
    }

    public async Task EndAudioTurn(string conversationId)
    {
        await _manager.EndUserSpeechAsync(conversationId);
    }
}
