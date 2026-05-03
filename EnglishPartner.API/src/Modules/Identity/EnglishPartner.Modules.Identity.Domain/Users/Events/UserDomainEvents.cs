using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Users.Events;

/// <summary>
/// Raised when a new user registers.
/// </summary>
public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string DisplayName) : DomainEvent;

/// <summary>
/// Raised when user completes the onboarding flow.
/// Carries all fields needed to publish OnboardingCompletedIntegrationEvent without a DB re-fetch.
/// </summary>
public sealed record OnboardingCompletedDomainEvent(
    Guid UserId,
    string ProficiencyLevel,
    string LearningGoal,
    string TargetLanguage,
    int DailyGoalMinutes,
    int? MorningReminderHour,
    int? EveningReminderHour,
    int UtcOffsetHours) : DomainEvent;

/// <summary>
/// Raised when user updates their profile preferences.
/// Consumed by: Learning (sync StudyPreferences).
/// </summary>
public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId,
    int DailyGoalMinutes,
    int? MorningReminderHour,
    int? EveningReminderHour,
    int UtcOffsetHours) : DomainEvent;

/// <summary>
/// Raised when user's streak is updated via daily check-in.
/// </summary>
public sealed record StreakUpdatedDomainEvent(Guid UserId, int CurrentStreak, int LongestStreak) : DomainEvent;
