using EnglishPartner.Messaging;

namespace EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user's streak increases after completing a study session.
/// Consumed by: Notifications (streak milestone push).
/// </summary>
public sealed record StreakUpdatedIntegrationEvent(
    Guid UserId,
    int CurrentStreak,
    int LongestStreak) : IntegrationEvent;

/// <summary>
/// Published when a user's CEFR level increases (e.g., A1 → A2) after earning XP.
/// Consumed by: Notifications (level-up push).
/// </summary>
public sealed record LevelUpIntegrationEvent(
    Guid UserId,
    string PreviousLevel,
    string NewLevel,
    int TotalXp) : IntegrationEvent;

/// <summary>
/// Published by DailyGoalReminderJob each hour for users whose reminder time matches current UTC hour.
/// Consumed by: Notifications (morning study nudge or evening goal-check push).
/// </summary>
public sealed record DailyGoalReminderIntegrationEvent(
    Guid UserId,
    string Email,
    ReminderType ReminderType,
    int MinutesStudied,
    int DailyGoalMinutes) : IntegrationEvent;

/// <summary>Type of daily reminder. Mirrors Domain.ReminderType — kept in Contracts to avoid cross-layer coupling.</summary>
public enum ReminderType { Morning, Evening }
