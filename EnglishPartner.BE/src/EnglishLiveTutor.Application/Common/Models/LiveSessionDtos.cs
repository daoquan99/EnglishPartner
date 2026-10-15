namespace EnglishLiveTutor.Application.Common.Models;

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
