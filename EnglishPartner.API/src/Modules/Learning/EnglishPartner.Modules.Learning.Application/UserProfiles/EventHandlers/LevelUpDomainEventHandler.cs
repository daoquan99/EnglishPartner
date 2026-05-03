using EnglishPartner.Modules.Learning.Domain.UserProfiles.Events;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
using EnglishPartner.SharedKernel.Events;
using EnglishPartner.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.EventHandlers;

/// <summary>
/// Converts LevelUpDomainEvent (in-process) into a LevelUpIntegrationEvent
/// published via the Outbox for cross-module consumption.
/// </summary>
internal sealed class LevelUpDomainEventHandler(
    IEventBus eventBus,
    ILogger<LevelUpDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<LevelUpDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<LevelUpDomainEvent> notificationWrapper,
        CancellationToken cancellationToken)
    {
        var domainEvent = notificationWrapper.DomainEvent;

        logger.LogInformation(
            "Learning: UserId {UserId} leveled up {Previous} → {New} (TotalXp: {Xp}).",
            domainEvent.UserId, domainEvent.PreviousLevel, domainEvent.NewLevel, domainEvent.TotalXp);

        await eventBus.PublishAsync(
            new LevelUpIntegrationEvent(
                domainEvent.UserId,
                domainEvent.PreviousLevel,
                domainEvent.NewLevel,
                domainEvent.TotalXp),
            cancellationToken);
    }
}
