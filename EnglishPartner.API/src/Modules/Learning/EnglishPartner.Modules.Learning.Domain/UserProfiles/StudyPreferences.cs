namespace EnglishPartner.Modules.Learning.Domain.UserProfiles;

/// <summary>
/// Owned entity storing user study preferences on UserLearningProfile.
/// Configured as EF [Owned] in UserLearningProfileConfiguration — stored as columns
/// in learning.user_learning_profiles, no separate table.
/// </summary>
public sealed class StudyPreferences
{
    private StudyPreferences() { }

    /// <summary>Target minutes of study per day. Default 15.</summary>
    public int DailyGoalMinutes { get; private set; } = 15;

    /// <summary>Local hour (0-23) for morning study reminder. Null = disabled.</summary>
    public int? MorningReminderHour { get; private set; }

    /// <summary>Local hour (0-23) for evening study reminder. Null = disabled.</summary>
    public int? EveningReminderHour { get; private set; }

    /// <summary>UTC offset in whole hours (e.g. +7 for Vietnam). Used to convert reminder hours to UTC.</summary>
    public int UtcOffsetHours { get; private set; } = 0;

    public static StudyPreferences Default => new();

    public void Update(int dailyGoalMinutes, int? morningReminderHour, int? eveningReminderHour, int utcOffsetHours)
    {
        DailyGoalMinutes    = dailyGoalMinutes > 0 ? dailyGoalMinutes : DailyGoalMinutes;
        MorningReminderHour = morningReminderHour;
        EveningReminderHour = eveningReminderHour;
        UtcOffsetHours      = utcOffsetHours;
    }
}
