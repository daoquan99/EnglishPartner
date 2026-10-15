namespace EnglishLiveTutor.Application.Common.Models;

public sealed record UpsertCefrLevelPromptRequest(
    string DisplayName,
    string LevelGuidance,
    int DisplayOrder = 1,
    bool IsActive = true
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

public sealed record BuildSystemPromptPreviewRequest(
    string CefrLevel = "B1",
    bool ExtendedThinking = false,
    string VoiceName = "Aoede",
    double SpeechSpeed = 1.0,
    string ConversationMode = "daily_conversation",
    string Topic = "",
    string? CustomSystemPrompt = null
);

public sealed record TeacherVoiceDto(
    string Id,
    string Name,
    string DisplayName,
    string Label,
    string Type,
    string Gender,
    string Persona,
    string Style,
    string Description,
    bool IsFeatured,
    bool IsEnabled,
    int DisplayOrder
);

public sealed record UpsertTeacherVoiceRequest(
    string Name,
    string DisplayName,
    string? Label,
    string Type = "prebuilt",
    string Gender = "Female",
    string Persona = "Clear",
    string? Style = null,
    string Description = "",
    bool IsFeatured = false,
    bool IsEnabled = true,
    int DisplayOrder = 1
);
