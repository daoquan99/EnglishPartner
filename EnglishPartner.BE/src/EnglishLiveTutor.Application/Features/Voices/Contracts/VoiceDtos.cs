namespace EnglishLiveTutor.Application.Features.Voices.Contracts;

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
