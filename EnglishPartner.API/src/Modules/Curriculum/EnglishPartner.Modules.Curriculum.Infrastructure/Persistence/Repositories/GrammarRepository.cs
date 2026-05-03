using EnglishPartner.Modules.Curriculum.Domain.Grammars;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class GrammarRepository(CurriculumDbContext dbContext) : IGrammarRepository
{
    public async Task<Grammar?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Grammars
            .Include(g => g.Examples)
                .ThenInclude(e => e.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Grammar>> GetByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken)
        => await dbContext.Grammars.AsNoTracking().Where(x => x.LanguageCode == languageCode).OrderBy(x => x.Title).ToListAsync(cancellationToken);

    public async Task<(List<Grammar> Items, int TotalCount)> GetPagedLookupAsync(string languageCode, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Grammars.AsNoTracking().Where(x => x.LanguageCode == languageCode);
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(x => EF.Functions.ILike(x.Title, $"%{searchTerm}%"));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<List<Grammar>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.Grammars.AsNoTracking().Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public void Add(Grammar grammar) => dbContext.Grammars.Add(grammar);
    public void Update(Grammar grammar) => dbContext.Grammars.Update(grammar);
    public void Remove(Grammar grammar, Guid? deletedBy = null, DateTime? deletedAt = null)
        => grammar.SoftDelete(deletedBy, deletedAt);
}

