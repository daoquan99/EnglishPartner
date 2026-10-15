using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Topics.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class TopicRepository : ITopicRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TopicRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<TopicEntity>> ListAsync(
        bool onlyActive = true,
        bool? supportsConversation = null,
        bool? supportsVocabulary = null,
        string? modeCode = null,
        string? category = null,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.Topics.AsNoTracking().AsQueryable();

        if (onlyActive)
        {
            query = query.Where(t => t.IsActive);
        }

        if (supportsConversation.HasValue)
        {
            query = query.Where(t => t.SupportsConversation == supportsConversation.Value);
        }

        if (supportsVocabulary.HasValue)
        {
            query = query.Where(t => t.SupportsVocabulary == supportsVocabulary.Value);
        }

        if (!string.IsNullOrWhiteSpace(modeCode))
        {
            string normMode = modeCode.Trim().ToLowerInvariant();
            query = query.Where(t => t.ModeCode == normMode || t.ModeCode == null);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            string normCat = category.Trim();
            query = query.Where(t => t.Category == normCat);
        }

        return await query
            .OrderByDescending(t => t.IsFeatured)
            .ThenBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<TopicEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Topics.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<TopicEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        string norm = code.Trim().ToLowerInvariant();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Topics.FirstOrDefaultAsync(t => t.Code == norm, ct);
    }

    public async Task<TopicEntity> UpsertAsync(TopicEntity topic, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Topics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == topic.Id || t.Code == topic.Code, ct);

        if (existing is null)
        {
            db.Topics.Add(topic);
            await db.SaveChangesAsync(ct);
            return topic;
        }

        existing.Restore();
        existing.UpdateDetails(
            topic.Name,
            topic.DisplayNameVi,
            topic.Category,
            topic.ModeCode,
            topic.SuggestedCefrLevel,
            topic.SupportsConversation,
            topic.SupportsVocabulary,
            topic.Description,
            topic.IsFeatured,
            topic.IsActive,
            topic.DisplayOrder);

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Topics.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (existing is null) return false;

        existing.MarkDeleted(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
