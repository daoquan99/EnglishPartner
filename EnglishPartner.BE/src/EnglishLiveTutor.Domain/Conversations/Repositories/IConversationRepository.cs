using EnglishLiveTutor.Domain.Conversations.Entities;

namespace EnglishLiveTutor.Domain.Conversations.Repositories;

public interface IConversationRepository
{
    Task<List<object>> ListConversationsAsync(CancellationToken ct = default);
    Task<(List<object> Items, int TotalCount)> ListConversationsPagedAsync(
        string? search = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);
    Task<ConversationEntity?> GetConversationWithHistoryAsync(string id, CancellationToken ct = default);

    Task<ConversationEntity> CreateOrUpdateConversationAsync(
        string? id,
        string? title,
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        CancellationToken ct = default);

    Task<bool> DeleteConversationAsync(string id, CancellationToken ct = default);

    Task<int> GetMaxSequenceOrderAsync(string conversationId, CancellationToken ct = default);

    Task UpsertMessageTranscriptAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullTranscript,
        bool isFinal,
        CancellationToken ct = default);

    Task UpsertMessageTranslationAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullVietnameseText,
        CancellationToken ct = default);

    Task SaveCorrectionAsync(LanguageCorrectionEntity correction, CancellationToken ct = default);
}
