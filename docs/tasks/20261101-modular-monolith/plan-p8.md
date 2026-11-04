# P8 — bỏ `AppDbContext` + 10 migration cũ (người dùng chốt 2026-11-01)

Trạng thái DB: `LingoMint` (local) đã apply 7 migration `MoveToModuleSchema_<X>`; `dbo` còn `IdempotencyRecords` + `__EFMigrationsHistory` (10 dòng cũ). Người dùng xác nhận luồng chính chạy OK.

## Ba loại DB phải chạy đúng sau P8

| Loại | Trạng thái | Kỳ vọng khi chạy app |
| --- | --- | --- |
| **Hiện tại** (LingoMint) | 7 history module đã có `MoveToModuleSchema_<X>` | 7 module: không làm gì (ID đã apply). Shared: chuyển `dbo.IdempotencyRecords` → `shared` |
| **Cũ, chưa refactor** (staging/prod/máy khác ở `8884da1`) | 49 bảng ở `dbo`, `dbo.__EFMigrationsHistory` đủ 10 migration | Chuyển bảng sang schema module (như hiện tại) |
| **Trống** | Không có bảng | Tạo đủ bảng trong schema module + `shared` |
| Cũ nhưng **chưa đủ 10 migration cũ** | Thiếu bảng/cột | **Dừng** với lỗi rõ ràng: phải nâng lên bản trước P8 (`2bc98e4`) rồi mới lên P8 |

## Quyết định

| # | Quyết định |
| --- | --- |
| F1 | Giữ **nguyên ID** 7 migration `MoveToModuleSchema_<X>` (DB đã apply không chạy lại). Viết lại `Up()` thành tự chứa, toàn bộ bằng SQL có điều kiện: (1) `EnsureSchema`; (2) **chặn bản cũ dở dang**: nếu có bất kỳ bảng nào của module ở `dbo` mà `dbo.__EFMigrationsHistory` không có `20261029040750_AddGrammarRoadmap` → `THROW 50000, N'…', 1`; (3) với từng bảng: `IF OBJECT_ID(N'[dbo].[T]', N'U') IS NOT NULL AND OBJECT_ID(N'[x].[T]', N'U') IS NULL ALTER SCHEMA [x] TRANSFER [dbo].[T];`; (4) **tạo nếu thiếu**: từng `CREATE TABLE` bọc `IF OBJECT_ID(N'[x].[T]', N'U') IS NULL`, từng `CREATE INDEX` bọc `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = … AND object_id = OBJECT_ID(…))`, từng FK bọc `IF OBJECT_ID(N'[x].[FK_…]', N'F') IS NULL`. SQL tạo bảng lấy từ `Database.GenerateCreateScript()` của model **tại migration này** (= snapshot hiện có), không viết tay. `Down()` giữ nguyên. File `.Designer.cs` + snapshot giữ nguyên |
| F2 | `SharedDbContext` (Shared.Infrastructure) cho `IdempotencyRecords`, schema `shared`, history `shared.__EFMigrationsHistory`. Migration `InitialCreate_Shared`: cùng mẫu F1 (chuyển từ `dbo` nếu có, tạo nếu thiếu). `IdempotencyService` dùng `SharedDbContext`; tham gia UoW thay `AppDbContext` |
| F3 | Xóa khỏi `src`: `AppDbContext`, `IAppDbContext`, `IAppDbContextFactory`, `AppDbContextFactoryWrapper`, thư mục `Infrastructure/Migrations` (10 migration + snapshot + file `.sql`). `DatabaseMigrationRunner`: `SharedDbContext` → 7 module (không còn chuỗi cũ). `DatabaseSeeder`: dùng context module/shared tương ứng thay `AppDbContext` (giữ thứ tự seed, giữ một lần save cho mỗi bước như hiện tại) |
| F4 | `dbo.__EFMigrationsHistory` cũ **không xóa** (dùng cho kiểm F1-(2); vô hại) |
| F5 | Test: context gộp chỉ dùng cho test (InMemory) chuyển vào `tests/EnglishLiveTutor.Tests.Common` (giữ tên `AppDbContext`/`IAppDbContext`, namespace cũ để test không phải sửa), apply configuration từ 7 assembly module + shared. Guard test: bỏ test "10 migration đóng băng"; test so bảng module với snapshot cũ → so với danh sách cố định 48 bảng + `shared.IdempotencyRecords`; thêm test reflection chạy `Up()` mỗi migration module chỉ sinh `EnsureSchemaOperation` + `SqlOperation` |
| F6 | Composition (`EnglishLiveTutor.Application` + `EnglishLiveTutor.Infrastructure`) **giữ** trong P8; gộp là việc riêng |

## Gói

| Gói | Nội dung | Worker |
| --- | --- | --- |
| P8a | F1 (7 migration tự chứa). Chuỗi cũ vẫn còn → hành vi hiện tại không đổi | AGY |
| P8b | F2–F5 | AGY |
| V | Kiểm trên **DB tạm** ở localhost (không đụng `LingoMint`): (1) DB trống; (2) restore backup trước refactor (`C:\Backups`); (3) bản sao `LingoMint` hiện tại (backup `COPY_ONLY`). Mỗi DB: `dotnet ef database update` cho shared + 7, so cấu trúc (bảng/cột/kiểu/null/index/FK) giữa 3 DB và số dòng với nguồn; xóa DB tạm sau khi kiểm | Orchestrator |
