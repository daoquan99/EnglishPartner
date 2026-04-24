using EnglishPartner.Messaging;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Notifications.Infrastructure.IntegrationEventHandlers;

internal sealed class StreakUpdatedIntegrationEventHandler
    : IdempotentIntegrationEventHandler<StreakUpdatedIntegrationEvent, NotificationDbContext>
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationPusher _pusher;
    private readonly ILogger<StreakUpdatedIntegrationEventHandler> _logger;

    public StreakUpdatedIntegrationEventHandler(
        NotificationDbContext dbContext,
        INotificationPusher pusher,
        TimeProvider timeProvider,
        ILogger<StreakUpdatedIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _dbContext = dbContext;
        _pusher = pusher;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(StreakUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating streak notification for user {UserId}. current streak: {Streak}", notification.UserId, notification.CurrentStreak);

        var title = "Streak Extended! 🔥";
        var message = $"You're on a {notification.CurrentStreak}-day streak. Keep up the great work!";

        var streakNotification = Notification.Create(
            notification.UserId,
            title,
            message,
            NotificationType.Achievement);

        _dbContext.Notifications.Add(streakNotification);

        // Notify client via SignalR WebSocket immediately.
        await _pusher.PushAsync(streakNotification, cancellationToken);
    }
}
