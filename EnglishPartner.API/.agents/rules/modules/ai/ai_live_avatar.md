---
trigger: manual
---

# AI — Live Avatar (SignalR Hub)

> **Scope:** Gemini Live Audio Streaming qua WebSocket  
> **Endpoint:** `/hubs/ai-live-avatar`  
> **@mention khi làm:** live avatar, signalr, websocket, audio streaming

---

## 1. Authentication & Protocol

Do WebSocket ở browser không thể gửi header `Authorization: Bearer`, kết nối Hub yêu cầu gửi token qua query string:
`ws://.../hubs/ai-live-avatar?access_token=<jwt>`

**Audio format:** 
- Input từ client: PCM 16-bit 16kHz mono.
- Output từ Gemini: PCM 24kHz.

---

## 2. SignalR Hub Flow

1. **Kết nối:** Client kết nối → Hub gọi `LiveAudioSessionManager.CreateSession(connectionId)`. `Manager` dùng `ConcurrentDictionary` để lưu theo kiểu Singleton.
2. **Gửi Audio:** Client gửi binary chunk liên tục qua hàm `SendAudioChunk(byte[])` của Hub.
3. **Chuyển tiếp:** Hub truyền chunk này vào `ILiveAudioSession.SendAudioAsync(chunk)`.
4. **Nhận Audio:** `GoogleGenAILiveAudioSession` nhận stream từ Gemini trả về, gọi callback.
5. **Gửi về client:** Hub bắn ngược chunk audio về client qua event `AudioChunkReceived(byte[])`.
6. **Ngắt kết nối:** `LiveAudioSessionManager.RemoveSessionAsync(connectionId)` gọi `DisposeAsync()`.
