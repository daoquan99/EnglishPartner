using EnglishLiveTutor.Domain.Conversations.Entities;

namespace EnglishLiveTutor.Application.Features.Conversations.Ports;

/// <summary>
/// Targeted realtime persistence port for finalized/debounced transcript, translation, and grammar correction writes.
/// Implementations must create an isolated DI scope per callback so singleton realtime session state never captures
/// a scoped DbContext or live HttpContext.
/// </summary>
public interface IConversationRealtimeWriter
{
    Task<int> GetMaxSequenceOrderAsync(string conversationId, CancellationToken ct = default);

    Task EnsureConversationSettingsAsync(
        string conversationId,
        string? title,
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode,
        string topic,
        string? customSystemPrompt,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task UpsertMessageTranscriptAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullTranscript,
        bool isFinal,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task UpsertMessageTranslationAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullVietnameseText,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task SaveCorrectionAsync(
        LanguageCorrectionEntity correction,
        string? actorUserId = null,
        CancellationToken ct = default);
}
