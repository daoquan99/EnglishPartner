using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Identity.Domain.Users.Events;
using EnglishPartner.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Identity.Infrastructure.DomainEventHandlers;

/// <summary>
/// Converts OnboardingCompletedDomainEvent into a cross-module
/// OnboardingCompletedIntegrationEvent published via the Outbox.
/// Consumed by: Learning (initialize learning path with proficiency/goal).
/// </summary>
internal sealed class OnboardingCompletedDomainEventHandler(
    IEventBus eventBus,
    ILogger<OnboardingCompletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<OnboardingCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<OnboardingCompletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        logger.LogInformation(
            "Identity: publishing OnboardingCompletedIntegrationEvent for UserId {UserId}",
            domainEvent.UserId);

        await eventBus.PublishAsync(
            new OnboardingCompletedIntegrationEvent(
                domainEvent.UserId,
                domainEvent.ProficiencyLevel,
                domainEvent.LearningGoal,
                domainEvent.TargetLanguage,
                domainEvent.DailyGoalMinutes,
                domainEvent.MorningReminderHour,
                domainEvent.EveningReminderHour,
                domainEvent.UtcOffsetHours),
            cancellationToken);
    }
}
