using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Services;

public sealed class VocabularyPronunciationProgressObserver : IPronunciationAssessmentObserver
{
    private readonly IVocabularyDbContext _db;

    public VocabularyPronunciationProgressObserver(IVocabularyDbContext db)
    {
        _db = db;
    }

    public async Task OnAssessedAsync(
        string userId,
        Guid senseId,
        decimal overallScore,
        CancellationToken ct = default)
    {
        var progress = await _db.UserVocabularyProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SenseId == senseId, ct);

        if (progress is not null)
        {
            progress.UpdateLatestPronunciationScore(overallScore);
        }
    }
}
