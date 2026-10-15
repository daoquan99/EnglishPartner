namespace EnglishLiveTutor.Application.Features.AiModels.Contracts;

public sealed record UpsertAiModelConfigRequest(
    string DisplayName,
    string ModelCode,
    string? RoutedModelCode = null,
    bool SupportsTextGeneration = true,
    bool SupportsStructuredOutput = true,
    bool SupportsAudioInput = false,
    bool SupportsPronunciationAssessment = false,
    bool SupportsLiveAudio = false,
    bool SupportsTranscription = false,
    bool SupportsTranslation = false,
    bool SupportsStreaming = true,
    bool IsAvailable = true,
    bool IsDeprecated = false,
    bool IsEnabled = true,
    string CapabilityNote = "",
    int DisplayOrder = 10
);

public sealed record AiModelConfigDto(
    Guid Id,
    string DisplayName,
    string ModelCode,
    string? RoutedModelCode,
    bool SupportsTextGeneration,
    bool SupportsStructuredOutput,
    bool SupportsAudioInput,
    bool SupportsPronunciationAssessment,
    bool SupportsLiveAudio,
    bool SupportsTranscription,
    bool SupportsTranslation,
    bool SupportsTts,
    bool SupportsStreaming,
    bool IsAvailable,
    bool IsDeprecated,
    bool IsEnabled,
    string? CapabilityNote,
    int DisplayOrder,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy
);
