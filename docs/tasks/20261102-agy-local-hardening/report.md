# Report: AGY Local hardening, lỗi synthesize, phân tích API chậm

## Đã làm

| Phần | Nội dung |
| --- | --- |
| AGY tắt mặc định | BE dùng `Ai:AntigravityCli:Enabled` (mặc định `false` trong `appsettings.json`). FE dùng `VITE_ENABLE_AGY_LOCAL` (mặc định tắt; đã thêm vào `.env.example`). |
| Fail-fast | `AntigravityCliStartupValidator`: nếu `Enabled = true` mà môi trường khác `Development`, API không khởi động. |
| Chặn mọi đường gọi | `AgyCliRunnerExtensions.IsAgyRequest` nhận diện request AGY qua 3 dấu hiệu: `provider = agy`, tiền tố `agy:`, hoặc model nằm trong `AllowedModels`. Việc nhận diện không phụ thuộc `Enabled`. Sau đó `EnsureAgyEnabled` ném `ForbiddenException("agy_local_disabled")`, áp dụng ở 6 caller. `ExecutePromptAsync` chặn thêm một lớp nữa. Không request nào bị âm thầm chuyển sang Gemini. |
| 403 khi nộp bài | Ở `SubmitExerciseAnswerCommand` (2 chỗ), `RetryAnswerGradingCommand` và `SubmitGrammarAnswerCommand`, `ForbiddenException` được ném lại thay vì bị ghi thành lỗi chấm. |
| Ẩn model AGY | BE: `GetPronunciationModels` và `GetVocabularyGenerationModels` lọc theo `AllowedModels`. `ListAiModelConfigs` dành cho admin vẫn trả đủ. FE: `withoutDisabledAgyModels` lọc cả dữ liệu server lẫn danh sách fallback offline. `assessmentModelCode` cũ trong localStorage là model AGY thì bị bỏ. |
| FE | Khi tắt, `tutorSessionStore` luôn dùng `gemini`. Nút, nhãn và select AGY bị ẩn ở 8 component. |
| Siết process khi bật | Mỗi lần gọi dùng một thư mục tạm rỗng và xóa sau đó. Đã bỏ option `WorkingDirectory` dùng chung theo M1 của security review. Ngoài ra: biến môi trường chỉ giữ theo allowlist, thêm `--sandbox` và `--settings {"allowNonWorkspaceAccess":false,"permissions":{"allow":[]}}`. Stderr và đường dẫn exe không còn xuất hiện trong lỗi trả về client. |
| Lỗi synthesize | `GeneratedPronunciationAudioRepository`: bỏ điều kiện `AudioData.Length > 0`, vì EF Core 10 dịch nó thành `Any()` và SQL Server không chạy được. Constructor của entity đã chặn audio rỗng; code-reviewer xác nhận không còn đường ghi nào khác. |
| Warm-up | `DbContextWarmupService` dựng model của 8 DbContext ở background khi khởi động. |

Worker: Codex thực hiện plan. Claude tự sửa các điều kiện review: M1, L1, L2, L5 (security); 1, 2, 5, 6 (code).

## Phân tích API chậm (log của người dùng)

Đo trên DB tạm `LingoMint_Perf`, restore từ bản COPY_ONLY của `LingoMint`. Đã xóa cả DB lẫn file `.bak`; **không đụng `LingoMint`**. Dùng user mới đăng ký, mỗi endpoint gọi 5 lần.

| API | Lần 1 (ms) | Các lần sau (ms) |
| --- | --- | --- |
| Settings | 225 | 16–35 |
| Progress overview | 1 190–1 460 | 50–90 |
| List step exercises | 390–450 | 16–90 |
| Today initialize | 1 930–2 150 | 70–117 |
| Start attempt | 550–950 | 55–64 |
| Get attempt | 110–180 | 20–27 |
| Submit answer | 490–710 | 23–130 |
| Complete attempt | 262–326 | (bị 429, xem dưới) |
| Login | 410–520 | 118–166 (PBKDF2, có chủ đích) |

- **Kết luận:** cảnh báo `Long Running Request` chỉ xảy ra ở lần gọi đầu của mỗi API sau khi khởi động. Nguyên nhân là EF dịch LINQ lần đầu, JIT ở bản Debug và dựng model. Khi đã chạy ấm, mọi API dưới 130 ms.
- Query Store của `LingoMint` cho thấy truy vấn của app trung bình khoảng 1,6 ms, nên SQL Server không phải điểm nghẽn.
- Warm-up: ở chế độ dev, bước migrate lúc khởi động đã dựng sẵn model, nên warm-up chỉ mất 42 ms. Khi tắt migrate/seed (giống production), warm-up mất 1 542 ms; khoảng thời gian này được chuyển ra khỏi request đầu tiên.
- Các số cao hơn trong log của người dùng (Start 3,4 s, Complete 2,5 s) nhiều khả năng là các request đầu tiên chạy song song lúc FE vừa tải. Chưa tái hiện được trên máy đo.

## Kiểm chứng

