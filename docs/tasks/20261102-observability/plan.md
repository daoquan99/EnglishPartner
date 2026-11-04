# Plan: Quan sát hệ thống bằng OpenTelemetry

- Mã task: `20261102-observability`
- Ngày: 2026-11-02
- Liên quan: luật 3, 4, 5, 8; ADR-0003 (BYO key), ADR-0006 (Shared.Infrastructure); nợ cũ "`GeminiVocabularyAiGenerator` còn `?key=`" (report `20261029-grammar-roadmap`)
- **Cần người dùng duyệt:** Có — thêm package NuGet, thêm cấu hình mới. Không đổi contract, không migration.
- Trạng thái: Đã duyệt 2026-11-02 ("ok bạn làm đi") — gồm các điểm cần chốt theo đề xuất

## Mục tiêu

Thu trace theo request (HTTP vào, HTTP ra, SignalR) và metrics cho mọi lần gọi Gemini: số lần, độ trễ, kết quả, số token vào/ra theo tính năng và model. Mục đích là biết chi phí AI và chỗ chậm khi chạy thật. Dữ liệu xuất qua OTLP. Ở môi trường dev, xem bằng Aspire Dashboard.

## Tiêu chí hoàn thành

- [ ] Khi đã cấu hình `OTEL_EXPORTER_OTLP_ENDPOINT`, app xuất trace, metrics và log qua OTLP. Khi chưa cấu hình, app không thêm exporter, chạy như hiện tại và không lỗi.
- [ ] 9 adapter HTTP gọi Gemini và `GeminiLiveSessionBridge` đều ghi metric `gen_ai.client.operation.duration` (histogram, đơn vị giây) và `gen_ai.client.token.usage` (histogram, gắn tag `gen_ai.token.type` = input/output, lấy từ `usageMetadata`). Tag: `gen_ai.provider.name=gemini`, `gen_ai.request.model`, `ept.ai.feature` (vd. `lesson.grade`, `vocabulary.generate`), `error.type` khi lỗi.
- [ ] Gemini Live: `ept.livetutor.sessions.active` (up-down counter) và `ept.livetutor.session.duration` (histogram). **Không** tạo activity hay metric cho từng chunk audio hoặc transcript.
- [ ] Không tag, span hay log nào chứa API key, prompt, nội dung trả lời, transcript hay audio. Có test chứng minh: gọi adapter với key giả, rồi quét toàn bộ tag và URL của activity đã thu xem key giả có xuất hiện không.
- [ ] `GeminiVocabularyAiGenerator` gửi key qua header `x-goog-api-key` thay vì `?key=`.
- [ ] Unit test cho `AiTelemetry` dùng `MeterListener`/`ActivityListener`: thành công, lỗi, timeout, có `usageMetadata`, thiếu `usageMetadata`.
- [ ] Build 0 lỗi, toàn bộ test pass, danh sách 174 endpoint không đổi.
- [ ] Có `docs/observability.md`: lệnh chạy Aspire Dashboard (image ghim phiên bản), biến môi trường, danh sách metric/tag, ví dụ truy vấn chi phí (token × đơn giá).

## Hiện trạng đã xác minh

- Chưa có code nào dùng OpenTelemetry, `ActivitySource` hay `Meter` trong `src`.
- Các gói OTel 1.20.x đã có trong cache NuGet trên máy: `OpenTelemetry.Extensions.Hosting` 1.20.1, `Instrumentation.AspNetCore` 1.20.0, `Instrumentation.Http` 1.20.0, `Instrumentation.Runtime` 1.20.0, `Exporter.OpenTelemetryProtocol` 1.20.0.
- Có 9 adapter gọi Gemini qua HTTP:
  - Grammar: `GeminiGrammarExerciseAiGenerator`
  - Lessons: `GeminiLessonAiGenerator`, `GeminiRolePlayReplyProvider`, `LessonAiGrader`, `DictationAiReviewer`
  - LiveTutor: `FastTextTranslatorFallback`, `GrammarCorrectionAnalyzerService`
  - Speech: `GeminiPronunciationAiAssessor`
  - Vocabulary: `GeminiVocabularyAiGenerator`
- Gemini Live chạy qua WebSocket trong `Shared.Infrastructure/Ai/GeminiLiveSessionBridge.cs`.
- Phần lớn adapter dùng `AddHttpClient<>`. `FastTextTranslatorFallback` và `GrammarCorrectionAnalyzerService` có constructor `new HttpClient(handler)` dành cho test.
- Chưa adapter nào đọc `usageMetadata`.

## Thiết kế

