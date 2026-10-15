using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;

public sealed record GetDailyVocabularyPlanQuery(
    string UserId,
    string? TimeZoneId,
    int? Target
) : IQuery<DailyVocabularyPlanResponseDto?>;
