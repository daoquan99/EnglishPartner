---
trigger: manual
---

# AI — Index

> **Scope:** Gemini API integration — Chat, TTS, STT, Evaluate, Live Audio  
> DB Schema: `ai`

---

## Tài liệu con

| File | Nội dung |
|---|---|
| [ai_core.md](./ai/ai_core.md) | Kiến trúc lõi (GeminiService), Rate Limits, Auth, Cross-module deps |
| [ai_chat.md](./ai/ai_chat.md) | Flow tạo session Chatbot, gửi tin nhắn, System Prompt tự động |
| [ai_evaluation.md](./ai/ai_evaluation.md) | Đánh giá (chấm điểm) Text và Audio (0-100 Score). Giao tiếp qua `IAiEvaluationService` (`EvaluateTextAnswerAsync`, `EvaluatePronunciationAsync`) |
| [ai_speech.md](./ai/ai_speech.md) | Text-to-Speech (Neural2 + Gemini) và Speech-to-Text. Giao tiếp qua `IAiEvaluationService.GenerateTtsAsync` |
| [ai_live_avatar.md](./ai/ai_live_avatar.md) | SignalR Hub cho Gemini Live Audio streaming |
| [ai_admin.md](./ai/ai_admin.md) | Quản lý request logs của AI |

---

## Key Entities & Aggregates

| Entity | Type | Key Fields |
|---|---|---|
| `ChatSession` | AggregateRoot | UserId, Title, SystemPrompt, Messages[] |
| `ChatMessage` | Entity (child) | SessionId, Role (ChatRole enum: User=0, Model=1), Text |
| `AiRequestLog` | **Entity** (NOT AggregateRoot) | RequestType, InputText, OutputText, ModelName, InputTokens?, OutputTokens?, LatencyMs, IsSuccess, ErrorMessage?, UserId? |

> **Lưu ý:**
> - `ChatSession.Start()`: factory method — gọi trực tiếp.
> - `AiRequestLog`: Immutable audit log. Auto-created trong `ExecuteGeminiRequestAsync` và được lưu tự động nhờ `UoWBehavior`. KHÔNG dùng SoftDelete.

---

## Rate Limits chung

| Endpoint | Policy | Default |
|---|---|---|
| `POST /evaluate` | `ai-scoring` | 50 req/3600s |
| `POST /evaluate-audio` | `ai-scoring` | 50 req/3600s |
| `POST /tts` | `ai-scoring` | 50 req/3600s |
| `POST /gemini-tts` | `ai-scoring` | 50 req/3600s |
| `POST /stt` | `ai-scoring` | 50 req/3600s |
| `POST /chat/.../messages` | `ai-chat` | 10 req/60s |

> *Các endpoint tạo content (`ai-generate`) nằm ở module Curriculum.*

---

## Error Codes (đầy đủ)

> Tất cả error codes đều có prefix `AI.` — ví dụ `"AI.ChatSession.NotFound"`

| Code | HTTP | When |
|---|---|---|
| `AI.ChatSession.NotFound` | 404 | sessionId không tồn tại |
| `AI.ChatSession.AccessDenied` | 403 | user cố truy cập session của người khác |
| `AI.Evaluate.ItemNotFound` | 404 | curriculum itemId không tồn tại (text eval) |
| `AI.Evaluate.MissingContext` | 400 | Không có itemId/itemType VÀ không có itemContext (text eval) |
| `AI.Evaluation.ParseFailed` | 502 | Gemini trả JSON không parse được |
| `AI.Evaluation.ServiceUnavailable`| 503 | Gemini API timeout/error |
| `AI.EvaluateAudio.ItemNotFound` | 404 | curriculum itemId không tồn tại (audio eval) |
| `AI.EvaluateAudio.MissingContext` | 400 | Không có itemId/itemType VÀ không có itemContext (audio eval) |
| `AI.EvaluateAudio.ReadFailed` | 400 | Không đọc được audio file |
| `AI.EvaluateAudio.UploadFailed` | 500 | Không save được audio file |
| `AI.Generation.ParseFailed` | 502 | AI generate content không parse được |
