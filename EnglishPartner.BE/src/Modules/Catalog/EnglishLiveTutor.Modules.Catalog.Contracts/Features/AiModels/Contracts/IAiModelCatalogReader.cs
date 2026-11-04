namespace EnglishLiveTutor.Application.Features.AiModels.Contracts;

public interface IAiModelCatalogReader
{
    Task<AiModelInfo?> FindByModelCodeAsync(string modelCode, CancellationToken ct = default);
}
