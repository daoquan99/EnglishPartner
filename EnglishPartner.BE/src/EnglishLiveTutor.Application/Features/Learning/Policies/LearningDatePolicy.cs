using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;

namespace EnglishLiveTutor.Application.Features.Learning.Policies;

public static class LearningDatePolicy
{
    public static string ResolveLocalLearningDate(string? timeZoneId = "Asia/Ho_Chi_Minh", DateTimeOffset? utcNow = null) =>
        DailyVocabularyPlanEntity.ResolveLocalLearningDate(timeZoneId, utcNow);
}
