using EnglishPartner.Modules.Learning.Contracts.Services;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Learning.Infrastructure.Services;

/// <summary>
/// Module Interface implementation — allows other modules (e.g. AI) to query Learning Profile metadata
/// without coupling to Learning's internal domains.
/// </summary>
internal sealed class LearningProfileQueryService(LearningDbContext context) : ILearningProfileQueryService
{
    public async Task<UserLearningLevelDto?> GetUserLevelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await context.UserLearningProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null) return null;

        return new UserLearningLevelDto(
            CurrentLevel: profile.CurrentLevel,
            TargetLanguage: profile.TargetLanguageCode,
            TotalXp: profile.TotalXp,
            CurrentStreak: profile.CurrentStreak);
    }
}
