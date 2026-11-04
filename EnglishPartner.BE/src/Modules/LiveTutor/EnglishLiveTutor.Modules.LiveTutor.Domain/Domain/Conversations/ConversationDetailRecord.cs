namespace EnglishLiveTutor.Domain.Conversations;

public sealed record ChatMessageRecord(
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

public sealed record LanguageCorrectionRecord(
    string Id,
    string ConversationId,
    string TurnId,
    string OriginalText,
    string CorrectedText,
    string ErrorCategory,
    string ExplanationVi,
    string CefrTip,
    DateTimeOffset CreatedAtUtc,
    string? Status,
    string? ImprovedText,
    string? IssuesJson
);

public sealed record ConversationDetailRecord(
    string Id,
    string UserId,
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
    IReadOnlyList<ChatMessageRecord> Messages,
    IReadOnlyList<LanguageCorrectionRecord> Corrections
);
