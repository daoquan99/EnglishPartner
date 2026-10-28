# Việc giao cho AGY: B2 — xác thực, token, phân quyền và rate limit (backend)

## Mục tiêu

Siết luồng xác thực: hash refresh token và phát hiện tái sử dụng, chỉ nhận token qua query cho SignalR, bỏ xác thực 2 lần, thống nhất kiểm tra quyền, gỡ `AllowAllPermissionChecker` khỏi DI production, và thêm rate limit. Liên quan review S7, S8, S11, S12, P5 và các mục Trung bình về cookie, ADMIN bypass, `AllowAllPermissionChecker`.

Batch B1 (JWT options, seeder, hardening HTTP) **đã được làm trong cùng worktree** — giữ nguyên, không hoàn tác.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc), `EnglishPartner.BE/AGENTS.md`
- Review: `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`
- Skill: `.agents/skills/security-audit/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`, `EnglishPartner.BE/.agents/skills/api-realtime/SKILL.md`
- Code:
  - `src/EnglishLiveTutor.Api/Security/JwtAndCsrfMiddleware.cs` — validate token rồi gán `context.User` (trùng với `EltAuthenticationHandler`), nhận `access_token` query cho mọi path.
  - `src/EnglishLiveTutor.Api/Security/EltAuthenticationHandler.cs`
  - `src/EnglishLiveTutor.Api/Security/PermissionAuthorizationExtensions.cs` và `src/EnglishLiveTutor.Api/Services/ClaimsPermissionChecker.cs` — logic ADMIN bypass lặp và lệch nhau (một bên xét claim `roles`, bên kia không).
  - `src/EnglishLiveTutor.Api/Endpoints/IdentityEndpoints.cs` — cookie `Secure = Request.IsHttps`.
  - `src/EnglishLiveTutor.Domain/Identity/UserEntity.cs`, `Domain/Identity/Entities/RefreshTokenEntity.cs`, `Infrastructure/Repositories/IdentityRepositories.cs` (`GetByRefreshTokenAsync`, `GetByEmailOrUsernameWithDetailsAsync` đều `Include(RefreshTokens)` toàn bộ), `Application/Features/Identity/Commands/{Login,RefreshToken,RevokeRefreshToken,RegisterUser}/*`.
  - `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs:41` — `TryAddSingleton<IPermissionChecker>(AllowAllPermissionChecker.Instance)`.

## Phạm vi

Được sửa trong `EnglishPartner.BE/`: `src/EnglishLiveTutor.Api/**` (Program, Security, Services, Endpoints — chỉ để gắn rate limit và sửa cookie), `src/EnglishLiveTutor.Domain/Identity/**`, `src/EnglishLiveTutor.Application/Features/Identity/**`, `src/EnglishLiveTutor.Application/Common/Interfaces/IIdentityServices.cs`, `src/EnglishLiveTutor.Infrastructure/Identity/**`, `src/EnglishLiveTutor.Infrastructure/Repositories/IdentityRepositories.cs`, `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs`, `src/EnglishLiveTutor.Infrastructure/Security/**`, `tests/**`.

Không được sửa: migration (không cần — cột `Token` dài 256 đủ chứa hash), FE, route/JSON thành công của API. Không thêm NuGet package (rate limiting và forwarded headers có sẵn trong ASP.NET Core).

## Yêu cầu

