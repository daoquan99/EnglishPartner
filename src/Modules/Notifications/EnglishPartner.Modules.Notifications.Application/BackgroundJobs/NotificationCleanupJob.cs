using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace EnglishPartner.Modules.Notifications.Application.BackgroundJobs;

/// <summary>
/// Quartz background job that cleans up old notifications based on appsettings.json config.
/// Use "Notifications:EnableAutoCleanup" and "Notifications:KeepHistoryDays" to control behavior.
/// </summary>
[DisallowConcurrentExecution]
public sealed class NotificationCleanupJob(
    INotificationRepository notificationRepository,
    IOptions<NotificationSettings> options,
    TimeProvider timeProvider,
    ILogger<NotificationCleanupJob> logger) : IJob
{
    private readonly NotificationSettings _settings = options.Value;

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_settings.EnableAutoCleanup)
        {
            logger.LogDebug("NotificationCleanupJob skipped — auto-cleanup is disabled.");
            return;
        }

        var limitDate = timeProvider.GetUtcNow().UtcDateTime.AddDays(-_settings.KeepHistoryDays);

        logger.LogInformation(
            "NotificationCleanupJob started. KeepDays={Days}, CutoffDate={Cutoff}",
            _settings.KeepHistoryDays, limitDate);

        var deletedCount = await notificationRepository.BulkDeleteOldNotificationsAsync(limitDate, context.CancellationToken);

        logger.LogInformation(
            "NotificationCleanupJob completed. Deleted {Count} old notifications.", deletedCount);
    }
}
