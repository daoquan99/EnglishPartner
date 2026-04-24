using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class ExampleSentenceRepository(CurriculumDbContext dbContext) : IExampleSentenceRepository
{
    public async Task<ExampleSentence?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences
        .Include(x => x.Translations)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<ExampleSentence>> GetByVocabularyIdAsync(Guid vocabularyId, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.AsNoTracking().Where(x => x.VocabularyId == vocabularyId).ToListAsync(cancellationToken);

    public async Task<List<ExampleSentence>> GetByGrammarIdAsync(Guid grammarId, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.AsNoTracking().Where(x => x.GrammarId == grammarId).ToListAsync(cancellationToken);

    public async Task<List<ExampleSentence>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.ExampleSentences.AsNoTracking().Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public async Task<(List<ExampleSentence> Items, int TotalCount)> GetPagedAsync(
        string languageCode, int page, int pageSize,
        string? searchTerm, Guid? vocabularyId, Guid? grammarId, Guid? topicId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExampleSentences.AsNoTracking()
            .Where(x => x.LanguageCode == languageCode);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(x => EF.Functions.ILike(x.Text, $"%{searchTerm}%"));

        if (vocabularyId.HasValue)
            query = query.Where(x => x.VocabularyId == vocabularyId.Value);

        if (grammarId.HasValue)
            query = query.Where(x => x.GrammarId == grammarId.Value);

        if (topicId.HasValue)
            query = query.Where(x => x.TopicId == topicId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(x => x.Translations)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(ExampleSentence exampleSentence) => dbContext.ExampleSentences.Add(exampleSentence);
    public void Update(ExampleSentence exampleSentence) => dbContext.ExampleSentences.Update(exampleSentence);
    public void Remove(ExampleSentence exampleSentence, Guid? deletedBy = null, DateTime? deletedAt = null)
        => exampleSentence.SoftDelete(deletedBy, deletedAt);
}

