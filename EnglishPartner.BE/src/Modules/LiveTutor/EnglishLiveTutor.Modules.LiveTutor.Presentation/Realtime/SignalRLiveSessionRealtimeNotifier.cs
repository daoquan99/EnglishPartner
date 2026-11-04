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
    private readonly IHubContext<GrammarCorrectionHub> _grammarHub;

    public SignalRLiveSessionRealtimeNotifier(
        IHubContext<TutorConversationHub> tutorHub,
        IHubContext<TranscribeLiveHub> transcribeHub,
        IHubContext<TranslateLiveHub> translateHub,
        IHubContext<GrammarCorrectionHub> grammarHub)
    {
        _tutorHub = tutorHub;
        _transcribeHub = transcribeHub;
        _translateHub = translateHub;
        _grammarHub = grammarHub;
    }

    private static (string GroupName, string ConversationId) ResolveGroupAndConversation(string target)
    {
        int colonIdx = target.IndexOf(':');
        if (colonIdx >= 0)
        {
            return (target, target[(colonIdx + 1)..]);
        }
        return (target, target);
    }

    public async Task SendTutorInterruptedAsync(string conversationId, CancellationToken ct = default)
    {
        var (groupName, convId) = ResolveGroupAndConversation(conversationId);
        await _tutorHub.Clients.Group(groupName).SendAsync("TutorInterrupted", convId, ct);
    }

    public async Task SendTutorAudioAsync(string conversationId, string turnId, string base64Audio24k, string interactionStatus, CancellationToken ct = default)
    {
        var (groupName, convId) = ResolveGroupAndConversation(conversationId);
        await _tutorHub.Clients.Group(groupName).SendAsync(
            "ReceiveTutorAudio",
            new TutorAudioEventDto(convId, turnId, base64Audio24k, interactionStatus),
            ct);
    }

    public async Task SendInteractionStatusAsync(string conversationId, string status, string model, CancellationToken ct = default)
    {
        var (groupName, convId) = ResolveGroupAndConversation(conversationId);
        await _tutorHub.Clients.Group(groupName).SendAsync(
            "ReceiveInteractionStatus",
            new
            {
                conversationId = convId,
                status,
                model
            },
            ct);
    }

    public async Task SendLanguageCorrectionAsync(string conversationId, LanguageCorrectionDto correction, CancellationToken ct = default)
    {
        var (groupName, convId) = ResolveGroupAndConversation(conversationId);
        var grammarGroup = groupName.StartsWith("grammar:", StringComparison.Ordinal)
            ? groupName
            : $"grammar:{groupName}";
        var payload = correction.ConversationId.Contains(':')
            ? correction with { ConversationId = convId }
            : correction;
        await _grammarHub.Clients.Group(grammarGroup).SendAsync("ReceiveLanguageCorrection", payload, ct);
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
        var (groupName, convId) = ResolveGroupAndConversation(conversationId);
        await _transcribeHub.Clients.Group(groupName).SendAsync(
            "ReceiveTranscriptLine1",
            new TranscribeEventDto(convId, speaker, turnId, sequenceOrder, text, isFinal, DateTimeOffset.UtcNow),
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
        var (groupName, convId) = ResolveGroupAndConversation(conversationId);
        await _translateHub.Clients.Group(groupName).SendAsync(
            "ReceiveTranslationLine2",
            new TranslateEventDto(convId, speaker, turnId, sequenceOrder, text, isFinal, DateTimeOffset.UtcNow),
            ct);
    }
}
