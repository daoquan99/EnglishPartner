# Plan: timeout cho các lời gọi AI (FE 30 phút, BE 10 phút, cấu hình trong appsettings)

Mã task: `20261102-ai-timeouts` · Loại: thay đổi cấu hình/hành vi nhỏ, không đổi contract · Người làm: Claude (trực tiếp)

## Yêu cầu của người dùng

- FE: mọi request gọi AI đều timeout **30 phút**.
- BE: mọi lời gọi provider AI mặc định **10 phút**. Timeout của từng loại nằm trong `appsettings.json` để dễ đổi.

## Thiết kế

### BE

Mỗi loại lời gọi có một section riêng trong `appsettings.json`. Giá trị mặc định là 600 giây, ở cả `appsettings.json` lẫn giá trị mặc định trong lớp options.

| Section | Dùng cho |
| --- | --- |
| `Ai:VocabularyGeneration:TimeoutSeconds` | Sinh từ vựng (trước đây 3000) |
| `Ai:GrammarGeneration:TimeoutSeconds` | Sinh bài, soạn lý thuyết Grammar (trước đây 120) |
| `Ai:LessonGeneration:TimeoutSeconds` | Sinh bài học (trước đây 120) |
| `Ai:LessonAssessment:TimeoutSeconds` | Chấm bài Lessons/Grammar, review dictation (trước đây 60) |
| `Ai:LessonRolePlay:TimeoutSeconds` | Role-play (trước đây 45) |
| `Ai:PronunciationAssessment:TimeoutSeconds` | **Mới.** Chấm phát âm (trước đây là mặc định 100 giây của HttpClient) |
| `Ai:PronunciationTts:TimeoutSeconds` | **Mới.** Thời gian chờ Gemini Live trả audio TTS (trước đây hard-code 18 giây) |
| `Ai:AntigravityCli:TimeoutSeconds` | AGY Local (trước đây 120) |

Giới hạn validate của Lessons được nâng từ 600 lên 3600 giây cho đồng bộ với Grammar và Vocabulary.

**Ngoại lệ có chủ đích:** `Ai:LiveTutor` gồm `TranslationFallbackTimeoutSeconds` = 8 và `GrammarCorrectionTimeoutSeconds` = 10. Hai giá trị này đã chuyển vào appsettings nhưng vẫn để ngắn. Đây là helper chạy trong phiên live: SignalR mặc định xử lý tuần tự các invocation của một connection, nên nếu provider bị treo 10 phút thì cả phiên live cũng đứng 10 phút. Muốn tăng thì người dùng sửa trong appsettings. Handshake mở websocket Gemini Live (10 giây, `GeminiLiveSessionBridge`) không thuộc nhóm lời gọi AI request/response, nên giữ nguyên.

### FE

- `src/shared/config/timeouts.ts` định nghĩa `AI_REQUEST_TIMEOUT_MS`, mặc định 30 phút, có thể ghi đè bằng `VITE_AI_REQUEST_TIMEOUT_MS`.
- Áp dụng cho:
  - Grammar: nộp bài, sinh bài, soạn lý thuyết AI, sinh bài phía admin.
  - Lessons: sinh bài, nộp bài, chấm lại, review dictation, synthesize.
  - Vocabulary: sinh từ vựng, synthesize, chấm phát âm.
  - Falling words: synthesize.
- Các request khác giữ mặc định 30 giây của `httpClient`.

### AGY (bổ sung sau khi chạy thử agy thật)

Đặt `SandboxedSettings` là `deny: read_file(*), write_file(*), command(*), mcp(*), read_url(*)`. Xem `docs/tasks/20261102-agy-local-hardening/report.md`.

## Kiểm chứng

- `dotnet build` / `dotnet test` (BE)
- `npm run build` / `npm run test` (FE)
- code-reviewer
