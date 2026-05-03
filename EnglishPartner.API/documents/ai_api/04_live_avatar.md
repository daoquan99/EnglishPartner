# AI API — Live Avatar (SignalR)

> **Protocol:** WebSocket via SignalR  
> **Endpoint:** `/hubs/ai-live-avatar`  
> **Auth:** Query string token (WebSocket không hỗ trợ header)

---

## 1. Kết nối

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/ai-live-avatar", {
    accessTokenFactory: () => accessToken
  })
  .build();
```

> URL thực tế: `wss://{host}/hubs/ai-live-avatar?access_token={jwt}`

---

## 2. Audio Format

| Direction | Format |
|---|---|
| **Client → Server** | PCM 16-bit, 16kHz, mono |
| **Server → Client** | PCM 24kHz |

---

## 3. Gửi Audio

```javascript
// Gửi chunk audio liên tục
connection.invoke("SendAudioChunk", audioChunkBytes);
```

---

## 4. Nhận Audio

```javascript
// Lắng nghe event audio từ AI
connection.on("AudioChunkReceived", (audioChunkBytes) => {
  // Play audio chunk
});
```

---

## 5. Flow tổng quát

1. **Connect** → Server tạo Gemini Live session
2. **Stream gửi** → Client gửi `SendAudioChunk(byte[])` liên tục
3. **Stream nhận** → Server bắn `AudioChunkReceived(byte[])` về client
4. **Disconnect** → Server dispose session tự động
