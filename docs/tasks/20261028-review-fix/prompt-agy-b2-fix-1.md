# Việc giao cho AGY: B2 fix 1 — forwarded headers, race refresh token, policy refresh (backend)

Batch B1, B2 và B1 fix 1 đã làm trong worktree này — giữ nguyên mọi thay đổi khác. Chỉ sửa đúng các điểm dưới đây (từ security review B2).

## Phạm vi

Chỉ được sửa: `EnglishPartner.BE/src/EnglishLiveTutor.Api/Program.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Api/Endpoints/IdentityEndpoints.cs` (chỉ policy rate limit), `EnglishPartner.BE/src/EnglishLiveTutor.Api/appsettings.json` (section `RateLimiting`, `ForwardedHeaders`), `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Identity/Commands/RefreshToken/**`, `EnglishPartner.BE/src/EnglishLiveTutor.Domain/Identity/**`, `EnglishPartner.BE/src/EnglishLiveTutor.Domain/Identity/Repositories/IUserRepository.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Repositories/IdentityRepositories.cs`, `EnglishPartner.BE/tests/**`. Không tạo migration.

## Yêu cầu

1. **H1 — ForwardedHeaders tin mọi proxy.** `Program.cs:~162-176` đang `KnownIPNetworks.Clear()` + `KnownProxies.Clear()`; khi cấu hình rỗng, ASP.NET Core chấp nhận `X-Forwarded-*` từ mọi client → giả IP vô hiệu rate limit `auth`. Sửa:
   - Không gọi `Clear()` (giữ loopback mặc định).
   - Chỉ thêm proxy/network đọc từ `ForwardedHeaders:KnownProxies` (IP) và `ForwardedHeaders:KnownNetworks` (CIDR). IP/CIDR không parse được → log warning nêu giá trị cấu hình sai (không phải secret).
   - Chỉ gọi `UseForwardedHeaders` khi có ít nhất một proxy/network hợp lệ được cấu hình; nếu không, bỏ qua middleware.
   - Tách cấu hình thành extension method (ví dụ `AddConfiguredForwardedHeaders` / `UseConfiguredForwardedHeaders`) để test được.
2. **M1 — race refresh.** Revoke token cũ phải có điều kiện: cập nhật chỉ khi `Id = @id AND RevokedAtUtc IS NULL` (ví dụ `ExecuteUpdateAsync` trên `RefreshTokens` trong repository) và yêu cầu đúng 1 dòng bị ảnh hưởng; 0 dòng → coi như token đã bị dùng (xử lý như reuse ở mục 3), không cấp token mới. Token mới chỉ được lưu sau khi revoke có điều kiện thành công, trong cùng transaction/commit. Nếu EF InMemory không hỗ trợ `ExecuteUpdateAsync`, tách thao tác vào repository và dùng nhánh tương đương cho InMemory (giống cách `UnitOfWork` kiểm tra provider), test dùng nhánh đó.
3. **L1 — khoảng ân hạn.** Token đã bị rotate trong vòng `Auth:RefreshReuseGraceSeconds` giây (cấu hình mới, mặc định 10) mà bị gửi lại → trả 401 như token không hợp lệ nhưng **không** revoke toàn bộ phiên (nhiều tab gửi đồng thời). Quá khoảng ân hạn → giữ hành vi reuse detection hiện tại (revoke toàn bộ).
4. **L2 — policy riêng cho refresh.** Tách policy `auth-refresh` (fixed window theo IP, mặc định 30 request/phút) cho `/api/auth/refresh`; `/login` và `/register` giữ policy `auth`. Cấu hình qua `RateLimiting:*`.
5. **L3 — test.**
   - Forwarded headers: không cấu hình proxy → `X-Forwarded-For` từ client bị bỏ qua (`RemoteIpAddress` giữ nguyên); có cấu hình proxy và request đến từ proxy đó → được áp dụng; IP cấu hình sai → bị bỏ qua và không ném lỗi.
   - Refresh: hai lần refresh liên tiếp cùng một token → lần 2 thất bại và không cấp token mới; gửi lại token cũ trong khoảng ân hạn → 401, phiên còn lại không bị revoke; ngoài khoảng ân hạn (fake `TimeProvider`) → revoke toàn bộ.
   - Rate limiter: policy `auth` trả 429 sau ngưỡng (dùng `TestServer`/`WebApplicationFactory` nếu project test đã có, nếu không thì test cấu hình policy qua extension method).

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

Không phát sinh test fail mới ngoài 9 test fail sẵn trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`.

## Ràng buộc

Không chạy `git add`/`git stash`/`git reset`/`git checkout`. Không commit/push/merge. Không thêm package (nếu cần `Microsoft.AspNetCore.Mvc.Testing` cho test mà project test chưa có thì **không** thêm — dùng cách test extension method). Không chạy API/SQL/Gemini, không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–5>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
