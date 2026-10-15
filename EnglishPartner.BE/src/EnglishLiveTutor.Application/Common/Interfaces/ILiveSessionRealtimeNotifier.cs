using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ILiveSessionRealtimeNotifier
{
    // Tutor Hub events
    Task SendTutorInterruptedAsync(string conversationId, CancellationToken ct = default);
    Task SendTutorAudioAsync(string conversationId, string turnId, string base64Audio24k, string interactionStatus, CancellationToken ct = default);
    Task SendInteractionStatusAsync(string conversationId, string status, string model, CancellationToken ct = default);
    Task SendLanguageCorrectionAsync(string conversationId, LanguageCorrectionDto correction, CancellationToken ct = default);

    // Transcribe Hub events
    Task SendTranscribeTranscriptAsync(string conversationId, string speaker, string turnId, int sequenceOrder, string text, bool isFinal, CancellationToken ct = default);

    // Translate Hub events
    Task SendTranslateTextAsync(string conversationId, string speaker, string turnId, int sequenceOrder, string text, bool isFinal, CancellationToken ct = default);
}
