using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Published when a new user registers.
/// Consumed by: Learning (create profile), Gamification (create profile), Notification (welcome).
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime RegisteredAt) : IntegrationEvent;

/// <summary>
/// Published when user completes onboarding.
/// Consumed by: Learning (initialize learning path).
/// </summary>
public sealed record OnboardingCompletedIntegrationEvent(
    Guid UserId,
    string ProficiencyLevel,
    string LearningGoal,
    string TargetLanguage,
    int DailyGoalMinutes) : IntegrationEvent;

/// <summary>
/// Published when user's streak changes.
/// Consumed by: Gamification (check streak achievements), Notification (milestone alert).
/// </summary>
public sealed record StreakUpdatedIntegrationEvent(
    Guid UserId,
    int CurrentStreak,
    int LongestStreak) : IntegrationEvent;
