# Notification — Daily Goal Reminder (Background)

> ⚠️ Đây **không phải API** mà là **background flow** chạy tự động phía server.  
> Document này giúp FE/team hiểu cơ chế nhắc nhở hằng ngày.

---

## Flow tổng quan

```
User set morningReminderHour / eveningReminderHour trong Profile
       ↓
Identity module → UserProfileUpdatedDomainEvent → IntegrationEvent
       ↓
Learning module sync vào UserLearningProfile (lưu reminderHour + utcOffset)
       ↓
Quartz: DailyGoalReminderJob (chạy mỗi giờ, phút 0)
  ├─ Query: tìm users có reminderHour khớp UTC hour hiện tại
  ├─ Tính minutesStudied hôm nay từ ActivityLogs
  └─ Publish DailyGoalReminderIntegrationEvent
       ↓
Notification module nhận event
  → Tạo Notification record (type = Reminder)
  → Push realtime qua SignalR Hub
```

---

## Nội dung thông báo

| Thời điểm | Điều kiện | Title | Message |
|---|---|---|---|
| Morning | Luôn gửi | "Time to study! ☀️" | "Start your {X}-minute session today. Consistency builds fluency!" |
| Evening | `minutesStudied < dailyGoal` | "Almost there! 📚" | "You've studied {X} min today. Just {Y} more to hit your daily goal!" |
| Evening | `minutesStudied >= dailyGoal` | "Goal reached! 🎉" | "Great work today! Keep the streak going with some extra vocabulary review." |

---

## Các file liên quan (Backend)

| Module | File | Vai trò |
|---|---|---|
| Learning | `BackgroundJobs/DailyGoalReminderJob.cs` | Quartz job, chạy mỗi giờ |
| Learning | `Contracts/IntegrationEvents/LearningIntegrationEvents.cs` | `DailyGoalReminderIntegrationEvent` |
| Learning | `Domain/UserProfiles/Repositories/IUserLearningProfileRepository.cs` | `GetUsersForReminderAsync()` |
| Notification | `IntegrationEventHandlers/DailyGoalReminderIntegrationEventHandler.cs` | Nhận event → tạo notification → push |
| Identity | `Profile/Commands/UpdateMyProfile` | User thay đổi reminderHour |

---

## Cách user bật/tắt

- **Bật:** `PUT /api/v1/profile/me` gửi `morningReminderHour: 8` hoặc `eveningReminderHour: 21`
- **Tắt:** Gửi `morningReminderHour: null` hoặc `eveningReminderHour: null`
- **Onboarding:** Chọn `"none"` → server nhận `null` → không schedule reminder

---

## Lưu ý kỹ thuật

- Job dùng **UTC hour** để match. Khi user set `morningReminderHour: 8` (local) + `utcOffsetHours: 7`, server tính `8 - 7 = 1` (UTC hour 1) để schedule.
- Nếu user chưa set reminder (null) → không bao giờ bị match → không nhận notification.
- FE nhận notification qua **SignalR Hub** (`NotificationReceived` event) — xem `02_hub.md`.
