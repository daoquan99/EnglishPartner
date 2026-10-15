using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;

public sealed class UpdateAiModelConfigCommandHandler(IAiModelCatalogRepository repository)
    : ICommandHandler<UpdateAiModelConfigCommand, AiModelConfigEntity?>
{
    public async Task<AiModelConfigEntity?> HandleAsync(UpdateAiModelConfigCommand command, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(command.Id, ct);
        if (entity is null) return null;
        var req = command.Request;
        entity.UpdateConfiguration(
            displayName: string.IsNullOrWhiteSpace(req.DisplayName) ? req.ModelCode : req.DisplayName,
            modelCode: req.ModelCode,
            routedModelCode: req.RoutedModelCode,
            supportsTextGeneration: req.SupportsTextGeneration,
            supportsStructuredOutput: req.SupportsStructuredOutput,
            supportsAudioInput: req.SupportsAudioInput,
            supportsPronunciationAssessment: req.SupportsPronunciationAssessment,
            supportsLiveAudio: req.SupportsLiveAudio,
            supportsTranscription: req.SupportsTranscription,
            supportsTranslation: req.SupportsTranslation,
            supportsStreaming: req.SupportsStreaming,
            isAvailable: req.IsAvailable,
            isDeprecated: req.IsDeprecated,
            isEnabled: req.IsEnabled,
            capabilityNote: req.CapabilityNote,
            displayOrder: req.DisplayOrder);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
