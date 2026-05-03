# Notification API — User Endpoints

> **Auth:** Bearer token required  
> **Base URL:** `/api/v1/notifications`

---

## 1. Lấy danh sách thông báo (Paged)

```http
GET /api/v1/notifications?page=1&pageSize=20
```

| Query Param | Type | Required | Default |
|---|---|---|---|
| `page` | int | ❌ | 1 |
| `pageSize` | int | ❌ | 20 |

> `UserId` tự động lấy từ Bearer token.

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "title": "Welcome to EnglishPartner!",
      "message": "Start your English learning journey today.",
      "type": 0,
      "isRead": false,
      "createdAt": "2026-04-23T10:00:00Z"
    }
  ],
  "totalCount": 25,
  "unreadCount": 3
}
```

**`type` values (NotificationType):**

| Value | Name | Mô tả |
|---|---|---|
| 0 | System | Thông báo hệ thống |
| 1 | Reminder | Nhắc nhở học tập |
| 2 | Achievement | Thành tích (streak, level up) |
| 3 | FeatureUpdate | Tính năng mới |

> 💡 `unreadCount` dùng để hiển thị badge số đỏ trên icon chuông.

---

## 2. Đánh dấu đã đọc (Từng cái)

```http
PATCH /api/v1/notifications/{id}/read
```

**Response `204 No Content`**

> ⚠️ Ownership check: Nếu notification không thuộc user hiện tại → `404 Notification.NotFound`.

---

## 3. Đánh dấu đã đọc (Tất cả)

```http
PATCH /api/v1/notifications/read-all
```

**Response `204 No Content`**

> Bulk update — đánh dấu tất cả thông báo chưa đọc của user.
