# Quan sát hệ thống (OpenTelemetry)

Backend xuất trace, metrics và log qua OTLP (task `docs/tasks/20261102-observability`). Nếu chưa cấu hình endpoint, app không thêm exporter và chạy như bình thường.

## Chạy local với Aspire Dashboard

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 \
  -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
  mcr.microsoft.com/dotnet/aspire-dashboard:9.1
```

Sau đó chạy API với biến môi trường dưới đây, rồi mở http://localhost:18888.

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run --project EnglishPartner.BE/src/EnglishLiveTutor.Api
```

`DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` chỉ dùng trên máy dev. Ở môi trường thật, trỏ endpoint tới collector có xác thực. Header xác thực đặt qua `OTEL_EXPORTER_OTLP_HEADERS` trong secret store. Không commit giá trị này.

## Cấu hình

| Khóa | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | trống | Địa chỉ OTLP collector, dùng chung cho trace, metrics và log. Không đặt endpoint nào thì app không đăng ký OpenTelemetry. |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` / `…_METRICS_ENDPOINT` / `…_LOGS_ENDPOINT` | trống | Endpoint riêng cho từng loại tín hiệu. Chỉ loại có endpoint mới được bật exporter. Nếu chỉ cấu hình log, cần đặt thêm `OTEL_SERVICE_NAME`; nếu không, log sẽ mang service name mặc định của SDK. |
| `OTEL_SERVICE_NAME` | `EnglishLiveTutor.Api` | Tên service hiển thị trên dashboard |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | Đặt `http/protobuf` khi dùng cổng 4318 |
| `OTEL_EXPORTER_OTLP_HEADERS` | trống | Header xác thực của collector (secret) |
| `Observability:TraceSampleRatio` | `1.0` ở Dev, `0.2` ở môi trường khác | Tỉ lệ lấy mẫu trace (ParentBased + TraceIdRatio) |

Service name: `EnglishLiveTutor.Api`.

## Dữ liệu thu

**Trace:**
- HTTP vào. Bỏ qua `/health*`.
- HTTP ra, gồm các lời gọi Gemini.
- SignalR (`Microsoft.AspNetCore.SignalR.Server`). Ba method stream audio (`StreamUserAudioToTutor`, `StreamAudioForTranscription`, `StreamAudioForTranslation`) bị bỏ qua, vì mỗi giây có khoảng 24 lần gọi cho mỗi học viên.
- Span cho mỗi lời gọi AI, nguồn `EnglishLiveTutor.Ai`.

**Metrics** (meter `EnglishLiveTutor.Ai`, đặt tên theo OpenTelemetry GenAI semantic conventions):

| Metric | Loại | Tag |
| --- | --- | --- |
| `gen_ai.client.operation.duration` (s) | histogram | `gen_ai.provider.name`, `gen_ai.operation.name`, `gen_ai.request.model`, `ept.ai.feature`, `error.type` (khi lỗi) |
| `gen_ai.client.token.usage` ({token}) | histogram | như trên, kèm `gen_ai.token.type` = `input` hoặc `output` |
| `ept.livetutor.sessions.active` | up-down counter | `ept.livetutor.role` = `tutor`, `transcribe`, `translate`, `tts` hoặc `other` |
| `ept.livetutor.session.duration` (s) | histogram | `ept.livetutor.role` |

Ghi chú:
- Mỗi phiên Live Tutor mở 3 kết nối Gemini Live, tương ứng role `tutor`, `transcribe` và `translate`. Khi đọc số phiên đang chạy, lọc theo role `tutor`.
- TTS phát âm cũng mở một kết nối Live ngắn, mang role `tts`.
- Token của Live mang `gen_ai.operation.name=live`, `ept.ai.feature=livetutor.session` và role. Hiện chưa xác minh với provider thật `usageMetadata` của Live là cộng dồn hay theo từng turn.
- Với các adapter Lessons (chạy qua `GeminiHttpHelper`), `gen_ai.client.operation.duration` tính cả thời gian các lần retry.
- `error.type` nhận các giá trị: `timeout`, `canceled`, `invalid_response`, `http_<status>` và `error`.
- `gen_ai.request.model` chỉ nhận mã model hợp lệ, viết thường, tối đa 64 ký tự. Mọi giá trị khác được ghi là `other`.

Ngoài ra còn các metric ASP.NET Core, HttpClient và runtime .NET mặc định.

**Giá trị `ept.ai.feature`:** `grammar.generate` (gồm cả AI soạn nháp lý thuyết, vì hai việc dùng chung một adapter), `lesson.generate`, `lesson.roleplay`, `lesson.grade`, `lesson.dictation_review`, `livetutor.translate`, `livetutor.correction`, `livetutor.session`, `speech.assess`, `vocabulary.generate`.

## Tính chi phí AI

Chi phí ước tính = tổng token × đơn giá của model. Lấy tổng token theo tính năng từ `gen_ai.client.token.usage`, ví dụ bằng PromQL khi đổ về Prometheus:

```promql
sum by (ept_ai_feature, gen_ai_request_model, gen_ai_token_type) (increase(gen_ai_client_token_usage_sum[1d]))
```

Đơn giá thay đổi theo model và theo thời điểm. Lấy đơn giá ở bảng giá Google AI Studio, không ghi cứng trong code.

## Không thu (theo thiết kế)

- Không thu API key, prompt, nội dung trả lời, transcript và audio. Có test chứng minh điều này: `GeminiAdapterTelemetrySecurityTests`.
- Không tạo span hay metric cho từng chunk audio của phiên live.
- Không gắn `userId` hoặc `conversationId` vào metric.
- Chưa đo thời gian truy vấn EF/SqlClient. Gói instrumentation EF Core vẫn đang ở bản beta.
