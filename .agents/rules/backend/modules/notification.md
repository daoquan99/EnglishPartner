---
trigger: manual
---

# Notification — Index

> **Scope:** In-app notifications, real-time push via SignalR  
> DB Schema: `notification`

---

## Tài liệu con

| File | Nội dung |
|---|---|
| [notification_core.md](./notification/notification_core.md) | Entity, Cleanup Job, Integration Events, Repository |
| [notification_hub.md](./notification/notification_hub.md) | SignalR Hub, Push flow |
| [notification_user.md](./notification/notification_user.md) | API User: Lấy danh sách, Mark as read |
| [notification_admin.md](./notification/notification_admin.md) | API Admin: Gửi thông báo, xóa, quản lý |

---

## Key Entities

| Entity | Type | Key Fields |
|---|---|---|
| `Notification` | AggregateRoot | UserId, Title, Message, Type (enum), IsRead, CreatedAt |

**NotificationType enum:** `System=0, Reminder=1, Achievement=2, FeatureUpdate=3`

> ⚠️ Notifications là **dữ liệu tạm thời (ephemeral)**. Hard-delete được cho phép (dùng `ExecuteDeleteAsync`). **Không dùng SoftDelete**.

---

## Lỗi (Error Codes)

- `Notification.NotFound`: Notification id không tồn tại hoặc không thuộc về UserId (lỗi ownership check).
