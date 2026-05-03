---
trigger: manual
---

# AI — Chatbot

> **Scope:** AI Tutor Chat  
> **Endpoints:** `/api/v1/ai/chat/*`  
> **Auth:** Bearer  
> **@mention khi làm:** chat session, send message, system prompt

---

## 1. Start Session

```
POST /api/v1/ai/chat/sessions
Auth: Bearer
Body: { "title": "Chat", "systemPrompt": null }
Response 201: { sessionId }
```

**Flow:**
1. Handler gọi `ILearningProfileQueryService.GetUserLevelAsync(userId)` (qua Contracts).
2. Xây dựng System Prompt dựa trên cấp độ tiếng Anh hiện tại của user (A1-C2).
3. (Nếu user truyền `SystemPrompt` trong body thì sẽ override).
4. `ChatSession.Start(userId, title, systemPrompt)`.

---

## 2. Lấy danh sách & Lịch sử

### Danh sách Sessions của User
```
GET /api/v1/ai/chat/sessions
Auth: Bearer
Response 200: List<ChatSessionResponse>
```

### Lịch sử tin nhắn
```
GET /api/v1/ai/chat/sessions/{sessionId:guid}/messages
Auth: Bearer
Response 200: List<ChatMessageResponse>
```

---

## 3. Send Message

```
POST /api/v1/ai/chat/sessions/{sessionId:guid}/messages
Auth: Bearer + RequireRateLimiting("ai-chat")
Body: { "text": "Hello tutor" }

Response 200: { messageId, reply }
```

**Flow:**
1. Lấy session kèm messages (`GetByIdWithMessagesAsync`).
2. Kiểm tra Ownership: `session.UserId == currentUser`.
3. Thêm message của user: `session.AddUserMessage(text)`.
4. Gọi AI: `geminiService.ChatAsync(systemPrompt, history, text)`.
5. Thêm message của AI: `session.AddModelMessage(reply)`.

**Lỗi:**
- `ChatSession.NotFound` (404)
- `ChatSession.AccessDenied` (403)
