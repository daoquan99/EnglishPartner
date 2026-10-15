using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;

public sealed record LearningProgressDto(
    string UserId,
    int TotalCatalogSenses,
    int TotalStudied,
    int MasteredCount,
    int ReviewCount,
    int LearningCount,
    int DueForReviewNow,
    double AverageMastery,
    int TotalPronunciationAssessments,
    decimal? AveragePronunciationScore
);

public sealed record GetLearningProgressQuery(string UserId) : IQuery<LearningProgressDto>;
