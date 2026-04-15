using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Learning.Domain.UserProfiles;

/// <summary>
/// Tracks a user's overall learning progress, streaks, and XP.
/// Created when a UserRegisteredIntegrationEvent is received.
/// </summary>
public sealed class UserLearningProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string TargetLanguageCode { get; private set; } = "en";
    public string CurrentLevel { get; private set; } = "A1";
    public int TotalXp { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastActivityDate { get; private set; }

    /// <summary>CEFR level XP thresholds (ascending). Keeps domain rule centralised.</summary>
    private static readonly (int MinXp, string Level)[] LevelThresholds =
    [
        (7500, "C2"),
        (5000, "C1"),
        (3000, "B2"),
        (1500, "B1"),
        (500,  "A2"),
        (0,    "A1"),
    ];

    private UserLearningProfile() { }

    public static UserLearningProfile Create(Guid userId, string displayName, string targetLanguageCode = "en")
    {
        return new UserLearningProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = displayName,
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
        UpdateLevel();
    }

    /// <summary>Admin: reduce XP, floored at 0.</summary>
    public void SubtractXp(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        TotalXp = Math.Max(0, TotalXp - amount);
        UpdateLevel();
    }

    /// <summary>Admin: reset XP to 0 and level to A1.</summary>
    public void ResetXp()
    {
        TotalXp = 0;
        CurrentLevel = "A1";
    }

    /// <summary>Recalculates CurrentLevel based on TotalXp thresholds.</summary>
    private void UpdateLevel()
    {
        foreach (var (minXp, level) in LevelThresholds)
        {
            if (TotalXp >= minXp)
            {
                CurrentLevel = level;
                return;
            }
        }
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
