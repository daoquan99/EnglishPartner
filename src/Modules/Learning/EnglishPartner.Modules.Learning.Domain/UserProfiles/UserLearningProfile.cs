using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.UserProfiles;

/// <summary>
/// Tracks a user's overall learning progress, streaks, and XP.
/// Created when a UserRegisteredIntegrationEvent is received.
/// </summary>
public sealed class UserLearningProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string TargetLanguageCode { get; private set; } = "en";
    public string CurrentLevel { get; private set; } = "A1";
    public int TotalXp { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastActivityDate { get; private set; }

    private UserLearningProfile() { }

    public static UserLearningProfile Create(Guid userId, string targetLanguageCode = "en")
    {
        return new UserLearningProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetLanguageCode = targetLanguageCode.ToLowerInvariant(),
            CurrentLevel = "A1",
            TotalXp = 0,
            CurrentStreak = 0,
            LongestStreak = 0,
            LastActivityDate = null
        };
    }

    public void AddXp(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        TotalXp += amount;
    }

    public void RecordActivity(DateTime activityDate)
    {
        var date = activityDate.Date;
        if (!LastActivityDate.HasValue)
        {
            CurrentStreak = 1;
            LongestStreak = 1;
        }
        else
        {
            var daysDifference = (date - LastActivityDate.Value.Date).Days;
            if (daysDifference == 1)
            {
                CurrentStreak++;
                if (CurrentStreak > LongestStreak) LongestStreak = CurrentStreak;
            }
            else if (daysDifference > 1)
            {
                CurrentStreak = 1;
            }
            // if daysDifference == 0, streak is maintained but not increased.
        }

        LastActivityDate = date;
    }

    public void ChangeLevel(string newLevel)
    {
        CurrentLevel = newLevel;
    }
}
