# Learning API — Conversations (Role-play)

> **Auth:** Bearer token required  
> **Mục đích:** Luyện hội thoại theo kiểu role-play. AI đóng vai nhân vật A, User đóng vai nhân vật B. Hai bên luân phiên nói theo mẫu (`ConversationLine`).

---

## Tổng quan

Hội thoại diễn ra theo kiểu **turn-based** (luân phiên):

```
Line 1 (AI - Barista):     "Hi! What can I get you today?"        → AI nói (TTS)
Line 2 (User - Customer):  "I'd like a cappuccino, please."       → User ghi âm & chấm điểm
Line 3 (AI - Barista):     "Sure! Would you like anything else?"   → AI nói (TTS)
Line 4 (User - Customer):  "No, that's all. Thank you!"           → User ghi âm & chấm điểm
```

Mỗi `ConversationLine` có trường `speaker` để FE phân biệt đây là lượt AI hay lượt User.

---

## Full Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│  1. POST /learning/sessions                                        │
│     → Nhận sessionId                                               │
│                                                                     │
│  2. GET /curriculum/conversations/{id}                              │
│     → Nhận conversation với lines[] (đã sort theo sortOrder)       │
│                                                                     │
│  3. Duyệt qua từng line theo thứ tự:                               │
│     │                                                               │
│     ├─ Nếu line là lượt AI:                                         │
│     │   ├─ Có audioUrl? → Play audio cho user nghe                  │
│     │   └─ Không có audioUrl? → Call POST /ai/tts để tạo → Play    │
│     │                                                               │
│     └─ Nếu line là lượt User:                                       │
│         ├─ (Tuỳ chọn) Play audioUrl mẫu để user nghe trước          │
│         ├─ Hiện text mẫu trên màn hình                               │
│         ├─ User bấm nút thu âm → Đọc theo → Dừng                    │
│         ├─ POST /ai/stt → Hiện text user nói                        │
│         ├─ POST /learning/conversations/evaluate-line                │
│         │  → { score, transcription, feedback }                      │
│         └─ Lưu score vào lineScores[]                                │
│                                                                     │
│  4. Tính avgScore = average(lineScores)                             │
│                                                                     │
│  5. POST /learning/sessions/{id}/answer                             │
│     { itemId: conversationId, itemType: 4, userAnswer: avgScore }  │
│     → SM-2 ghi nhận Mastery cho toàn bộ Conversation                │
│                                                                     │
│  6. PUT /learning/sessions/{id}/submit                              │
│     { score: avgScore }                                             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Bước 1: Lấy dữ liệu Conversation

```http
GET /api/v1/curriculum/conversations/{id}
```

**Response `200 OK`:**
```json
{
  "id": "conv-guid",
  "title": "At the coffee shop",
  "languageCode": "en",
  "scenario": "Ordering coffee at a café",
  "level": "A2",
  "imageUrl": "https://storage.../coffee_shop.jpg",
  "topicId": "topic-guid",
  "grammarId": null,
  "lines": [
    {
      "id": "line-1-guid",
      "speaker": "Barista",
      "text": "Hi! What can I get you today?",
      "audioUrl": "https://storage.../line1.mp3",
      "sortOrder": 1,
      "translations": [
        { "languageCode": "vi", "translatedText": "Xin chào! Bạn muốn gọi gì ạ?" }
      ]
    },
    {
      "id": "line-2-guid",
      "speaker": "Customer",
      "text": "I'd like a cappuccino, please.",
      "audioUrl": "https://storage.../line2.mp3",
      "sortOrder": 2,
      "translations": [
        { "languageCode": "vi", "translatedText": "Cho tôi một ly cappuccino." }
      ]
    },
    {
      "id": "line-3-guid",
      "speaker": "Barista",
      "text": "Sure! Would you like anything else?",
      "audioUrl": null,
      "sortOrder": 3,
      "translations": []
    }
  ]
}
```

### FE Logic: Phân biệt lượt AI vs User

