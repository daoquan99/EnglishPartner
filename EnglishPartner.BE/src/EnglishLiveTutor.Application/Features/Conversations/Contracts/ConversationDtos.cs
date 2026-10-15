namespace EnglishLiveTutor.Application.Features.Conversations.Contracts;

public sealed record CreateConversationRequest(
    string? Title,
    string CefrLevel = "B1",
    bool ExtendedThinking = false,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ConversationMode = "daily_conversation",
    string Topic = "",
    string? CustomSystemPrompt = null
);

public sealed record UpdateConversationSettingsRequest(
    string? Title,
    string CefrLevel = "B1",
    bool ExtendedThinking = false,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ConversationMode = "daily_conversation",
    string Topic = "",
    string? CustomSystemPrompt = null
);

public sealed record ConversationSummaryDto(
    string Id,
    string Title,
    string CefrLevel,
    bool ExtendedThinking,
    string VoiceName,
    double SpeechSpeed,
    string ConversationMode,
    string Topic,
    string? CustomSystemPrompt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    int MessageCount,
    int CorrectionCount,
    string LastMessagePreview
);

public sealed record ChatMessageDto(
    string Id,
    string ConversationId,
    string TurnId,
    string Speaker,
    string TranscriptLine1,
    string TranslationLine2,
    bool IsFinal,
    int SequenceOrder,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record LanguageCorrectionItemDto(
    string Id,
    string ConversationId,
    string TurnId,
    string OriginalText,
    string CorrectedText,
    string ErrorCategory,
    string ExplanationVi,
    string CefrTip,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
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
    List<ChatMessageDto> Messages,
    List<LanguageCorrectionItemDto> Corrections,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record StartTutorSessionRequest(
    string ConversationId,
    string ApiKey,
    string CefrLevel = "B1",
    bool ExtendedThinking = false,
    string ThinkingLevel = "medium",
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ConversationMode = "daily_conversation",
    string Topic = "",
    string? CustomSystemPrompt = null
);

public sealed record StartSubHubSessionRequest(
    string ConversationId,
    string ApiKey
);

public sealed record AudioChunkPayload(
    string ConversationId,
    string Speaker,
    string? TurnId,
    string Base64Pcm16k
);

public sealed record TranscribeEventDto(
    string ConversationId,
    string Speaker,
    string TurnId,
    int SequenceOrder,
    string FullTranscriptText,
    bool IsFinal,
    DateTimeOffset Timestamp
);

public sealed record TranslateEventDto(
    string ConversationId,
    string Speaker,
    string TurnId,
    int SequenceOrder,
    string FullVietnameseText,
    bool IsFinal,
    DateTimeOffset Timestamp
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
    DateTimeOffset Timestamp
);

public sealed record TutorAudioEventDto(
    string ConversationId,
    string TurnId,
    string Base64Pcm24k,
    string InteractionStatus
);
