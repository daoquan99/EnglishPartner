# Việc giao cho AGY: B1 fix 1 — 3 điểm từ security review (backend)

Batch B1 và B2 đã làm trong worktree này — giữ nguyên mọi thay đổi khác. Chỉ sửa đúng 3 điểm sau.

## Phạm vi

Chỉ được sửa: `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Identity/SecurityServices.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Api/Program.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Persistence/DatabaseSeeder.cs`, `EnglishPartner.BE/tests/**`.

## Yêu cầu

1. **Fail-closed khi thiếu môi trường.** `SecurityServices.cs` (constructor `HmacJwtTokenService`): `bool isDev = environment?.IsDevelopment() ?? (environment == null);` → `?? false`. Test nào cần key tạm thời của Development phải truyền tường minh một `IHostEnvironment` có `EnvironmentName = "Development"` (tạo fake trong test). Test không truyền môi trường và không có key phải kỳ vọng exception.
2. **Validate cấu hình trước migrate/seed.** Trong `Program.cs`, ngay sau `var app = builder.Build();` và **trước** khối migrate/seed: resolve `.Value` của `IOptions<JwtOptions>`, `IOptions<LessonGenerationOptions>`, `IOptions<LessonAssessmentOptions>`, `IOptions<LessonRolePlayOptions>`, `IOptions<LessonMediaStorageOptions>`, `IOptions<AntigravityCliOptions>` và `IJwtTokenService`, để `OptionsValidationException` xảy ra trước khi chạm DB.
3. **Bỏ đường cấu hình không tài liệu hóa.** Trong `DatabaseSeeder.cs`, xóa overload/nhánh đọc `SeedData:AdminUser` / `SeedData:DemoLearner` (và overload theo `IConfiguration` nếu `Program.cs` không dùng). Chỉ giữ section `AdminUser` và `DemoLearner`. Cập nhật test nếu có test dùng đường này.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

Không phát sinh test fail mới ngoài 9 test fail sẵn trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`.

## Ràng buộc

Không chạy `git add`/`git stash`/`git reset`/`git checkout`. Không commit/push/merge, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–3>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
