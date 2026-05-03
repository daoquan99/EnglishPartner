# Notification API — SignalR Hub (Real-time)

> **Protocol:** WebSocket via SignalR  
> **Endpoint:** `/hubs/notifications`  
> **Auth:** Standard Bearer JWT header

---

## 1. Kết nối

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", {
    accessTokenFactory: () => accessToken
  })
  .build();

await connection.start();
```

> Khác với Live Avatar Hub (query string), Notification Hub dùng chuẩn header Bearer.

---

## 2. Lắng nghe thông báo mới

```javascript
connection.on("NotificationReceived", (notification) => {
  console.log(notification);
  // {
  //   "id": "guid",
  //   "title": "Streak Milestone!",
  //   "message": "You've maintained a 7-day streak!",
  //   "type": 2,
  //   "isRead": false,
  //   "createdAt": "2026-04-23T10:00:00Z"
  // }
});
```

> ⚠️ Event name là **`NotificationReceived`** — KHÔNG PHẢI `ReceiveNotification`.

---

## 3. Khi nào nhận push

| Khi nào | Type |
|---|---|
| User mới đăng ký | `System (0)` — Welcome |
| Streak tăng (milestone) | `Achievement (2)` |
| Level up (CEFR) | `Achievement (2)` |
| Nhắc nhở học tập | `Reminder (1)` |
| Admin gửi thủ công | Tùy admin chọn |

---

## 4. Ghi chú cho Frontend

1. **Kết nối khi app khởi động** và giữ mở suốt session.
2. **Khi nhận push:** Tăng badge count + show toast notification.
3. **Reconnect:** SignalR tự reconnect. Nếu mất kết nối lâu → gọi `GET /notifications` để sync lại.
