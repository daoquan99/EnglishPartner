using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.AiModels.Services;

public sealed class AiModelCatalogReader(ICatalogDbContext db) : IAiModelCatalogReader
{
    public async Task<AiModelInfo?> FindByModelCodeAsync(string modelCode, CancellationToken ct = default)
    {
        var model = await db.AiModelCatalog
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModelCode == modelCode, ct);

        return model is null
            ? null
            : new AiModelInfo(
                model.DisplayName,
                model.ModelCode,
                model.SupportsTextGeneration,
                model.SupportsAudioInput,
                model.SupportsPronunciationAssessment,
                model.SupportsLiveAudio,
                model.IsAvailable,
                model.IsEnabled,
                model.GetEffectiveModelCode());
    }
}
