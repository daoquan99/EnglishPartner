# Learning API — Study Sessions

> **Auth:** Bearer token required  
> **Flow:** Start → Submit Answers (Text / Audio) → Complete Session

---

## 1. Bắt đầu Study Session

```http
POST /api/v1/learning/sessions
```

**Request Body:**
```json
{
  "sessionTitle": "Grammar Practice",
  "totalItems": 10
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `sessionTitle` | string | ✅ | Tiêu đề session |
| `totalItems` | int | ✅ | Tổng số câu hỏi dự kiến |

**Response `201 Created`:** `"guid"` (session id)

> ⚠️ Nếu user đang có session InProgress cũ hơn 30 phút, nó sẽ **tự động bị Abandon**.

---

## 2. Submit Text Answer (Bài tập / Viết)

Dùng cho: **Exercise** (itemType 5), **Vocabulary** (0), **Grammar** (1), **ExampleSentence** (2), **Topic** (3), **Conversation chốt điểm** (4).

```http
POST /api/v1/learning/sessions/{id}/answer
Content-Type: application/json
```

**Request Body:**
```json
{
  "itemId": "guid",
  "itemType": 5,
  "userAnswer": "She goes to school every day.",
  "timeSpentSeconds": 15
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `itemId` | guid | ✅ | ID curriculum item |
| `itemType` | int | ✅ | ItemType enum (0-5), xem bảng bên dưới |
| `userAnswer` | string | ✅ | Câu trả lời text của user |
| `timeSpentSeconds` | int | ✅ | Thời gian làm (giây) |

> ⚠️ **FE KHÔNG gửi `qualityScore`** — AI tự chấm điểm server-side.

**Response `200 OK`:**
```json
{
  "qualityScore": 4,
  "isCorrect": true,
  "suggestedCorrection": null,
  "explanation": "Perfect! Your answer matches the expected response.",
  "newIntervalDays": 6,
  "newEasinessFactor": 2.6,
  "nextReviewDate": "2026-04-29T00:00:00Z"
}
```

| Field | Hiển thị cho user? | Mô tả |
|---|---|---|
| `qualityScore` | ✅ Có thể | Điểm SM-2 (0-5). Dùng hiển thị sao ⭐ hoặc thanh tiến trình |
| `isCorrect` | ✅ Nên | Đúng / Sai — dùng highlight xanh / đỏ |
| `suggestedCorrection` | ✅ Nên | Đáp án gợi ý (null nếu đúng) |
| `explanation` | ✅ Nên | Giải thích chi tiết từ AI |
| `newIntervalDays` | ❌ Không | Metadata SM-2 — không cần hiển thị |
| `newEasinessFactor` | ❌ Không | Metadata SM-2 |
| `nextReviewDate` | 💡 Tuỳ | Có thể hiện "Ôn lại sau X ngày" |

**Error:**  
- `404` — `Learning.Session.NotFound`
- `400` — `Learning.Session.NotInProgress` (session đã Complete hoặc Abandoned)

---

## 3. Submit Audio Answer (Luyện phát âm)

Dùng cho: **Vocabulary** (itemType 0), **Grammar** (1), **ExampleSentence** (2) — khi user luyện **nói** thay vì viết.

```http
POST /api/v1/learning/sessions/{id}/answer/audio
Content-Type: multipart/form-data
```

**Request Form-Data:**

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `itemId` | guid | ✅ | ID curriculum item |
| `itemType` | int | ✅ | `0` (Vocab), `1` (Grammar), hoặc `2` (Sentence) |
| `timeSpentSeconds` | int | ✅ | Thời gian làm (giây) |
| `audioFile` | file | ✅ | File ghi âm (.webm, .wav, .mp3, .m4a) |

**Cách gửi từ FE (JavaScript):**
```javascript
const formData = new FormData();
formData.append('itemId', itemId);
formData.append('itemType', itemType);
formData.append('timeSpentSeconds', timeSpent);
formData.append('audioFile', audioBlob, 'recording.webm');

const response = await fetch(
  `/api/v1/learning/sessions/${sessionId}/answer/audio`,
  {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    // ⚠️ KHÔNG set Content-Type — browser tự thêm boundary
    body: formData
  }
);
```

**Response `200 OK`:**
```json
{
  "qualityScore": 4,
  "transcription": "She goes to school every day.",
  "pronunciationFeedback": "Good job! Try to pronounce 'school' with a longer 'oo' sound.",
  "newIntervalDays": 6,
  "newEasinessFactor": 2.6,
  "nextReviewDate": "2026-04-29T00:00:00Z"
}
```

| Field | Hiển thị cho user? | Mô tả |
|---|---|---|
| `qualityScore` | ✅ | Điểm SM-2 (0-5) |
| `transcription` | ✅ Nên | Text mà AI nghe được — so sánh với text gốc |
| `pronunciationFeedback` | ✅ Nên | Nhận xét sửa lỗi phát âm |
| `newIntervalDays` | ❌ | Metadata SM-2 |
| `newEasinessFactor` | ❌ | Metadata SM-2 |
| `nextReviewDate` | 💡 | Có thể hiện "Ôn lại sau X ngày" |

> 💡 FE nên hiển thị `transcription` bên cạnh text gốc, highlight chỗ khác biệt (diff) để user thấy đọc sai ở đâu.

**Error:**  
- `404` — `Learning.Session.NotFound` hoặc `Common.NotFound` (item không tồn tại)
- `400` — Lỗi định dạng file hoặc AI không thể phân tích audio

---

## 4. Ping Session (Keep-alive)

```http
POST /api/v1/learning/sessions/{id}/ping
```

> Gọi mỗi **2-5 phút** để giữ session sống. Nếu không ping trong 30 phút → session bị đánh dấu Stale và tự Abandon khi user start session mới.

**Response `204 No Content`**  
**Error:** `404` — `Learning.Session.NotFound`

**FE Implementation:**
```javascript
// Khi session start
const pingInterval = setInterval(() => {
  fetch(`/api/v1/learning/sessions/${sessionId}/ping`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` }
  });
}, 3 * 60 * 1000); // Mỗi 3 phút

