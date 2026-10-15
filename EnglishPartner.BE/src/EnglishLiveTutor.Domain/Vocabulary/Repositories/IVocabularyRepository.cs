namespace EnglishLiveTutor.Domain.Vocabulary.Repositories;

public interface IVocabularyRepository
{
    Task<VocabularyEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<VocabularyEntity?> GetByNormalizedWordAsync(string normalizedWord, string languageCode = "en", CancellationToken ct = default);
    Task<List<string>> ListAllNormalizedWordsAsync(CancellationToken ct = default);
    Task AddAsync(VocabularyEntity vocabulary, CancellationToken ct = default);
    Task UpdateAsync(VocabularyEntity vocabulary, CancellationToken ct = default);
    Task AddGenerationAuditAsync(VocabularyGenerationAuditEntity audit, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
