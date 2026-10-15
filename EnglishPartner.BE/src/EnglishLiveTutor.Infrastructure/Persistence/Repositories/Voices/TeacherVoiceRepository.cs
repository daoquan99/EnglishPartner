using EnglishLiveTutor.Domain.Voices;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Voices;

public sealed class TeacherVoiceRepository : ITeacherVoiceRepository
{
    private readonly AppDbContext _db;

    public TeacherVoiceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TeacherVoiceEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        string cleanId = id.Trim();
        return await _db.TeacherVoices
            .FirstOrDefaultAsync(v => v.Id == cleanId, ct);
    }

    public async Task<TeacherVoiceEntity?> GetByIdIncludingDeletedAsync(string id, CancellationToken ct = default)
    {
        string cleanId = id.Trim();
        return await _db.TeacherVoices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == cleanId, ct);
    }

    public async Task<List<TeacherVoiceEntity>> ListVoicesAsync(
        bool onlyEnabled = true,
        bool onlyFeatured = false,
        string? gender = null,
        CancellationToken ct = default)
    {
        var q = _db.TeacherVoices.AsNoTracking().AsQueryable();

        if (onlyEnabled)
        {
            q = q.Where(v => v.IsEnabled);
        }

        if (onlyFeatured)
        {
            q = q.Where(v => v.IsFeatured);
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            string cleanGender = gender.Trim();
            q = q.Where(v => v.Gender == cleanGender);
        }

        return await q
            .OrderByDescending(v => v.IsFeatured)
            .ThenBy(v => v.DisplayOrder)
            .ThenBy(v => v.DisplayName)
            .ToListAsync(ct);
    }

    public Task AddAsync(TeacherVoiceEntity voice, CancellationToken ct = default)
    {
        _db.TeacherVoices.Add(voice);
        return Task.CompletedTask;
    }
}
