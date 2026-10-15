using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;

public sealed class GetLearningHistoryQueryHandler : IQueryHandler<GetLearningHistoryQuery, IReadOnlyList<LearningHistoryItemDto>>
{
    private readonly IAppDbContextFactory _dbFactory;

    public GetLearningHistoryQueryHandler(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<LearningHistoryItemDto>> HandleAsync(GetLearningHistoryQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var recentReviews = await db.VocabularyReviewAttempts
            .AsNoTracking()
            .Where(r => r.UserId == query.UserId)
            .OrderByDescending(r => r.ReviewedAt)
            .Take(query.Limit)
            .ToListAsync(ct);

        var vocabIds = recentReviews.Select(r => r.VocabularyId).Distinct().ToList();
        var vocabDict = await db.Vocabularies
            .AsNoTracking()
            .Where(v => vocabIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Word, ct);

        return recentReviews.Select(r => new LearningHistoryItemDto(
            r.Id,
            r.VocabularyId,
            r.SenseId,
            vocabDict.TryGetValue(r.VocabularyId, out var w) ? w : "Vocabulary",
            r.ReviewType,
            r.Rating,
            r.ReviewedAt,
            r.NextReviewAtSnapshot
        )).ToList();
    }
}
