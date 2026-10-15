using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;

public sealed record ChatMessageDto(
    string Id,
    string ConversationId,
    string TurnId,
    string Speaker,
    string TranscriptLine1,
    string TranslationLine2,
    bool IsFinal,
    int SequenceOrder,
    DateTimeOffset CreatedAtUtc
);

public sealed record LanguageCorrectionDto(
    string Id,
    string ConversationId,
    string TurnId,
    string OriginalText,
    string CorrectedText,
    string ErrorCategory,
    string ExplanationVi,
    string CefrTip,
    DateTimeOffset CreatedAtUtc
);

public sealed record ConversationDetailDto(
    string Id,
    string Title,
    string CefrLevel,
    bool ExtendedThinking,
    string VoiceName,
    double SpeechSpeed,
    string ConversationMode,
    string Topic,
    string? CustomSystemPrompt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    List<ChatMessageDto> Messages,
    List<LanguageCorrectionDto> Corrections
);

public sealed record GetConversationHistoryQuery(string Id) : IQuery<ConversationDetailDto?>;
