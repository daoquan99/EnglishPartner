using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class VocabularyRepository : IVocabularyRepository
{
    private readonly AppDbContext _db;

    public VocabularyRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<VocabularyEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Vocabularies
            .Include(v => v.Pronunciations)
            .Include(v => v.WordFamilies)
            .Include(v => v.Senses).ThenInclude(s => s.Relations)
            .Include(v => v.Senses).ThenInclude(s => s.Collocations)
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<VocabularyEntity?> GetByNormalizedWordAsync(string normalizedWord, string languageCode = "en", CancellationToken ct = default)
    {
        string norm = VocabularyEntity.NormalizeWord(normalizedWord);
        return await _db.Vocabularies
            .Include(v => v.Senses)
            .FirstOrDefaultAsync(v => v.NormalizedWord == norm && v.LanguageCode == languageCode, ct);
    }

    public async Task<List<string>> ListAllNormalizedWordsAsync(CancellationToken ct = default)
    {
        return await _db.Vocabularies
            .AsNoTracking()
            .Select(v => v.NormalizedWord)
            .ToListAsync(ct);
    }

    public Task AddAsync(VocabularyEntity vocabulary, CancellationToken ct = default)
    {
        _db.Vocabularies.Add(vocabulary);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(VocabularyEntity vocabulary, CancellationToken ct = default)
    {
        _db.Vocabularies.Update(vocabulary);
        return Task.CompletedTask;
    }

    public Task AddGenerationAuditAsync(VocabularyGenerationAuditEntity audit, CancellationToken ct = default)
    {
        _db.VocabularyGenerationAudits.Add(audit);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
