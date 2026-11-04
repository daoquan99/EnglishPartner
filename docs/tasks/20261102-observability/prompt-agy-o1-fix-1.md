# Sửa sau review — O1/O2 (vòng 1)

Đây là cùng task `docs/tasks/20261102-observability/plan.md`. Hai reviewer độc lập kết luận ĐẠT CÓ ĐIỀU KIỆN. Hãy sửa **tất cả** các mục dưới đây.

Ràng buộc giữ nguyên như prompt trước:
- không mạng;
- không thêm package;
- không commit;
- không đổi route, DTO hay hub;
- không đụng `docs/tasks/20261102-grammar-a2-content/` và `docs/tasks/20261102-learning-progress/`.

## Bảo mật

1. **Bỏ `?key=` ở mọi adapter HTTP.** Các chỗ cần sửa:
   - `GeminiPronunciationAiAssessor.cs:67`
   - `FastTextTranslatorFallback.cs:44` (GET list models) và `:162`
   - `GrammarCorrectionAnalyzerService.cs:107`

   Chuyển sang `HttpRequestMessage` và header `x-goog-api-key`, làm giống `GeminiVocabularyAiGenerator`. Sau khi sửa, `grep -rn "key=" src --include=*.cs` chỉ được còn `GeminiLiveSessionBridge`.

   Với **Gemini Live (WebSocket), giữ nguyên `?key=`**: chưa kiểm được provider có nhận header hay không. `ClientWebSocket` không đi qua HttpClient instrumentation. Hãy kiểm tra để chắc chắn không có log hay exception nào in ra URI có key, và ghi lại trong report.

2. Trong `ObservabilityServiceCollectionExtensions`, đặt `logging.IncludeScopes = false`. Lý do: scope của IHttpClientFactory chứa URI request.

3. **Bỏ `rawBody` khỏi message exception** ở `GeminiPronunciationAiAssessor.cs` (khoảng dòng 101) và `GeminiVocabularyAiGenerator.cs` (khoảng dòng 86). Message chỉ giữ mã HTTP, theo mẫu `GeminiGrammarExerciseAiGenerator`. Không đổi loại exception và không đổi status trả về client.

4. **Viết lại test `GeminiAdapterTelemetrySecurityTests`.** Phủ cả 7 adapter HTTP:
   - Grammar
   - LessonGenerator, RolePlay, LessonAiGrader, DictationAiReviewer (qua `GeminiHttpHelper`)
   - Translator, Correction, Pronunciation, Vocabulary

   Gặp adapter nào khó dựng thì ghi lý do vào report, nhưng tối thiểu phải có 6 adapter. Với mỗi adapter:
   - dùng key giả `TEST-KEY-123456`;
   - fake handler ghi lại mọi request;
   - assert `RequestUri` không chứa key và header `x-goog-api-key` bằng key giả;
   - gắn `ILoggerProvider` thu mọi log, cả message lẫn exception (`ToString`). Đồng thời `ActivityListener` nghe **mọi** source (`ShouldListenTo = _ => true`) và `MeterListener` nghe **mọi** instrument. Assert key giả không xuất hiện ở bất kỳ tag, giá trị, log hay exception nào;
   - mỗi adapter có hai ca: thành công, và HTTP 500 (body lỗi chứa key giả, để chứng minh body không lọt vào exception hay log).

## Performance và chất lượng dữ liệu

5. **H1 — span cho từng chunk audio.** Tạo một sampler bọc ngoài (ví dụ `HubStreamDroppingSampler`). Sampler trả `SamplingResult(SamplingDecision.Drop)` khi tên activity kết thúc bằng `/StreamUserAudioToTutor`, `/StreamAudioForTranscription` hoặc `/StreamAudioForTranslation`. Các trường hợp khác chuyển cho sampler bên trong.

   Bên trong dùng `ParentBasedSampler(new TraceIdRatioBasedSampler(ratio), remoteParentSampled: ratio, remoteParentNotSampled: ratio)`, với `ratio` là cùng sampler TraceIdRatio. Mục đích là để client không ép được việc lấy mẫu.

   Thêm unit test cho sampler: tên stream thì bị Drop, tên hub method khác và tên HTTP thì đi tiếp vào sampler bên trong.

6. **M4 — chi phí khi chưa cấu hình OTLP.** Nếu không có `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` hay `OTEL_EXPORTER_OTLP_METRICS_ENDPOINT` (đọc từ cấu hình hoặc biến môi trường) thì **không gọi** `AddOpenTelemetry()` và thoát sớm. Khi có endpoint riêng cho từng loại tín hiệu thì chỉ bật exporter của loại đó. `TraceSampleRatio` phải được kẹp vào khoảng [0, 1].

7. **M1 perf — giới hạn giá trị tag model.** Trong `AiTelemetry.Start`, chuẩn hóa `modelCode`: nếu khớp `^[a-z0-9][a-z0-9.\-]{0,63}$` thì giữ, ngược lại ghi `"other"`. Thêm test.

8. **M2 perf — lỗi bị ghi là thành công.** Thêm `call.Succeed()`. Khi `Dispose`:
   - đã gọi `Succeed` thì ghi như thành công;
   - đã gọi `Fail` thì giữ `error.type`;
   - chưa gọi cả hai thì ghi `error.type = "canceled"` nếu `OperationCanceledException`, ngược lại `"error"`. Cách làm đơn giản: tham số tùy chọn `CancellationToken` trong `Start` để biết request đã bị hủy hay chưa.

   Mọi adapter phải gọi `Succeed()` **sau khi** parse response thành công. Các đường `GeminiHttpHelper` (Lessons) có ném exception ra ngoài cũng phải được ghi nhận đúng. Thêm test cho ca "dispose khi chưa Succeed".

9. **M3 — metric Live.**
   - Bỏ model gắn cứng. Bridge nhận model code thật (lấy từ chỗ tạo setup payload trong `GeminiLiveConnectionManager` và `GeminiLivePronunciationTtsSynthesizer`). Truyền vào qua constructor hoặc tham số `ConnectAndSetupAsync`, giữ tối thiểu thay đổi chữ ký.
   - Thêm tag `ept.livetutor.role` lấy từ `RoleTag`, chuẩn hóa về tập cố định `tutor`, `transcribe`, `translate`, `tts`, `other`. Gắn tag này vào `sessions.active`, `session.duration` và token Live.
   - Giữ cách cộng token như hiện tại, nhưng thêm comment: "chưa xác minh `usageMetadata` của Live là cộng dồn hay theo turn — kiểm với provider thật".

10. **L3 — lệch counter khi race.** Ở chỗ tăng counter khi kết nối và khi reconnect: sau `Interlocked.Exchange`, kiểm tra lại `_isDisposed`. Nếu đã dispose thì ghi bù `-1` ngay, hoặc dùng một cờ `_sessionStopped` để luôn khớp cặp +1/−1. Trong `DisposeAsync`, đặt `_isDisposed = true` trước khi thực hiện các bước khác (hiện đã đúng, chỉ cần xác nhận).

## Kiểm chứng (trong `EnglishPartner.BE`)

```bash
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
grep -rn "key=" src --include=*.cs
```

Kết quả bắt buộc:
- build 0 lỗi;
- toàn bộ test pass;
- snapshot không sửa;
- lệnh grep chỉ còn `GeminiLiveSessionBridge`.

## Worker report

Ghi theo từng mục 1–10: đã sửa gì (file:dòng), test nào chứng minh, và những gì không làm được kèm lý do. Kèm lệnh đã chạy và số test pass/fail.
