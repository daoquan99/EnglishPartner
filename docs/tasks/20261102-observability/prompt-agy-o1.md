# Việc giao cho AGY: O1 + O2 — OpenTelemetry và telemetry cho lời gọi Gemini

## Bối cảnh (session mới, đọc kỹ)

- Thiết kế và tiêu chí: `docs/tasks/20261102-observability/plan.md`. **Đọc toàn bộ file này trước.**
- Luật chung: `AGENTS.md` (luật 3, 4, 5, 8), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0003-byo-api-key.md`.
- Skill cần đọc:
  - `EnglishPartner.BE/.agents/skills/ai-provider-integration/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
  - `.agents/skills/realtime-session/SKILL.md`
- Solution: `EnglishPartner.BE/EnglishLiveTutor.slnx`. Làm trực tiếp trên `development`, **không commit**.
- Không có mạng. Chỉ dùng package đã có trong cache NuGet và **ghim đúng version** dưới đây:
  - `OpenTelemetry.Extensions.Hosting` 1.20.1
  - `OpenTelemetry.Instrumentation.AspNetCore` 1.20.0
  - `OpenTelemetry.Instrumentation.Http` 1.20.0
  - `OpenTelemetry.Instrumentation.Runtime` 1.20.0
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.20.0

  Nếu restore báo thiếu package phụ thuộc, hạ cả nhóm xuống bản 1.19.x cùng có trong cache và ghi lại vào report.
- Một worker khác đang tạo file trong `docs/tasks/20261102-grammar-a2-content/`. Không đụng vào thư mục đó.

## Yêu cầu

1. **O1, hạ tầng** trong `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Observability/`:
   - Thêm package vào `EnglishLiveTutor.Shared.Infrastructure.csproj`. **Giữ** dòng ghim `System.Security.Cryptography.Pkcs` (đây là bản ghim bảo mật).
   - Tạo `AiTelemetry` và `AiCallScope` theo phần Thiết kế của plan:
     - Tên source/meter: `EnglishLiveTutor.Ai`.
     - Metric: `gen_ai.client.operation.duration` (histogram, đơn vị `s`) và `gen_ai.client.token.usage` (histogram, đơn vị `{token}`, tag `gen_ai.token.type` = `input` | `output`).
     - Tag: `gen_ai.provider.name=gemini`, `gen_ai.operation.name` (`generate_content` hoặc `live`), `gen_ai.request.model`, `ept.ai.feature`, `error.type`.
     - Live: `ept.livetutor.sessions.active` (UpDownCounter) và `ept.livetutor.session.duration` (histogram, đơn vị `s`).
     - Metric **không** được có tag `userId` hay `conversationId`.
   - Tạo `AddEnglishPartnerObservability(this WebApplicationBuilder builder)` (hoặc dạng tương đương trên `IHostApplicationBuilder`):
     - tracing, metrics và logging như plan mô tả;
     - chỉ thêm exporter OTLP khi `OTEL_EXPORTER_OTLP_ENDPOINT` (cấu hình hoặc biến môi trường) có giá trị;
     - đọc sampler từ `Observability:TraceSampleRatio`;
     - bỏ qua `/health*` khi trace.
   - Trong `Program.cs`: gọi hàm trên đúng một lần, ngay sau khi tạo builder.
   - Trong `appsettings.json`: thêm `Observability:TraceSampleRatio` = `0.2`. Trong `appsettings.Development.json`: `1.0`.
2. **O2, gắn telemetry vào adapter.** Mỗi adapter dưới đây bọc lời gọi Gemini bằng `AiTelemetry.Start(feature, model)`, gọi `RecordUsage` sau khi nhận response JSON, và gọi `Fail` khi có lỗi hoặc timeout. **Không đổi** logic prompt, retry, timeout hay parse.

   | Adapter | `ept.ai.feature` |
   | --- | --- |
   | `GeminiGrammarExerciseAiGenerator` | `grammar.generate` (dùng tên riêng cho đường soạn lý thuyết nếu có, ví dụ `grammar.theory_draft`) |
   | `GeminiLessonAiGenerator` | `lesson.generate` |
   | `GeminiRolePlayReplyProvider` | `lesson.roleplay` |
   | `LessonAiGrader` | `lesson.grade` |
   | `DictationAiReviewer` | `lesson.dictation_review` |
   | `FastTextTranslatorFallback` | `livetutor.translate` |
   | `GrammarCorrectionAnalyzerService` | `livetutor.correction` |
   | `GeminiPronunciationAiAssessor` | `speech.assess` |
   | `GeminiVocabularyAiGenerator` | `vocabulary.generate` |

   - `GeminiLiveSessionBridge` (`src/Shared/.../Ai/`):
     - khi kết nối thành công: active +1;
     - khi `DisposeAsync`: active −1 (chỉ trừ một lần, kể cả khi dispose nhiều lần), đồng thời ghi thời lượng phiên;
     - nếu message server có `usageMetadata` thì cộng dồn token và ghi **một lần** khi dispose.
     - Không ghi gì ở đường gửi/nhận audio chunk.
   - `GeminiVocabularyAiGenerator`: bỏ `?key=` trên URL, gửi key qua header `x-goog-api-key` giống các adapter khác.
3. **Test** (đặt trong project test của module tương ứng hoặc `tests/EnglishLiveTutor.Api.IntegrationTests`):
   - `AiTelemetry`, đo bằng `MeterListener` và `ActivityListener`:
     - thành công có `usageMetadata`: ghi duration và token input/output đúng;
     - thiếu `usageMetadata`: không ghi token;
     - `Fail` với timeout, `HttpRequestException` có status, và JSON hỏng: `error.type` đúng;
     - dispose hai lần: chỉ ghi một lần.
   - Ít nhất 2 adapter (trong đó có `GeminiVocabularyAiGenerator`) dùng fake `HttpMessageHandler`:
     - có ghi metric với đúng `ept.ai.feature`;
     - API key giả (ví dụ `TEST-KEY-123456`) **không** xuất hiện trong request URI, trong mọi tag của activity hay metric, và trong log đã thu;
     - key có mặt trong header `x-goog-api-key`.
   - Startup không có `OTEL_EXPORTER_OTLP_ENDPOINT`: `ApiTestFactory` vẫn chạy và toàn bộ test hiện có pass.

## Kiểm chứng (chạy trong `EnglishPartner.BE`)

```bash
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
dotnet list EnglishLiveTutor.slnx package --vulnerable --include-transitive
```

Yêu cầu: build 0 lỗi; toàn bộ test pass (hiện có 681 test, cộng thêm test mới); `ApiContractSnapshotTests` pass mà **không sửa file snapshot**; không có package có lỗ hổng.

## Không được

- Đổi route, DTO hay hub method/event.
- Ghi prompt, nội dung response, transcript, audio hay API key vào tag, span hoặc log.
- Thêm package ngoài 5 package trên.
- Sửa migration.
- Commit.

## Worker report

Nộp report gồm:
- các file đã tạo hoặc sửa;
- version package thực tế đã dùng;
- bảng adapter → feature → vị trí gọi;
- lệnh đã chạy kèm kết quả (số test pass/fail, kết quả lệnh `--vulnerable`);
- những gì chưa làm được và lý do.
