using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;

public sealed record GetVocabularyGenerationModelsQuery() : IQuery<List<AiModelConfigEntity>>;

public sealed class GetVocabularyGenerationModelsQueryHandler : IQueryHandler<GetVocabularyGenerationModelsQuery, List<AiModelConfigEntity>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetVocabularyGenerationModelsQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<AiModelConfigEntity>> HandleAsync(GetVocabularyGenerationModelsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.AiModelCatalog
            .AsNoTracking()
            .Where(m => m.SupportsTextGeneration && !m.SupportsLiveAudio)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }
}
