# Notification API — Admin

> **Auth:** Bearer + `notifications.admin` permission  
> **Base URL:** `/api/v1/admin/notifications`

---

## 1. Lấy tất cả thông báo (Paginated)

```http
GET /api/v1/admin/notifications?userId=guid&isRead=false&page=1&pageSize=20
```

| Query Param | Type | Required | Default |
|---|---|---|---|
| `userId` | guid | ❌ | null (tất cả users) |
| `isRead` | bool | ❌ | null (tất cả) |
| `page` | int | ❌ | 1 |
| `pageSize` | int | ❌ | 20 |

**Response `200 OK`:** Paginated list

---

## 2. Lấy thông báo của một User

```http
GET /api/v1/admin/notifications/users/{userId}?isRead=false&page=1&pageSize=20
```

**Response `200 OK`:** Paginated list

---

## 3. Gửi thông báo (Push ngay lập tức)

```http
POST /api/v1/admin/notifications/send
```

```json
{
  "userId": "guid",
  "title": "Important Update",
  "message": "A new feature has been released!",
  "type": "FeatureUpdate"
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `userId` | guid | ✅ | User nhận thông báo |
| `title` | string | ✅ | Tiêu đề |
| `message` | string | ✅ | Nội dung |
| `type` | string | ✅ | `"System"`, `"Reminder"`, `"Achievement"`, `"FeatureUpdate"` |

> ⚠️ `type` gửi dạng **string** (enum name), KHÔNG gửi int.  
> Endpoint này tự động push real-time qua SignalR đến user.

**Response `201 Created`:**
```json
{ "id": "guid" }
```

---

## 4. Xóa một thông báo

```http
DELETE /api/v1/admin/notifications/{id}
```

**Response `204 No Content`**

> ⚠️ Hard-delete (xóa vĩnh viễn, không soft-delete).

---

## 5. Đánh dấu đã đọc tất cả (cho một User)

```http
POST /api/v1/admin/notifications/users/{userId}/read-all
```

**Response `204 No Content`**

---

## 6. Xóa sạch hộp thư (cho một User)

```http
DELETE /api/v1/admin/notifications/users/{userId}/all
```

**Response `204 No Content`**

> ⚠️ Hard-delete toàn bộ — không thể hoàn tác.
