using EnglishLiveTutor.Domain.Topics;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Topics;

public sealed class TopicRepository : ITopicRepository
{
    private readonly AppDbContext _db;

    public TopicRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<TopicEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Topics.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TopicEntity?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return Task.FromResult<TopicEntity?>(null);
        string norm = code.Trim().ToLowerInvariant();
        return _db.Topics.FirstOrDefaultAsync(t => t.Code == norm, ct);
    }

    public Task<TopicEntity?> GetByIdOrCodeIncludingDeletedAsync(Guid? id, string code, CancellationToken ct = default)
    {
        string norm = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (id.HasValue && id.Value != Guid.Empty)
        {
            Guid targetId = id.Value;
            return _db.Topics
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == targetId || t.Code == norm, ct);
        }

        return _db.Topics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == norm, ct);
    }

    public Task AddAsync(TopicEntity topic, CancellationToken ct = default)
    {
        _db.Topics.Add(topic);
        return Task.CompletedTask;
    }
}
