---
trigger: manual
---

# Identity — Streak

> **Scope:** Streak GET, Check-in  
> **@mention khi làm:** Streak, Check-in, Freeze token, Daily activity

---

## 1. GET /api/v1/streaks

```
Auth: Bearer <accessToken>

Response 200:
{
  "currentStreak": 5,
  "longestStreak": 12,
  "lastActivityDate": "2025-04-21",
  "freezeTokens": 2
}
```

| Field | Mô tả |
|---|---|
| `currentStreak` | Số ngày liên tiếp hiện tại |
| `longestStreak` | Streak dài nhất từ trước đến nay |
| `lastActivityDate` | Ngày check-in gần nhất (ISO date `YYYY-MM-DD`) |
| `freezeTokens` | Số lượt freeze streak còn lại |

---

## 2. POST /api/v1/streaks/check-in

Gọi 1 lần/ngày khi user hoàn thành học tập.

```
Auth: Bearer <accessToken>
(no body)

Response 200:
{
  "currentStreak": 6,
  "longestStreak": 12,
  "isNewRecord": false
}
```

**Lỗi:** `Identity.Streak.AlreadyCheckedIn` (400) — Đã check-in hôm nay rồi

---

## 3. Ghi chú kiến trúc

> Streak logic đã chuyển sang **Learning module** (`UserLearningProfile.RecordActivity()`).  
> Identity chỉ expose API để FE đọc, không còn manage streak logic.
