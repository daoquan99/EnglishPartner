using EnglishPartner.Modules.Curriculum.Domain.Topics;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class TopicRepository(CurriculumDbContext dbContext) : ITopicRepository
{
    public async Task<Topic?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Topics.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Topic>> GetByLanguageCodeAsync(string languageCode, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Topics.AsNoTracking().Where(x => x.LanguageCode == languageCode);
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => EF.Functions.ILike(x.Title, $"%{searchTerm}%") || 
                                     EF.Functions.ILike(x.Description ?? "", $"%{searchTerm}%"));
        }
        
        return await query.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
    }

    public void Add(Topic topic) => dbContext.Topics.Add(topic);
    public void Update(Topic topic) => dbContext.Topics.Update(topic);
    public void Remove(Topic topic, Guid? deletedBy = null, DateTime? deletedAt = null)
        => topic.SoftDelete(deletedBy, deletedAt);
}