Quy ước: FE lấy `speaker` của line đầu tiên = AI. Các line có cùng `speaker` đó = lượt AI. Còn lại = lượt User.

```javascript
const aiSpeaker = conversation.lines[0].speaker; // "Barista" = AI

conversation.lines.forEach(line => {
  if (line.speaker === aiSpeaker) {
    // Lượt AI → Play audio
    playAiLine(line);
  } else {
    // Lượt User → Thu âm + Chấm điểm
    recordAndEvaluate(line);
  }
});
```

---

## Bước 2a: Play audio AI (Lượt AI nói)

Khi đến lượt AI, FE cần play audio cho user nghe.

### Trường hợp 1: `audioUrl` đã có sẵn

```javascript
// Đơn giản: play trực tiếp
const audio = new Audio(line.audioUrl);
audio.play();

// Nút replay để user nghe lại
replayButton.onclick = () => audio.play();
```

### Trường hợp 2: `audioUrl` là `null` → Call TTS on-the-fly

Dùng API TTS có sẵn để tạo audio tạm thời:

```http
POST /api/v1/ai/tts
Content-Type: application/json
```

```json
{
  "text": "Hi! What can I get you today?",
  "voice": "en-US-Neural2-F",
  "speakingRate": 1.0
}
```

**Response:** Binary audio file (`audio/mpeg`)

```javascript
const response = await fetch('/api/v1/ai/tts', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    text: line.text,
    voice: 'en-US-Neural2-F'
  })
});

const audioBlob = await response.blob();
const audioUrl = URL.createObjectURL(audioBlob);
const audio = new Audio(audioUrl);
audio.play();
```

**Hoặc dùng Gemini TTS** (giọng tự nhiên hơn):

```http
POST /api/v1/ai/gemini-tts
```

```json
{
  "text": "Hi! What can I get you today?",
  "voice": "zephyr",
  "stylePrompt": "Speak naturally like a friendly barista.",
  "languageCode": "en"
}
```

### Lấy danh sách voices

```http
GET /api/v1/ai/tts/voices          → Neural2 voices (Google Cloud)
GET /api/v1/ai/live-voices          → Gemini voices (polyglot)
```

---

## Bước 2b: Generate & Lưu Audio vĩnh viễn (Admin)

### Tạo audio cho 1 line

```http
POST /api/v1/admin/curriculum/conversations/{conversationId}/lines/{lineId}/generate-audio
Content-Type: application/json
```

**Request Body (optional):**
```json
{
  "voice": "en-US-Neural2-F"
}
```

> Nếu không gửi body hoặc `voice` = null → dùng voice mặc định.

**Response `200 OK`:**
```json
{
  "audioUrl": "https://storage.../conversations/abc123/audio/line1.mp3"
}
```

> ⚠️ **Admin only** — cần permission `curriculum.conversation.write`.  
> Backend tự động: TTS → Upload file → Cập nhật `audioUrl` trên `ConversationLine`.  
> Nếu line đã có `audioUrl`, sẽ **ghi đè** (re-generate).

---

### Tạo audio cho TẤT CẢ lines chưa có audio

```http
POST /api/v1/admin/curriculum/conversations/{conversationId}/generate-all-audio
Content-Type: application/json
```

**Request Body (optional):**
```json
{
  "voice": "en-US-Neural2-F"
}
```

**Response `200 OK`:**
```json
{
  "totalLines": 6,
  "generatedCount": 4,
  "skippedCount": 0,
  "generatedLines": [
    { "lineId": "line-1-guid", "audioUrl": "https://storage.../line1.mp3" },
    { "lineId": "line-3-guid", "audioUrl": "https://storage.../line3.mp3" },
    { "lineId": "line-5-guid", "audioUrl": "https://storage.../line5.mp3" },
    { "lineId": "line-6-guid", "audioUrl": "https://storage.../line6.mp3" }
  ]
}
```

