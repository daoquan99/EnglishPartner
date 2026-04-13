using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Notifications.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Subscribes to the Identity module's UserRegisteredIntegrationEvent to create a welcome notification.
/// This runs automatically via MediatR because ProcessOutboxMessagesJob deserializes and publishes IntegrationEvents.
/// Inherits from IdempotentIntegrationEventHandler to enforce Inbox pattern (prevents duplicate execution).
/// </summary>
internal sealed class UserRegisteredIntegrationEventHandler
    : IdempotentIntegrationEventHandler<UserRegisteredIntegrationEvent, NotificationDbContext>
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationPusher _pusher;
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        NotificationDbContext dbContext,
        INotificationPusher pusher,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
        : base(dbContext, logger)
    {
        _dbContext = dbContext;
        _pusher = pusher;
        _logger = logger;
    }
    // The Inbox checks are automatically handled by the base class.
    // This HandleInternalAsync is ONLY called if the event hasn't been processed yet.
    protected override async Task HandleInternalAsync(UserRegisteredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating welcome notification for new user {UserId}", notification.UserId);

        var title = "Welcome to EnglishPartner!";
        var message = $"Hello {notification.DisplayName}, we're excited to have you onboard! Start your learning journey today.";

        var welcomeNotification = Notification.Create(
            notification.UserId,
            title,
            message,
            NotificationType.System);

        _dbContext.Notifications.Add(welcomeNotification);

        // Notify client via SignalR WebSocket immediately.
        await _pusher.PushAsync(welcomeNotification, cancellationToken);
    }
}
