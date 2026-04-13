using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;

public sealed record GetUserDashboardQuery(Guid UserId) : IQuery<UserDashboardResponse>;

public sealed record UserDashboardResponse(
    Guid UserId,
    string CurrentLevel,
    int TotalXp,
    int CurrentStreak,
    int LongestStreak);
