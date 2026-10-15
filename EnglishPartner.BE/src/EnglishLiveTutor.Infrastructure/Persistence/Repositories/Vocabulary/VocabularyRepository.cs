using EnglishLiveTutor.Application.Features.Vocabulary.Ports;
using EnglishLiveTutor.Domain.Vocabulary.GenerationRuns;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Vocabulary;

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

    public async Task<VocabularyEntity?> GetByNormalizedWordAsync(
        string normalizedWord,
        string languageCode = "en",
        CancellationToken ct = default)
    {
        string norm = VocabularyEntity.NormalizeWord(normalizedWord);
        return await _db.Vocabularies
            .Include(v => v.Senses)
            .FirstOrDefaultAsync(v => v.NormalizedWord == norm && v.LanguageCode == languageCode, ct);
    }

    public async Task<List<VocabularyEntity>> GetByNormalizedWordsWithSensesAsync(
        string languageCode,
        IReadOnlyCollection<string> normalizedWords,
        CancellationToken ct = default)
    {
        if (normalizedWords.Count == 0)
            return new List<VocabularyEntity>();

        var words = normalizedWords
            .Select(VocabularyEntity.NormalizeWord)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct()
            .ToList();

        return await _db.Vocabularies
            .Include(v => v.Senses)
            .Where(v => v.LanguageCode == languageCode && words.Contains(v.NormalizedWord))
            .ToListAsync(ct);
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

    public Task AddSenseAsync(VocabularySenseEntity sense, CancellationToken ct = default)
    {
        if (_db.Entry(sense).State == EntityState.Detached)
        {
            _db.VocabularySenses.Add(sense);
        }
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

    public void RemoveFlashcardDetails(VocabularyEntity vocabulary, VocabularySenseEntity? sense)
    {
        _db.VocabularyPronunciations.RemoveRange(vocabulary.Pronunciations);
        _db.VocabularyWordFamilies.RemoveRange(vocabulary.WordFamilies);
        if (sense is null) return;
        _db.VocabularyRelations.RemoveRange(sense.Relations);
        _db.VocabularyCollocations.RemoveRange(sense.Collocations);
        _db.VocabularyExamples.RemoveRange(sense.Examples);
        _db.VocabularyGrammarPatterns.RemoveRange(sense.GrammarPatterns);
    }
}

public sealed class IsolatedVocabularyGenerationFailureAuditWriter : IVocabularyGenerationFailureAuditWriter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;

    public IsolatedVocabularyGenerationFailureAuditWriter(
        IDbContextFactory<AppDbContext> dbFactory,
        TimeProvider? timeProvider = null)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task RecordFailureAsync(
        string userId,
        string modelCode,
        string promptVersion,
        int requestedCount,
        string errorMessage,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.ActorOverride = string.IsNullOrWhiteSpace(userId) ? "system" : userId.Trim();
        var audit = new VocabularyGenerationAuditEntity(userId, modelCode, promptVersion, requestedCount);
        audit.MarkFailed(errorMessage, _timeProvider.GetUtcNow());
        db.VocabularyGenerationAudits.Add(audit);
        await db.SaveChangesAsync(ct);
    }
}
