using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Identity.Domain.Users.Events;
using EnglishPartner.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Identity.Infrastructure.DomainEventHandlers;

/// <summary>
/// Converts the in-process UserRegisteredDomainEvent into a cross-module
/// UserRegisteredIntegrationEvent published via the outbox.
/// Consumed by: Learning (create UserLearningProfile), Notifications (welcome message).
/// </summary>
internal sealed class UserRegisteredDomainEventHandler(
    IEventBus eventBus,
    ILogger<UserRegisteredDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserRegisteredDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRegisteredDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        logger.LogInformation(
            "Identity: publishing UserRegisteredIntegrationEvent for UserId {UserId}",
            domainEvent.UserId);

        await eventBus.PublishAsync(
            new UserRegisteredIntegrationEvent(
                domainEvent.UserId,
                domainEvent.Email,
                domainEvent.DisplayName,
                DateTime.UtcNow),
            cancellationToken);
    }
}