> Chỉ generate cho lines **chưa có** `audioUrl`. Lines đã có audio sẽ bị bỏ qua (không đếm vào `skippedCount`).  
> `skippedCount` chỉ đếm lines TTS **thất bại** (lỗi mạng, AI không phản hồi).

---

## Bước 3: User ghi âm & Chấm điểm (Lượt User nói)

Khi đến lượt User, FE hiện text mẫu và nút thu âm. Flow gồm **2 bước**:

```
User bấm ghi âm → Vừa ghi âm vừa truyền audio qua WebSocket (STT Real-time) → Hiện chữ realtime
           Dừng ghi âm → Gọi evaluate-line (POST audio) → Hiện điểm + feedback
```

### Tuỳ chọn: Play audio mẫu trước

Nếu `line.audioUrl` tồn tại (dù là lượt user), FE có thể cho user nghe audio mẫu trước khi tự đọc:

```javascript
if (line.audioUrl) {
  sampleButton.onclick = () => new Audio(line.audioUrl).play();
}
```

### Bước 3a: Ghi âm & STT Real-time (Hiện chữ khi đang nói)

Khi user bắt đầu nói, mở kết nối WebSocket và đẩy dữ liệu audio liên tục.

```http
GET wss://{domain}/api/v1/ai/stt/stream?languages=en-US&mimeType=audio/webm
```

**Query Parameters:**
| Tham số | Mô tả |
|---|---|
| `languages` | (Tuỳ chọn) BCP-47 language codes, vd: `en-US` |
| `mimeType` | (Tuỳ chọn) Định dạng audio gửi lên, mặc định `audio/webm` |

**Cách hoạt động:**
1. FE dùng `MediaRecorder`, cài đặt `timeslice` (ví dụ: `250ms`) để lấy audio chunk liên tục.
2. Gửi binary data (Blob/ArrayBuffer) qua `socket.send()`.
3. Nhận về JSON Text messages từ server chứa transcript realtime.

**Server trả về (JSON Text):**

```json
{
  "transcript": "I'd like a cappuccino...",
  "isFinal": false,
  "confidence": 0.95,
  "detectedLanguage": "en-US"
}
```
*Ghi chú: `isFinal = false` là kết quả đang nói dở (interim). `isFinal = true` là khi Google chốt câu.*

**FE hiện kết quả:**

```
┌──────────────────────────────────────────────────┐
│  🎙️ Bạn nói:                                     │
│  "I'd like a cappuccino, please."                │
│  Confidence: 95%                                 │
│                                                  │
│  📝 Text mẫu:                                    │
│  "I'd like a cappuccino, please."                │
│                                                  │
│  ⏳ Đang chấm điểm phát âm...                     │
└──────────────────────────────────────────────────┘
```

### Bước 3b: Chấm điểm phát âm (Evaluate Line)

Sau khi user bấm **Dừng ghi âm**, FE gộp tất cả audio chunks lại thành 1 file hoàn chỉnh và gửi lên endpoint đánh giá phát âm:

```http
POST /api/v1/learning/conversations/evaluate-line
Content-Type: multipart/form-data
```

**Request Form-Data:**

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `conversationId` | guid | ✅ | ID của Conversation |
| `lineId` | guid | ✅ | ID của ConversationLine cần chấm |
| `audioFile` | binary (IFormFile) | ✅ | File ghi âm (.webm, .wav, .mp3) |

**Response `200 OK`:**

```json
{
  "score": 85,
  "transcription": "I'd like a cappuccino, please.",
  "pronunciationFeedback": "Good pronunciation! Stress the second syllable of 'cappuccino' a bit more."
}
```

| Field | Mô tả |
|---|---|
| `score` | Điểm phát âm 0-100 |
| `transcription` | Text AI (Gemini) nghe được |
| `pronunciationFeedback` | Nhận xét sửa lỗi (null nếu hoàn hảo) |

> ⚠️ API này **KHÔNG** ghi SM-2, **KHÔNG** ghi `LearningActivityLog`. Chỉ trả điểm tức thì.

