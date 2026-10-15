using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Domain.AiCatalog;

namespace EnglishLiveTutor.Application.Features.AiModels.Mapping;

public static class AiModelMapping
{
    public static AiModelConfigDto ToDto(this AiModelConfigEntity e) => new(
        Id: e.Id,
        DisplayName: e.DisplayName,
        ModelCode: e.ModelCode,
        RoutedModelCode: e.RoutedModelCode,
        SupportsTextGeneration: e.SupportsTextGeneration,
        SupportsStructuredOutput: e.SupportsStructuredOutput,
        SupportsAudioInput: e.SupportsAudioInput,
        SupportsPronunciationAssessment: e.SupportsPronunciationAssessment,
        SupportsLiveAudio: e.SupportsLiveAudio,
        SupportsTranscription: e.SupportsTranscription,
        SupportsTranslation: e.SupportsTranslation,
        SupportsTts: e.SupportsTts,
        SupportsStreaming: e.SupportsStreaming,
        IsAvailable: e.IsAvailable,
        IsDeprecated: e.IsDeprecated,
        IsEnabled: e.IsEnabled,
        CapabilityNote: e.CapabilityNote,
        DisplayOrder: e.DisplayOrder,
        CreatedAtUtc: e.CreatedAtUtc,
        CreatedBy: e.CreatedBy,
        UpdatedAtUtc: e.UpdatedAtUtc,
        UpdatedBy: e.UpdatedBy,
        IsDeleted: e.IsDeleted,
        DeletedAtUtc: e.DeletedAtUtc,
        DeletedBy: e.DeletedBy
    );
}
