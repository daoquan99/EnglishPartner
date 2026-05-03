---
trigger: manual
---

# Learning — Index

> **Scope:** SRS (SM-2), StudySession, ItemMastery, Dashboard, Leaderboard, Due Reviews  
> DB Schema: `learning`

---

## Tài liệu con

| File | Nội dung |
|---|---|
| [learning_study_session.md](./learning/learning_study_session.md) | Bắt đầu / Nộp session, Submit answer, SM-2 scoring flow |
| [learning_dashboard.md](./learning/learning_dashboard.md) | Lấy Dashboard XP, Streak, Due Reviews, Leaderboard |
| [learning_admin.md](./learning/learning_admin.md) | Admin API quản lý XP, Masteries, Abort Session của user |
| [learning_core.md](./learning/learning_core.md) | Thuật toán SM-2, XP/Streak logic, Events, Cross-module interface |

---

## Key Entities & Aggregates

| Entity | Type | Key Fields |
|---|---|---|
| `UserLearningProfile` | AggregateRoot | UserId, DisplayName, TargetLanguageCode, CurrentLevel, TotalXp, CurrentStreak, LongestStreak, LastActivityDate?, Preferences (StudyPreferences owned) |
| `StudyPreferences` | Owned Entity (of Profile) | DailyGoalMinutes (default 15), MorningReminderHour?, EveningReminderHour?, UtcOffsetHours |
| `StudySession` | AggregateRoot | UserId, SessionTitle, TotalItems, Score, Status (enum: InProgress/Completed/Abandoned), StartTime, EndTime?, LastActivityTime? |
| `ItemMastery` | AggregateRoot | UserId, ItemId, Type (ItemType enum), EasinessFactor, IntervalDays, Repetitions, NextReviewDate |
| `LearningActivityLog` | **Entity** (NOT AggregateRoot) | UserId, SessionId, ItemId, UserAnswer, IsCorrect, ScoreGranted, TimeSpentSeconds, Timestamp. Immutable log — never updated or soft-deleted |
| `XpPolicy` | Static class (Domain rule) | XpPerSessionItem=5, HighScoreBonusXp=10, HighScoreThreshold=0.8, XpPerStandaloneReview=2 |

---

## Authorization

- Toàn bộ User endpoints bắt buộc `.RequireAuthorization()` (group level)
- Admin endpoints: `.RequireAuthorization("learning.admin.write")` ở group level (chỉ dùng permission string, KHÔNG có "Admin" policy riêng)

---

## Domain Events

| Domain Event | Raised by | Consumer |
|---|---|---|
| `StudySessionCompletedDomainEvent(SessionId, UserId, Score, TotalItems)` | `StudySession.Complete()` | Handler: tính XP, update streak, publish integration events |
| `LevelUpDomainEvent(ProfileId, UserId, PreviousLevel, NewLevel, TotalXp)` | `UserLearningProfile.AddXp()` | Handler: publish `LevelUpIntegrationEvent` → Notifications |

---

## Integration Events (Publish — from Learning.Contracts)

| Event | Published when | Consumer |
|---|---|---|
| `StreakUpdatedIntegrationEvent(UserId, CurrentStreak, LongestStreak)` | Streak tăng sau session complete | Notifications (streak milestone push) |
| `LevelUpIntegrationEvent(UserId, PreviousLevel, NewLevel, TotalXp)` | CEFR level tăng sau AddXp | Notifications (level-up push) |
| `DailyGoalReminderIntegrationEvent(UserId, ReminderType, MinutesStudied, DailyGoalMinutes)` | Quartz `DailyGoalReminderJob` mỗi giờ | Notifications (morning/evening study nudge) |

---

## Integration Events (Consume)

| Event | Source | Action |
|---|---|---|
| `UserRegisteredIntegrationEvent` | Identity | Khởi tạo `UserLearningProfile` |
| `OnboardingCompletedIntegrationEvent` | Identity | Set `CurrentLevel` lần đầu |
| `UserProfileUpdatedIntegrationEvent` | Identity | Sync `StudyPreferences` (dailyGoalMinutes, reminderHours, utcOffsetHours) |

---

## Cross-Module Interface (Sync)

```csharp
// Learning.Contracts/Services/ILearningProfileQueryService.cs
public interface ILearningProfileQueryService
{
    Task<UserLearningLevelDto?> GetUserLevelAsync(Guid userId, CancellationToken ct);
}

public sealed record UserLearningLevelDto(
    string CurrentLevel, string TargetLanguage, int TotalXp, int CurrentStreak);
```
> Cho phép modules khác (AI) query Learning profile metadata mà không coupling đến Domain/Application layers.

---

## Error Codes

| Code | HTTP | When |
|---|---|---|
| `Learning.Session.NotFound` | 404 | Session không tồn tại hoặc không thuộc user |
| `Learning.Session.NotInProgress` | 400 | Session đã Complete hoặc Abandoned — không thể thao tác |
| `Learning.Profile.NotFound` | 404 | Chưa có learning profile (race condition khi mới đăng ký) |
