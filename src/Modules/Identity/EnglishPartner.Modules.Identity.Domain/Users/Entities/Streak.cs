using EnglishPartner.SharedKernel.Domain;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Domain.Users.Entities;

/// <summary>
/// Tracks user's daily learning streak and freeze count.
/// </summary>
public sealed class Streak : Entity
{
    private Streak() { }

    public static Streak Create(Guid userId)
    {
        return new Streak
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStreak = 0,
            LongestStreak = 0,
            StreakFreezes = 2
        };
    }

    public Guid UserId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateOnly? LastActivityDate { get; private set; }
    public int StreakFreezes { get; private set; } = 2;

    /// <summary>
    /// Records daily check-in. Increases streak if consecutive,
    /// resets if gap > 1 day (unless freeze is available).
    /// </summary>
    public Result CheckIn(DateOnly today)
    {
        if (LastActivityDate == today)
            return Result.Failure(IdentityErrors.Streak.AlreadyCheckedIn);

        if (LastActivityDate.HasValue)
        {
            var gap = today.DayNumber - LastActivityDate.Value.DayNumber;
            if (gap == 1) CurrentStreak++;
            else if (gap == 2 && StreakFreezes > 0) { StreakFreezes--; CurrentStreak++; }
            else CurrentStreak = 1;
        }
        else CurrentStreak = 1;

        if (CurrentStreak > LongestStreak) LongestStreak = CurrentStreak;
        LastActivityDate = today;
        return Result.Success();
    }
}
