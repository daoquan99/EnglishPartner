# Việc giao cho AGY: B1 — cấu hình, secret và hardening HTTP (backend)

## Mục tiêu

Loại bỏ secret/mật khẩu/fallback cứng trong code và cấu hình, fail-fast khi thiếu cấu hình bắt buộc, và bật hardening HTTP. Liên quan review S1, S5, S6, S10, A6, P8.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc), `EnglishPartner.BE/AGENTS.md`
- Review: `docs/reviews/2026-10-28-security-performance-architecture.md` mục S1, S5, S6, S10, A6, P8 (trong thư mục task được add-dir: đọc bằng đường dẫn tuyệt đối `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`)
- Skill: `.agents/skills/security-audit/SKILL.md`, `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
- Code: `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Identity/SecurityServices.cs` (`HmacJwtTokenService` có secret mặc định), `Infrastructure/Identity/AdminUserSeedOptions.cs`, `Infrastructure/Persistence/DatabaseSeeder.cs` (tạo user admin + `default-learner` với mật khẩu cứng, khoảng dòng 1128-1156), `Infrastructure/DependencyInjection.cs` (fallback connection string), `Api/Program.cs`, `Api/appsettings.json`, `Api/appsettings.Development.json`, `Application/DependencyInjection.cs` (đọc license MediatR).

## Phạm vi

Chỉ được sửa trong `EnglishPartner.BE/`:
- `src/EnglishLiveTutor.Api/Program.cs`, `src/EnglishLiveTutor.Api/appsettings.json`, `src/EnglishLiveTutor.Api/appsettings.Development.json`, `src/EnglishLiveTutor.Api/EnglishLiveTutor.Api.csproj` (chỉ thêm `UserSecretsId`)
- File mới trong `src/EnglishLiveTutor.Api/Security/` (security headers)
- `src/EnglishLiveTutor.Infrastructure/Identity/*`, `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs`, `src/EnglishLiveTutor.Infrastructure/Persistence/DatabaseSeeder.cs`, option class trong `src/EnglishLiveTutor.Infrastructure/**/` cần `ValidateOnStart`
- `tests/EnglishLiveTutor.Domain.Tests/*` (sửa/thêm test)

Không được sửa: migration, entity Domain, endpoint, FE, file ngoài danh sách trên. Không đổi route hay JSON của API.

## Yêu cầu

1. **JWT (S1).** Tạo `JwtOptions` (section `Jwt`: `SecretKey`, `Issuer` mặc định `EnglishLiveTutor.Api`, `Audience` mặc định `EnglishLiveTutor.Client`, `AccessTokenMinutes` mặc định 60). `HmacJwtTokenService` lấy key từ options, **xóa chuỗi secret mặc định**. Quy tắc: nếu thiếu `Jwt:SecretKey` và môi trường là Development → sinh key ngẫu nhiên 64 byte cho tiến trình và log warning (token mất hiệu lực khi restart); môi trường khác, hoặc key < 32 byte UTF-8 → ứng dụng fail khi khởi động với message rõ ràng nêu tên cấu hình (`ValidateOnStart` hoặc kiểm tra khi build host). Không log giá trị key. Giữ cách tạo service dễ dùng trong test (ví dụ constructor nhận options).
2. **MediatR license (S5).** Xóa section `MediatR` khỏi `appsettings.json`. Giữ đọc `MediatR:LicenseKey` từ configuration (user-secrets/biến môi trường) và fallback biến môi trường `MEDIATR_LICENSE_KEY` đã có. Thêm `<UserSecretsId>` (GUID mới) vào `EnglishLiveTutor.Api.csproj` để dùng `dotnet user-secrets`.
3. **Connection string.** Bỏ chuỗi fallback cứng trong `Infrastructure/DependencyInjection.cs`; thiếu cả `DefaultConnection` và `SqlServer` → throw `InvalidOperationException` nêu tên cấu hình. Giữ `ConnectionStrings:DefaultConnection` hiện có trong `appsettings.json` (Trusted_Connection, không chứa mật khẩu).
4. **Seed tài khoản (S6).** `DatabaseSeeder` dùng `AdminUserSeedOptions` (section `AdminUser`); bỏ giá trị mặc định của `Password` (để null). Thiếu `AdminUser:Password` → không tạo admin, log warning giải thích cách cấu hình (không crash). Mật khẩu phải ≥ 8 ký tự. Học viên demo (`default-learner`) chỉ được tạo khi môi trường là Development **và** có cấu hình `DemoLearner:Password`; không còn mật khẩu nào viết cứng trong code. Cập nhật chữ ký `EnsureSchemaAndSeedAsync` cho phù hợp (truyền options, môi trường, logger) và các test hiện có (`DatabaseSeederAdminUserTests`).
5. **Migrate/seed lúc khởi động (P8).** Thêm cấu hình `Database:MigrateOnStartup` và `Database:SeedOnStartup`: `true` trong `appsettings.Development.json`, `false` trong `appsettings.json`. Development: lỗi khi migrate/seed chỉ log warning như hiện tại. Môi trường khác: nếu đã bật mà lỗi → để ứng dụng fail (không nuốt exception).
6. **Hardening HTTP (S10).**
   - Ngoài Development: `UseHsts()` và `UseHttpsRedirection()`.
   - Middleware security headers cho mọi response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (API chỉ trả JSON/stream audio, không trả HTML).
   - SignalR `EnableDetailedErrors` chỉ bật khi Development.
   - `AllowedHosts` trong `appsettings.json` đổi thành `localhost;127.0.0.1`; ghi chú trong report rằng production phải cấu hình host thật.
7. **Validate options (A6).** `ValidateOnStart` cho `LessonGenerationOptions`, `LessonAssessmentOptions`, `LessonRolePlayOptions` (TimeoutSeconds 1..600, MaxRetries 0..5), `LessonMediaStorageOptions` (MaxFileSizeBytes > 0), `AntigravityCliOptions` (TimeoutSeconds > 0, AllowedModels không rỗng).
8. **Test.** Thêm test: key JWT < 32 byte bị từ chối ngoài Development; Development không có key vẫn tạo được token hợp lệ; seeder không tạo admin khi thiếu mật khẩu; seeder không tạo demo learner ngoài Development; token ký bằng key A không validate được với key B.

## Tiêu chí hoàn thành

- [ ] Không còn chuỗi secret JWT, mật khẩu seed hay license key nào trong source/appsettings (grep `Admin@123`, `Learner@123`, `Super_Secret`, `LicenseKey"` trong `appsettings.json` không còn kết quả).
- [ ] Build pass; test mới pass; không phát sinh test fail mới ngoài 9 test fail sẵn (liệt kê trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`).

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

## Ràng buộc

- Không commit, push, merge; không tạo branch. Không chạy `git add`, `git stash`, `git reset` hay `git checkout` (orchestrator quản lý index).
- Không thêm NuGet package.
- Không in hoặc ghi secret, token, API key, connection string thật.
- Không chạy API, không chạy migration/seed trên SQL Server, không gọi Gemini.
- Gặp điều mơ hồ hoặc cần sửa ngoài phạm vi: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt từng yêu cầu 1–8>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
