using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Identity.Domain.Users.Events;
using EnglishPartner.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Identity.Infrastructure.DomainEventHandlers;

/// <summary>
/// Converts UserProfileUpdatedDomainEvent into a cross-module
/// UserProfileUpdatedIntegrationEvent published via the Outbox.
/// Consumed by: Learning (sync StudyPreferences — DailyGoalMinutes, reminder hours, timezone).
/// </summary>
internal sealed class UserProfileUpdatedDomainEventHandler(
    IEventBus eventBus,
    ILogger<UserProfileUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserProfileUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserProfileUpdatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        logger.LogInformation(
            "Identity: publishing UserProfileUpdatedIntegrationEvent for UserId {UserId}.",
            domainEvent.UserId);

        await eventBus.PublishAsync(
            new UserProfileUpdatedIntegrationEvent(
                domainEvent.UserId,
                domainEvent.DailyGoalMinutes,
                domainEvent.MorningReminderHour,
                domainEvent.EveningReminderHour,
                domainEvent.UtcOffsetHours),
            cancellationToken);
    }
}
