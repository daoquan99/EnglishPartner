using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;

public sealed class GetDailyVocabularyPlanQueryHandler : IQueryHandler<GetDailyVocabularyPlanQuery, DailyVocabularyPlanResponseDto?>
{
    private readonly IVocabularyDbContext _db;
    private readonly IPronunciationScoreReader _scoreReader;

    public GetDailyVocabularyPlanQueryHandler(
        IVocabularyDbContext db,
        IPronunciationScoreReader scoreReader)
    {
        _db = db;
        _scoreReader = scoreReader;
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

        return await VocabularyProjectionHelper.BuildDailyPlanResponseAsync(_db, _scoreReader, plan.Id, query.UserId, ct);
    }
}
