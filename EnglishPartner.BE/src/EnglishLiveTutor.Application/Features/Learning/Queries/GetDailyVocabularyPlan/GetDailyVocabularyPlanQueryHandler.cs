using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;

public sealed class GetDailyVocabularyPlanQueryHandler : IQueryHandler<GetDailyVocabularyPlanQuery, DailyVocabularyPlanResponseDto?>
{
    private readonly IAppDbContext _db;

    public GetDailyVocabularyPlanQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<DailyVocabularyPlanResponseDto?> HandleAsync(GetDailyVocabularyPlanQuery query, CancellationToken ct = default)
    {
        string learningDate = DailyVocabularyPlanEntity.ResolveLocalLearningDate(query.TimeZoneId);
        var plan = await _db.DailyVocabularyPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == query.UserId && p.LearningDate == learningDate, ct);

        if (plan is null)
        {
            return null;
        }

        return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, plan.Id, query.UserId, ct);
    }
}
