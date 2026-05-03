namespace EnglishPartner.Modules.Notifications.Application.Settings;

public sealed class NotificationSettings
{
    public const string SectionName = "Notifications";

    /// <summary>
    /// If true, the background job will permanently delete old notifications.
    /// If false, old notifications will be kept forever.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// How many days before a notification is eligible for cleanup.
    /// </summary>
    public int KeepHistoryDays { get; set; } = 30;

    /// <summary>
    /// How often (in hours) the cleanup job runs.
    /// Set to 0 to disable scheduled cleanup regardless of EnableAutoCleanup.
    /// Default: 24 hours (once per day).
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}
