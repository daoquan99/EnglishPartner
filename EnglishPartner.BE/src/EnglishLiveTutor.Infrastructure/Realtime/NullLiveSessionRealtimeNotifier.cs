using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Infrastructure.Realtime;

public sealed class NullLiveSessionRealtimeNotifier : ILiveSessionRealtimeNotifier
{
    public static readonly NullLiveSessionRealtimeNotifier Instance = new();

    public Task SendTutorInterruptedAsync(string conversationId, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendTutorAudioAsync(string conversationId, string turnId, string base64Audio24k, string interactionStatus, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendInteractionStatusAsync(string conversationId, string status, string model, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendLanguageCorrectionAsync(string conversationId, LanguageCorrectionDto correction, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendTranscribeTranscriptAsync(string conversationId, string speaker, string turnId, int sequenceOrder, string text, bool isFinal, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendTranslateTextAsync(string conversationId, string speaker, string turnId, int sequenceOrder, string text, bool isFinal, CancellationToken ct = default) => Task.CompletedTask;
}
