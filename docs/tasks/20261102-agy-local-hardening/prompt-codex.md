# Việc giao cho Codex: tắt mặc định và siết AGY Local (BE + FE)

## Bối cảnh

- Đọc hết `docs/tasks/20261102-agy-local-hardening/plan.md`. Plan là phạm vi duy nhất được phép làm: làm đúng mục "Thiết kế" (BE 1–6, FE 1–4).
- Luật: `AGENTS.md` (luật 3, 5, 9, 11, 13), `EnglishPartner.BE/AGENTS.md`, `EnglishPartner.FE/AGENTS.md`.
- Skill:
  - `.agents/skills/security-audit/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/ai-provider-integration/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md` (nếu có)
- Làm trên `development`, **không commit**.
- Không chạy `agy`, API hay SQL. Không cài package mới.
- Không có mạng: chạy `dotnet restore --ignore-failed-sources` rồi dùng `--no-restore`.
- Working tree đã có 3 thay đổi của orchestrator. **Không sửa, không revert** 3 file này:
  - `GeneratedPronunciationAudioRepository.cs`
  - `DbContextWarmupService.cs`
  - dòng `AddHostedService<DbContextWarmupService>()` trong `DependencyInjection.cs`

## Điểm cần lưu ý

- Runner: `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Ai/AgyCliRunner.cs`, options trong `AntigravityCliOptions.cs`, interface `IAgyCliRunner` trong Shared.Application. Đăng ký trong `src/EnglishLiveTutor.Api/Composition/Infrastructure/DependencyInjection.cs`.
- Caller tự nhận diện AGY:
  - `GeminiGrammarExerciseAiGenerator`
  - `GeminiLessonAiGenerator`
  - `DictationAiReviewer`
  - `LessonAiGrader`
  - `GeminiVocabularyAiGenerator`
  - `GenerateVocabularyCommandHandler`

  Tìm tất cả bằng `rg -n "\"agy\"|agy:" EnglishPartner.BE/src`. Khi AGY tắt mà request chọn AGY, ném `ForbiddenException("agy_local_disabled")` (namespace `EnglishLiveTutor.Application.Common.Exceptions`). Không được âm thầm chuyển sang Gemini. Riêng `LessonAiGrader` đang có nhánh fallback audio → Gemini: giữ nguyên nhánh đó khi AGY **bật**.
- Fail-fast ở môi trường không phải Development: dùng `IHostEnvironment` trong DI (`AddOptions…Validate` không có env, nên kiểm trong `Program.cs`, ngay sau dòng `_ = app.Services.GetRequiredService<IOptions<AntigravityCliOptions>>().Value;`). Thông báo lỗi không được chứa secret.
- Catalog: chỉ lọc ở các query học viên dùng để chọn model. Admin CRUD (`ListAiModelConfigs`) giữ nguyên.
- FE: các file và chỗ cần sửa đã liệt kê trong plan (mục FE). Test hiện có tham chiếu AGY nằm ở:
  - `src/features/grammar/__tests__/*`
  - `src/features/vocabulary/__tests__/aiVocabularyGeneratorDialog.test.tsx`
  - `dailyVocabularyAutoInit.test.tsx`

  Nếu test cũ cần AGY bật, mock `@/shared/config/features` để trả `isAgyLocalEnabled = true`. Không xóa test cũ.
- i18n: chỉ thêm key khi thật sự cần (vi + en).

## Kiểm chứng phải chạy và ghi vào report

- `dotnet build EnglishPartner.BE/EnglishLiveTutor.slnx --no-restore`
- `dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx --no-restore --no-build`. Toàn bộ phải pass; baseline hiện tại là 777 test.
- Trong `EnglishPartner.FE`: `npm run build` và `npm run test`.

## Report

Ghi `docs/tasks/20261102-agy-local-hardening/runs/codex-report.md`, gồm:
- Danh sách file đã sửa.
- Các lệnh đã chạy, kèm số test pass/fail.
- Những điểm chưa làm được hoặc làm khác plan, kèm lý do.
