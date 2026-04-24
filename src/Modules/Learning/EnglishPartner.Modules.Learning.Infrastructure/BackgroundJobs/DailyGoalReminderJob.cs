using EnglishPartner.Messaging;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using ContractsReminderType = EnglishPartner.Modules.Learning.Contracts.IntegrationEvents.ReminderType;
using DomainReminderType = EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories.ReminderType;

namespace EnglishPartner.Modules.Learning.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs every hour at minute 0. Finds users whose morning or evening reminder hour
/// matches the current UTC hour, calculates minutes studied today, and publishes
/// DailyGoalReminderIntegrationEvent for each via Outbox.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DailyGoalReminderJob(
    IUserLearningProfileRepository profileRepository,
    LearningDbContext dbContext,
    IEventBus eventBus,
    TimeProvider timeProvider,
    ILogger<DailyGoalReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var utcNow     = timeProvider.GetUtcNow().UtcDateTime;
        var utcHour    = utcNow.Hour;
        var todayUtc   = DateOnly.FromDateTime(utcNow);
        var ct         = context.CancellationToken;

        var targets = await profileRepository.GetUsersForReminderAsync(utcHour, ct);
        if (targets.Count == 0)
        {
            logger.LogDebug("DailyGoalReminderJob: no users have a reminder scheduled for UTC hour {Hour}.", utcHour);
            return;
        }

        logger.LogInformation(
            "DailyGoalReminderJob: found {Count} reminder(s) for UTC hour {Hour}.", targets.Count, utcHour);

        foreach (var target in targets)
        {
            var minutesStudied = await GetMinutesStudiedTodayAsync(target.UserId, todayUtc, ct);

            await eventBus.PublishAsync(
                new DailyGoalReminderIntegrationEvent(
                    target.UserId,
                    target.Email,
                    target.ReminderType == DomainReminderType.Morning
                        ? ContractsReminderType.Morning
                        : ContractsReminderType.Evening,
                    minutesStudied,
                    target.DailyGoalMinutes),
                ct);
        }

        // Save outbox messages created by eventBus.PublishAsync
        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<int> GetMinutesStudiedTodayAsync(Guid userId, DateOnly todayUtc, CancellationToken ct)
    {
        var totalSeconds = await dbContext.ActivityLogs
            .Where(x => x.UserId == userId
                     && DateOnly.FromDateTime(x.Timestamp) == todayUtc)
            .SumAsync(x => (long)x.TimeSpentSeconds, ct);

        return (int)(totalSeconds / 60);
    }
}
