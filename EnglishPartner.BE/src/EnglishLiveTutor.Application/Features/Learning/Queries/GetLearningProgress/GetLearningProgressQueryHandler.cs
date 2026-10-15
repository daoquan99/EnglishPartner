using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;

public sealed class GetLearningProgressQueryHandler : IQueryHandler<GetLearningProgressQuery, LearningProgressDto>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetLearningProgressQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
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

        int totalAssessments = await db.PronunciationAssessments.CountAsync(a => a.UserId == userId, ct);
        decimal? avgPronunciation = await db.PronunciationAssessments
            .Where(a => a.UserId == userId && a.OverallScore != null)
            .AverageAsync(a => a.OverallScore, ct);

        return new LearningProgressDto(
            userId,
            totalCatalogSenses,
            progresses.Count,
            progresses.Count(p => p.LearningStatus == "Mastered"),
            progresses.Count(p => p.LearningStatus == "Review"),
            progresses.Count(p => p.LearningStatus == "Learning"),
            progresses.Count(p => p.NextReviewAt != null && p.NextReviewAt <= now),
            progresses.Count == 0 ? 0 : Math.Round(progresses.Average(p => p.MasteryLevel), 1),
            totalAssessments,
            avgPronunciation.HasValue ? Math.Round(avgPronunciation.Value, 1) : null
        );
    }
}
