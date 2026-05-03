---
trigger: manual
---

# Notification — SignalR Hub

> **Scope:** Đẩy thông báo real-time qua WebSocket  
> **@mention khi làm:** signalR, hub, push notification

---

## 1. Hub Configuration

Khác với `LiveAudioSession` (dùng querystring), Notification Hub dùng chuẩn header bình thường của web app.

- **Hub path:** `/hubs/notifications`
- **Auth:** Standard Bearer JWT header
- **Group:** `userId.ToString()` — mỗi user kết nối sẽ được thêm vào group có tên là ID của chính họ.

---

## 2. Dữ liệu đẩy về Client

Khi có thông báo mới, server sẽ đẩy event về cho client:

```json
// Tên sự kiện: "NotificationReceived" (KHÔNG PHẢI "ReceiveNotification")
// Payload:
{
  "id": "guid",
  "title": "Welcome",
  "message": "Welcome to EnglishPartner",
  "type": 0, // NotificationType
  "isRead": false,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

---

## 3. Flow Đẩy Thông Báo (Push Flow)

`INotificationPusher` (nằm ở Application) được implement bởi `SignalRNotificationPusher` (nằm ở Api layer).

**Code luồng:**
1. Tạo entity `Notification`.
2. Gọi `await _pusher.PushAsync(notification, ct)`.
3. Pusher gọi `hubContext.Clients.Group(userId.ToString()).SendAsync("NotificationReceived", dto)`.
