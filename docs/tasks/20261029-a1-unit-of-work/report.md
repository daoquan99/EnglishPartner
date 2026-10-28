# Report: A1/A3 — command ghi qua DbContext scoped và một lần commit

- Mã task: `20261029-a1-unit-of-work` · Plan: [plan.md](plan.md)
- Worker: AGY `gemini-3.8-flash-high` (C1, C2). Orchestrator: plan, prompt, kiểm chứng, review, sửa sau review.
- Trạng thái: đã đưa về working tree chính dưới dạng **thay đổi chưa commit** (từ worktree `ai/20261029-a1-unit-of-work-agy`, base `308c8f2`).

## Kết quả kiểm chứng (working tree chính)

| Lệnh | Kết quả |
| --- | --- |
| `dotnet build EnglishLiveTutor.sln` | 0 lỗi; 2 warning có sẵn (`LessonAiGrader.cs`) |
| `dotnet test EnglishLiveTutor.sln` | 350 pass / 6 fail / 356. 6 fail = `PronunciationSynthesisAudioCacheTests` (EF InMemory không hỗ trợ `DataLength`), có từ trước. Base task: 317/8 |
| `grep IAppDbContextFactory` trong Commands + writer | chỉ còn `VocabularyGenerationWriter.RecordFailureAsync` (ngoại lệ có chủ đích) |

Chưa kiểm chứng: SQL Server thật (đặc biệt `RotateRefreshTokenConditionallyAsync` nhánh `ExecuteUpdate`), provider Gemini/AGY.

## Đã làm

- **C1 — 12 command Identity** (`AdminCreateUser`, `AdminUpdateUser`, `AssignUserRoles`, `CreateRole`, `DeletePermission`, `DeleteRole`, `DeleteUser`, `Login`, `RefreshToken`, `RegisterUser`, `UpdatePermission`, `UpdateRolePermissions`): bỏ `IAppDbContextFactory`; đọc qua `IAppDbContext` scoped; commit một lần qua `IUnitOfWork`. Mỗi request auth bớt một DbContext/kết nối. RefreshToken: reuse ngoài grace vẫn commit thu hồi cả chuỗi trước khi trả 401; rotation vẫn nguyên tử trong repository.
- **C2**: `AssessPronunciation` (gọi provider trước, ghi sau, một commit), `GenerateVocabulary` (đọc catalog qua context scoped), `VocabularyGenerationWriter` (scoped + UoW; DI Transient → Scoped), `ImportVocabularyJson`, `CreatePermission`, `RevokeRefreshToken`.
- **Bảo mật**: audit lỗi sinh từ vựng không còn lưu `ex.Message` thô — `SanitizeErrorMessage` lưu `<loại exception>: <message đã che>` (≤ 500 ký tự), che `key=`/`key%3D`, header `x-goog-api-key`, JSON `"key"`, `Bearer`, key `AIza…`.
- **Test mới**: `IdentityCommandsUnitOfWorkTests` (18: đếm commit, reuse thu hồi chuỗi đọc bằng context mới, purge token, JWT đúng role, reflection không còn factory), `AiCommandsUnitOfWorkTests` (provider trước khi ghi kiểm bằng change tracker, provider lỗi không ghi gì, audit không chứa secret, 6 case che key), `ManualVocabularyGenerationTests.Import_WithoutTodaysPlan_DoesNotCreateOne`.

## Orchestrator sửa sau review

- **High — hoàn tác đổi hành vi của AGY**: AGY khôi phục việc writer tự tạo kế hoạch hôm nay để 2 test cũ pass. Commit `2556ae4` đã **cố ý** bỏ hành vi đó (kế hoạch chỉ do `InitializeDailyVocabularyPlan` tạo theo settings, carry-over, review đến hạn). Bản AGY sẽ tạo plan sai cả ngày và có race với unique index `(UserId, LearningDate)` làm mất từ vựng vừa sinh. Đã trả writer về logic `2556ae4` (chỉ gán vào plan đã tồn tại).
- 2 test `ManualVocabularyGenerationTests` lỗi thời từ `2556ae4` được cập nhật: seed plan hôm nay trước khi import (đổi tên `Import_ShouldSaveFullGraphAndAssignToExistingTodaysPlan_…`), thêm test không tạo plan khi chưa có. Test import của AGY cũng seed plan.
- Low: mở rộng regex che key; test thứ tự provider kiểm change tracker thay vì DB.

## Còn lại / nợ kỹ thuật

- **Rủi ro chưa kiểm chứng (có từ trước)**: nhánh SQL của `UserRepository.RotateRefreshTokenConditionallyAsync` tách token cũ khỏi context nhưng `user.RefreshTokens` vẫn giữ object cũ chưa thu hồi; commit sau có thể gắn lại và ghi đè. Cần test tích hợp SQL Server.
- `GeminiVocabularyAiGenerator` (Infrastructure) vẫn đặt API key trong query string `?key=` — nên chuyển sang header `x-goog-api-key`.
- Interface repository Domain vẫn có `SaveChangesAsync` (13 interface); bỏ khi mọi caller đã dùng UoW.
- Query handler vẫn dùng factory (kiến trúc cho phép read factory).
- Diff bỏ BOM ở đầu một số file handler (không ảnh hưởng hành vi).
