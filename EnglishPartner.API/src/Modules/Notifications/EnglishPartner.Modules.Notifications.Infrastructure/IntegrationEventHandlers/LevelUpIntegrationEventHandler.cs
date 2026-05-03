using EnglishPartner.Messaging;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Notifications.Infrastructure.IntegrationEventHandlers;

internal sealed class LevelUpIntegrationEventHandler
    : IdempotentIntegrationEventHandler<LevelUpIntegrationEvent, NotificationDbContext>
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationPusher _pusher;
    private readonly ILogger<LevelUpIntegrationEventHandler> _logger;

    public LevelUpIntegrationEventHandler(
        NotificationDbContext dbContext,
        INotificationPusher pusher,
        TimeProvider timeProvider,
        ILogger<LevelUpIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _dbContext = dbContext;
        _pusher = pusher;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(LevelUpIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating level-up notification for UserId {UserId}: {Prev} → {New}.",
            notification.UserId, notification.PreviousLevel, notification.NewLevel);

        var title   = $"Level Up! 🎉 You reached {notification.NewLevel}";
        var message = $"Congratulations! You leveled up from {notification.PreviousLevel} to {notification.NewLevel} " +
                      $"with {notification.TotalXp:N0} total XP. Keep learning!";

        var levelUpNotification = Notification.Create(
            notification.UserId,
            title,
            message,
            NotificationType.Achievement);

        _dbContext.Notifications.Add(levelUpNotification);

        // Push immediately via SignalR WebSocket.
        await _pusher.PushAsync(levelUpNotification, cancellationToken);
    }
}