### FE Code mẫu — Full flow (WebSocket STT + Evaluate)

```javascript
let recorder;
let socket;
let audioChunks = [];

// 1. Khi bấm BẮT ĐẦU thu âm
startButton.onclick = async () => {
  audioChunks = [];
  
  // Mở WebSocket (lưu ý truyền JWT token vào query string vì WS không hỗ trợ Header)
  socket = new WebSocket(`wss://api.domain.com/api/v1/ai/stt/stream?languages=en-US&access_token=${token}`);
  
  socket.onmessage = (event) => {
    const data = JSON.parse(event.data);
    // Hiện text realtime: "Bạn đang nói: I'd like a cappuccino..."
    showLiveTranscript(data.transcript, data.isFinal);
  };

  const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
  recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });

  // Gửi audio chunk liên tục mỗi 250ms
  recorder.ondataavailable = e => {
    if (e.data.size > 0) {
      audioChunks.push(e.data);
      if (socket.readyState === WebSocket.OPEN) {
        socket.send(e.data); // Gửi raw binary lên server
      }
    }
  };

  recorder.start(250); // timeslice = 250ms
};

// 2. Khi bấm DỪNG thu âm
stopButton.onclick = async () => {
  recorder.stop();
  socket.close(); // Đóng WebSocket
  
  // Gộp audio chunks thành file hoàn chỉnh
  const blob = new Blob(audioChunks, { type: 'audio/webm' });

  // ── Bước 3b: Evaluate — Chấm điểm phát âm bằng Gemini ──
  showLoadingScore();

  const evalFormData = new FormData();
  evalFormData.append('conversationId', conversationId);
  evalFormData.append('lineId', line.id);
  evalFormData.append('audioFile', blob, 'recording.webm');

  const evalRes = await fetch('/api/v1/learning/conversations/evaluate-line', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: evalFormData
  });
  const evalResult = await evalRes.json();

  lineScores.push(evalResult.score);
  showScore(evalResult);
  // Hiện: ⭐⭐⭐⭐ 85/100 + feedback
};
```

**Error:**
- `404` — `Common.NotFound` (lineId không tồn tại)
- `400` — `AI.EvaluationFailed` (audio ồn, trống, sai format)

---

## Bước 4: Chốt điểm vào Session

Khi user hoàn thành tất cả các lượt nói, FE tính **điểm trung bình** rồi gửi chốt.

### FE Logic

```javascript
// lineScores = [80, 90, 70, 85] (từ bước 3)
const avgScore = Math.round(
  lineScores.reduce((a, b) => a + b, 0) / lineScores.length
); // = 81

// Chốt mastery cho Conversation
const answerRes = await fetch(`/api/v1/learning/sessions/${sessionId}/answer`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    itemId: conversationId,     // ID Conversation, KHÔNG phải Line!
    itemType: 4,                // 4 = Conversation
    userAnswer: String(avgScore), // Điểm trung bình dạng string
    timeSpentSeconds: totalTime
  })
});

