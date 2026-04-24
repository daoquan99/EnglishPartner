using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Learning.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Consumes OnboardingCompletedIntegrationEvent to set the user's initial CEFR level
/// on their UserLearningProfile based on the self-reported proficiency from onboarding.
/// </summary>
internal sealed class OnboardingCompletedIntegrationEventHandler
    : IdempotentIntegrationEventHandler<OnboardingCompletedIntegrationEvent, LearningDbContext>
{
    private readonly IUserLearningProfileRepository _profileRepository;
    private readonly ILogger<OnboardingCompletedIntegrationEventHandler> _logger;

    public OnboardingCompletedIntegrationEventHandler(
        LearningDbContext dbContext,
        IUserLearningProfileRepository profileRepository,
        TimeProvider timeProvider,
        ILogger<OnboardingCompletedIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    protected override async Task HandleInternalAsync(OnboardingCompletedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning(
                "Learning: UserLearningProfile not found for UserId {UserId} on OnboardingCompleted — profile may not have been initialized yet.",
                notification.UserId);
            return;
        }

        // Map self-reported proficiency level to CEFR and apply to learning profile
        // ProficiencyLevel from onboarding (e.g. "beginner", "intermediate", "advanced") → CEFR
        var cefrLevel = notification.ProficiencyLevel.ToLowerInvariant() switch
        {
            "beginner"     => "A1",
            "elementary"   => "A2",
            "intermediate" => "B1",
            "upper-intermediate" => "B2",
            "advanced"     => "C1",
            "mastery"      => "C2",
            _              => "A1"
        };

        profile.ChangeLevel(cefrLevel);
        profile.UpdatePreferences(
            notification.DailyGoalMinutes,
            notification.MorningReminderHour,
            notification.EveningReminderHour,
            notification.UtcOffsetHours);
        _profileRepository.Update(profile);

        _logger.LogInformation(
            "Learning: Set initial CEFR level {Level} for UserId {UserId} based on onboarding proficiency '{Proficiency}'.",
            cefrLevel, notification.UserId, notification.ProficiencyLevel);
    }
}
