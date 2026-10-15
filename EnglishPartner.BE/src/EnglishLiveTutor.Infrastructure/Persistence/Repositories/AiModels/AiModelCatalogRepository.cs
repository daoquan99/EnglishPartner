using EnglishLiveTutor.Domain.AiCatalog;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.AiModels;

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
            .FirstOrDefaultAsync(m => m.ModelCode == modelCode, ct);
    }

    public Task<AiModelConfigEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.AiModelCatalog.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task AddAsync(AiModelConfigEntity model, CancellationToken ct = default)
    {
        _db.AiModelCatalog.Add(model);
        return Task.CompletedTask;
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
