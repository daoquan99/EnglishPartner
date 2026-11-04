using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;

public sealed class GetLearningProgressQueryHandler : IQueryHandler<GetLearningProgressQuery, LearningProgressDto>
{
    private readonly IModuleDbContextFactory<IVocabularyDbContext> _dbFactory;
    private readonly IPronunciationScoreReader _scoreReader;

    public GetLearningProgressQueryHandler(
        IModuleDbContextFactory<IVocabularyDbContext> dbFactory,
        IPronunciationScoreReader scoreReader)
    {
        _dbFactory = dbFactory;
        _scoreReader = scoreReader;
    }

    public async Task<LearningProgressDto> HandleAsync(GetLearningProgressQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var userId = query.UserId;

        var progresses = await db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        int totalCatalogSenses = await (
            from s in db.VocabularySenses.AsNoTracking()
            join v in db.Vocabularies.AsNoTracking() on s.VocabularyId equals v.Id
            select s.Id
        ).CountAsync(ct);

        var assessmentSummary = await _scoreReader.GetSummaryAsync(userId, ct);

        return new LearningProgressDto(
            userId,
            totalCatalogSenses,
            progresses.Count,
            progresses.Count(p => p.LearningStatus == "Mastered"),
            progresses.Count(p => p.LearningStatus == "Review"),
            progresses.Count(p => p.LearningStatus == "Learning"),
            progresses.Count(p => p.NextReviewAt != null && p.NextReviewAt <= now),
            progresses.Count == 0 ? 0 : Math.Round(progresses.Average(p => p.MasteryLevel), 1),
            assessmentSummary.TotalCount,
            assessmentSummary.AverageScore.HasValue ? Math.Round(assessmentSummary.AverageScore.Value, 1) : null
        );
    }
}
