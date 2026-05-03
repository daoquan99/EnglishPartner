# Tích hợp Live Audio Avatar vào AI Chat Session (Hướng dẫn FE)

Tài liệu này hướng dẫn Frontend cách kết hợp giữa **AI Chat Session** (REST API) và tính năng **Live Audio Avatar** (SignalR WebSocket) để người dùng có thể thực hiện "Gọi điện trực tiếp" ngay trong một phiên chat.

---

## Quy trình tích hợp (Workflow cho Frontend)

### Bước 1: Lấy thông tin Chat Session
Khởi tạo session mới hoặc chọn một session đang có bằng các REST API (`POST /api/v1/ai/chat/sessions` hoặc `GET /api/v1/ai/chat/sessions`). Từ đó bạn sẽ lấy được thông tin `systemPrompt` (gồm cấu hình và topic) để truyền vào Live Audio.

### Bước 2: Khởi tạo kết nối SignalR
Sử dụng `systemPrompt` đã được mã hoá (URL Encode) để mở kết nối:
```javascript
const prompt = encodeURIComponent(combinedSystemPrompt);
const url = `wss://api.domain.com/hubs/ai-live-avatar?access_token=${token}&systemPrompt=${prompt}`;

const connection = new signalR.HubConnectionBuilder()
    .withUrl(url)
    .withAutomaticReconnect()
    .build();

await connection.start();
```

### Bước 3: Giao tiếp thời gian thực
Thực hiện stream audio 2 chiều như hướng dẫn trong file `06_ai_live_avatar.md`:
- Bắn `SendAudioChunk` khi user nói vào Mic.
- Lắng nghe `AudioChunkReceived` để phát tiếng AI.
- Lắng nghe `TextChunkReceived` để hiển thị phụ đề.

### Bước 4: (Tùy chọn) Lưu lại lịch sử sau khi gọi xong
**Lưu ý quan trọng:** Hiện tại backend chưa có API để "chỉ lưu tin nhắn vào DB mà không gọi AI". Lệnh `POST /chat/sessions/{sessionId}/messages` (`SendMessageRequest`) sẽ tự động gọi AI text sinh ra câu trả lời mới.
Do đó:
- **Hướng A:** Chấp nhận việc cuộc gọi Live Audio là "phút ngẫu hứng ngoài luồng", hiển thị phụ đề tạm thời trên giao diện, nhưng **không lưu** vào Database lịch sử chat. (Cách phổ biến nhất và dễ nhất hiện tại).
- **Hướng B:** Nếu business yêu cầu phải lưu text của cuộc gọi Live vào Chat Session, backend sẽ cần cung cấp thêm 1 API dạng `POST /chat/sessions/{sessionId}/messages/sync` để chèn raw data từ frontend xuống DB.

---

## Tóm tắt cho Frontend Developer
1. Dùng `ChatEndpoints.cs` để hiện thị danh sách session, tin nhắn text bình thường.
2. Khi user bấm biểu tượng "Micro" / "Gọi thoại":
   - Tự động lấy ngữ cảnh hiện tại ghép vào `systemPrompt`.
   - Kết nối SignalR `AiLiveAvatarHub`.
   - Chuyển UI sang màn hình/pop-up Calling (Live Voice).
3. Khi user cúp máy: Ngắt SignalR, quay lại màn hình Chat (không bắt buộc phải hiện lịch sử lời nói trong cuộc gọi vào khung chat).
