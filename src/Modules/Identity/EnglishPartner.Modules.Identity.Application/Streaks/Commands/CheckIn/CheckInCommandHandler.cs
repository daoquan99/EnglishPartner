using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Streaks.Commands.CheckIn;

internal sealed class CheckInCommandHandler(
    IStreakRepository streakRepository) : ICommandHandler<CheckInCommand, CheckInResponse>
{
    public async Task<Result<CheckInResponse>> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var streak = await streakRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (streak is null)
            return Result.Failure<CheckInResponse>(IdentityErrors.Streak.NotFound);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = streak.CheckIn(today);
        if (result.IsFailure)
            return Result.Failure<CheckInResponse>(result.Error);

        streakRepository.Update(streak);

        return new CheckInResponse(streak.CurrentStreak, streak.LongestStreak, streak.LastActivityDate!.Value);
    }
}
