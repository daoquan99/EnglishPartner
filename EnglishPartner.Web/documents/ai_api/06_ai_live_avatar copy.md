# AI API — Live Avatar (Realtime Native Audio)

> **Auth:** Cần truyền Token JWT qua Query String  
> **Endpoint:** `wss://{domain}/hubs/ai-live-avatar?access_token={token}`  
> **Giao thức:** SignalR (WebSocket)

Đây là API cung cấp khả năng giao tiếp 2 chiều theo thời gian thực (Full Duplex) với mô hình AI (Gemini Live). 
Khác với quy trình truyền thống (STT -> LLM -> TTS) làm tăng độ trễ, API này stream trực tiếp chuỗi âm thanh (Audio Bytes) lên cho AI và AI stream ngược lại Audio Bytes ngay lập tức, đem lại độ trễ cực thấp (phản hồi tức thì như đang gọi điện thoại).

---

## 1. Kết nối với SignalR Hub

Vì giao thức WebSocket của trình duyệt không hỗ trợ header `Authorization`, bạn phải đính kèm JWT Token vào cuối URL khi khởi tạo `HubConnection`.

**Frontend (TypeScript / JavaScript):**

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`https://api.domain.com/hubs/ai-live-avatar?access_token=${token}`)
    .withAutomaticReconnect()
    .build();

await connection.start();
console.log("Connected to AI Live Avatar Hub!");
```

---

## 2. Các sự kiện nhận từ Server (Lắng nghe AI)

Server sẽ gọi 2 event này trên Frontend khi AI bắt đầu phản hồi:

### a. `AudioChunkReceived`
Hứng luồng âm thanh do AI trả về. 
- **Data type:** `byte[]` (Trong JS sẽ là mảng Uint8Array hoặc ArrayBuffer).
- **Audio Format:** PCM 24kHz (Gemini mặc định trả về tần số này).
- **Xử lý FE:** Frontend dùng `AudioContext` để đưa các mảng PCM này vào Buffer và phát ra loa.

```javascript
connection.on("AudioChunkReceived", (pcmAudioBytes) => {
    // pcmAudioBytes là dạng thô (Raw PCM 24kHz).
    // Gọi hàm Web Audio API để phát âm thanh.
    playPcmAudioStream(pcmAudioBytes);
});
```

### b. `TextChunkReceived`
Phụ đề (Subtitles) đồng bộ với giọng nói của AI.
- **Data type:** `string`

```javascript
connection.on("TextChunkReceived", (text) => {
    // Ghép các cụm text lại để hiển thị phụ đề cho User đọc
    console.log("AI says:", text);
});
```

---

## 3. Gửi dữ liệu lên Server (User nói chuyện)

### a. Gửi Audio (Mic)
User nói vào Mic, Frontend cắt âm thanh ra thành từng khối nhỏ (chunk) và truyền liên tục lên Server qua hàm `SendAudioChunk`.
- **Audio Format bắt buộc:** Raw PCM 16-bit, 16kHz, Mono.
- **Lưu ý:** Không gửi định dạng nén như WebM, MP3 hay OGG. Bắt buộc phải decode ra Raw PCM ở Frontend (có thể dùng `AudioWorklet` hoặc `ScriptProcessorNode`) trước khi gửi qua SignalR.

```javascript
// pcmChunk là ArrayBuffer chứa Raw PCM 16-bit 16kHz
await connection.invoke("SendAudioChunk", pcmChunk);
```

### b. Gửi Text (Nút bấm, kịch bản)
Trong một số trường hợp, thay vì user nói, hệ thống có thể muốn tiêm một câu lệnh dạng Text vào não AI (Ví dụ: User bấm nút "Yêu cầu gợi ý", hoặc tự động đổi chủ đề).

```javascript
const textInput = "Can you ask me a question about my hobbies?";
await connection.invoke("SendTextMessage", textInput);
```

---

## 4. Flow Tổng Thể (Ví dụ luyện nói Tiếng Anh rảnh tay)

1. User bấm nút **"Start Live Chat"**.
2. FE khởi tạo `HubConnection` và `connection.start()`.
3. BE kết nối thành công tới Gemini Live API (Google Cloud).
4. Khởi động Micro của User, set tần số lấy mẫu (`sampleRate = 16000`).
5. **(Luồng gửi):** Mỗi `250ms`, FE thu thập bộ đệm PCM, nén thành byte array và gọi `connection.invoke("SendAudioChunk", chunk)`.
6. **(Luồng nhận):** Khi User ngừng nói hoặc AI muốn chen ngang, Gemini đẩy data về BE, BE gọi ngược lại `connection.on("AudioChunkReceived")` về FE.
7. FE nạp data này vào loa ngoài (`AudioContext`). 
8. Cuộc hội thoại diễn ra theo thời gian thực (Độ trễ xấp xỉ < 0.5s).
9. User bấm **"Stop"** -> Gọi `connection.stop()`, BE tự động giải phóng session trên Google Cloud.
