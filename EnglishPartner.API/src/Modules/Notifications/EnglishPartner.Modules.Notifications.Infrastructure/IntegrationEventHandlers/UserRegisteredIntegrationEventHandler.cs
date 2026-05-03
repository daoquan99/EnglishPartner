using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Domain.Notifications;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using EnglishPartner.SharedKernel.Email;
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
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        NotificationDbContext dbContext,
        INotificationPusher pusher,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _dbContext = dbContext;
        _pusher = pusher;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(UserRegisteredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating welcome notification for new user {UserId}", notification.UserId);

        var title = "Welcome to EnglishPartner!";
        var message = $"Hello {notification.DisplayName}, we're excited to have you onboard! Start your learning journey today.";

        // 1. In-app notification (SignalR push)
        var welcomeNotification = Notification.Create(
            notification.UserId,
            title,
            message,
            NotificationType.System);

        _dbContext.Notifications.Add(welcomeNotification);
        await _pusher.PushAsync(welcomeNotification, cancellationToken);

        // 2. Welcome email
        var html = $"""
            <h2>Welcome to EnglishPartner! 🎉</h2>
            <p>Hi {notification.DisplayName},</p>
            <p>We're excited to have you onboard! Here's how to get started:</p>
            <ul>
                <li>Complete your onboarding to set your daily study goal</li>
                <li>Start your first conversation practice</li>
                <li>Track your streak and earn XP</li>
            </ul>
            <p>Consistency is key — even 10 minutes a day makes a huge difference!</p>
            <p style="color:#888;font-size:12px;">— The EnglishPartner Team</p>
            """;

        await _emailSender.SendAsync(new EmailMessage(
            To: notification.Email,
            Subject: title,
            HtmlBody: html,
            PlainTextBody: message
        ), cancellationToken);
    }
}
