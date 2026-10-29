using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Learning;

namespace EnglishLiveTutor.Application.Features.Learning.Services;

public interface IDailyVocabularyPlanInitializer
{
    Task<DailyVocabularyPlanResponseDto> InitializeOrGetPlanAsync(
        string userId,
        CancellationToken ct = default);

    Task<(DailyVocabularyPlanEntity Plan, bool IsNewlyCreated)> PrepareTodayPlanAsync(
        string userId,
        int reservedQuota = 0,
        IEnumerable<Guid>? excludedVocabularyIds = null,
        IEnumerable<Guid>? excludedSenseIds = null,
        CancellationToken ct = default);
}