- **`Shared.Infrastructure/Observability/`**:
  - `ObservabilityServiceCollectionExtensions.AddEnglishPartnerObservability(builder)`: đặt resource `service.name=EnglishLiveTutor.Api` và version; tracing gồm AspNetCore (bỏ `/health*`), HttpClient, các source `Microsoft.AspNetCore.SignalR.Server` và `EnglishLiveTutor.Ai`; metrics gồm AspNetCore, HttpClient, Runtime và meter `EnglishLiveTutor.Ai`; logging qua `builder.Logging.AddOpenTelemetry`. Exporter OTLP chỉ được thêm khi có endpoint. Mẫu trace: `ParentBased(TraceIdRatio)`, tỉ lệ đọc từ `Observability:TraceSampleRatio` (mặc định 1.0 ở Dev, 0.2 ở môi trường khác).
  - `AiTelemetry` (static `ActivitySource` + `Meter`) và `AiCallScope : IDisposable`. Cách dùng: `using var call = AiTelemetry.Start("lesson.grade", model); … call.RecordUsage(responseJson); call.Fail(ex)`. Scope tự đo thời gian và ghi `error.type` (`timeout`, `http_<status>`, `invalid_response`, `canceled`). `RecordUsage` chỉ đọc `usageMetadata.promptTokenCount` và `candidatesTokenCount`/`totalTokenCount`.
- **Gắn vào adapter:** mỗi adapter chỉ thêm vài dòng bao quanh lời gọi hiện có. Không đổi logic retry, timeout hay parse.
- **Live:** `GeminiLiveSessionBridge` tăng counter khi kết nối và giảm khi đóng, ghi thời lượng phiên. Nếu message `usageMetadata` của Live có token thì cộng dồn và ghi một lần khi phiên kết thúc.
- **Bảo mật:** OTel HttpClient instrumentation 1.20 mặc định che giá trị query string trong `url.full`. Dù vậy vẫn bỏ `?key=` (yêu cầu ở trên) và có test kiểm. Không bật `RecordException` cho exception có thể chứa body.
- **Cấu hình:** dùng các biến môi trường OTel chuẩn (`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`). Thêm `Observability:TraceSampleRatio`. Không commit endpoint thật.

## Phạm vi

| Được sửa | Không được sửa |
| --- | --- |
| `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Observability/**` (mới), `.csproj` Shared.Infrastructure (package) | Route, DTO, hub method/event |
| `src/EnglishLiveTutor.Api/Program.cs` (1 lời gọi), `appsettings*.json` (chỉ `Observability:TraceSampleRatio`) | Migration, DbContext |
| 9 adapter Gemini nêu trên + `GeminiLiveSessionBridge` (chỉ thêm telemetry, cùng việc bỏ `?key=`) | Logic prompt, retry, parse |
| `tests/EnglishLiveTutor.Api.IntegrationTests` hoặc `tests/Modules/*/…Tests` (test mới) | Snapshot contract |
| `docs/observability.md` | |

## Contract thay đổi

Không có.

## Dữ liệu và migration

Không có.

## Phân công

| Phần việc | Worker | Scope | Skill cần đọc |
| --- | --- | --- | --- |
| O1. Hạ tầng OTel + `AiTelemetry` + test | AGY | be | `EnglishPartner.BE/.agents/skills/ai-provider-integration/SKILL.md`, `.../infrastructure-persistence/SKILL.md`, `.../backend-testing/SKILL.md` |
| O2. Gắn telemetry vào 9 adapter + Live + bỏ `?key=` + test không lộ key | AGY (cùng session) | be | như trên + `.agents/skills/realtime-session/SKILL.md` |
| O3. `docs/observability.md` | Claude | docs | |

Worker chạy `--in-place` trên `development`, không có mạng, restore bằng `--ignore-failed-sources`.

## Kiểm chứng

```bash
# trong EnglishPartner.BE
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
dotnet list EnglishLiveTutor.slnx package --vulnerable
```

Orchestrator tự chạy Aspire Dashboard bằng docker, khởi động app với `OTEL_EXPORTER_OTLP_ENDPOINT` và xác nhận có trace của một request.

Không kiểm chứng được tĩnh: số token thật từ Gemini và Gemini Live (cần provider thật). Bạn chạy một lần với key thật rồi xem dashboard.

## Review cần chạy

- [ ] security-reviewer (key, nội dung prompt trong telemetry)
- [ ] performance-reviewer (đường realtime, cardinality của tag, tỉ lệ sampling)
- [ ] code-reviewer

## Rủi ro và câu hỏi mở

- Cardinality: `gen_ai.request.model` và `ept.ai.feature` là tập nhỏ, cố định. Không gắn `userId` hay `conversationId` vào metric (chỉ được đưa vào span attribute nếu cần, và hiện cũng không đưa).
- Package mới làm tăng bề mặt phụ thuộc. Chạy `--vulnerable` và giữ bản ghim `System.Security.Cryptography.Pkcs`.
- Instrumentation cho EF Core/SqlClient (thời gian query) để ngoài phạm vi, vì gói EF instrumentation vẫn là beta. Có thể thêm sau.
