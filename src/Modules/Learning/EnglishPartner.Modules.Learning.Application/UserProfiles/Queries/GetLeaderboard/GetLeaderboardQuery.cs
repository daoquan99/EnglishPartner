using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(int TopCount = 50) : IQuery<List<LeaderboardEntryResponse>>;

public sealed record LeaderboardEntryResponse(
    Guid UserId,
    string DisplayName,
    int TotalXp,
    string CurrentLevel,
    int CurrentStreak);
