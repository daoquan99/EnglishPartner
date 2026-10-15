namespace EnglishLiveTutor.Domain.AiCatalog;

public interface IAiModelCatalogRepository
{
    Task<AiModelConfigEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AiModelConfigEntity model, CancellationToken ct = default);
    Task<AiModelConfigEntity?> GetByModelCodeAsync(string modelCode, CancellationToken ct = default);
    Task<List<AiModelConfigEntity>> ListTextGenerationModelsAsync(CancellationToken ct = default);
    Task<List<AiModelConfigEntity>> ListNonLiveAudioModelsAsync(CancellationToken ct = default);
}
