# AI API — Chat (AI Tutor)

> **Auth:** Bearer token required  
> **Rate Limit:** `ai-chat` — 10 messages/phút (chỉ SendMessage)

---

## 1. Tạo Chat Session

```http
POST /api/v1/ai/chat/sessions
```

```json
{
  "title": "Grammar Help",
  "systemPrompt": null
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `title` | string | ✅ | Tiêu đề cuộc trò chuyện |
| `systemPrompt` | string | ❌ | Override system prompt (null = auto theo CEFR level) |

> 💡 Nếu `systemPrompt` = null, server tự build prompt dựa trên CEFR level hiện tại (A1-C2) của user.

**Response `201 Created`:**
```json
{ "sessionId": "guid" }
```

---

## 2. Lấy danh sách Sessions

```http
GET /api/v1/ai/chat/sessions
```

**Response `200 OK`:**
```json
[
  {
    "id": "guid",
    "title": "Grammar Help",
    "createdAt": "2026-04-23T10:00:00Z"
  }
]
```

---

## 3. Lấy lịch sử tin nhắn

```http
GET /api/v1/ai/chat/sessions/{sessionId}/messages
```

**Response `200 OK`:**
```json
[
  {
    "id": "guid",
    "role": "User",
    "text": "Can you explain present simple?",
    "createdAt": "2026-04-23T10:01:00Z"
  },
  {
    "id": "guid",
    "role": "Model",
    "text": "Sure! Present simple is used for...",
    "createdAt": "2026-04-23T10:01:02Z"
  }
]
```

---

## 4. Gửi tin nhắn

```http
POST /api/v1/ai/chat/sessions/{sessionId}/messages
```

```json
{
  "text": "Can you explain present simple?"
}
```

**Response `200 OK`:**
```json
{
  "messageId": "guid",
  "reply": "Sure! Present simple is used for habits and general truths..."
}
```

> ⚠️ Rate limited: 10 req/phút. Nếu vượt quá → `429 Too Many Requests`.

---

## Errors

| Code | HTTP | Khi nào |
|---|---|---|
| `AI.ChatSession.NotFound` | 404 | Session không tồn tại |
| `AI.ChatSession.AccessDenied` | 403 | Truy cập session người khác |
