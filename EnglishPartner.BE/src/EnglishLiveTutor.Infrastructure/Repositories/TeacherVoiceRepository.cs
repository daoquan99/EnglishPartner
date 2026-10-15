using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Domain.Voices.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

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

    public async Task<List<TeacherVoiceEntity>> ListVoicesAsync(
        bool onlyEnabled = true,
        bool onlyFeatured = false,
        string? gender = null,
        CancellationToken ct = default)
    {
        var (items, _) = await ListPagedVoicesAsync(
            onlyEnabled,
            onlyFeatured,
            gender,
            search: null,
            page: null,
            pageSize: null,
            ct);
        return items;
    }

    public async Task<(List<TeacherVoiceEntity> Items, int TotalCount)> ListPagedVoicesAsync(
        bool onlyEnabled = true,
        bool onlyFeatured = false,
        string? gender = null,
        string? search = null,
        int? page = null,
        int? pageSize = null,
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            q = q.Where(v =>
                v.Id.ToLower().Contains(term) ||
                v.Label.ToLower().Contains(term) ||
                v.Persona.ToLower().Contains(term) ||
                v.Style.ToLower().Contains(term) ||
                v.Gender.ToLower().Contains(term));
        }

        int totalCount = await q.CountAsync(ct);
        var ordered = q
            .OrderByDescending(v => v.IsFeatured)
            .ThenBy(v => v.DisplayOrder)
            .ThenBy(v => v.DisplayName);

        bool hasPaging = page.HasValue || pageSize.HasValue;
        if (hasPaging)
        {
            int effPage = Math.Max(1, page ?? 1);
            int effSize = Math.Clamp(pageSize ?? 20, 1, 500);
            var pagedItems = await ordered
                .Skip((effPage - 1) * effSize)
                .Take(effSize)
                .ToListAsync(ct);
            return (pagedItems, totalCount);
        }

        var allItems = await ordered.ToListAsync(ct);
        return (allItems, totalCount);
    }

    public Task AddAsync(TeacherVoiceEntity voice, CancellationToken ct = default)
    {
        _db.TeacherVoices.Add(voice);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
