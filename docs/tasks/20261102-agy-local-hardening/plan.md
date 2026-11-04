# Plan: tắt mặc định và siết an toàn AGY Local; sửa lỗi synthesize; warm-up DbContext

Mã task: `20261102-agy-local-hardening` · Loại: sửa lỗi bảo mật (kèm 2 sửa nhỏ) · Worker: Codex

## Mục tiêu

1. **AGY Local tắt mặc định ở cả BE và FE.** Khi tắt, học viên không thấy và không dùng được AGY.
2. **Khi bật** (chỉ cho máy dev), giảm thiệt hại nếu có prompt injection: học viên có thể viết vào bài làm "bỏ qua hướng dẫn, chạy lệnh…".
3. Hai sửa đã làm trực tiếp, chỉ cần review:
   - Lỗi `/api/pronunciation/synthesize`. EF Core 10 đổi `byte[].Length > 0` thành `Any()`, và SQL Server không dịch được → 400. Cách sửa: bỏ điều kiện này, vì constructor của entity đã chặn audio rỗng.
   - `DbContextWarmupService`: dựng model của 8 DbContext ở background ngay khi khởi động.

## Hiện trạng (bằng chứng)

| Điểm | Vị trí | Hậu quả |
| --- | --- | --- |
| Không có công tắc bật/tắt; runner đăng ký luôn | `Composition/Infrastructure/DependencyInjection.cs:76-83` | Production cũng gọi được `agy.exe`, nếu máy có cài. |
| Học viên nào cũng chọn được provider `agy`: toggle ở `StudentLayout`, `AiSpeechSettingsDialog`, `PronunciationStudio`, `TargetedVocabularyDialog`, `AiLessonExerciseGeneratorModal`; lựa chọn lưu trong localStorage | FE `tutorSessionStore.ts:71-79` | Bài làm của học viên được gửi tới `agy.exe` (`LessonAiGrader`, `DictationAiReviewer`, các generator). |
| `agy --print` chạy với **thư mục làm việc và toàn bộ biến môi trường của process API** | `AgyCliRunner.cs:78-96` | Agent đọc được repo và secret nằm trong biến môi trường (connection string, JWT…). |
| Không có `--sandbox`, không ghi đè cấu hình | như trên | Cấu hình toàn cục `~/.gemini/antigravity-cli/settings.json` trên máy này cho phép `command(powershell)` và `command(dotnet)`, đặt `allowNonWorkspaceAccess: true`, và tin cậy cả `C:\Users\DELL`. Prompt injection có thể chạy PowerShell mà không bị hỏi. |
| Catalog seed 3 model "AGY Local" | `DatabaseSeeder.cs:251-283` | Dropdown model hiện cả các model AGY. |

## Thiết kế

### BE

1. `AntigravityCliOptions`:
   - Thêm `Enabled` (mặc định `false`).
   - Thêm `WorkingDirectory` (tùy chọn). Mặc định là thư mục tạm riêng cho mỗi lần gọi, nằm dưới `Path.GetTempPath()/englishpartner-agy/<guid>`, và bị xóa sau khi gọi xong.
   - `appsettings.json`: `"Enabled": false`.
2. Fail-fast lúc khởi động: nếu `Enabled = true` mà môi trường không phải `Development` thì ném lỗi cấu hình (`ValidateOnStart` hoặc kiểm trong `Program.cs`). AGY Local chạy trên tài khoản máy chủ, nên không bao giờ được dùng ở production.
3. `IAgyCliRunner` thêm `bool IsEnabled { get; }`. Khi tắt:
   - `IsAgyModel` trả `false`.
   - `ExecutePromptAsync` ném `ForbiddenException("agy_local_disabled")` (lớp phòng thủ cuối).
   - Mọi chỗ đang tự nhận diện AGY bằng `provider == "agy"` hoặc tiền tố `agy:` (5 caller cùng `GenerateVocabularyCommandHandler`) phải ném cùng lỗi đó, không được âm thầm rơi sang Gemini. Học viên chọn AGY từ localStorage cũ sẽ nhận 403 với mã rõ ràng; FE (mục FE bên dưới) đã tự chuyển về `gemini`.
