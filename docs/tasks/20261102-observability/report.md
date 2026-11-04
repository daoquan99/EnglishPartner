# Report — 20261102-observability

Trạng thái: **Xong**, chờ commit trên `development`. Tài liệu vận hành: [docs/observability.md](../../observability.md).

## Đã làm

| Phần | Nội dung |
| --- | --- |
| Hạ tầng | `Shared.Infrastructure/Observability/`. `AddEnglishPartnerObservability` cấu hình trace (AspNetCore bỏ `/health*`, HttpClient, SignalR, nguồn `EnglishLiveTutor.Ai`), metrics (AspNetCore, HttpClient, runtime, meter AI) và log qua OTLP. Không có biến `OTEL_EXPORTER_OTLP_*ENDPOINT` nào thì không gọi `AddOpenTelemetry()`. Mỗi loại tín hiệu chỉ bật exporter khi có endpoint của nó. SDK tự đọc endpoint, protocol và header. `OTEL_SERVICE_NAME` ghi đè được tên mặc định `EnglishLiveTutor.Api`. |
| Lấy mẫu | `ParentBased(TraceIdRatio)`, dùng cùng ratio cho remote parent nên client không ép lấy mẫu được. Ratio kẹp trong [0, 1]: 1.0 ở Dev, 0.2 ở môi trường khác. `HubStreamDroppingSampler` drop 3 method stream audio. `HubStreamFilteringProcessor` là lớp dự phòng. |
| `AiTelemetry` | `gen_ai.client.operation.duration`, `gen_ai.client.token.usage` (input/output lấy từ `usageMetadata`). Tag gồm provider, operation, model (chuẩn hóa về tập giới hạn, ngoài tập thì ghi `other`), `ept.ai.feature`, `error.type`. Bắt buộc gọi `Succeed()`; nếu dispose mà chưa gọi thì ghi lỗi. Phân biệt `timeout` với `canceled` theo token của caller. |
| 9 adapter HTTP | Grammar, Lessons (generate, roleplay, grade, dictation), LiveTutor (translate, correction), Speech (assess), Vocabulary (generate). Logic prompt, retry và parse giữ nguyên. |
| Gemini Live | `ept.livetutor.sessions.active`, `ept.livetutor.session.duration` và token theo `ept.livetutor.role` (`tutor`, `transcribe`, `translate`, `tts`), dùng model thật. Cặp +1/−1 được giữ đúng cả khi race với dispose. Không đo theo từng chunk. |
| Bảo mật | Bỏ `?key=` ở 5 lời gọi HTTP (Vocabulary, Pronunciation, Translator ×2, Correction); key gửi qua header `x-goog-api-key`. Bỏ `rawBody` khỏi message exception ở Pronunciation và Vocabulary (loại exception và status giữ nguyên). Tắt `IncludeScopes`. Không bật `RecordException`. |

Các hành vi phụ đã chấp nhận:
- Pronunciation escape model code trong URL.
- Message lỗi Pronunciation và Vocabulary không còn body của provider.
- `GeminiLiveSessionBridge.DisposeAsync` giờ idempotent: lần gọi thứ hai trả về ngay, trước đây ném `ObjectDisposedException`.
- Thêm package test `Microsoft.AspNetCore.SignalR.Client` **10.0.13**. Bản 10.0.12 trong cache kéo theo `SignalR.Common` có lỗ hổng mức High (NU1903).

## Worker và vòng sửa

