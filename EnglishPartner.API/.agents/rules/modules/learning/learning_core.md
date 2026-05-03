---
trigger: manual
---

# Learning — Core Architecture

> **Scope:** Thuật toán SM-2, XP Policy, Streak Logic, CEFR Levels, Events, ItemType Mapping, Cross-module interface

---

## 1. SM-2 Algorithm

Thuật toán Spaced Repetition hoạt động dựa trên `QualityScore` (0-5) từ AI:

```
If QualityScore < 3:  LAPSE → reset Repetitions=0, IntervalDays=1
If QualityScore >= 3: CORRECT
  Repetitions == 0: IntervalDays = 1
  Repetitions == 1: IntervalDays = 6
  Repetitions >= 2: IntervalDays = round(previous * EasinessFactor)

EasinessFactor += 0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)
EasinessFactor = max(1.3, EasinessFactor)
```

**MasteryLevel thresholds:**
- `New` → 0 repetitions
- `Learning` → 1-2 reps, interval < 7d
- `Reviewing` → interval 7-21d
- `Mastered` → interval > 21d

---

## 2. Score Mapping (`ScoreMapper.cs`)

AI trả điểm 0-100. SM-2 cần 0-5. Mapping qua `ScoreMapper.ToSm2QualityScore(rawScore)`:

| AI Score | SM-2 Quality | Ý nghĩa |
|---|---|---|
| 90-100 | 5 | Perfect |
| 80-89 | 4 | Good |
| 60-79 | 3 | Correct with difficulty |
| 40-59 | 2 | Incorrect, easy to recall |
| 20-39 | 1 | Incorrect, vaguely remembered |
| 0-19 | 0 | Complete blackout |

---

## 3. XP Policy (`XpPolicy.cs`)

Tất cả XP logic PHẢI đi qua `XpPolicy` static class — KHÔNG inline trong handlers.

```csharp
public static class XpPolicy
{
    public const int XpPerSessionItem = 5;
    public const int HighScoreBonusXp = 10;
    public const double HighScoreThreshold = 0.8;  // 80%
    public const int XpPerStandaloneReview = 2;

    // Session XP = (totalItems * 5) + (10 if accuracy >= 80%)
    public static int FromStudySession(int totalItems, int score);

    // Standalone review = fixed 2 XP
    public static int FromStandaloneReview() => XpPerStandaloneReview;
}
```

---

## 4. CEFR Level Thresholds

XP → Level auto-promotion (trong `UserLearningProfile.UpdateLevel()`):

| Level | Min XP |
|---|---|
| C2 | 7,500 |
| C1 | 5,000 |
| B2 | 3,000 |
| B1 | 1,500 |
| A2 | 500 |
| A1 | 0 |

> Khi `AddXp()` khiến level tăng → raise `LevelUpDomainEvent` tự động.

---

## 5. Streak Logic

Nằm trong `UserLearningProfile.RecordActivity(activityDate)`:

```
1. Nếu chưa có LastActivityDate: streak = 1
2. Nếu daysDifference == 1: streak++, cập nhật longestStreak
3. Nếu daysDifference > 1: streak reset = 1
4. Nếu daysDifference == 0: giữ nguyên (không tăng)
```

> Sự kiện streak milestone → publish `StreakUpdatedIntegrationEvent` (Learning.Contracts).

---

## 6. ItemType Mapping

`ItemType` trong Learning module bắt đầu từ 0:

| Learning.ItemType | Value |
|---|---|
| `Vocabulary` | 0 |
| `Grammar` | 1 |
| `ExampleSentence` | 2 |
| `Topic` | 3 |
| `Conversation` | 4 |
| `Exercise` | 5 |

---

## 7. Domain Events (Raise)

| Domain Event | Raised by | Parameters |
|---|---|---|
| `StudySessionCompletedDomainEvent` | `StudySession.Complete()` | StudySessionId, UserId, Score, TotalItems |
| `LevelUpDomainEvent` | `UserLearningProfile.AddXp()` | ProfileId, UserId, PreviousLevel, NewLevel, TotalXp |

---

## 8. Integration Events (Publish — Learning.Contracts)

| Event | Published when | Consumer |
|---|---|---|
| `StreakUpdatedIntegrationEvent(UserId, CurrentStreak, LongestStreak)` | Streak tăng sau session complete | Notifications |
| `LevelUpIntegrationEvent(UserId, PreviousLevel, NewLevel, TotalXp)` | CEFR level tăng | Notifications |
| `DailyGoalReminderIntegrationEvent(UserId, ReminderType, MinutesStudied, DailyGoalMinutes)` | Quartz `DailyGoalReminderJob` mỗi giờ | Notifications |

> `ReminderType` enum: `Morning`, `Evening` — defined in Learning.Contracts (duplicate, NOT cross-layer reference).

---

## 9. Integration Events (Consume)

| Event | Source | Action |
|---|---|---|
| `UserRegisteredIntegrationEvent` | Identity | Khởi tạo `UserLearningProfile` (displayName, targetLanguageCode) |
| `OnboardingCompletedIntegrationEvent` | Identity | Set `CurrentLevel` lần đầu cho profile (từ proficiency level) |
| `UserProfileUpdatedIntegrationEvent` | Identity | Sync `StudyPreferences` (dailyGoalMinutes, morningReminderHour, eveningReminderHour, utcOffsetHours) |

---

## 10. Cross-Module Sync Interface

```csharp
// Learning.Contracts/Services/ILearningProfileQueryService.cs
public interface ILearningProfileQueryService
{
    Task<UserLearningLevelDto?> GetUserLevelAsync(Guid userId, CancellationToken ct);
}
```

> Cho phép AI module query current CEFR level + target language mà không coupling.
