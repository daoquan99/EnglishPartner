# Sửa sau review — vòng 2

Đây vẫn là task `docs/tasks/20261102-observability/plan.md`. Ràng buộc giữ nguyên như vòng trước:
- không mạng;
- không commit;
- không đổi route, DTO hay hub;
- không đụng `docs/tasks/20261102-grammar-a2-content/` và `docs/tasks/20261102-learning-progress/`.

**Ngoại lệ duy nhất về package:** được thêm `Microsoft.AspNetCore.SignalR.Client` bản **10.0.12** (đã có trong cache) vào `tests/EnglishLiveTutor.Api.IntegrationTests` để viết test end-to-end. Không thêm package vào `src`.

## Cao

1. **Sampler lọc stream audio không bao giờ khớp.** Trên .NET 10, SignalR đặt tên activity là hằng `Microsoft.AspNetCore.SignalR.Server.InvocationIn`; tên method nằm trong tag `rpc.method`, `rpc.service` là tên hub. `DisplayName` chỉ được đặt sau khi activity đã tạo.
   - `HubStreamDroppingSampler`: trả Drop khi `Name == "Microsoft.AspNetCore.SignalR.Server.InvocationIn"` **và** `samplingParameters.Tags` có `rpc.method` thuộc {`StreamUserAudioToTutor`, `StreamAudioForTranscription`, `StreamAudioForTranslation`}. Giữ điều kiện `EndsWith` làm dự phòng.
   - Thêm `HubStreamFilteringProcessor : BaseProcessor<Activity>`, đăng ký **trước** exporter. Trong `OnEnd`, nếu `activity.GetTagItem("rpc.method")` thuộc tập trên thì `activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded` để exporter bỏ qua. Processor này phòng trường hợp tag chưa có lúc sampling.
   - **Test end-to-end bắt buộc** (`Api.IntegrationTests`):
     - Dùng `ApiTestFactory`/`WebApplicationFactory`. Dựng `HubConnection` thật tới `/hubs/tutor` với `HttpMessageHandlerFactory = _ => server.CreateHandler()` và transport LongPolling. Auth theo cách các test hub hiện có đang làm; nếu chưa có cách nào thì tạo JWT test giống test auth hiện có.
     - Gọi `StreamUserAudioToTutor` với payload hợp lệ nhỏ. Được phép lỗi ở tầng nghiệp vụ, ví dụ chưa có phiên; chỉ cần có invocation.
     - Thu activity bằng `ActivityListener` nghe source `Microsoft.AspNetCore.SignalR.Server`. Assert tên thật của activity và tag `rpc.method`, và ghi tên đó vào report.
     - Đưa đúng activity thu được qua **chính sampler và processor** của app. Assert nó bị Drop hoặc không còn cờ Recorded, còn một invocation hub khác (method không phải stream) thì vẫn giữ.
     - Nếu không dựng được `HubConnection` thì dừng lại, ghi rõ lý do vào report. Không thay bằng test giả tên.

2. **`GeminiLiveSessionBridge` đổi loại exception.** Bỏ khối bọc `ConnectAsync` thành `InvalidOperationException` (khoảng dòng 100–104). Exception gốc phải được ném nguyên như trước vòng sửa (xem `git diff`). Thông điệp của `WebSocketException`/`HttpRequestException` lúc upgrade không chứa query string. Thêm test: hủy token lúc connect thì ném `OperationCanceledException`.

3. **Ghi đè `opt.Endpoint` làm hỏng `http/protobuf`.** Bỏ ba khối `opt.Endpoint = …` trong `ObservabilityServiceCollectionExtensions` và để SDK tự đọc `OTEL_EXPORTER_OTLP_*`. Chỉ giữ phần kiểm tra có endpoint để quyết định có thêm exporter hay không. Khi chỉ có `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT` thì vẫn phải bật log exporter, không được thoát sớm.

## Trung bình

