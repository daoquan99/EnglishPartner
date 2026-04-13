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
        => await dbContext.Vocabularies.Where(x => x.TopicId == topicId).ToListAsync(cancellationToken);

    public async Task<List<Vocabulary>> GetByLanguageCodeAsync(string languageCode, int page, int pageSize, CancellationToken cancellationToken)
        => await dbContext.Vocabularies
            .Where(x => x.LanguageCode == languageCode)
            .OrderBy(x => x.Word)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByLanguageCodeAsync(string languageCode, CancellationToken cancellationToken)
        => await dbContext.Vocabularies.CountAsync(x => x.LanguageCode == languageCode, cancellationToken);

    public async Task<List<Vocabulary>> SearchAsync(string query, string languageCode, CancellationToken cancellationToken)
        => await dbContext.Vocabularies
            .Where(x => x.LanguageCode == languageCode && (x.Word.Contains(query) || x.Definition.Contains(query)))
            .Take(50)
            .ToListAsync(cancellationToken);

    public void Add(Vocabulary vocabulary) => dbContext.Vocabularies.Add(vocabulary);
    public void Update(Vocabulary vocabulary) => dbContext.Vocabularies.Update(vocabulary);
    public void Remove(Vocabulary vocabulary) => vocabulary.SoftDelete();
}
