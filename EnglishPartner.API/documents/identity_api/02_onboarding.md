# Identity API — Onboarding

> **Auth:** Bearer token required  
> **Flow:** Chỉ gọi **1 lần** sau khi user đăng ký. Khi `isOnboardingCompleted = true` → `400 Identity.User.OnboardingAlreadyCompleted`.

---

## 1. Lấy danh sách câu hỏi Onboarding

```http
GET /api/v1/onboarding/questions
```

> ⚠️ **AllowAnonymous** — không cần Bearer token.

**Response `200 OK`:**
```json
[
  {
    "key": "native_language",
    "text": "What is your native language?",
    "options": ["vi", "en", "ja", "ko", "zh", "other"],
    "order": 1
  },
  {
    "key": "target_language",
    "text": "Which language do you want to learn?",
    "options": ["en", "ja", "ko", "zh", "other"],
    "order": 2
  },
  {
    "key": "proficiency_level",
    "text": "What is your current level?",
    "options": ["Beginner", "Elementary", "Intermediate", "UpperIntermediate", "Advanced", "Mastery"],
    "order": 3
  },
  {
    "key": "learning_goal",
    "text": "What is your main learning goal?",
    "options": ["Communication", "Academic", "Business", "Travel", "ExamPrep", "Other"],
    "order": 4
  },
  {
    "key": "daily_goal",
    "text": "How many minutes per day do you want to learn?",
    "options": ["5", "10", "15", "30", "60"],
    "order": 5
  },
  {
    "key": "morning_reminder_hour",
    "text": "When should we remind you in the morning?",
    "options": ["none", "6", "7", "8", "9", "10", "11", "12"],
    "order": 6
  },
  {
    "key": "evening_reminder_hour",
    "text": "When should we remind you in the evening?",
    "options": ["none", "18", "19", "20", "21", "22", "23", "24"],
    "order": 7
  }
]
```

> 💡 FE dùng danh sách này để render wizard onboarding động. `key` tương ứng với field trong `POST /complete`.

---

## 2. Hoàn tất Onboarding

```http
POST /api/v1/onboarding/complete
```

**Request Body:**
```json
{
  "nativeLanguage": "vi",
  "targetLanguage": "en",
  "proficiencyLevel": "Beginner",
  "learningGoal": "Communication",
  "dailyGoalMinutes": 15,
  "morningReminderHour": 8,
  "eveningReminderHour": 21
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `nativeLanguage` | string | ✅ | Ngôn ngữ mẹ đẻ |
| `targetLanguage` | string | ✅ | Ngôn ngữ muốn học |
| `proficiencyLevel` | string | ✅ | CEFR level (lấy từ questions) |
| `learningGoal` | string | ✅ | Mục tiêu học (lấy từ questions) |
| `dailyGoalMinutes` | int | ✅ | Phút học mỗi ngày |
| `morningReminderHour` | int? | ❌ | Giờ nhắc buổi sáng (null = tắt) |
| `eveningReminderHour` | int? | ❌ | Giờ nhắc buổi tối (null = tắt) |

> ⚠️ Chọn `"none"` trong wizard → gửi `null`.  
> ⚠️ Chỉ gọi được 1 lần. Lần thứ 2 → `400 Identity.User.OnboardingAlreadyCompleted`.

**Response `204 No Content`**

---

## Ghi chú cho Frontend

1. **Flow:** `GET /questions` → Render wizard → User điền → `POST /complete`
2. **Sau khi complete:** FE set `isOnboardingCompleted = true` trong auth store, redirect sang trang chính.
3. **Không dùng questions cho Settings:** Trang Settings/Profile dùng `PUT /api/v1/profile/me` trực tiếp, không cần gọi lại `/questions`.
