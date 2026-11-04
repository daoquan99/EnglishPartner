using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Application.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.AiModels.Queries.GetPronunciationModels;

public sealed class GetPronunciationModelsQueryHandler : IQueryHandler<GetPronunciationModelsQuery, List<AiModelConfigEntity>>
{
    private readonly ICatalogDbContext _db;
    private readonly IAgyCliRunner _agyRunner;

    public GetPronunciationModelsQueryHandler(ICatalogDbContext db, IAgyCliRunner agyRunner)
    {
        _db = db;
        _agyRunner = agyRunner;
    }

    public async Task<List<AiModelConfigEntity>> HandleAsync(GetPronunciationModelsQuery query, CancellationToken ct = default)
    {
        IReadOnlyCollection<string> hiddenAgyModels = _agyRunner.IsEnabled
            ? Array.Empty<string>()
            : _agyRunner.AllowedModels;

        var models = _db.AiModelCatalog
            .AsNoTracking()
            .Where(m => !m.SupportsLiveAudio);

        if (hiddenAgyModels.Count > 0)
        {
            models = models.Where(m => !hiddenAgyModels.Contains(m.ModelCode));
        }

        return await models
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }
}
