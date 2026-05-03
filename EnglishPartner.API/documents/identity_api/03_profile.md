# Identity API — Profile

> **Auth:** Bearer token required  
> **Base URL:** `/api/v1/profile`

---

## 1. Lấy thông tin cá nhân

```http
GET /api/v1/profile/me
```

**Response `200 OK`:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "nativeLanguage": "vi",
  "targetLanguage": "en",
  "proficiencyLevel": "Beginner",
  "learningGoal": "Communication",
  "dailyGoalMinutes": 15,
  "avatarUrl": null,
  "dateOfBirth": null,
  "morningReminderHour": 8,
  "eveningReminderHour": 21,
  "utcOffsetHours": 7,
  "isOnboardingCompleted": true
}
```

> 💡 Nếu user chưa có profile → các field trả default (`targetLanguage: "en"`, `dailyGoalMinutes: 15`, còn lại `null`).

---

## 2. Cập nhật thông tin cá nhân

```http
PUT /api/v1/profile/me
```

> Tất cả fields đều **optional** — chỉ gửi field nào muốn sửa.  
> 💡 **Upsert:** Nếu user chưa có profile, server tự tạo mới.  
> 💡 Các dropdown cho `proficiencyLevel`, `learningGoal`, `nativeLanguage`, `targetLanguage`, `dailyGoalMinutes`, `morningReminderHour`, `eveningReminderHour` → **lấy options từ `GET /api/v1/onboarding/questions`** theo `key` tương ứng.

**Request Body:**
```json
{
  "displayName": "Tên mới",
  "nativeLanguage": "vi",
  "targetLanguage": "en",
  "proficiencyLevel": "Intermediate",
  "learningGoal": "Business",
  "dailyGoalMinutes": 30,
  "avatarUrl": "https://...",
  "dateOfBirth": "1995-06-15",
  "morningReminderHour": 7,
  "eveningReminderHour": null,
  "utcOffsetHours": 7
}
```

| Field | Type | Required | Options từ Questions |
|---|---|---|---|
| `displayName` | string | ❌ | — (free text) |
| `nativeLanguage` | string | ❌ | `native_language` |
| `targetLanguage` | string | ❌ | `target_language` |
| `proficiencyLevel` | string | ❌ | `proficiency_level` |
| `learningGoal` | string | ❌ | `learning_goal` |
| `dailyGoalMinutes` | int | ❌ | `daily_goal` |
| `avatarUrl` | string | ❌ | — (URL) |
| `dateOfBirth` | date | ❌ | — (date picker) |
| `morningReminderHour` | int? | ❌ | `morning_reminder_hour` (null = tắt) |
| `eveningReminderHour` | int? | ❌ | `evening_reminder_hour` (null = tắt) |
| `utcOffsetHours` | int | ❌ | — (auto detect) |

**Response `204 No Content`**

---

## 3. Danh sách tài khoản MXH đã liên kết

```http
GET /api/v1/profile/me/external-logins
```

**Response `200 OK`:**
```json
[
  {
    "provider": "Google",
    "providerKey": "google-uid-123",
    "linkedAt": "2025-04-01T08:00:00Z"
  }
]
```

---

## 4. Xóa liên kết mạng xã hội

```http
DELETE /api/v1/profile/me/external-logins/{provider}
```

| Path Param | Mô tả |
|---|---|
| `provider` | `Google` hoặc `Facebook` |

**Response `204 No Content`**  
**Error:** `400` — `Identity.ExternalLogin.CannotRemoveLast` (user chưa cài password mà muốn gỡ Social login duy nhất)
