# Việc giao cho Codex: P4a — DbContext + schema theo module (chưa có migration)

## Bối cảnh (session mới — đọc kỹ)

- `AGENTS.md` (gốc; luật 12–15), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`, skill `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`.
- Thiết kế P4: `docs/tasks/20261101-modular-monolith/plan-p4.md` (đọc toàn bộ). Gói này là **P4a = E1–E5**. **Không tạo migration** trong gói này (P4b), không chạy `database update`, không kết nối SQL Server.
- Trạng thái: P1–P3 xong. 7 module là project riêng (`src/Modules/<X>/`), mỗi module có `I<X>DbContext` (trong project module), `AppDbContext` (Infrastructure) implement cả 7 + `IdempotencyRecords`; DI hiện trả cùng instance `AppDbContext` cho mọi interface. Build 0 lỗi 0 warning, `dotnet test` 624/624, allowlist 0, `has-pending-model-changes` (AppDbContext) = không đổi. Không có mạng: `--no-restore`.

## Yêu cầu

1. **E1 schema:** `src/EnglishLiveTutor.Infrastructure/Persistence/ModuleSchemas.cs` (`public static class` với const `Identity = "identity"`, `Catalog = "catalog"`, `Speech = "speech"`, `Lessons = "lessons"`, `Grammar = "grammar"`, `Vocabulary = "vocabulary"`, `LiveTutor = "livetutor"`). Trong từng file `Persistence/Configurations/*Configurations.cs`, đổi `ToTable("X")` → `ToTable("X", ModuleSchemas.<Module>)` theo module của entity (bảng trong skill/`ModuleMap`). **Không** đổi tên bảng, cột, index, khóa, kiểu. `IdempotencyConfigurations` giữ `dbo`. Nếu một `ToTable` đã có tham số khác (table builder, trigger…) → giữ nguyên tham số, chỉ thêm schema.
2. **E2 context:** `src/EnglishLiveTutor.Infrastructure/Persistence/Modules/<X>DbContext.cs` cho 7 module: `public sealed class <X>DbContext(DbContextOptions<<X>DbContext> options) : DbContext(options), I<X>DbContext`; property `DbSet` đúng tên trong interface; `OnModelCreating` apply **chỉ** configuration của module (danh sách `ApplyConfiguration(new …())` tường minh, hoặc `ApplyConfigurationsFromAssembly` với predicate theo danh sách type — test kiểm đủ/không thừa). `SaveChangesAsync` của interface được `DbContext` cung cấp.
3. **E3 connection + UoW:**
   - DI (`Infrastructure/DependencyInjection.cs`): đăng ký scoped `SqlConnection` (`Microsoft.Data.SqlClient`) từ connection string hiện có (không đọc config mới, không log giá trị). Mỗi `<X>DbContext` đăng ký `AddDbContext<<X>DbContext>((sp, o) => o.UseSqlServer(sp.GetRequiredService<SqlConnection>(), sql => { sql.UseQuerySplittingBehavior(SplitQuery); sql.MigrationsHistoryTable("__EFMigrationsHistory", ModuleSchemas.<X>); sql.MigrationsAssembly(<Infrastructure>); }); o.AddInterceptors(<AuditingSaveChangesInterceptor>); o.ConfigureWarnings(<giống AppDbContext>))`. `AppDbContext` scoped cũng dùng cùng `SqlConnection` scoped (giữ `IDbContextFactory<AppDbContext>` cho factory như cũ, factory tạo connection riêng).
   - Cho test InMemory: nếu provider không phải relational (ví dụ test thay options), không được ném lỗi — giữ cơ chế test hiện tại hoạt động (đọc `ApiTestFactory`, test DI) và mô tả cách làm trong report.
   - `UnitOfWork` (`Persistence/UnitOfWork.cs`): nhận các context trong scope (`AppDbContext` + 7 module context). `CommitChangesAsync`: lấy các context `ChangeTracker.HasChanges()`; nếu 0 → trả 0; nếu 1 → `SaveChangesAsync` như cũ; nếu >1 và relational → mở transaction trên connection chung (context đầu `Database.BeginTransactionAsync`, context khác `Database.UseTransactionAsync(tx.GetDbTransaction())`), save lần lượt, commit; lỗi → rollback, ném lại. Giữ nguyên chữ ký/kết quả trả về hiện tại. `ExecuteInTransactionAsync`: transaction trên connection chung, mọi context dùng chung transaction; giữ execution strategy/retry nếu code cũ có.
4. **E4 DI + repository + factory:**
   - `I<X>DbContext` → `sp.GetRequiredService<<X>DbContext>()` (thay vì `AppDbContext`). `IAppDbContext` → vẫn `AppDbContext` (seeder, idempotency, test).
   - `ModuleDbContextFactory<TDb>`: tạo `<X>DbContext` tương ứng `TDb` với connection riêng (dùng `IDbContextFactory<<X>DbContext>` — đăng ký `AddDbContextFactory` cho từng module context với cùng options trừ connection: dùng connection string), dispose đúng như hiện tại. Đăng ký đóng (closed) cho 7 interface thay vì generic mở; `IModuleDbContextFactory<IAppDbContext>` không resolve được.
   - Repository trong `Infrastructure/Repositories/*` và adapter Infrastructure khác đang nhận `AppDbContext`/`IAppDbContext`/`IDbContextFactory<AppDbContext>` cho dữ liệu module → chuyển sang context/factory của module đó (theo `ModuleMap`). Ngoại lệ được giữ `AppDbContext`: `DatabaseSeeder`, `LessonRubricSeeder`, `IdempotencyService`, `CefrPromptBuilder` nếu còn (liệt kê). Repository mixed (ví dụ `IdentityRepositories.cs` chỉ Identity) → context module tương ứng.
5. **E5 guard test** (`tests/.../Architecture/` hoặc `Persistence/`):
   - Mapping: với model của `AppDbContext` và hợp 7 context module (dùng `UseSqlServer("Server=.;Database=x")` chỉ để build model, **không** mở kết nối), mỗi entity CLR có cùng (schema, table) và cùng tập (column, kiểu store) — `AppDbContext` có thêm `IdempotencyRecords` (`dbo`).
   - Mỗi context module: mọi entity ở schema của module; không entity của module khác; `GetDefaultSchema`/history table đúng schema.
   - Số migration của `AppDbContext` (type có `[DbContext(typeof(AppDbContext))]` + `[Migration]`) = 21 (đóng băng).
   - DI: trong một scope, 7 `I<X>DbContext` là 7 instance khác nhau, mỗi cái là `<X>DbContext`; tất cả dùng **cùng** `DbConnection` instance (`Database.GetDbConnection()`), và `AppDbContext` cũng vậy.
   - UoW: test InMemory: thay đổi ở 2 context → cả hai được lưu (đếm `SaveChanges` gọi trên từng context hoặc kiểm dữ liệu sau commit).
6. Test cũ: phải pass. Được sửa wiring (ví dụ test tạo `AppDbContext` InMemory và resolve `I<X>DbContext` từ DI) — mô tả từng loại sửa. Không đổi assertion nghiệp vụ.

## Không được

Tạo/sửa migration (kể cả 21 migration cũ và `AppDbContextModelSnapshot`), chạy `dotnet ef database update`, đổi tên bảng/cột, đổi route/DTO, thêm package mới (dùng `Microsoft.Data.SqlClient` có sẵn qua EF SqlServer), commit.

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet build EnglishLiveTutor.sln --no-restore
dotnet test EnglishLiveTutor.sln --no-build
dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~Architecture"
```

(`has-pending-model-changes` của module context sẽ báo thay đổi vì chưa có migration — bình thường ở P4a; ghi kết quả.)

## Worker report (bắt buộc)

```
### Worker report
- File đã sửa/tạo: <danh sách theo nhóm>
- Configuration: số ToTable đã thêm schema theo module
- Repository/adapter chuyển context: <bảng file → context>; giữ AppDbContext: <file: lý do>
- UoW: mô tả commit nhiều context; hành vi khi InMemory
- Test cũ phải sửa wiring: <file: cách sửa>
- Lệnh đã chạy và kết quả: <lệnh → số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
