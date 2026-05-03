using EnglishPartner.Modules.Learning.Domain.StudySessions.Events;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Domain.XpPolicy;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
using EnglishPartner.SharedKernel.Events;
using EnglishPartner.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.EventHandlers;

internal sealed class StudySessionCompletedDomainEventHandler(
    IUserLearningProfileRepository profileRepository,
    IEventBus eventBus,
    TimeProvider timeProvider,
    ILogger<StudySessionCompletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<StudySessionCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<StudySessionCompletedDomainEvent> notificationWrapper,
        CancellationToken cancellationToken)
    {
        var domainEvent = notificationWrapper.DomainEvent;

        var profile = await profileRepository.GetByUserIdAsync(domainEvent.UserId, cancellationToken);
        if (profile is null)
        {
            logger.LogWarning(
                "UserLearningProfile not found for UserId: {UserId} — skipping XP and streak update.",
                domainEvent.UserId);
            return;
        }

        var xpEarned  = XpPolicy.FromStudySession(domainEvent.TotalItems, domainEvent.Score);
        var oldStreak = profile.CurrentStreak;

        // AddXp internally raises LevelUpDomainEvent if CEFR level changes.
        profile.AddXp(xpEarned);
        profile.RecordActivity(timeProvider.GetUtcNow().UtcDateTime);

        profileRepository.Update(profile);

        if (profile.CurrentStreak > oldStreak)
        {
            await eventBus.PublishAsync(
                new StreakUpdatedIntegrationEvent(profile.UserId, profile.CurrentStreak, profile.LongestStreak),
                cancellationToken);
        }

        logger.LogInformation(
            "UserId {UserId} earned {XpEarned} XP (streak: {Streak}) for completing StudySession {SessionId}.",
            domainEvent.UserId, xpEarned, profile.CurrentStreak, domainEvent.StudySessionId);
    }
}
