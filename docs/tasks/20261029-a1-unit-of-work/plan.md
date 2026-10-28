# Plan: A1/A3 — command ghi qua DbContext scoped và một lần commit

- Mã task: `20261029-a1-unit-of-work`
- Ngày: 2026-10-29
- Liên quan: review 2026-10-28 mục **A1**, **A3** ([review](../../reviews/2026-10-28-security-performance-architecture.md)); [report đợt trước](../20261028-review-fix/report.md) mục 6; ADR-0001; Luật 8, 9 (`AGENTS.md`); `EnglishPartner.BE/.agents/architecture.md` §3.
- **Cần người dùng duyệt:** không bắt buộc (không migration, không đổi contract, không đụng dữ liệu thật). Người dùng đã yêu cầu "lên plan và kiểm soát AGY code".
- Worker: AGY `gemini-3.8-flash-high`, `--allow-shell`, worktree `D:/Projects/EnglishPartner.worktrees/20261029-a1-unit-of-work-agy` (từ commit `308c8f2`).

## Mục tiêu

Command ghi dữ liệu theo pattern mới: repository/`IAppDbContext` **scoped** (cùng một `AppDbContext` của request) và **một** lần commit qua `IUnitOfWork.CommitChangesAsync`. Không mở thêm DbContext bằng `IAppDbContextFactory` trong command. Không giữ transaction quanh lời gọi provider AI.

## Hiện trạng (đã khảo sát)

- `UserRepository`/`RoleRepository`/`PermissionRepository` đã dùng `AppDbContext` scoped, nhưng handler commit bằng `repo.SaveChangesAsync` rồi mở **thêm** context qua `_dbFactory` để đọc role/permission hoặc ghi.
- `AssessPronunciationCommandHandler`, `GenerateVocabularyCommandHandler`, `VocabularyGenerationWriter` tạo context riêng bằng factory để ghi.
- Chưa command nào dùng `ITransactionalCommand`.

## Phạm vi — file

Batch C1 (Identity), `src/EnglishLiveTutor.Application/Features/Identity/Commands/`:
`AdminCreateUser`, `AdminUpdateUser`, `AssignUserRoles`, `CreateRole`, `DeletePermission`, `DeleteRole`, `DeleteUser`, `Login`, `RefreshToken`, `RegisterUser`, `UpdatePermission`, `UpdateRolePermissions` (+ `IdentityProjectionHelper` nếu cần nhận `IAppDbContext`).

Batch C2 (AI writes): `Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommandHandler.cs`, `Features/Vocabulary/Commands/GenerateVocabulary/GenerateVocabularyCommandHandler.cs`, `Features/Vocabulary/VocabularyGenerationWriter.cs` và nơi gọi writer (ví dụ `ImportVocabularyJson`).

Test: `tests/**` liên quan.

**Ngoài phạm vi (ghi nợ):** query handler dùng factory (kiến trúc cho phép read factory); bỏ `SaveChangesAsync` khỏi interface repository Domain (13 interface, làm khi mọi caller đã chuyển); `ConversationRepository` (realtime singleton, ngoại lệ có chủ đích); `UserRepository.RotateRefreshTokenAsync` giữ transaction + `ExecuteUpdateAsync` có điều kiện (an toàn đồng thời của B2).

## Hành vi phải giữ

- Route, DTO, status code, message lỗi.
- Refresh token: phát hiện tái sử dụng vẫn **commit việc thu hồi cả chuỗi trước khi** trả 401; rotation vẫn nguyên tử.
- Login: purge token hết hạn + ghi lần đăng nhập + phát token trong một commit.
- GenerateVocabulary: provider lỗi → không có dữ liệu từ vựng ghi dở; audit lỗi vẫn được lưu (như hiện tại). Không gọi provider trong transaction.
- AssessPronunciation: gọi provider trước, ghi kết quả sau, một commit.

## Kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln        # 0 warning
dotnet test EnglishLiveTutor.sln         # không fail mới ngoài 8 baseline
grep -rn "IAppDbContextFactory" src/EnglishLiveTutor.Application/Features --include=*.cs   # không còn trong Commands/ thuộc phạm vi và VocabularyGenerationWriter
```

Baseline (commit `308c8f2`): 317 pass / 8 fail (`PronunciationSynthesisAudioCacheTests` ×6, `ManualVocabularyGenerationTests` ×2).

## Review

security-reviewer (C1 đụng auth/refresh token) ‖ performance-reviewer (số lần commit/kết nối, AI ngoài transaction) → code-reviewer.
