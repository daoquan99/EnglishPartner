---
trigger: manual
---

# AI — Core Architecture

> **Scope:** Core services, GeminiService internals, Cross-module dependencies

---

## 1. Service Interfaces (`Application.Abstractions`)

| Interface | Nhiệm vụ | Implementation |
|---|---|---|
| `IGeminiService` | Đánh giá, Chat, Generate content (JSON) | `MsccGeminiService` |
| `ISpeechService` | TTS Neural2, STT qua Google Cloud SDK | `GoogleCloudSpeechService` |
| `IGeminiTtsService`| TTS qua Gemini texttospeech REST API | `GeminiTtsService` |
| `ILiveAudioSession`| Quản lý connection tới Gemini Live | `GoogleGenAILiveAudioSession`|

---

## 2. GeminiService — Internal Architecture

Mọi request tới Gemini trong `IGeminiService` **BẮT BUỘC** đi qua hàm `ExecuteGeminiRequestAsync()`:

1. Gửi POST tới Gemini REST endpoint.
2. Đo latency bằng Stopwatch.
3. Parse response JSON (`candidates[0].content.parts[0].text`).
4. Tự động tạo `AiRequestLog`.
5. Gọi `dbContext.AiRequestLogs.Add(log)` (Không gọi SaveChanges — để UoWBehavior xử lý sau khi handler xong).

### Config:
- **Tắt kiểm duyệt an toàn:** Set `GeminiAI:DisableAllSafetyRestrictions = true` (quan trọng để học tiếng Anh tự nhiên).
- Dùng `responseMimeType: "application/json"` cho tất cả prompt cần cấu trúc.

---

## 3. Cross-Module Dependencies

Module AI chỉ giao tiếp với các module khác thông qua project `.Contracts`. KHÔNG tham chiếu trực tiếp.

| Contracts Interface | Nguồn (Module) | Sử dụng tại |
|---|---|---|
| `ICurriculumQueryService` | Curriculum | Lấy text context cho Evaluate |
| `ICurriculumContentGenerationService`| Curriculum | Gọi hàm generate của AI |
| `ILearningProfileQueryService` | Learning | Lấy `CurrentLevel` của user để làm System Prompt cho Chat |