// Kết thúc session
await fetch(`/api/v1/learning/sessions/${sessionId}/submit`, {
  method: 'PUT',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({ score: avgScore })
});
```

### API: Chốt Answer

```http
POST /api/v1/learning/sessions/{id}/answer
Content-Type: application/json
```

```json
{
  "itemId": "conversation-guid",
  "itemType": 4,
  "userAnswer": "81",
  "timeSpentSeconds": 120
}
```

**Response `200 OK`:**
```json
{
  "qualityScore": 4,
  "isCorrect": true,
  "suggestedCorrection": null,
  "explanation": null,
  "newIntervalDays": 6,
  "newEasinessFactor": 2.6,
  "nextReviewDate": "2026-05-02T00:00:00Z"
}
```

> ⚠️ Khi `itemType = 4` (Conversation), backend **KHÔNG gọi AI** chấm điểm lại. Nó parse trực tiếp `userAnswer` thành score (0-100) rồi quy đổi → SM-2.

---

## UI Gợi ý

```
┌─────────────────────────────────────────────────────┐
│  ☕ At the coffee shop                    A2        │
│  "Ordering coffee at a café"                        │
├─────────────────────────────────────────────────────┤
│                                                     │
│  🤖 Barista                                         │
│  ┌─────────────────────────────────────────────┐    │
│  │ "Hi! What can I get you today?"              │    │
│  │                                   🔊 ▶ Play  │    │
│  │ 🔁 Replay                                    │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  👤 Customer (Bạn)                                   │
│  ┌─────────────────────────────────────────────┐    │
│  │ 📝 Text mẫu:                                 │    │
│  │ "I'd like a cappuccino, please."             │    │
│  │                                              │    │
│  │ 🎤 Nghe mẫu    🔴 Bấm để ghi âm             │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  ── Bạn nói ──                                       │
│  ┌─────────────────────────────────────────────┐    │
│  │ 🎙️ "I'd like a cappuccino, please."          │    │
│  │ Confidence: 95%                  ✅ Khớp!     │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  ── Chấm điểm phát âm ──                            │
│  ┌─────────────────────────────────────────────┐    │
│  │ ⭐⭐⭐⭐ 85/100                                │    │
│  │ 💡 Stress "cappuccino" ở âm thứ 2            │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  🤖 Barista                                         │
│  ┌─────────────────────────────────────────────┐    │
│  │ "Sure! Would you like anything else?"        │    │
│  │                                   🔊 ▶ Play  │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  ...                                                │
│                                                     │
│  ═══════════════════════════════════════════════     │
│  Trung bình: 82/100 ⭐⭐⭐⭐                         │
│  [🏁 Hoàn thành]                                     │
└─────────────────────────────────────────────────────┘
```

---

## Tổng hợp API cần dùng

### User APIs (Bearer token)

| Bước | API | Method | Mục đích |
|---|---|---|---|
| 1 | `/api/v1/learning/sessions` | POST | Bắt đầu session |
| 2 | `/api/v1/curriculum/conversations/{id}` | GET | Lấy conversation + lines |
| 3 (AI) | `/api/v1/ai/tts` | POST | TTS on-the-fly nếu line AI chưa có audioUrl |
| 3 (AI) | `/api/v1/ai/gemini-tts` | POST | TTS (giọng Gemini, tự nhiên hơn) |
| 3 (AI) | `/api/v1/ai/tts/voices` | GET | Danh sách Neural2 voices |
| 3 (AI) | `/api/v1/ai/live-voices` | GET | Danh sách Gemini voices |
| 3a | `wss://{domain}/api/v1/ai/stt/stream` | WS | STT Realtime qua WebSocket |
| 3b | `/api/v1/learning/conversations/evaluate-line` | POST | Chấm điểm phát âm user (stateless) |
| 4 | `/api/v1/learning/sessions/{id}/answer` | POST | Chốt điểm trung bình → SM-2 |
| 5 | `/api/v1/learning/sessions/{id}/submit` | PUT | Hoàn thành session |

### Admin APIs (Bearer + permission `curriculum.conversation.write`)

| API | Method | Mục đích |
|---|---|---|
| `/api/v1/admin/curriculum/conversations/{cId}/lines/{lId}/generate-audio` | POST | Generate TTS + lưu audioUrl cho 1 line |
| `/api/v1/admin/curriculum/conversations/{cId}/generate-all-audio` | POST | Generate TTS + lưu audioUrl cho TẤT CẢ lines chưa có |

---

## Quy đổi điểm (Tham khảo)

| AI Score (0-100) | qualityScore (0-5) | Ý nghĩa |
|---|---|---|
| 90-100 | 5 | Hoàn hảo |
| 80-89 | 4 | Tốt |
| 60-79 | 3 | Đạt |
| 40-59 | 2 | Yếu |
| 20-39 | 1 | Kém |
| 0-19 | 0 | Không đạt |
