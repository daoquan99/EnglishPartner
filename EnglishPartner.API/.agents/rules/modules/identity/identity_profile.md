---
trigger: manual
---

# Identity — Profile & Onboarding

> **Scope:** Onboarding, Profile CRUD, External Login management  
> **@mention khi làm:** Onboarding, Profile, Avatar, DateOfBirth, Reminder, External Login, Social Account

---

## 1. POST /api/v1/onboarding/complete

Gọi ngay sau khi register lần đầu. Bắt buộc trước khi vào app.

```
Auth: Bearer <accessToken>
Body:
{
  "nativeLanguage": "vi",            // ISO 639-1
  "targetLanguage": "en",
  "proficiencyLevel": "Beginner",    // xem bảng bên dưới
  "learningGoal": "Communication",   // xem bảng bên dưới
  "dailyGoalMinutes": 15,            // 5–120
  "morningReminderHour": 8,          // 0–23, null = tắt
  "eveningReminderHour": 21          // 0–23, null = tắt
}

→ CompleteOnboardingCommand
→ profile.Update(...)
→ user.CompleteOnboarding(...)
    → RaiseDomainEvent(OnboardingCompletedDomainEvent)
        → OnboardingCompletedIntegrationEvent → Learning module

Response 204 NoContent
```

**ProficiencyLevel values:**

| Value | CEFR |
|---|---|
| `Beginner` | A1 |
| `Elementary` | A2 |
| `Intermediate` | B1 |
| `UpperIntermediate` | B2 |
| `Advanced` | C1 |
| `Mastery` | C2 |

**LearningGoal values:**
`"Communication"`, `"Career & Business"`, `"Travel"`, `"Education"`, `"Exam Preparation"`, `"Just for fun"`

**Lỗi:** `Identity.User.OnboardingAlreadyCompleted` (400)

---

## 2. GET /api/v1/profile/me

```
Auth: Bearer <accessToken>

Response 200:
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "avatarUrl": null,
  "dateOfBirth": null,
  "nativeLanguage": "vi",
  "targetLanguage": "en",
  "proficiencyLevel": "Beginner",
  "learningGoal": "Communication",
  "dailyGoalMinutes": 15,
  "morningReminderHour": 8,
  "eveningReminderHour": 21,
  "isOnboardingCompleted": true,
  "currentStreak": 5,
  "longestStreak": 12
}
```

---

## 3. PUT /api/v1/profile/me

Tất cả fields đều **optional** — chỉ gửi fields muốn thay đổi.

```
Auth: Bearer <accessToken>
Body (tất cả optional):
{
  "displayName": "Tên mới",
  "nativeLanguage": "vi",
  "targetLanguage": "en",
  "proficiencyLevel": "Intermediate",
  "learningGoal": "Career & Business",
  "dailyGoalMinutes": 30,
  "avatarUrl": "https://...",
  "dateOfBirth": "1990-01-15T00:00:00Z",
  "morningReminderHour": 7,
  "eveningReminderHour": 20,
  "utcOffsetHours": 7
}

Response 204 NoContent
```

---

## 4. External Login

### GET /api/v1/profile/me/external-logins

Lấy danh sách tài khoản mạng xã hội đã liên kết.

```
Auth: Bearer <accessToken>

Response 200:
[
  { "provider": "Google", "providerKey": "google-uid-123", "linkedAt": "2025-04-01T08:00:00Z" }
]
```

### DELETE /api/v1/profile/me/external-logins/{provider}

Gỡ liên kết tài khoản mạng xã hội.

```
Auth: Bearer <accessToken>

Response 204 NoContent
```

**Lỗi:**
- `Identity.ExternalLogin.NotFound` (404) — Provider chưa được liên kết
- `Identity.ExternalLogin.CannotRemoveLast` (400) — Không còn password và đây là login method duy nhất

---

## 5. Domain Events Published

| DomainEvent | Raised by | → Integration Event |
|---|---|---|
| `UserRegisteredDomainEvent` | `User.Create()` | `UserRegisteredIntegrationEvent` → Learning, Notifications |
| `OnboardingCompletedDomainEvent` | `User.CompleteOnboarding(...)` | `OnboardingCompletedIntegrationEvent` → Learning |
| `UserProfileUpdatedDomainEvent` | `User.RaiseProfileUpdated(...)` | → Learning (sync StudyPreferences: dailyGoalMinutes, reminderHours, utcOffsetHours) |
| `StreakUpdatedDomainEvent` | CheckIn handler | → Notifications (streak milestone push) |
