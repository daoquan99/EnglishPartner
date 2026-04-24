using EnglishPartner.Messaging;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Email;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Notifications.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Handles daily goal reminder events published by DailyGoalReminderJob in the Learning module.
/// Morning: always sends a study nudge.
/// Evening: sends a goal-reminder if not yet met, or a practice encouragement if already met.
/// Sends both in-app notification (SignalR) and email.
/// </summary>
internal sealed class DailyGoalReminderIntegrationEventHandler
    : IdempotentIntegrationEventHandler<DailyGoalReminderIntegrationEvent, NotificationDbContext>
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationPusher _pusher;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DailyGoalReminderIntegrationEventHandler> _logger;

    public DailyGoalReminderIntegrationEventHandler(
        NotificationDbContext dbContext,
        INotificationPusher pusher,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<DailyGoalReminderIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _dbContext   = dbContext;
        _pusher      = pusher;
        _emailSender = emailSender;
        _logger      = logger;
    }

    protected override async Task HandleInternalAsync(
        DailyGoalReminderIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var (title, message) = BuildMessage(notification);

        _logger.LogInformation(
            "DailyGoalReminderIntegrationEventHandler: sending {Type} reminder to UserId {UserId}.",
            notification.ReminderType, notification.UserId);

        // 1. In-app notification (SignalR push)
        var push = Notification.Create(notification.UserId, title, message, NotificationType.Reminder);
        _dbContext.Notifications.Add(push);
        await _pusher.PushAsync(push, cancellationToken);

        // 2. Email reminder
        if (!string.IsNullOrWhiteSpace(notification.Email))
        {
            var html = $"""
                <h2>{title}</h2>
                <p>{message}</p>
                <p style="color:#888;font-size:12px;">— EnglishPartner</p>
                """;

            await _emailSender.SendAsync(new EmailMessage(
                To: notification.Email,
                Subject: title,
                HtmlBody: html,
                PlainTextBody: message
            ), cancellationToken);
        }
    }

    private static (string Title, string Message) BuildMessage(DailyGoalReminderIntegrationEvent e)
    {
        if (e.ReminderType == ReminderType.Morning)
        {
            return (
                "Time to study! ☀️",
                $"Start your {e.DailyGoalMinutes}-minute session today. Consistency builds fluency!");
        }

        // Evening
        if (e.MinutesStudied < e.DailyGoalMinutes)
        {
            var remaining = e.DailyGoalMinutes - e.MinutesStudied;
            return (
                "Almost there! 📚",
                $"You've studied {e.MinutesStudied} min today. Just {remaining} more to hit your daily goal!");
        }

        return (
            "Goal reached! 🎉",
            "Great work today! Keep the streak going with some extra vocabulary review.");
    }
}