// Khi session submit — clear interval
clearInterval(pingInterval);
```

---

## 5. Complete Session

```http
PUT /api/v1/learning/sessions/{id}/submit
```

**Request Body:**
```json
{
  "score": 80
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `score` | int | ✅ | Điểm tổng (0-100) do FE tính từ kết quả các câu trả lời |

**Response `204 No Content`**

> **XP được tính server-side:**  
> `XP = (totalItems × 5) + (10 bonus nếu score ≥ 80%)`  
> Ví dụ: 10 items, score 85% → `50 + 10 = 60 XP`

**FE tính score:**
```javascript
const score = Math.round((correctCount / totalItems) * 100);
```

**Error:**  
- `404` — `Learning.Session.NotFound`
- `400` — `Learning.Session.NotInProgress`

---

## ItemType Reference

| Value | Type | Dùng API nào |
|---|---|---|
| 0 | Vocabulary | `/answer` (viết) hoặc `/answer/audio` (nói) |
| 1 | Grammar | `/answer` (viết) hoặc `/answer/audio` (nói) |
| 2 | ExampleSentence | `/answer` (viết) hoặc `/answer/audio` (nói) |
| 3 | Topic | `/answer` |
| 4 | Conversation | `/answer` (chốt điểm trung bình). Xem [05_conversations.md](./05_conversations.md) |
| 5 | Exercise | `/answer` |

---

## Quy đổi điểm (Tham khảo)

FE không cần tính — chỉ để hiểu `qualityScore` trong response:

| AI Score (0-100) | qualityScore (0-5) | Ý nghĩa | Hiển thị gợi ý |
|---|---|---|---|
| 90-100 | 5 | Hoàn hảo | ⭐⭐⭐⭐⭐ |
| 80-89 | 4 | Tốt | ⭐⭐⭐⭐ |
| 60-79 | 3 | Đạt | ⭐⭐⭐ |
| 40-59 | 2 | Yếu | ⭐⭐ |
| 20-39 | 1 | Kém | ⭐ |
| 0-19 | 0 | Không đạt | 💀 |

---

## Full Flow Diagrams

### Flow A: Bài tập thông thường (Text)

```
POST /sessions { sessionTitle, totalItems }
    → sessionId
    │
    ├─ setInterval(ping, 3min)
    │
    ├─ Lặp N lần:
    │   POST /sessions/{id}/answer
    │   { itemId, itemType: 5, userAnswer, timeSpentSeconds }
    │   → { qualityScore, isCorrect, explanation }
    │   → Hiển thị feedback
    │
    ├─ clearInterval(ping)
    │
    └─ PUT /sessions/{id}/submit { score }
```

### Flow B: Luyện phát âm (Audio)

```
POST /sessions { sessionTitle, totalItems }
    → sessionId
    │
    ├─ setInterval(ping, 3min)
    │
    ├─ Lặp N lần:
    │   POST /sessions/{id}/answer/audio
    │   FormData: { itemId, itemType: 0|1|2, timeSpentSeconds, audioFile }
    │   → { qualityScore, transcription, pronunciationFeedback }
    │   → Hiển thị diff transcription vs text gốc
    │
    ├─ clearInterval(ping)
    │
    └─ PUT /sessions/{id}/submit { score }
```

### Flow C: Conversation Role-play

> Xem chi tiết tại [05_conversations.md](./05_conversations.md)

```
POST /sessions { sessionTitle, totalItems: 1 }
    → sessionId
    │
    ├─ GET /curriculum/conversations/{id}
    │   → lines[] với audioUrl
    │
    ├─ Lặp qua lines (user turn):
    │   POST /conversations/evaluate-line
    │   FormData: { conversationId, lineId, audioFile }
    │   → { score, transcription, feedback }
    │   → Lưu score vào lineScores[]
    │
    ├─ avgScore = average(lineScores)
    │
    ├─ POST /sessions/{id}/answer
    │   { itemId: conversationId, itemType: 4, userAnswer: avgScore }
    │
    └─ PUT /sessions/{id}/submit { score: avgScore }
```

---

## Ghi chú cho Frontend

1. **Flow chuẩn:** `POST /sessions` → lặp `POST /answer` hoặc `/answer/audio` → `PUT /submit`
2. **Ping:** Set interval 3 phút khi start, clear khi submit.
3. **Score tính bởi FE:** `score = (số câu đúng / totalItems) * 100`, gửi lên khi submit.
4. **Fallback AI:** Nếu AI unavailable, server dùng `qualityScore = 0`. FE vẫn nhận response bình thường.
5. **Audio format:** `.webm` (MediaRecorder default) được recommend. Server cũng nhận `.wav`, `.mp3`, `.m4a`.
6. **Conversation:** Xem [05_conversations.md](./05_conversations.md) cho flow chi tiết.
