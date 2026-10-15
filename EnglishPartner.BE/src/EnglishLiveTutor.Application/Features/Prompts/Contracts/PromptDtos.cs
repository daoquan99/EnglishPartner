namespace EnglishLiveTutor.Application.Features.Prompts.Contracts;

public sealed record UpsertCefrLevelPromptRequest(
    string DisplayName,
    string LevelGuidance,
    int DisplayOrder = 1,
    bool IsActive = true
);

public sealed record CefrLevelPromptDto(
    Guid Id,
    string CefrLevel,
    string DisplayName,
    string LevelGuidance,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy
);

public sealed record UpsertConversationModePromptRequest(
    string ModeCode,
    string Category,
    string DisplayName,
    string DirectiveTemplate,
    int DisplayOrder = 1,
    bool IsDefault = false,
    bool IsActive = true
);

public sealed record ConversationModePromptDto(
    Guid Id,
    string ModeCode,
    string Category,
    string DisplayName,
    string DirectiveTemplate,
    int DisplayOrder,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy
);

public sealed record BuildSystemPromptPreviewRequest(
    string CefrLevel = "B1",
    bool ExtendedThinking = false,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ConversationMode = "daily_conversation",
    string Topic = "",
    string? CustomSystemPrompt = null
);

public sealed record SpeechSpeedOptionDto(
    Guid Id,
    string Code,
    double Value,
    string Label,
    string DescriptionVi,
    string RecommendedCefrRange,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder
);

public sealed record UpsertSpeechSpeedRequest(
    string Code,
    double Value,
    string Label,
    string DescriptionVi,
    string RecommendedCefrRange = "B1 - B2",
    bool IsDefault = false,
    bool IsActive = true,
    int DisplayOrder = 1
);
