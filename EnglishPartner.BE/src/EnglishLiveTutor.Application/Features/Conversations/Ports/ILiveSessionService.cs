using EnglishLiveTutor.Application.Features.Conversations.Contracts;

namespace EnglishLiveTutor.Application.Features.Conversations.Ports;

public interface ILiveSessionService
{
    Task StartTutorSessionAsync(
        StartTutorSessionRequest request,
        string? connectionId = null,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task StartTranscribeHubSessionAsync(
        StartSubHubSessionRequest request,
        string? connectionId = null,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task StartTranslateHubSessionAsync(
        StartSubHubSessionRequest request,
        string? connectionId = null,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task ForwardTutorHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default);
    Task ForwardTranscribeHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default);
    Task ForwardTranslateHubAudioAsync(string conversationId, string base64Pcm16k, CancellationToken ct = default);

    Task EndUserSpeechAsync(string conversationId, CancellationToken ct = default);
    Task EndTranslateSpeechAsync(string conversationId, CancellationToken ct = default);

    Task AnalyzeTurnGrammarManualAsync(
        string conversationId,
        string turnId,
        string transcriptText,
        string cefrLevel,
        CancellationToken ct = default);

    Task StopAllForConversationAsync(string conversationId, CancellationToken ct = default);

    /// <summary>
    /// Removes a disconnected SignalR connection from any tracked conversations and stops only those sessions
    /// that no longer have any active connection owners.
    /// </summary>
    Task HandleConnectionDisconnectedAsync(string connectionId, CancellationToken ct = default);
}
