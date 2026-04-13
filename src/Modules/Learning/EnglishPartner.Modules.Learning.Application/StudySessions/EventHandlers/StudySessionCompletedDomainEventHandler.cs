using EnglishPartner.Modules.Learning.Domain.StudySessions.Events;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents; // Required since the event is currently registered here
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Events;
using EnglishPartner.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.EventHandlers;

internal sealed class StudySessionCompletedDomainEventHandler(
    IUserLearningProfileRepository profileRepository,
    IEventBus eventBus,
    ILogger<StudySessionCompletedDomainEventHandler> logger) : INotificationHandler<DomainEventNotification<StudySessionCompletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<StudySessionCompletedDomainEvent> notificationWrapper, CancellationToken cancellationToken)
    {
        var notification = notificationWrapper.DomainEvent;
        var profile = await profileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile is null)
        {
            logger.LogWarning("UserLearningProfile not found for UserId: {UserId}", notification.UserId);
            return;
        }

        // Logic Gamification
        // EX: Earn Base XP + Bonus depend on Score
        var baseXP = notification.TotalItems * 5;
        var percentage = notification.TotalItems > 0 ? (double)notification.Score / notification.TotalItems : 0;
        var bonusXP = percentage >= 0.8 ? 10 : 0; // 80%+ gets bonus

        var xpEarned = baseXP + bonusXP;
        
        var oldStreak = profile.CurrentStreak;

        profile.AddXp(xpEarned);
        
        // This calculates the Streaks!
        profile.RecordActivity(DateTime.UtcNow);

        profileRepository.Update(profile);

        if (profile.CurrentStreak > oldStreak)
        {
            await eventBus.PublishAsync(new StreakUpdatedIntegrationEvent(profile.UserId, profile.CurrentStreak, profile.LongestStreak), cancellationToken);
        }

        logger.LogInformation("UserId {UserId} earned {XpEarned} XP and updated streaks for completing StudySession {StudySessionId}", notification.UserId, xpEarned, notification.StudySessionId);
    }
}
