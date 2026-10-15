using EnglishLiveTutor.Domain.Vocabulary.GenerationRuns;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies;

public interface IVocabularyRepository
{
    Task<VocabularyEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<VocabularyEntity?> GetByNormalizedWordAsync(string normalizedWord, string languageCode = "en", CancellationToken ct = default);
    Task<List<VocabularyEntity>> GetByNormalizedWordsWithSensesAsync(string languageCode, IReadOnlyCollection<string> normalizedWords, CancellationToken ct = default);
    Task<List<string>> ListAllNormalizedWordsAsync(CancellationToken ct = default);
    Task AddAsync(VocabularyEntity vocabulary, CancellationToken ct = default);
    Task AddSenseAsync(VocabularySenseEntity sense, CancellationToken ct = default);
    Task UpdateAsync(VocabularyEntity vocabulary, CancellationToken ct = default);
    void RemoveFlashcardDetails(VocabularyEntity vocabulary, VocabularySenseEntity? sense);
    Task AddGenerationAuditAsync(VocabularyGenerationAuditEntity audit, CancellationToken ct = default);
}
