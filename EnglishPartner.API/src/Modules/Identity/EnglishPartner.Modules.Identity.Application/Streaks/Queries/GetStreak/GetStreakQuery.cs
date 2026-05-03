using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Streaks.Queries.GetStreak;

public sealed record GetStreakQuery(Guid UserId) : IQuery<StreakResponse>;

public sealed record StreakResponse(int CurrentStreak, int LongestStreak, DateOnly? LastActivityDate, int StreakFreezes);
