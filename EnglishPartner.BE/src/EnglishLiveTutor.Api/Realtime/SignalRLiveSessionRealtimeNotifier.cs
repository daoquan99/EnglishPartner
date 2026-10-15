using EnglishLiveTutor.Api.Hubs;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.AspNetCore.SignalR;

namespace EnglishLiveTutor.Api.Realtime;

public sealed class SignalRLiveSessionRealtimeNotifier : ILiveSessionRealtimeNotifier
{
    private readonly IHubContext<TutorConversationHub> _tutorHub;
    private readonly IHubContext<TranscribeLiveHub> _transcribeHub;
    private readonly IHubContext<TranslateLiveHub> _translateHub;

    public SignalRLiveSessionRealtimeNotifier(
        IHubContext<TutorConversationHub> tutorHub,
        IHubContext<TranscribeLiveHub> transcribeHub,
        IHubContext<TranslateLiveHub> translateHub)
    {
        _tutorHub = tutorHub;
        _transcribeHub = transcribeHub;
        _translateHub = translateHub;
    }

    public async Task SendTutorInterruptedAsync(string conversationId, CancellationToken ct = default)
    {
        await _tutorHub.Clients.Group(conversationId).SendAsync("TutorInterrupted", conversationId, ct);
    }

    public async Task SendTutorAudioAsync(string conversationId, string turnId, string base64Audio24k, string interactionStatus, CancellationToken ct = default)
    {
        await _tutorHub.Clients.Group(conversationId).SendAsync(
            "ReceiveTutorAudio",
            new TutorAudioEventDto(conversationId, turnId, base64Audio24k, interactionStatus),
            ct);
    }

    public async Task SendInteractionStatusAsync(string conversationId, string status, string model, CancellationToken ct = default)
    {
        await _tutorHub.Clients.Group(conversationId).SendAsync(
            "ReceiveInteractionStatus",
            new
            {
                conversationId,
                status,
                model
            },
            ct);
    }

    public async Task SendLanguageCorrectionAsync(string conversationId, LanguageCorrectionDto correction, CancellationToken ct = default)
    {
        await _tutorHub.Clients.Group(conversationId).SendAsync("ReceiveLanguageCorrection", correction, ct);
    }

    public async Task SendTranscribeTranscriptAsync(
        string conversationId,
        string speaker,
        string turnId,
        int sequenceOrder,
        string text,
        bool isFinal,
        CancellationToken ct = default)
    {
        await _transcribeHub.Clients.Group(conversationId).SendAsync(
            "ReceiveTranscriptLine1",
            new TranscribeEventDto(conversationId, speaker, turnId, sequenceOrder, text, isFinal, DateTimeOffset.UtcNow),
            ct);
    }

    public async Task SendTranslateTextAsync(
        string conversationId,
        string speaker,
        string turnId,
        int sequenceOrder,
        string text,
        bool isFinal,
        CancellationToken ct = default)
    {
        await _translateHub.Clients.Group(conversationId).SendAsync(
            "ReceiveTranslationLine2",
            new TranslateEventDto(conversationId, speaker, turnId, sequenceOrder, text, isFinal, DateTimeOffset.UtcNow),
            ct);
    }
}
