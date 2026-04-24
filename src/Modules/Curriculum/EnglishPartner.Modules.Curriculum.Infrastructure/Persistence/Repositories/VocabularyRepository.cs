using EnglishPartner.Modules.Curriculum.Domain.Vocabularies;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class VocabularyRepository(CurriculumDbContext dbContext) : IVocabularyRepository
{
    public async Task<Vocabulary?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Vocabularies
            .Include(v => v.Synonyms)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<Vocabulary>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
        => await dbContext.Vocabularies.AsNoTracking().Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public async Task<List<Vocabulary>> GetByLanguageCodeAsync(string languageCode, int page, int pageSize, CancellationToken cancellationToken)
        => await dbContext.Vocabularies
            .Where(x => x.LanguageCode == languageCode)
            .OrderBy(x => x.Word)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken)
        => await dbContext.Vocabularies.CountAsync(x => x.LanguageCode == languageCode, cancellationToken);

    public async Task<(List<Vocabulary> Items, int TotalCount)> GetPagedAsync(
        string languageCode, int page, int pageSize,
        string? searchTerm, string? level, Guid? topicId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Vocabularies.AsNoTracking()
            .Where(x => x.LanguageCode == languageCode);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(x =>
                EF.Functions.ILike(x.Word, $"%{searchTerm}%") ||
                EF.Functions.ILike(x.Definition, $"%{searchTerm}%"));

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);

        if (topicId.HasValue)
            query = query.Where(x => x.TopicId == topicId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Word)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Vocabulary> Items, int TotalCount)> GetPagedLookupAsync(
        string languageCode, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Vocabularies.AsNoTracking()
            .Where(x => x.LanguageCode == languageCode);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(x => EF.Functions.ILike(x.Word, $"%{searchTerm}%"));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Word)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Vocabulary>> SearchAsync(string query, string languageCode, CancellationToken cancellationToken)
        => await dbContext.Vocabularies
            .Where(x => x.LanguageCode == languageCode &&
                        (EF.Functions.ILike(x.Word, $"%{query}%") ||
                         EF.Functions.ILike(x.Definition, $"%{query}%")))
            .Take(50)
            .ToListAsync(cancellationToken);

    public void Add(Vocabulary vocabulary) => dbContext.Vocabularies.Add(vocabulary);
    public void Update(Vocabulary vocabulary) => dbContext.Vocabularies.Update(vocabulary);
    public void Remove(Vocabulary vocabulary, Guid? deletedBy = null, DateTime? deletedAt = null)
        => vocabulary.SoftDelete(deletedBy, deletedAt);
}

