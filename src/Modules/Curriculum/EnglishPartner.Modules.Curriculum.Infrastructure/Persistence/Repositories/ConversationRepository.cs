using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class ConversationRepository(CurriculumDbContext dbContext) : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Conversations
            .Include(c => c.Lines)
            .ThenInclude(l => l.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Conversation>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.Conversations.AsNoTracking().Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public async Task<List<Conversation>> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken)
        => await dbContext.Conversations.AsNoTracking().Where(x => x.LanguageCode == languageCode).OrderBy(x => x.Title).ToListAsync(cancellationToken);

    public async Task<List<Conversation>> GetFilteredAsync(string languageCode, Guid? topicId, string? level, CancellationToken cancellationToken)
    {
        var query = dbContext.Conversations.AsNoTracking().Where(x => x.LanguageCode == languageCode);

        if (topicId.HasValue)
            query = query.Where(x => x.TopicId == topicId.Value);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);

        return await query.OrderBy(x => x.Title).ToListAsync(cancellationToken);
    }

    public void Add(Conversation conversation) => dbContext.Conversations.Add(conversation);
    public void Update(Conversation conversation) => dbContext.Conversations.Update(conversation);
    public void Remove(Conversation conversation, Guid? deletedBy = null, DateTime? deletedAt = null)
        => conversation.SoftDelete(deletedBy, deletedAt);
}

