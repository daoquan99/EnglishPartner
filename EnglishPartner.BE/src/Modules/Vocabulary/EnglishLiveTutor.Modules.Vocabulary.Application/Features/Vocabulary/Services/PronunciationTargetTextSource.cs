using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Services;

public sealed class PronunciationTargetTextSource : IPronunciationTargetTextSource
{
    private readonly IVocabularyDbContext _db;

    public PronunciationTargetTextSource(IVocabularyDbContext db)
    {
        _db = db;
    }

    public async Task<PronunciationAssessmentVocabularyInfo?> GetAssessmentVocabularyAsync(
        Guid vocabularyId,
        CancellationToken ct = default)
    {
        var vocabulary = await _db.Vocabularies
            .AsNoTracking()
            .Include(v => v.Pronunciations)
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .FirstOrDefaultAsync(v => v.Id == vocabularyId, ct);

        if (vocabulary is null)
            return null;

        var examples = vocabulary.Senses
            .SelectMany(s => s.Examples)
            .Select(e => new PronunciationTargetExampleInfo(e.Id, e.SenseId, e.SentenceEn))
            .ToList();

        return new PronunciationAssessmentVocabularyInfo(vocabulary.Word, examples);
    }

    public async Task<PronunciationTargetWordInfo?> FindWordAsync(
        Guid vocabularyId,
        CancellationToken ct = default)
    {
        var vocabulary = await _db.Vocabularies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vocabularyId && !v.IsDeleted, ct);

        return vocabulary is null
            ? null
            : new PronunciationTargetWordInfo(vocabulary.Id, vocabulary.Word);
    }

    public async Task<PronunciationTargetExampleInfo?> FindExampleAsync(
        Guid exampleId,
        CancellationToken ct = default)
    {
        var example = await _db.VocabularyExamples
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exampleId, ct);

        return example is null
            ? null
            : new PronunciationTargetExampleInfo(example.Id, example.SenseId, example.SentenceEn);
    }

    public async Task<PronunciationTargetSenseInfo?> FindSenseAsync(
        Guid senseId,
        CancellationToken ct = default)
    {
        var sense = await _db.VocabularySenses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == senseId, ct);

        return sense is null
            ? null
            : new PronunciationTargetSenseInfo(sense.Id, sense.VocabularyId);
    }
}
