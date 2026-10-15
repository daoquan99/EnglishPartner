namespace EnglishLiveTutor.Domain.AiCatalog.Repositories;

public interface IAiModelCatalogRepository
{
    Task<AiModelConfigEntity?> GetByModelCodeAsync(string modelCode, CancellationToken ct = default);
    Task<List<AiModelConfigEntity>> ListTextGenerationModelsAsync(CancellationToken ct = default);
    Task<List<AiModelConfigEntity>> ListNonLiveAudioModelsAsync(CancellationToken ct = default);
}
