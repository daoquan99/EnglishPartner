---
trigger: manual
---

# Notification — Core Architecture

> **Scope:** Tự động tạo thông báo, dọn dẹp dữ liệu cũ, Repository  
> **@mention khi làm:** background job cleanup, event handler, notification repository

---

## 1. Creating Notifications (From Events)

Module Notification phản ứng lại các sự kiện từ module khác (Identity, Learning) thông qua `IdempotentIntegrationEventHandler<TEvent, NotificationDbContext>` (giúp chống trùng lặp qua Inbox pattern).

### Integration Events Consumed
| Event | Nguồn (Module) | Hành động |
|---|---|---|
| `UserRegisteredIntegrationEvent` | Identity | Tạo welcome notification |
| `StreakUpdatedIntegrationEvent` | Learning | Tạo streak achievement notification |
| `LevelUpIntegrationEvent` | Learning | Tạo level-up congratulation notification |
| `DailyGoalReminderIntegrationEvent` | Learning | Tạo study reminder notification (morning/evening) |

**Ví dụ xử lý Event:**
```csharp
var notification = Notification.Create(userId, title, message, NotificationType.Achievement);
_dbContext.Notifications.Add(notification);
await _pusher.PushAsync(notification, cancellationToken);
// SaveChanges được gọi ngầm bởi class cha IdempotentIntegrationEventHandler
```

---

## 2. Cleanup Job (`NotificationCleanupJob`)

Là một background job dùng **Quartz** để dọn rác (vì thông báo sinh ra liên tục sẽ làm đầy DB).

- **Đăng ký:** Nằm ở `Program.cs` (central Quartz config) — KHÔNG nằm trong module Infrastructure DI.
- **Trigger:** Chạy mỗi `Notifications:CleanupIntervalHours` giờ (mặc định 24h).
- **Điều kiện:** Kiểm tra `Notifications:EnableAutoCleanup` lúc runtime, nếu false thì skip.
- **Cutoff:** Cắt ở mức `utcNow - KeepHistoryDays` (mặc định 30 ngày).
- **Thực thi:** Gọi `ExecuteDeleteAsync` để xóa cứng hàng loạt, không tracking.

---

## 3. Repository Key Methods (`INotificationRepository`)

Dùng các EF Core bulk extensions để tối ưu hiệu suất thao tác trên lượng dữ liệu lớn.

- `GetByUserIdPagedAsync(userId, page, pageSize)`: Ordered sao cho tin chưa đọc lên đầu, sau đó mới đến mới nhất.
- `CountByUserIdAsync(userId)`: Trả về total count phục vụ phân trang.
- `CountUnreadByUserIdAsync(userId)`: Lấy số lượng unread để hiển thị badge số đỏ ở icon cái chuông.
- `BulkDeleteOldNotificationsAsync(cutoffDate)`: Dùng `ExecuteDeleteAsync` (Không tracking, xóa cứng).
- `MarkAllAsReadByUserIdAsync(userId)`: Dùng `ExecuteUpdateAsync` (Update bulk, không tracking).
