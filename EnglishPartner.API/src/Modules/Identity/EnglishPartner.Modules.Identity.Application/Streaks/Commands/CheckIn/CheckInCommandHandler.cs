using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Streaks.Commands.CheckIn;

internal sealed class CheckInCommandHandler(
    IStreakRepository streakRepository,
    TimeProvider timeProvider) : ICommandHandler<CheckInCommand, CheckInResponse>
{
    public async Task<Result<CheckInResponse>> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var streak = await streakRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var isNew = streak is null;

        if (isNew)
            streak = Streak.Create(request.UserId);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var result = streak!.CheckIn(today);
        if (result.IsFailure)
            return Result.Failure<CheckInResponse>(result.Error);

        if (isNew)
            streakRepository.Add(streak);
        else
            streakRepository.Update(streak);

        return new CheckInResponse(streak.CurrentStreak, streak.LongestStreak, streak.LastActivityDate!.Value);
    }
}