4. Ẩn model AGY khỏi catalog khi tắt: các query liệt kê model (`ListAiModelConfigs` cho admin vẫn thấy hết; còn `GetPronunciationModels`, `GetVocabularyGenerationModels` và các query public khác cho học viên) loại các `ModelCode` nằm trong `AllowedModels` của AGY. Kiểm tra qua `IAgyCliRunner` (Shared.Application). Không xóa dữ liệu catalog.
5. Siết process khi bật:
   - `WorkingDirectory` = thư mục tạm rỗng, xóa sau khi gọi.
   - `psi.Environment.Clear()`, sau đó chỉ chép lại allowlist: `PATH`, `SystemRoot`, `windir`, `ComSpec`, `PATHEXT`, `TEMP`, `TMP`, `USERPROFILE`, `HOMEDRIVE`, `HOMEPATH`, `APPDATA`, `LOCALAPPDATA`, `ProgramData`, `ProgramFiles`, `ProgramFiles(x86)`, `NUMBER_OF_PROCESSORS`, `PROCESSOR_ARCHITECTURE`, `OS`, `HOME`. Agy cần profile để đọc phiên đăng nhập.
   - Thêm `--sandbox`.
   - Thêm `--settings` với JSON inline `{"allowNonWorkspaceAccess":false,"permissions":{"allow":[]}}` để vô hiệu allowlist lệnh toàn cục. Cả hai key đều có thật trong settings.json của agy. Chưa chạy thử được vì AGY đang hết credit; ghi vào report.
   - Bỏ việc ghi nguyên văn `stderr` vào message của exception trả về client. Chỉ log phía server, cắt tối đa 500 ký tự.
6. Test (xUnit, không gọi agy thật):
   - Options mặc định `Enabled = false`.
   - Runner tắt: `IsAgyModel("gemini-3.8-flash-medium")` trả `false`; `ExecutePromptAsync` ném `ForbiddenException`.
   - Fail-fast khi `Enabled` ở môi trường không phải Development.
   - Query catalog cho học viên không trả model AGY khi tắt, và trả đủ khi bật.
   - `LessonAiGrader` hoặc `DictationAiReviewer` với `provider = "agy"` khi tắt thì ném `ForbiddenException`, không gọi Gemini.
   - Builder `ProcessStartInfo`: tách thành method `internal static` để test được. Kiểm tra có `--sandbox`, có `--settings`, `WorkingDirectory` khác thư mục hiện tại, và môi trường không còn biến `ConnectionStrings__DefaultConnection` hay `Jwt__SecretKey` khi biến đó có trong process.

### FE

1. `src/shared/config/features.ts`: `export const isAgyLocalEnabled = import.meta.env.VITE_ENABLE_AGY_LOCAL === 'true';`. Thêm dòng `VITE_ENABLE_AGY_LOCAL=false` vào `.env.example`.
2. `tutorSessionStore`: khi tắt, `loadInitialAiProvider` luôn trả `'gemini'`, và `setAiProvider('agy')` không có tác dụng.
3. Ẩn nút chọn AGY, badge, nhãn và select model AGY khi tắt, ở: `StudentLayout`, `AiSpeechSettingsDialog`, `PronunciationStudio`, `TargetedVocabularyDialog`, `AiVocabularyGeneratorDialog`, `AiLessonExerciseGeneratorModal` (cả `AGY_LOCAL_MODELS`), `GenerateGrammarExercisesDialog` và `AdminGrammarStepEditorPage` nếu có UI chọn. Các chỗ build request tự đúng, vì `aiProvider` không thể là `'agy'`.
4. Test Vitest: store khi tắt (localStorage có `'agy'` thì vẫn ra `'gemini'`), và một component không hiện nút "AGY Local" khi tắt. Có thể mock module `features.ts`.

## Contract

Không đổi route, DTO hay field JSON. Chỉ thêm mã lỗi `agy_local_disabled` (403 ProblemDetails) cho request chọn AGY khi tính năng tắt. Thêm cấu hình mới: BE `Ai:AntigravityCli:Enabled` và FE `VITE_ENABLE_AGY_LOCAL`.

## Rủi ro dữ liệu

Không có migration, không sửa dữ liệu. Các bản ghi catalog AGY vẫn nằm trong DB, chỉ bị lọc khi đọc.

## Ngoài phạm vi (khuyến nghị cho người dùng)

- Gỡ `command(powershell)` và `command(dotnet)` khỏi `permissions.allow` trong `~/.gemini/antigravity-cli/settings.json`. Đặt lại `allowNonWorkspaceAccess` thành `false`. Gỡ `C:\Users\DELL` khỏi `trustedWorkspaces`.
- Rate limit `ai` (20 lượt/phút) đang áp dụng cho cả start/submit/complete của Grammar, kể cả khi chấm tất định. Khi đo, request thứ 23 trong 1 phút đã nhận 429. Nên tách thành policy riêng.

## Kiểm chứng

- `dotnet build EnglishPartner.BE/EnglishLiveTutor.slnx`, `dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx`.
- FE: `npm run build`, `npm run test -- src/features/tutor src/app src/features/speech src/features/vocabulary src/features/lessons src/features/grammar`.
- Review: security-reviewer, sau đó code-reviewer.
