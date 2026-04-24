using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Published when a new user registers.
/// Consumed by: Learning (create profile), Notifications (welcome).
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime RegisteredAt) : IntegrationEvent;

/// <summary>
/// Published when user completes onboarding.
/// Consumed by: Learning (set initial CEFR level + study preferences).
/// </summary>
public sealed record OnboardingCompletedIntegrationEvent(
    Guid UserId,
    string ProficiencyLevel,
    string LearningGoal,
    string TargetLanguage,
    int DailyGoalMinutes,
    int? MorningReminderHour,
    int? EveningReminderHour,
    int UtcOffsetHours) : IntegrationEvent;

/// <summary>
/// Published when user updates their profile preferences (DailyGoalMinutes, reminders, timezone).
/// Consumed by: Learning (sync StudyPreferences on UserLearningProfile).
/// </summary>
public sealed record UserProfileUpdatedIntegrationEvent(
    Guid UserId,
    int DailyGoalMinutes,
    int? MorningReminderHour,
    int? EveningReminderHour,
    int UtcOffsetHours) : IntegrationEvent;
