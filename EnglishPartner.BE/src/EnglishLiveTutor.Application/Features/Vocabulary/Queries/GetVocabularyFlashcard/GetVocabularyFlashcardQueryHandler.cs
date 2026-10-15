using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;

public sealed class GetVocabularyFlashcardQueryHandler : IQueryHandler<GetVocabularyFlashcardQuery, VocabularyFlashcardDto?>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetVocabularyFlashcardQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<VocabularyFlashcardDto?> HandleAsync(GetVocabularyFlashcardQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(
            db,
            query.Id,
            query.SenseId,
            query.UserId,
            ct);
    }
}
