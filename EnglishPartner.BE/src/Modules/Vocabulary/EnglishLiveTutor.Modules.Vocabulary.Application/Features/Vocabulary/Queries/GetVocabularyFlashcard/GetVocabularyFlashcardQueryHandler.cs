using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Vocabulary;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;

public sealed class GetVocabularyFlashcardQueryHandler : IQueryHandler<GetVocabularyFlashcardQuery, VocabularyFlashcardDto?>
{
    private readonly IModuleDbContextFactory<IVocabularyDbContext> _dbFactory;
    private readonly IPronunciationScoreReader _scoreReader;

    public GetVocabularyFlashcardQueryHandler(
        IModuleDbContextFactory<IVocabularyDbContext> dbFactory,
        IPronunciationScoreReader scoreReader)
    {
        _dbFactory = dbFactory;
        _scoreReader = scoreReader;
    }

    public async Task<VocabularyFlashcardDto?> HandleAsync(GetVocabularyFlashcardQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(
            db,
            _scoreReader,
            query.Id,
            query.SenseId,
            query.UserId,
            ct);
    }
}
