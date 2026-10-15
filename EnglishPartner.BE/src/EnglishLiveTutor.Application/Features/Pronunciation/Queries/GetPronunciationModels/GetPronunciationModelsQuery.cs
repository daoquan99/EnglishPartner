using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;

public sealed record GetPronunciationModelsQuery() : IQuery<List<AiModelConfigEntity>>;

public sealed class GetPronunciationModelsQueryHandler : IQueryHandler<GetPronunciationModelsQuery, List<AiModelConfigEntity>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetPronunciationModelsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<AiModelConfigEntity>> HandleAsync(GetPronunciationModelsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AiModelCatalog
            .AsNoTracking()
            .Where(m => !m.SupportsLiveAudio)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }
}
