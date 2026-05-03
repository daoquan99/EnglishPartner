using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Consumes UserProfileUpdatedIntegrationEvent to sync study preferences
/// (DailyGoalMinutes, reminder hours, timezone) onto UserLearningProfile.
/// </summary>
internal sealed class UserProfileUpdatedIntegrationEventHandler
    : IdempotentIntegrationEventHandler<UserProfileUpdatedIntegrationEvent, LearningDbContext>
{
    private readonly IUserLearningProfileRepository _profileRepository;
    private readonly ILogger<UserProfileUpdatedIntegrationEventHandler> _logger;

    public UserProfileUpdatedIntegrationEventHandler(
        LearningDbContext dbContext,
        IUserLearningProfileRepository profileRepository,
        TimeProvider timeProvider,
        ILogger<UserProfileUpdatedIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(
        UserProfileUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning(
                "Learning: UserLearningProfile not found for UserId {UserId} on UserProfileUpdated — skipping preferences sync.",
                notification.UserId);
            return;
        }

        profile.UpdatePreferences(
            notification.DailyGoalMinutes,
            notification.MorningReminderHour,
            notification.EveningReminderHour,
            notification.UtcOffsetHours);

        _profileRepository.Update(profile);

        _logger.LogInformation(
            "Learning: Synced StudyPreferences for UserId {UserId} — Goal: {Goal}min, Morning: {Morning}h, Evening: {Evening}h, UTC+{Offset}.",
            notification.UserId, notification.DailyGoalMinutes,
            notification.MorningReminderHour, notification.EveningReminderHour, notification.UtcOffsetHours);
    }
}
