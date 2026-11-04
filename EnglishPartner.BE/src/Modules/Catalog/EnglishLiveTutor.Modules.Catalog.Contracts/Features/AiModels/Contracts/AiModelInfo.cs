namespace EnglishLiveTutor.Application.Features.AiModels.Contracts;

public sealed record AiModelInfo(
    string DisplayName,
    string ModelCode,
    bool SupportsTextGeneration,
    bool SupportsAudioInput,
    bool SupportsPronunciationAssessment,
    bool SupportsLiveAudio,
    bool IsAvailable,
    bool IsEnabled,
    string EffectiveModelCode);
