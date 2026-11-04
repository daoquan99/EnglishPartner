using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Application.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.AiModels.Queries.GetVocabularyGenerationModels;

public sealed class GetVocabularyGenerationModelsQueryHandler : IQueryHandler<GetVocabularyGenerationModelsQuery, List<AiModelConfigEntity>>
{
    private readonly ICatalogDbContext _db;
    private readonly IAgyCliRunner _agyRunner;

    public GetVocabularyGenerationModelsQueryHandler(ICatalogDbContext db, IAgyCliRunner agyRunner)
    {
        _db = db;
        _agyRunner = agyRunner;
    }

    public async Task<List<AiModelConfigEntity>> HandleAsync(GetVocabularyGenerationModelsQuery query, CancellationToken ct = default)
    {
        IReadOnlyCollection<string> hiddenAgyModels = _agyRunner.IsEnabled
            ? Array.Empty<string>()
            : _agyRunner.AllowedModels;

        var models = _db.AiModelCatalog
            .AsNoTracking()
            .Where(m => m.SupportsTextGeneration && !m.SupportsLiveAudio);

        if (hiddenAgyModels.Count > 0)
        {
            models = models.Where(m => !hiddenAgyModels.Contains(m.ModelCode));
        }

        return await models
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }
}
