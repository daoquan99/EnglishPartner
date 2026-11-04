# Việc giao cho AGY: P8a — 7 migration module tự chứa (không cần chuỗi migration cũ)

## Bối cảnh (session mới — đọc kỹ)

- Luật: `AGENTS.md` (**luật 12**: migration), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`.
- Thiết kế: `docs/tasks/20261101-modular-monolith/plan-p8.md` — gói này làm **F1**. Đọc kỹ bảng "Ba loại DB".
- Solution `EnglishPartner.BE/EnglishLiveTutor.slnx`. Build 0 lỗi; `dotnet test` 681/681. Làm trên `development`, không commit. **Không chạy `database update`, không kết nối SQL Server.** Không mạng: `--no-restore`.
- 7 migration: `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure/Persistence/Modules/Migrations/<X>/<id>_MoveToModuleSchema_<X>.cs` (X = Identity, Catalog, Speech, Lessons, Grammar, Vocabulary, LiveTutor). Hiện `Up()` = `EnsureSchema` + `RenameTable(dbo → <schema>)` cho mọi bảng; chạy sau chuỗi 10 migration cũ của `AppDbContext`. P8b sẽ xóa chuỗi cũ, nên `Up()` phải tự lo cả DB trống.

## Yêu cầu

1. **Giữ nguyên** tên file, class, attribute `[Migration("<id>")]`, `[DbContext]`, file `.Designer.cs`, `<X>DbContextModelSnapshot.cs`, và `Down()`. Chỉ viết lại thân `Up()`.
2. `Up()` mới (thứ tự, toàn bộ là `migrationBuilder.EnsureSchema(...)` + `migrationBuilder.Sql(...)`):
   1. `migrationBuilder.EnsureSchema("<schema>")`.
   2. **Chặn DB cũ dở dang**: một câu SQL: nếu `OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL` và có ít nhất một bảng của module ở `dbo` và `NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261029040750_AddGrammarRoadmap')` → `THROW 50000, N'<schema>: database is on an older pre-modular schema. Upgrade with the release before P8 (commit 2bc98e4) first.', 1;`. Ngoài ra nếu có bảng module ở `dbo` mà **không có** bảng history `dbo` → cũng `THROW` (không xác định được phiên bản).
   3. **Chuyển nếu có ở dbo**: với từng bảng `T` của module (đúng danh sách `RenameTable` hiện có): `IF OBJECT_ID(N'[dbo].[T]', N'U') IS NOT NULL AND OBJECT_ID(N'[<schema>].[T]', N'U') IS NULL ALTER SCHEMA [<schema>] TRANSFER [dbo].[T];`
   4. **Tạo nếu thiếu**: lấy SQL tạo model của `<X>DbContext` **từ snapshot của migration này** — cách làm: viết một test/console tạm (không commit) build model từ `<X>DbContextModelSnapshot` hoặc dùng `context.Database.GenerateCreateScript()` với `UseSqlServer("Server=.;Database=x")` (không mở kết nối) — rồi tách thành từng lệnh (`CREATE TABLE`, `CREATE INDEX`/`CREATE UNIQUE INDEX`, `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY`, bỏ `CREATE SCHEMA` vì đã có bước 1). Bọc **từng lệnh**:
      - `CREATE TABLE [s].[T]` → `IF OBJECT_ID(N'[s].[T]', N'U') IS NULL BEGIN EXEC(N'<lệnh, nhân đôi dấu nháy đơn>') END`
      - index `[IX]` trên `[s].[T]` → `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX' AND [object_id] = OBJECT_ID(N'[s].[T]')) EXEC(N'…')`
      - FK `[FK]` → `IF OBJECT_ID(N'[s].[FK]', N'F') IS NULL EXEC(N'…')`
      - FK tạo **sau** khi mọi bảng của module đã tạo (thứ tự giống script EF).
      - Nếu `GenerateCreateScript` sinh lệnh khác (default constraint riêng, sequence, check) → bọc tương tự theo `sys.*` phù hợp và ghi report.
   5. Không còn `RenameTable`, không `CreateTable`/`AddColumn`… dạng operation EF (để migration chạy được cả 3 trường hợp).
3. Tạo một helper dùng chung (ví dụ `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Persistence/ModuleSchemaMigrationSql.cs`) sinh các câu SQL bọc điều kiện ở bước 2–3 nếu giúp giảm lặp — **nhưng** SQL tạo bảng (bước 4) phải là chuỗi tĩnh trong file migration (đóng băng theo thời điểm migration, không sinh lúc chạy từ model hiện tại).
4. **Test** (ArchitectureTests): (a) chạy `Up()` mỗi migration với `MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer")`: chỉ gồm `EnsureSchemaOperation` + `SqlOperation`; (b) SQL của mỗi migration chứa đúng một `ALTER SCHEMA [s] TRANSFER [dbo].[T]` cho mỗi bảng của module và một `CREATE TABLE [s].[T]` cho mỗi bảng (danh sách bảng lấy từ snapshot `<X>DbContextModelSnapshot`); (c) mọi `CREATE TABLE`/`CREATE INDEX`/`FOREIGN KEY` đều nằm trong khối `IF`. Cập nhật test guard cũ đang kiểm "chỉ EnsureSchema + RenameTable".
5. `dotnet ef migrations has-pending-model-changes` cho 7 context vẫn "No changes" (không đổi model/snapshot).

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
# 7 lần: dotnet ef migrations has-pending-model-changes --context <X>DbContext --project src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
# 7 lần (không cần DB): dotnet ef migrations script --idempotent --context <X>DbContext --project … --startup-project … --no-build -o <thư mục tạm>/<X>.sql
```

Đọc lại script idempotent của ít nhất 1 module, dán đoạn đầu (chặn + chuyển + 1 CREATE TABLE + 1 index + 1 FK) vào report.

## Không được

Đổi ID/tên migration, `.Designer.cs`, snapshot, `Down()`; sửa 10 migration cũ; chạy `database update`; kết nối SQL; thêm package; commit.

## Worker report

File sửa/tạo; số bảng/index/FK mỗi module trong SQL tạo; đoạn SQL mẫu; lệnh + kết quả (test, 7 has-pending, 7 script).
