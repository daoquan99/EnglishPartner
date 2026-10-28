# Việc giao cho AGY: B5 — global exception handler và ProblemDetails (backend + 1 hàm FE)

## Mục tiêu

Mọi lỗi API trả ProblemDetails (RFC 9457) có thêm field `error` để client cũ vẫn đọc được; không lộ message của exception không lường trước; endpoint mỏng hơn. Liên quan ADR-0005 (coi là Accepted), review A4 và mục "Không có global exception handler". Batch B1–B4 đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, Luật 10, 11), `EnglishPartner.BE/AGENTS.md`, `EnglishPartner.BE/.agents/architecture.md` mục 5 (giữ contract `{ error }`)
- ADR: `D:/Projects/EnglishPartner/docs/adr/0005-problem-details-error-contract.md`
- Skill: `.agents/skills/api-contract-sync/SKILL.md`, `EnglishPartner.BE/.agents/skills/api-realtime/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`, `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Code: `src/EnglishLiveTutor.Api/Program.cs`, `src/EnglishLiveTutor.Api/Endpoints/*.cs` (mỗi endpoint tự `try/catch` và trả `{ error = ex.Message }`), `src/EnglishLiveTutor.Application/Common/Exceptions/*`, `src/EnglishLiveTutor.Application/Common/Validation/ValidationException.cs`, `src/EnglishLiveTutor.Api/Security/*` (401/403/429 tự viết JSON). FE: `EnglishPartner.FE/src/shared/api/httpClient.ts` hàm `extractApiErrorMessage`, test `EnglishPartner.FE/src/shared/api/__tests__/httpClientErrors.test.ts`.

## Phạm vi

Được sửa: `EnglishPartner.BE/src/EnglishLiveTutor.Api/**`, `EnglishPartner.BE/tests/**`, `EnglishPartner.FE/src/shared/api/httpClient.ts` (chỉ `extractApiErrorMessage`), `EnglishPartner.FE/src/shared/api/__tests__/httpClientErrors.test.ts`.

Không được sửa: Application/Domain/Infrastructure (trừ khi thật cần một exception type mới trong `Application/Common/Exceptions` — ghi lý do), migration, phần FE khác.

**Status code của từng endpoint phải giữ nguyên.** Chỉ đổi hình dạng body lỗi.

## Yêu cầu

1. `AddProblemDetails` + một `IExceptionHandler` toàn cục, đăng ký `UseExceptionHandler` sớm trong pipeline. Ánh xạ mặc định:
   - `ValidationException` (FluentValidation/app) → 400, có `errors` (field → messages).
   - `NotFoundException` → 404; `ForbiddenException` → 403; `ConflictException` → 409.
   - `InvalidOperationException`, `ArgumentException` → 400 với message (đây là lỗi nghiệp vụ hiện đang trả cho client).
   - `OperationCanceledException` khi client hủy → không log error, trả 499/không body.
   - Còn lại → 500, `detail` chung chung tiếng Việt, **không** lộ `ex.Message`; log đầy đủ ở server.
   - Mọi body có `type`, `title`, `status`, `detail`, `traceId`, và **`error` = cùng nội dung với `detail`**.
2. Phản hồi 401/403 từ `EltAuthenticationHandler`, endpoint filter `RequirePermission`, CSRF middleware và rate limiter (429) cũng dùng cùng định dạng (có `error`).
3. Gỡ `try/catch` ở endpoint **khi** ánh xạ của nó trùng ánh xạ mặc định ở mục 1. Giữ catch cục bộ khi endpoint cố ý trả khác mặc định (ví dụ `ForbiddenException` → 404 ở lesson media, `InvalidOperationException` → 409 ở role-play, → 401 và xóa cookie ở `/api/auth/refresh`) nhưng các catch đó cũng trả body ProblemDetails qua một helper chung (ví dụ `ApiProblem.Result(status, message)`).
4. `return Results.BadRequest(new { error = ... })`, `Results.NotFound(new { error = ... })`… còn lại trong endpoint chuyển sang helper chung để body đồng nhất.
5. FE `extractApiErrorMessage`: thêm đọc `error` (string) — thứ tự ưu tiên `detail` → `error` → `message` → `errors` → `title`. Thêm test FE cho body ProblemDetails có `error` và body cũ chỉ có `{ error }`.
6. Test BE: mỗi loại exception ở mục 1 ra đúng status và có `error`; exception lạ không lộ message; một endpoint giữ ánh xạ riêng (vd lesson media Forbidden → 404) vẫn đúng.

## Tiêu chí hoàn thành

- [ ] `grep -rn "new { error" src/EnglishLiveTutor.Api` không còn (trừ helper chung).
- [ ] Status code endpoint không đổi; body lỗi có `error`.
- [ ] BE build/test pass (không test fail mới ngoài 9 test fail sẵn trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`); FE `npm run build` và `npx vitest run src/shared/api/__tests__/httpClientErrors.test.ts` pass.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE && dotnet build EnglishLiveTutor.sln && dotnet test EnglishLiveTutor.sln
cd ../EnglishPartner.FE && npm run build && npx vitest run src/shared/api/__tests__/httpClientErrors.test.ts
```

## Ràng buộc

- Không chạy `git add`, `git stash`, `git reset`, `git checkout` (orchestrator quản lý index). Không commit, push, merge, tạo branch; không thêm package (NuGet/npm); không chạy API/SQL/Gemini.
- Cần sửa ngoài phạm vi hoặc gặp điều mơ hồ: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt yêu cầu 1–6; danh sách endpoint giữ catch cục bộ và lý do>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