1. **Hash refresh token (S8).** DB chỉ lưu SHA-256 (hex, chữ thường) của refresh token; client vẫn nhận token gốc như hiện tại. Mọi chỗ tra cứu/revoke dùng hash. Token hiện có trong DB (plaintext) sẽ không còn khớp — chấp nhận, người dùng đăng nhập lại; ghi vào report.
2. **Phát hiện tái sử dụng (S8).** Khi refresh bằng token đã bị revoke do rotate (có `ReplacedByToken`) → revoke **toàn bộ** refresh token đang active của user đó (lý do "Reuse detected"), trả lỗi như token không hợp lệ.
3. **Không nạp toàn bộ token (P5).** Login/refresh không `Include` tất cả `RefreshTokens`; chỉ nạp token liên quan (filtered include hoặc truy vấn bảng con). Khi cấp token mới, xóa các token của user đã hết hạn hoặc đã revoke quá 30 ngày.
4. **`access_token` query (S11).** Chỉ chấp nhận token từ query string khi path bắt đầu bằng `/hubs`.
5. **Bỏ xác thực 2 lần (P5).** `JwtAndCsrfMiddleware` không còn parse/validate token — chỉ giữ kiểm tra CSRF header (đổi tên class/file cho đúng vai trò nếu hợp lý và cập nhật `Program.cs`). Xác thực do `EltAuthenticationHandler` đảm nhận; xóa nhánh "đã authenticated bởi middleware trước" trong handler.
6. **Thống nhất kiểm tra quyền.** Một helper duy nhất (ví dụ `ClaimsPrincipalPermissionExtensions.HasPermission(this ClaimsPrincipal, string)`) dùng cho cả endpoint filter `RequirePermission` và `ClaimsPermissionChecker`. ADMIN nhận diện qua claim role (`ClaimTypes.Role`) so sánh không phân biệt hoa thường.
7. **`AllowAllPermissionChecker`.** Không đăng ký trong `AddInfrastructureServices` nữa; `IPermissionChecker` do API đăng ký. Nếu test cần, chuyển class sang project test và đăng ký tường minh ở test.
8. **Cookie refresh.** `Secure = true` luôn (trình duyệt chấp nhận Secure cookie trên `http://localhost`). Thêm `ForwardedHeaders` (X-Forwarded-For, X-Forwarded-Proto) với `KnownProxies`/`KnownNetworks` đọc từ cấu hình `ForwardedHeaders:KnownProxies` (mặc định rỗng), đặt trước authentication.
9. **Register (S12).** Kiểm tra `RegisterUserCommandHandler` chỉ gán role `LEARNER`, không nhận role từ request; nếu không có role LEARNER thì báo lỗi thay vì tạo user không role. Ghi kết luận vào report.
10. **Rate limit (S7).** Dùng `AddRateLimiter` built-in, trả 429 với JSON `{ "error": "..." }` tiếng Việt:
    - Policy `auth`: fixed window theo IP, 10 request/phút — gắn cho `/api/auth/login`, `/register`, `/refresh`.
    - Policy `ai`: theo user id (claim `sub`), token bucket 20 request/phút — gắn cho các endpoint gọi AI: `POST /api/vocabularies/generate`, `POST /api/pronunciation/synthesize`, `POST /api/pronunciation/assessments`, `POST /api/lessons/generation/generate`, `POST /api/lesson-attempts/{id}/answers`, `.../retry-grading`, `.../retry`, `.../ai-review`, `.../exercises/{exerciseId}/synthesize`, `POST /api/lesson-roleplay/sessions`, `.../turns`, `POST /api/vocabulary-games/.../synthesize` (kiểm tra route thật trong file endpoint).
    - Giới hạn đọc từ cấu hình `RateLimiting:*` với mặc định trên.
11. **Mật khẩu mặc định khi admin tạo user.** `Application/Features/Identity/Commands/AdminCreateUser/AdminCreateUserCommandHandler.cs` đang dùng một mật khẩu cố định khi request không có `Password`. Bỏ giá trị cứng: `Password` bắt buộc (≥ 8 ký tự) qua validator `AdminCreateUserValidator`; thiếu → 400 như các lỗi validation khác. Kiểm tra `AdminUpdateUser` và `RegisterUser` không có mặc định tương tự.
12. **Test.** Refresh token lưu dạng hash (token gốc không có trong DB); rotate rồi dùng lại token cũ → mọi token active bị revoke; `access_token` query bị bỏ qua ngoài `/hubs`; helper quyền: ADMIN qua claim role, user có permission, user không có permission.

## Tiêu chí hoàn thành

- [ ] Các yêu cầu 1–12 xong; contract thành công của API không đổi.
- [ ] Build pass; test mới pass; không phát sinh test fail mới ngoài 9 test fail sẵn liệt kê trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

## Ràng buộc

- Không commit, push, merge; không tạo branch. Không chạy `git add`, `git stash`, `git reset` hay `git checkout` (orchestrator quản lý index). Không thêm package.
- Không in secret/token thật. Không chạy API, migration, SQL Server hay Gemini.
- Cần sửa ngoài phạm vi hoặc gặp điều mơ hồ: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt yêu cầu 1–12>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
