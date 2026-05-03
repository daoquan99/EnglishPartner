# AI API — Speech (TTS / STT)

> **Auth:** Bearer token required  
> **Rate Limit:** `ai-scoring` — 50 requests/giờ

---

## 1. Lấy danh sách Neural2 Voices

```http
GET /api/v1/ai/tts/voices
```

**Response `200 OK`:**
```json
[
  {
    "id": "en-US-Neural2-C",
    "displayName": "US Female (Clara)",
    "accent": "US",
    "gender": "Female",
    "languageCode": "en-US"
  }
]
```

---

## 2. Lấy danh sách Gemini Live Voices

```http
GET /api/v1/ai/live-voices
```

**Response `200 OK`:**
```json
[
  {
    "id": "zephyr",
    "displayName": "Zephyr",
    "gender": "Male"
  }
]
```

> 30 giọng polyglot. Mặc định: `zephyr`.

---

## 3. Text-to-Speech (Neural2)

```http
POST /api/v1/ai/tts
```

```json
{
  "text": "Hello, how are you today?",
  "voice": "en-US-Neural2-C",
  "speakingRate": 1.0
}
```

| Field | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `text` | string | ✅ | — | Text cần đọc (max 5000 ký tự) |
| `voice` | string | ❌ | null | Voice ID (từ `/tts/voices`) |
| `speakingRate` | double | ❌ | 1.0 | Tốc độ đọc |

**Response `200 OK`:** Binary file `audio/mpeg` (MP3)

> 💡 FE tạo `Blob` từ response → `URL.createObjectURL()` → `<audio>` element.

---

## 4. Text-to-Speech (Gemini — Truyền cảm)

```http
POST /api/v1/ai/gemini-tts
```

```json
{
  "text": "Hello, how are you today?",
  "voice": "zephyr",
  "stylePrompt": "Speak slowly and clearly for English learners",
  "languageCode": "en-US"
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `text` | string | ✅ | Max 5000 ký tự |
| `voice` | string | ❌ | Gemini voice (default: `zephyr`) |
| `stylePrompt` | string | ❌ | Hướng dẫn cách đọc |
| `languageCode` | string | ❌ | Language code |

**Response `200 OK`:** Binary file `audio/mpeg` (MP3)

> ⚡ Server cache 24 giờ (SHA-256 key). Gọi lại cùng text+voice → trả từ cache.

---

## 5. Speech-to-Text (Real-time Streaming)

Thay vì POST file một lần, API này hỗ trợ **WebSocket** để nhận audio liên tục và trả về text realtime (Live Subtitle).

```http
GET wss://{domain}/api/v1/ai/stt/stream?languages=en-US&mimeType=audio/webm
```

**Query Parameters:**
| Field | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `languages` | string | ❌ | `en-US` | Comma-separated BCP-47 codes: `"en-US,vi-VN"` |
| `mimeType`  | string | ❌ | `audio/webm` | Định dạng audio FE gửi lên (`audio/webm`, `audio/wav`, `audio/flac`, `audio/mp3`, `audio/amr`) |

**Quy trình hoạt động (FE <-> BE):**

1. FE mở kết nối WebSocket tới endpoint trên.
2. FE dùng `MediaRecorder`, gọi `recorder.start(250)` để cắt audio thành từng chunk nhỏ 250ms.
3. Trong sự kiện `ondataavailable`, FE gửi Binary chunk lên server qua `socket.send(blob)`.
4. Server đẩy audio chunk lên Google Cloud gRPC.
5. Google Cloud trả text về server.
6. Server trả JSON Text message về FE qua sự kiện `onmessage`.

**Message từ Server (JSON Text):**
```json
{
  "transcript": "Hello, how are you",
  "isFinal": false,
  "confidence": 0.95,
  "detectedLanguage": "en-US"
}
```

* `isFinal`: `false` nghĩa là text tạm thời (interim - có thể thay đổi khi Google nghe rõ hơn câu sau). `true` nghĩa là Google đã chốt câu đó.

**FE Code Mẫu:**

```javascript
// Gửi token qua URL parameter (vì WebSocket API của JS không hỗ trợ set Headers)
const socket = new WebSocket(`wss://api.domain.com/api/v1/ai/stt/stream?languages=en-US&access_token=${token}`);

socket.onmessage = (event) => {
    const result = JSON.parse(event.data);
    if (result.error) {
        console.error("STT Error:", result.error);
        return;
    }
    console.log(`Bạn đang nói: ${result.transcript} (Final: ${result.isFinal})`);
};

const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
const recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });

recorder.ondataavailable = async (e) => {
    if (e.data.size > 0 && socket.readyState === WebSocket.OPEN) {
        // Gửi Raw Binary Blob thẳng qua WebSocket
        socket.send(e.data);
    }
};

// Bắt đầu đẩy dữ liệu lên mỗi 250ms
recorder.start(250); 
```