4. **Timeout bị ghi thành `canceled`.** Trong `AiCallScope` (đã có token từ `Start`): nếu `ex is OperationCanceledException` và token của scope **chưa** bị hủy, hoặc `ex.InnerException is TimeoutException`, thì ghi `timeout`. `Dispose` khi chưa gọi `Succeed` hay `Fail` cũng xét theo cùng logic. Mọi adapter phải truyền `ct` vào `Start`.
   - Thêm test: `HttpClient { Timeout = 50ms }` với handler delay 1 giây thì ra `error.type=timeout`; hủy token của caller thì ra `canceled`.
5. **Assert telemetry cho từng adapter.** Trong `GeminiAdapterTelemetrySecurityTests`, thêm helper kiểm:
   - ca thành công: có measurement `gen_ai.client.operation.duration` với `ept.ai.feature` đúng và không có `error.type`; có token input/output khi response có `usageMetadata`;
   - ca HTTP 500: `error.type=http_500`.

   Ca nào cần phân biệt measurement của test song song thì lọc theo một model code riêng cho mỗi test.
6. **Test telemetry của bridge.** Dùng lớp con giống `ControllableBridge` (`tests/Modules/LiveTutor/.../ConversationOwnershipAndRealtimeLifecycleTests.cs:205`) hoặc một seam tương đương, không gọi mạng. Kiểm:
   - connect rồi dispose thì cặp +1/−1 đúng một lần;
   - dispose hai lần thì −1 chỉ một lần;
   - dispose trong lúc connect thì counter vẫn cân bằng.

   Nếu không làm được mà không gọi mạng thì ghi lý do vào report.
7. **Listener trong test.**
   - `SetupTelemetryListeners` trả về `IDisposable` và được dùng với `using`.
   - `AiTelemetryUnitTests` không dùng `Assert.Single` trên meter static dùng chung. Thay bằng lọc theo một giá trị riêng của test (model code hoặc feature riêng).

## Thấp

8. Đổi `_isDisposed` sang `Volatile.Read/Write` hoặc `int` với `Interlocked`. Giữ cặp +1/−1 đúng.
9. `NormalizeModelCode`: gọi `ToLowerInvariant()` trước khi so regex.
10. Bridge chỉ giữ **một** cách truyền model code (qua constructor). Bỏ hai overload `ConnectAndSetupAsync` mới. Model code Live trong `GeminiLiveConnectionManager` (khoảng các dòng 792/870, 975/1065) tách thành hằng hoặc biến dùng chung, không gõ lặp chuỗi.
11. Token output của Live: đọc `responseTokenCount` trước, sau đó `candidatesTokenCount`, cuối cùng mới tính `total − prompt`. Gom logic đọc token thành một helper `internal` trong `AiTelemetry` để bridge và scope cùng dùng.
12. `LessonAiGrader` / `DictationAiReviewer`: bỏ lần `JsonNode.Parse` thêm vào ở vòng trước. Gọi `Succeed()` sau đoạn parse sẵn có; parse lỗi thì `Fail("invalid_response")`. Hành vi và message giữ nguyên như trước task.
13. `Fail(Exception)`: bỏ việc phân loại theo nội dung message (`"timeout"`, `"JSON"`, `"hợp lệ"`). Adapter tự gọi `Fail("invalid_response")` ở đúng nhánh.
14. Sắp xếp `using EnglishLiveTutor.Infrastructure.Observability;` theo đúng thứ tự `using` của file xung quanh.

## Kiểm chứng (trong `EnglishPartner.BE`)

```bash
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
grep -rn "key=" src --include=*.cs
```

Kết quả bắt buộc:
- build 0 lỗi và 0 warning;
- toàn bộ test pass;
- snapshot không sửa.

## Worker report

Ghi lần lượt cho từng mục 1–14:
- đã sửa gì (file:dòng);
- test nào chứng minh;
- **tên activity SignalR thật quan sát được** (riêng mục 1);
- những gì không làm được, kèm lý do.

Cuối report ghi lệnh đã chạy và số test.
