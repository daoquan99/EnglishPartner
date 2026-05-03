---
trigger: manual
---

# Notification — User Endpoints

> **Scope:** API dành cho User đọc thông báo  
> **Base URL:** `/api/v1/notifications`  
> **Auth:** Bearer  
> **@mention khi làm:** read notifications, user notifications

---

## 1. Lấy danh sách thông báo

```
GET /api/v1/notifications?page=1&pageSize=20
Auth: Bearer

Response 200: GetMyNotificationsResponse (bao gồm cả totalCount và unreadCount)
```

> **Lưu ý:**
> - UserId tự động được lấy qua `ICurrentUserService` ở trong Handler (không truyền qua Query Params).
> - `unreadCount` được Handler query song song cùng lúc với items để tiết kiệm thời gian.

---

## 2. Đánh dấu ĐÃ ĐỌC (Từng cái)

```
PATCH /api/v1/notifications/{id:guid}/read
Auth: Bearer
Response 204
```

> Kèm theo Ownership check: Nếu `notification.UserId != currentUser.UserId` thì văng lỗi `NotFound` (đảm bảo bảo mật).

---

## 3. Đánh dấu ĐÃ ĐỌC (Tất cả)

```
PATCH /api/v1/notifications/read-all
Auth: Bearer
Response 204
```

> Dùng EF Core `ExecuteUpdateAsync` ngầm ở repo để cập nhật hàng loạt.
