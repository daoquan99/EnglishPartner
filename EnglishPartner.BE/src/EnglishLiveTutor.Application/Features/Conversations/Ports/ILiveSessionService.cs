using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Conversations.Ports;

public interface ILiveSessionService
{
    Task StartTutorSessionAsync(StartTutorSessionRequest request);
    Task ForwardTutorHubAudioAsync(string conversationId, string base64Pcm16k);
    Task EndUserSpeechAsync(string conversationId);
    Task AnalyzeTurnGrammarManualAsync(string conversationId, string turnId, string transcriptText, string cefrLevel);
    Task StopAllForConversationAsync(string conversationId);

    Task StartTranscribeHubSessionAsync(StartSubHubSessionRequest request);
    Task ForwardTranscribeHubAudioAsync(string conversationId, string base64Pcm16k);

    Task StartTranslateHubSessionAsync(StartSubHubSessionRequest request);
    Task ForwardTranslateHubAudioAsync(string conversationId, string base64Pcm16k);
    Task EndTranslateSpeechAsync(string conversationId);
}
