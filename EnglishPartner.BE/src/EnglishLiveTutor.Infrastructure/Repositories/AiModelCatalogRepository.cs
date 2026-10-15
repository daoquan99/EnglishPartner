using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class AiModelCatalogRepository : IAiModelCatalogRepository
{
    private readonly AppDbContext _db;

    public AiModelCatalogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AiModelConfigEntity?> GetByModelCodeAsync(string modelCode, CancellationToken ct = default)
    {
        return await _db.AiModelCatalog
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModelCode == modelCode, ct);
    }

    public async Task<List<AiModelConfigEntity>> ListTextGenerationModelsAsync(CancellationToken ct = default)
    {
        return await _db.AiModelCatalog
            .AsNoTracking()
            .Where(m => m.SupportsTextGeneration && !m.SupportsLiveAudio)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<List<AiModelConfigEntity>> ListNonLiveAudioModelsAsync(CancellationToken ct = default)
    {
        return await _db.AiModelCatalog
            .AsNoTracking()
            .Where(m => !m.SupportsLiveAudio)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(ct);
    }
}
