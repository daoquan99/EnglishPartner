namespace EnglishLiveTutor.Application.Common.Models;

public sealed record TopicDto(
    Guid Id,
    string Code,
    string Name,
    string DisplayNameVi,
    string Category,
    string? ModeCode,
    string SuggestedCefrLevel,
    bool SupportsConversation,
    bool SupportsVocabulary,
    string Description,
    bool IsFeatured,
    bool IsActive,
    int DisplayOrder
);

public sealed record UpsertTopicRequest(
    string Code,
    string Name,
    string DisplayNameVi,
    string Category = "General",
    string? ModeCode = null,
    string SuggestedCefrLevel = "B1",
    bool SupportsConversation = true,
    bool SupportsVocabulary = true,
    string Description = "",
    bool IsFeatured = false,
    bool IsActive = true,
    int DisplayOrder = 100
);

public sealed record SpeechSpeedOptionDto(
    double Value,
    string Label,
    string DescriptionVi,
    string RecommendedCefrRange,
    bool IsDefault
);
