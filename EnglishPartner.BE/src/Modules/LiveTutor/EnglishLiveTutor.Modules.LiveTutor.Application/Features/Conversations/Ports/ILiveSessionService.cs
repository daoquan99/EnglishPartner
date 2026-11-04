using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Conversations.Ports;

public interface ILiveSessionService
{
    Task StartTutorSessionAsync(string userId, string connectionId, StartTutorSessionRequest request);
    Task ForwardTutorHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k);
    Task EndUserSpeechAsync(string userId, string connectionId, string conversationId);
    Task AnalyzeTurnGrammarManualAsync(string userId, string connectionId, string conversationId, string turnId, string transcriptText, string cefrLevel, CancellationToken ct = default);
    Task StopAllForConversationAsync(string userId, string connectionId, string conversationId);

    Task StartTranscribeHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest request);
    Task ForwardTranscribeHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k);

    Task StartTranslateHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest request);
    Task ForwardTranslateHubAudioAsync(string userId, string connectionId, string conversationId, string base64Pcm16k);
    Task EndTranslateSpeechAsync(string userId, string connectionId, string conversationId);

    Task StartGrammarHubSessionAsync(string userId, string connectionId, StartSubHubSessionRequest request);
    Task<FeedbackAudioResultDto> RequestFeedbackAudioAsync(
        string userId,
        string connectionId,
        string conversationId,
        string correctionId,
        string variant,
        CancellationToken ct = default);

    Task OnClientDisconnectedAsync(string connectionId);
}
