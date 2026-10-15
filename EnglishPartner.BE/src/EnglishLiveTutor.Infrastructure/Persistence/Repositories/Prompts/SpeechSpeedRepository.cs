using EnglishLiveTutor.Domain.Prompts.SpeechSpeeds;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Prompts;

public sealed class SpeechSpeedRepository : ISpeechSpeedRepository
{
    private readonly AppDbContext _db;

    public SpeechSpeedRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<SpeechSpeedEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SpeechSpeeds.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<SpeechSpeedEntity?> GetByCodeIncludingDeletedAsync(string code, CancellationToken ct = default) =>
        _db.SpeechSpeeds.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Code == code.Trim(), ct);

    public Task<List<SpeechSpeedEntity>> ListDefaultsAsync(CancellationToken ct = default) =>
        _db.SpeechSpeeds.Where(s => s.IsDefault).ToListAsync(ct);

    public Task AddAsync(SpeechSpeedEntity entity, CancellationToken ct = default)
    {
        _db.SpeechSpeeds.Add(entity);
        return Task.CompletedTask;
    }
}
