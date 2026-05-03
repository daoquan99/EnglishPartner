---
trigger: manual
---

# Notification — Admin Endpoints

> **Scope:** API quản lý thông báo cho Admin  
> **Base URL:** `/api/v1/admin/notifications`  
> **Auth:** Bearer + `notifications.admin` ở group level  
> **@mention khi làm:** admin notifications

---

## 1. Truy vấn thông báo (Tất cả Users)

```
GET /api/v1/admin/notifications?userId=guid&isRead=false&page=1&pageSize=20
Auth: Bearer + notifications.admin

Response 200: Paginated List
```

> **Quy tắc:** Tất cả Admin GET APIs đều phải đi qua hệ thống Query (`GetAllNotificationsQuery`), KHÔNG được inject Repository trực tiếp vào endpoint.

---

## 2. Truy vấn thông báo (Một User)

```
GET /api/v1/admin/notifications/users/{userId:guid}
Auth: Bearer + notifications.admin

Response 200: Paginated List
```

---

## 3. Gửi thông báo chủ động

```
POST /api/v1/admin/notifications/send
Auth: Bearer + notifications.admin
Body: SendNotificationCommand { userId, title, message, type }

Response 201: { id }
```

> Endpoint này không chỉ tạo trong DB mà còn tự động kích hoạt `SignalR Pusher` để đẩy real-time về user đó.

---

## 4. Xóa một thông báo

```
DELETE /api/v1/admin/notifications/{id:guid}
Auth: Bearer + notifications.admin

Response 204
```

---

## 5. Thao tác hàng loạt trên một User

**Đánh dấu đã đọc tất cả:**
```
POST /api/v1/admin/notifications/users/{userId:guid}/read-all
Auth: Bearer + notifications.admin
Response 204
```

**Xóa sạch hộp thư:**
```
DELETE /api/v1/admin/notifications/users/{userId:guid}/all
Auth: Bearer + notifications.admin
Response 204
```