| Vòng | Worker | Kết quả review |
| --- | --- | --- |
| O1+O2 | AGY | Security, performance và code đều ĐẠT CÓ ĐIỀU KIỆN: còn 5 chỗ `?key=`, test chưa đủ chặt, span cho từng chunk audio, tag model không giới hạn, lỗi bị ghi là thành công, metric Live sai. |
| fix-1 | AGY | Security ĐẠT CÓ ĐIỀU KIỆN. Performance KHÔNG ĐẠT: sampler so tên activity tự đặt, trong khi .NET 10 dùng `Microsoft.AspNetCore.SignalR.Server.InvocationIn` kèm tag `rpc.method`. Code: bridge đổi loại exception, ghi đè endpoint làm hỏng `http/protobuf`, timeout bị ghi là `canceled`. |
| fix-2 | AGY | Security ĐẠT. Performance ĐẠT CÓ ĐIỀU KIỆN. Code KHÔNG ĐẠT: worker sửa luôn phần xử lý close/reconnect của bridge và thêm method chết `SendTextTurnAsync`, cả hai ngoài phạm vi. |
| Hoàn thiện | Claude | Khôi phục phần close/reconnect và các đoạn chỉ đổi hình thức về đúng như HEAD. Bỏ `SendTextTurnAsync`. Ghi `timeout` cho các lỗi từ `GeminiHttpHelper`. Giữ nguyên `JsonException` ở Grader và Dictation. Bổ sung `OTEL_SERVICE_NAME`, nâng gói SignalR.Client. Thêm test pipeline xuất thật. |

Worker vượt phạm vi 2 lần trong cùng task (đổi loại exception, đổi phần xử lý close), nên orchestrator tự sửa phần cuối theo luật trong `CLAUDE.md`.

## Kiểm chứng (orchestrator tự chạy)

| Lệnh / bước | Kết quả |
| --- | --- |
| `dotnet build EnglishLiveTutor.slnx` | 0 lỗi, 0 warning (build tăng dần). Build sạch chỉ còn 6 warning nullable có sẵn trong `GrammarContentPackImporter`. |
| `dotnet test EnglishLiveTutor.slnx` | **749/749 pass** (baseline 681). `ApiContractSnapshotTests` pass, không sửa snapshot. |
| `dotnet list EnglishLiveTutor.slnx package --vulnerable --include-transitive` | Không có package nào có lỗ hổng. |
| `grep -rn "key=" src --include=*.cs` | Chỉ còn `GeminiLiveSessionBridge` (nợ T1) và chuỗi che `key=[REDACTED]`. |
| E2E OTLP qua Jaeger 1.62 (docker), API chạy trên DB tạm `LingoMint_OtelCheck` (đã xóa) | `grpc` (4317): 3 trace. `http/protobuf` (4318): 3 trace. `/health` không có trace. Log không có `fail` hay `crit`. **Không đụng `LingoMint`.** |
| `SignalRHubStreamExportPipelineTests` | `HubConnection` thật chạy qua `TracerProvider` thật (sampler, processor, exporter). Span `NotifyUserSpeechEnd` được xuất, span `StreamUserAudioToTutor` thì không. Sampler đã thấy `rpc.method` ngay lúc sampling, nên activity của chunk bị drop từ đầu. |

## Nợ còn lại

- **T1 (bảo mật, Thấp):** Gemini Live WebSocket vẫn gửi key trong query (`GeminiLiveSessionBridge.cs:17`).
  - Đã giảm thiểu: không đi qua IHttpClientFactory, không có log hay scope chứa URI, exception kết nối giữ nguyên message gốc (thường chỉ có host và port).
  - **Không được bật** `System.Net.Http.DisableUriRedaction` hoặc `OTEL_DOTNET_EXPERIMENTAL_*_DISABLE_URL_QUERY_REDACTION`.
  - Task sau: thử `_ws.Options.SetRequestHeader("x-goog-api-key", …)` với provider thật.
  - Ngoài phạm vi, có từ trước: `FastTextTranslatorFallback._discoveredModelsByKey` dùng key thô làm khóa dictionary trong singleton, trái ADR-0003. Nên mở task riêng.
- `GrammarCorrectionAnalyzerService`: khi `correctedText` rỗng thì ghi `invalid_response`, nên tỉ lệ lỗi `livetutor.correction` có thể cao hơn thực tế.
- Chưa đo thời gian truy vấn EF/SqlClient, vì gói instrumentation EF đang là beta.

## Chưa kiểm chứng được

- Số token thật từ Gemini HTTP và Gemini Live. Riêng `usageMetadata` của Live là cộng dồn hay theo turn thì chưa rõ. Việc này cần bạn chạy app với key thật rồi xem dashboard.
- Metrics thực sự tới collector: Jaeger chỉ nhận trace. Metrics dùng cùng cấu hình exporter và đã có unit test bằng `MeterListener`.
- Số span mỗi giây khi chạy một phiên live thật.
