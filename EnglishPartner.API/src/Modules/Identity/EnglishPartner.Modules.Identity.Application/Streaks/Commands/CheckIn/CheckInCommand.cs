using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Streaks.Commands.CheckIn;

public sealed record CheckInCommand(Guid UserId) : ICommand<CheckInResponse>;

public sealed record CheckInResponse(int CurrentStreak, int LongestStreak, DateOnly LastActivityDate);
