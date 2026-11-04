---
name: realtime-session
description: Thiết kế, sửa hoặc review luồng realtime của Live Tutor — 3 SignalR hub (tutor, transcribe, translate), GeminiLiveConnectionManager/GeminiLiveSessionBridge, Redis backplane, ownership theo connection, dọn state; phía FE là useThreeHubLiveSession, microphone, AudioContext/AudioWorklet. Dùng khi đụng hub, phiên live hoặc audio.
---

# Realtime session

Đọc [AGENTS.md gốc](../../../AGENTS.md), [ADR-0004](../../../docs/adr/0004-redis-scale-out.md) và các mục S3, P2, P7 của [review](../../../docs/reviews/2026-10-28-security-performance-architecture.md).

## Bản đồ

- Hub: `EnglishPartner.BE/src/EnglishLiveTutor.Api/Hubs/{TutorConversation,TranscribeLive,TranslateLive}Hub.cs`, map tại `/hubs/tutor|transcribe|translate` trong `Program.cs`.
- Điều phối: `Infrastructure/Realtime/GeminiLiveConnectionManager.cs` (Singleton, giữ `_bridges`, `_turnStates`, `_sessionApiKeys`), `Infrastructure/Ai/GeminiLiveSessionBridge.cs` (WebSocket tới Gemini Live; Shared Kernel từ 2026-11-01 vì TTS của Speech cũng dùng).
- Lưu transcript: `Infrastructure/Repositories/ConversationRepository.cs`.
- Thông báo client: `Api/Realtime/SignalRLiveSessionRealtimeNotifier.cs`.
- FE: `src/features/tutor/hooks/useThreeHubLiveSession.ts`, `src/features/tutor/store/tutorSessionStore.ts`.

## Luật

- **Ownership:** mỗi hub method lấy user từ `Context.User`, kiểm tra conversation thuộc user trước `Groups.AddToGroupAsync` và trước mọi thao tác stream/stop/analyze. Lưu ánh xạ connectionId → (userId, conversationId); từ chối conversationId khác với phiên đã mở.
- **Vòng đời:** override `OnDisconnectedAsync` để đóng bridge, xóa turn state và API key của phiên; có TTL/idle timeout cho phiên mồ côi. Không để `ConcurrentDictionary` tăng vô hạn.
- **Scale-out (ADR-0004):** Redis backplane chỉ chuyển message SignalR giữa instance; WebSocket Gemini và state phiên vẫn nằm ở instance đã mở → bắt buộc sticky session. Không tự nhận là "stateless" khi state còn trong RAM.
- **Ghi DB:** không dùng lock toàn cục; chỉ persist transcript khi `isFinal` hoặc theo debounce; dựa vào unique index `(ConversationId, TurnId, Speaker)`.
- **Contract:** giữ tên hub method/event và payload; đổi thì theo [api-contract-sync](../api-contract-sync/SKILL.md).
- **API key BYO:** truyền qua payload khởi tạo phiên, chỉ giữ trong RAM của phiên, không log.

## FE

- `accessTokenFactory` lấy token mới mỗi lần (đã có); reconnect phải khởi tạo lại phiên phía server.
- Khi dừng, rời trang hoặc đổi conversation: dừng track microphone, `disconnect()` node, `close()` AudioContext, `stop()` 3 kết nối hub; bỏ qua event đến muộn của conversation cũ.
- Không giữ transcript chỉ trong state client như dữ liệu chính thức; lịch sử lấy từ API.

## Kiểm chứng

Unit test cho manager/hub với notifier giả; ghi rõ trong report rằng chưa kiểm chứng WebSocket Gemini thật, microphone và nhiều instance trừ khi đã chạy thật.
