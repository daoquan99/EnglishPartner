using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(int TopCount = 50) : ICacheableQuery<List<LeaderboardEntryResponse>>
{
    public string CacheKey   => $"leaderboard:top{(TopCount > 0 && TopCount <= 100 ? TopCount : 50)}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}


public sealed record LeaderboardEntryResponse(
    Guid UserId,
    string DisplayName,
    int TotalXp,
    string CurrentLevel,
    int CurrentStreak);
