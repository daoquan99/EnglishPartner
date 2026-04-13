using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Streaks.Queries.GetStreak;

internal sealed class GetStreakQueryHandler(
    IStreakRepository streakRepository) : IQueryHandler<GetStreakQuery, StreakResponse>
{
    public async Task<Result<StreakResponse>> Handle(GetStreakQuery request, CancellationToken cancellationToken)
    {
        var streak = await streakRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (streak is null)
            return Result.Failure<StreakResponse>(IdentityErrors.Streak.NotFound);

        return new StreakResponse(streak.CurrentStreak, streak.LongestStreak, streak.LastActivityDate, streak.StreakFreezes);
    }
}
