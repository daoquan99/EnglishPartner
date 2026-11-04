# P4b — migration chuyển schema cho 7 module + thứ tự migrate khi khởi động (E6, E7)

Theo `plan-p4.md` E6, E7. **Chỉ viết migration, không chạy `database update`, không kết nối SQL Server.** Người dùng sẽ tự chạy project để apply.

## Yêu cầu

1. Với mỗi module X (Identity, Catalog, Speech, Lessons, Grammar, Vocabulary, LiveTutor): tạo migration cho `<X>DbContext` vào `src/EnglishLiveTutor.Infrastructure/Persistence/Modules/Migrations/<X>/` bằng `dotnet ef migrations add MoveToModuleSchema_<X> --context <X>DbContext --project src/EnglishLiveTutor.Infrastructure --startup-project src/EnglishLiveTutor.Api --output-dir Persistence/Modules/Migrations/<X> --no-build` (sau khi build). Nếu lệnh cần design-time factory hoặc connection string, dùng đúng cơ chế mà `AppDbContext` đang dùng; không thêm secret.
2. EF sẽ sinh `CreateTable` (vì context chưa có snapshot). **Thay toàn bộ** thân `Up()` bằng:
   - `migrationBuilder.EnsureSchema(name: "<schema>");`
   - với **mỗi** bảng của module (đúng danh sách entity trong snapshot vừa sinh): `migrationBuilder.RenameTable(name: "<Bảng>", schema: "dbo", newName: "<Bảng>", newSchema: "<schema>");`
   - Không có `CreateTable`, `DropTable`, `AddColumn`, `AlterColumn`, `CreateIndex`, `DropIndex`, `Sql(...)` nào khác.
   `Down()`: `RenameTable` ngược (`schema: "<schema>"` → `newSchema: "dbo"`) cho mọi bảng (không drop schema, không drop bảng). Giữ nguyên file `.Designer.cs` và `<X>DbContextModelSnapshot.cs` do EF sinh (đó là model ở schema mới — đúng).
3. **E7** `src/EnglishLiveTutor.Infrastructure/Persistence/DatabaseSeeder.cs` `EnsureSchemaAndSeedAsync`: ngay sau `db.Database.MigrateAsync()` hiện có (chuỗi `AppDbContext` cũ), gọi `MigrateAsync()` lần lượt cho `IdentityDbContext`, `CatalogDbContext`, `SpeechDbContext`, `LessonsDbContext`, `GrammarDbContext`, `VocabularyDbContext`, `LiveTutorDbContext` (resolve từ cùng scope/service provider mà seeder đang dùng; nếu seeder nhận `AppDbContext` trực tiếp, thêm tham số/scope cần thiết — đọc `Program.cs` ~138-150). Giữ nguyên điều kiện `Database:MigrateOnStartup`, try/catch/log hiện có. Seed chạy sau tất cả migrate.
4. **Kiểm tĩnh** (không cần DB): với mỗi context chạy `dotnet ef migrations script --idempotent --context <X>DbContext --project … --startup-project … --no-build -o <scratch>/<X>.sql`, đọc SQL: chỉ được có `CREATE SCHEMA` (trong `IF SCHEMA_ID(...) IS NULL`), tạo bảng `__EFMigrationsHistory` trong schema module, `ALTER SCHEMA [<schema>] TRANSFER [dbo].[<Bảng>]` cho đúng số bảng, và `INSERT INTO [<schema>].[__EFMigrationsHistory]`. Dán phần thân (không gồm history) vào report cho **một** module và số dòng `ALTER SCHEMA` cho cả 7. Không lưu file .sql vào repo.
5. `dotnet ef migrations has-pending-model-changes` cho 7 module context → không có thay đổi. `AppDbContext` báo có thay đổi — **đúng** (đóng băng), không tạo migration cho nó.
6. Test:
   - Guard: mỗi migration module chỉ chứa `EnsureSchema` + `RenameTable` (reflection: chạy `Up()` vào một `MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer")` và kiểm kiểu operation ∈ {`EnsureSchemaOperation`, `RenameTableOperation`}), số `RenameTableOperation` = số entity type của context, mọi rename từ `dbo` sang đúng schema, `Down()` chỉ `RenameTableOperation` ngược.
   - Hợp tên bảng của 7 migration = toàn bộ bảng của `AppDbContext` trừ `IdempotencyRecords`; không trùng.
   - Thứ tự migrate trong seeder: legacy trước, 7 module sau (test bằng fake/ghi thứ tự nếu seeder cho phép; nếu không, kiểm bằng đọc source là không được — mô tả cách đã làm).

## Không được

Sửa 10 migration cũ hoặc `AppDbContextModelSnapshot`; tạo migration cho `AppDbContext`; chạy `database update`; đổi cờ `MigrateOnStartup`; thêm package; commit.

## Kiểm chứng

```bash
dotnet build EnglishLiveTutor.sln --no-restore
dotnet test EnglishLiveTutor.sln --no-build
# + lệnh migrations script / has-pending-model-changes ở trên
```

Worker report: danh sách migration (file), số bảng mỗi module, đoạn SQL mẫu, kết quả has-pending cho 8 context, thay đổi seeder (dòng trước/sau), lệnh + số test.
