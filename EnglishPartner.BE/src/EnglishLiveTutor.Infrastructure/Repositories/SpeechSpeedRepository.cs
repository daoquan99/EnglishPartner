using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Prompts.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class SpeechSpeedRepository(AppDbContext db) : ISpeechSpeedRepository
{
    public Task<SpeechSpeedEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SpeechSpeeds.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<SpeechSpeedEntity?> GetByCodeIncludingDeletedAsync(string code, CancellationToken ct = default) =>
        db.SpeechSpeeds.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Code == code.Trim(), ct);

    public Task<List<SpeechSpeedEntity>> ListDefaultsAsync(CancellationToken ct = default) =>
        db.SpeechSpeeds.Where(s => s.IsDefault).ToListAsync(ct);

    public Task AddAsync(SpeechSpeedEntity entity, CancellationToken ct = default)
    {
        db.SpeechSpeeds.Add(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) => await db.SaveChangesAsync(ct);
}
