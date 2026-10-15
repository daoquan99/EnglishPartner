using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;

namespace EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;

public sealed class CreateAiModelConfigCommandHandler(IAiModelCatalogRepository repository)
    : ICommandHandler<CreateAiModelConfigCommand, AiModelConfigEntity>
{
    public async Task<AiModelConfigEntity> HandleAsync(CreateAiModelConfigCommand command, CancellationToken ct = default)
    {
        var req = command.Request;

        var entity = new AiModelConfigEntity(
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

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return entity;
    }
}
