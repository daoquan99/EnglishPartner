namespace EnglishLiveTutor.Application.Common.Models;

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
