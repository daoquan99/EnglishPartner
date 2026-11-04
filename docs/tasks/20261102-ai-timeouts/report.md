# Report: timeout cho các lời gọi AI

## Đã làm

### FE

- Thêm `src/shared/config/timeouts.ts` với hằng `AI_REQUEST_TIMEOUT_MS`.
  - Mặc định 30 phút.
  - Đổi được qua `VITE_AI_REQUEST_TIMEOUT_MS`. Giá trị không hợp lệ, hoặc lớn hơn 2³¹−1 ms (timer của trình duyệt bị tràn), sẽ quay về mặc định.
  - Đã thêm biến này vào `.env.example`.
- Áp dụng cho 13 lời gọi AI:
  - Grammar: submit, generate, AI draft lý thuyết, generate phía admin.
  - Lessons: generate, answers, retry, ai-review, synthesize.
  - Vocabulary: generate, synthesize, assessments.
  - Falling words: synthesize.
- Các request còn lại giữ 30 giây như cũ. Không đổi `/api/pronunciation/audio` (chỉ đọc cache) và `/api/prompts/preview` (chỉ dựng chuỗi prompt).

### BE

Mỗi loại lời gọi có section riêng trong `appsettings.json`. Mặc định 600 giây, giới hạn hợp lệ từ 1 đến 3600.

| Section | Trước | Sau |
| --- | --- | --- |
| `Ai:VocabularyGeneration` | 3000 | 600 |
| `Ai:GrammarGeneration` | 120 | 600 |
| `Ai:LessonGeneration` | 120 | 600 |
| `Ai:LessonAssessment` | 60 | 600 |
| `Ai:LessonRolePlay` | 45 | 600 |
| `Ai:PronunciationAssessment` (mới) | 100 (mặc định của HttpClient) | 600 |
| `Ai:PronunciationTts` (mới) | 18 (hard-code) | 600 |
| `Ai:AntigravityCli` | 120 | 600 |
| `Ai:LiveTutor` (mới) | 8 / 10 (hard-code) | **8 / 10, giữ ngắn có chủ đích** |

Vì sao giữ ngắn `Ai:LiveTutor`: đây là helper chạy trong phiên live (dịch dự phòng, gợi ý sửa ngữ pháp). Nếu để timeout 10 phút mà provider bị treo, phiên live cũng bị đứng. Giá trị này vẫn đổi được trong appsettings.

### Sửa theo code review

| Finding | Xử lý |
| --- | --- |
| 1. TTS chờ 600 s sẽ treo request khi websocket đóng (không có `turnComplete`) | Thêm event `GeminiLiveSessionBridge.OnConnectionLost`, gọi khi nhận Close hoặc khi lỗi nhận dữ liệu. TTS bắt event này để dừng chờ ngay (`IsComplete = false`). `turnCompleted` lấy từ kết quả thật của TCS. |
| 2. Retry ở BE có thể vượt 30 phút của FE | `GeminiHttpHelper.PostJsonWithRetryAsync` có thêm một deadline chung bằng `HttpClient.Timeout`. Vì vậy `TimeoutSeconds` là tổng thời gian chờ, kể cả retry và backoff. |
| 3. Thiếu test DI | Thêm test: timeout của typed client chấm phát âm, giá trị mặc định của các options, validate biên (5 trường hợp), helper LiveTutor nhận timeout từ options. Mở rộng `TypedHttpClientConstructorTests` sang assembly Speech. |
| 4. Số 600 bị lặp | Dùng hằng `PronunciationTtsOptions.DefaultTimeoutSeconds`. |
| 5. Validate của AGY | Đổi thành 1..3600. |
| 6. Env FE quá lớn | Giới hạn trần trong `readTimeout`, có test. |

### AGY

Đổi `AgyCliRunner.SandboxedSettings` sang settings có `deny` cho mọi tool. Kết quả chạy thật xem trong `docs/tasks/20261102-agy-local-hardening/report.md`.

## Kiểm chứng

| Lệnh | Kết quả |
| --- | --- |
| `dotnet build EnglishLiveTutor.slnx` | 0 lỗi |
| `dotnet test EnglishLiveTutor.slnx` | 811/811 pass (lần chạy đầu, test deadline bị fail vì kiểm bằng thời gian thực khi máy quá tải; đã sửa sang kiểm số lần gọi và chạy lại thì pass) |
| `npm run build` | pass |
| `npx vitest run` (các thư mục bị ảnh hưởng) | 310/310 pass |
| code-reviewer | ĐẠT CÓ ĐIỀU KIỆN; các điều kiện đã xử lý như bảng trên |

## Chưa kiểm chứng

- Chưa thử timeout với Gemini và Gemini Live thật. Ví dụ: chưa thử một request kéo dài quá 10 phút, chưa thử websocket TTS bị đóng giữa chừng.
- Lời gọi Gemini trực tiếp từ FE trong `useThreeHubLiveSession.ts:294` dùng `fetch` không có timeout. Nó thuộc phiên live và nằm ngoài phạm vi task này.
