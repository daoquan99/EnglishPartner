using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace EnglishLiveTutor.Api.Hubs;

public sealed class TutorConversationHub : Hub
{
    private readonly GeminiLiveConnectionManager _manager;

    public TutorConversationHub(GeminiLiveConnectionManager manager) => _manager = manager;

    public async Task InitializeTutorSession(StartTutorSessionRequest request)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, request.ConversationId);
        await _manager.StartTutorSessionAsync(request);
        await Clients.Caller.SendAsync("TutorSessionReady", new
        {
            request.ConversationId,
            Model = request.ExtendedThinking ? "gemini-3.8-live-extended-thinking" : "gemini-3.8-live",
            request.CefrLevel,
            request.ExtendedThinking,
            request.VoiceName,
            request.SpeechSpeed
        });
    }

    public async Task StreamUserAudioToTutor(AudioChunkPayload payload)
    {
        await _manager.ForwardTutorHubAudioAsync(payload.ConversationId, payload.Base64Pcm16k);
    }

    public async Task NotifyUserSpeechEnd(string conversationId)
    {
        await _manager.EndUserSpeechAsync(conversationId);
    }

    public async Task AnalyzeTurnGrammar(string conversationId, string turnId, string transcriptText, string cefrLevel)
    {
        await _manager.AnalyzeTurnGrammarManualAsync(conversationId, turnId, transcriptText, cefrLevel);
    }

    public async Task StopSession(string conversationId)
    {
        await _manager.StopAllForConversationAsync(conversationId);
    }
}
