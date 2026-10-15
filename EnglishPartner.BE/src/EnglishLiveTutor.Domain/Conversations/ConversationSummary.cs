namespace EnglishLiveTutor.Domain.Conversations;

public sealed record ConversationSummary(
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
