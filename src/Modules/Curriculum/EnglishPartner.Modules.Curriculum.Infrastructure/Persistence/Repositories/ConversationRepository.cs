using EnglishPartner.Modules.Curriculum.Domain.Conversations;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class ConversationRepository(CurriculumDbContext dbContext) : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Conversations.Include(c => c.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Conversation>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.Conversations.Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public async Task<List<Conversation>> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken)
        => await dbContext.Conversations.Where(x => x.LanguageCode == languageCode).OrderBy(x => x.Title).ToListAsync(cancellationToken);

    public void Add(Conversation conversation) => dbContext.Conversations.Add(conversation);
    public void Update(Conversation conversation) => dbContext.Conversations.Update(conversation);
    public void Remove(Conversation conversation) => conversation.SoftDelete();
}
