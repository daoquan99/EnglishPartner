# Việc giao cho AGY: C1 — Identity commands dùng DbContext scoped + một lần commit (backend)

Đọc trước: `AGENTS.md` (gốc, Luật 8–9), `EnglishPartner.BE/AGENTS.md`, plan `D:/Projects/EnglishPartner/docs/tasks/20261029-a1-unit-of-work/plan.md`, skill `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`, `EnglishPartner.BE/.agents/skills/feature-cqrs/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`, `.agents/skills/security-audit/SKILL.md`.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Application/Features/Identity/Commands/**`, `src/EnglishLiveTutor.Application/Features/Identity/IdentityProjectionHelper.cs` (hoặc vị trí thực của helper), `tests/**`.
Không được sửa: route/DTO/status code/message lỗi, migration, Domain interface, `UserRepository.RotateRefreshTokenAsync`, query handler, FE, package.

## Yêu cầu

1. 12 handler: `AdminCreateUser`, `AdminUpdateUser`, `AssignUserRoles`, `CreateRole`, `DeletePermission`, `DeleteRole`, `DeleteUser`, `Login`, `RefreshToken`, `RegisterUser`, `UpdatePermission`, `UpdateRolePermissions`.
   - Bỏ `IAppDbContextFactory`/`CreateDbContextAsync`. Đọc (role/permission, kiểm tra trùng…) qua `IAppDbContext` scoped hoặc repository đã inject — cùng một `AppDbContext` của request.
   - Commit đúng **một lần** qua `IUnitOfWork.CommitChangesAsync(ct)`, thay cho `_xxxRepo.SaveChangesAsync`. Không commit giữa chừng trừ chỗ nêu ở mục 2.
   - Đọc dữ liệu cần cho response (role/permission codes để phát JWT) **sau** khi commit nếu hiện tại đang đọc sau commit — giữ đúng kết quả.
2. `RefreshToken`: nhánh phát hiện tái sử dụng phải **commit việc thu hồi cả chuỗi trước khi ném** lỗi 401 (giữ hành vi B2). Rotation vẫn gọi `RotateRefreshTokenAsync` (giữ nguyên transaction + update có điều kiện bên trong repository). Nếu sau rotation còn thay đổi cần lưu, commit qua UoW một lần.
3. `IdentityProjectionHelper.ResolveUserRolesAndPermissionsAsync` (và helper tương tự) nhận `IAppDbContext` scoped; không mở context mới.
4. Constructor gọn: không còn tham số factory. Cập nhật DI nếu cần (handler resolve qua MediatR, không đăng ký tay).
5. **Test** (EF InMemory, không mạng):
   - Mỗi handler đã đổi: chạy được với `IAppDbContext` + `IUnitOfWork` thật trên InMemory; dùng `IUnitOfWork` đếm số lần commit (decorator đếm) cho Login, RegisterUser, RefreshToken (rotation), AdminCreateUser, UpdateRolePermissions → đúng 1 lần.
   - RefreshToken reuse: token đã xoay bị dùng lại → 401 **và** toàn bộ chuỗi token của user bị thu hồi trong DB (đọc bằng context mới).
   - Login: token hết hạn/đã thu hồi bị purge; JWT chứa đúng role/permission sau khi `AssignUserRoles`.
   - Không handler nào trong phạm vi còn phụ thuộc `IAppDbContextFactory` (test reflection trên constructor các handler `Features.Identity.Commands`).
   - Cập nhật test cũ đang `new Handler(..., factory, ...)`.

## Lệnh kiểm chứng

Chỉ chạy `dotnet build` và `dotnet test EnglishLiveTutor.sln --no-build --filter "<lớp test liên quan>"`; chạy toàn bộ suite tối đa một lần ở cuối.

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~Identity|FullyQualifiedName~Auth|FullyQualifiedName~Jwt|FullyQualifiedName~Security"
```

Build 0 warning. Không fail mới ngoài 8 baseline (`PronunciationSynthesisAudioCacheTests` ×6, `ManualVocabularyGenerationTests` ×2).

## Ràng buộc

Không `git add`/`stash`/`reset`/`checkout`, không commit/push, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi. Gặp chỗ mơ hồ (ví dụ handler cần commit 2 lần để giữ hành vi) → làm theo hành vi hiện tại và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <theo từng handler: trước/sau — số lần commit, còn đọc gì>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test, số warning>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