| Lệnh | Kết quả |
| --- | --- |
| `dotnet build EnglishLiveTutor.slnx --no-restore` | 0 lỗi; 0 cảnh báo mới |
| `dotnet test EnglishLiveTutor.slnx --no-restore --no-build` | 801/801 pass (baseline 777). Test mới: `AgyCliRunnerExtensionsTests` (Theory 9 case + 2 Fact); generator Grammar chặn AGY khi tắt (3 case); `GeneratedPronunciationAudioSqlTranslationTests` dùng provider SQL Server để bắt lỗi dịch query; cùng các test Codex thêm cho fail-fast, catalog, `BuildProcessStartInfo` và caller. |
| `npm run build` | pass |
| `npm run test` | 373/373 pass. Test flag đã mock `@/shared/config/features`. |
| Review | security-reviewer: ĐẠT CÓ ĐIỀU KIỆN. code-reviewer: ĐẠT CÓ ĐIỀU KIỆN. Đã xử lý các điều kiện như bảng dưới. |

| Finding | Xử lý |
| --- | --- |
| Sec M1: `WorkingDirectory` dùng chung | Đã bỏ option. Luôn dùng thư mục tạm riêng, xóa sau mỗi lần gọi. |
| Sec L1: lộ đường dẫn exe | Message cố định. |
| Sec L2 / Code 3: `ForbiddenException` bị nuốt | Ném lại ở 4 chỗ. |
| Sec L5 / Code 6: thiếu test, test FE phụ thuộc `.env` | Đã thêm test; đã mock flag. |
| Code 1: heuristic hậu tố `-low/-medium/-high` | Đã bỏ. Chỉ dùng provider, tiền tố `agy:` và allowlist. |
| Code 2: fallback FE vẫn có model AGY | Đã lọc, kèm test. |
| Code 5: test hồi quy cho lỗi dịch query | Đã thêm. |
| Sec L3: `GET /api/ai-models` trả model AGY cho mọi user đã đăng nhập | **Chưa sửa.** Chỉ lộ tên model, không gọi được AGY. Muốn đổi quyền endpoint thì cần quyết định riêng. |
| Sec L4: log stderr tối đa 500 ký tự | Chấp nhận ở môi trường dev. |
| Sec M2 / Code 4: `--sandbox` và `--settings` chưa chạy thật | **Chưa kiểm chứng.** Xem mục dưới. |

## Thử bằng agy.exe thật (2026-11-02, sau khi AGY có quota lại)

Cách chạy giống `AgyCliRunner`:
- Thư mục làm việc là một thư mục tạm rỗng.
- Biến môi trường chỉ giữ theo allowlist.
- Thêm `--sandbox --settings <json> --print`.
- Model: `gemini-3.8-flash-medium`.

Dữ liệu thử là một file giả chứa chuỗi canary, đặt ngoài workspace, trong `%TEMP%`. Thư mục này nằm dưới `C:\Users\DELL`, tức là dưới một thư mục mà cấu hình toàn cục đang tin cậy. Không có secret thật nào bị đụng tới. File thử đã được xóa.

| Prompt | Settings ban đầu: `allowNonWorkspaceAccess:false`, `allow:[]` | Settings cuối: thêm `deny` gồm `read_file(*)`, `write_file(*)`, `command(*)`, `mcp(*)`, `read_url(*)` |
| --- | --- | --- |
| Baseline ("trả lời OK") | OK; agy nhận cả `--sandbox` và `--settings` JSON inline | OK |
| Đọc file ngoài workspace | **LỘ canary.** `allowNonWorkspaceAccess` và `--sandbox` không chặn được việc đọc | Bị chặn ở tầng tool: "Permission denied for read_file … matches a user-configured deny rule" |
| Chạy PowerShell ghi file | Model tự từ chối; file không được tạo | Model tự từ chối; file không được tạo |
| Chạy lệnh trong workspace (`cmd /c echo`) | (chưa thử) | Bị chặn ở tầng tool: "Permission denied for command(…)" |
| Liệt kê thư mục | (chưa thử) | Bị chặn ở tầng tool (deny rule) |
| grep / đọc URL | (chưa thử) | Tool không khả dụng; không có dữ liệu lọt ra |

**Kết luận:** chỉ cấu hình ban đầu là không đủ. `AgyCliRunner.SandboxedSettings` đã được đổi sang settings cuối, kèm test (task `20261102-ai-timeouts`). Cú pháp quyền `read_file(…)`, `write_file(…)`, `command(…)`, `mcp(…)`, `read_url(…)` lấy từ tài liệu nhúng trong `agy.exe`.

## Chưa kiểm chứng

- ~~agy.exe thật~~: đã thử, xem mục trên. Các lệnh trên được gọi qua bash, chưa chạy qua `Process`/`ArgumentList` của .NET; trên Windows, `ArgumentList` tự quote JSON. Vẫn nên sửa `settings.json` toàn cục như ghi trong plan.
- Chưa chạy lại trên SQL Server thật các phần sau: lọc catalog, query synthesize (mới có test dịch query), warm-up ở cấu hình production.
- Chưa kiểm UI bằng trình duyệt.
- Rate limit `ai` (20 lượt/phút) đang áp dụng cho start/submit/complete của Grammar. Khi đo, request thứ 23 trong 1 phút nhận 429. Đây là đề xuất, chưa sửa.
