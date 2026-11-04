# P4 — DbContext + schema theo module (thiết kế của orchestrator)

Ràng buộc người dùng (2026-11-01): **viết migration, không apply**; người dùng tự chạy project (`Database:MigrateOnStartup`) để apply lên `LingoMint`. Backup đã có ở `C:\Backups`. Vì migration tự apply khi khởi động, chúng phải đúng cho **cả DB đang có dữ liệu lẫn DB trống**.

## Quyết định

| # | Quyết định |
| --- | --- |
| E1 | Schema: `identity`, `catalog`, `speech`, `lessons`, `grammar`, `vocabulary`, `livetutor`. Đặt trong từng configuration (`builder.ToTable("<TênCũ>", ModuleSchemas.<X>)`), tên bảng/cột/index **giữ nguyên**. `IdempotencyRecords` (Shared) ở lại `dbo`. |
| E2 | Mỗi module một context `Infrastructure/Persistence/Modules/<X>DbContext.cs : DbContext, I<X>DbContext`, chỉ apply configuration của module (danh sách tường minh), `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")`, interceptor auditing như `AppDbContext`, `SplitQuery` như cũ. |
| E3 | Một `SqlConnection` scoped dùng chung cho mọi context module + `AppDbContext` trong một request. `UnitOfWork` commit mọi context có thay đổi trong **một** transaction trên connection đó (context đầu `BeginTransaction`, các context khác `UseTransaction`); `ExecuteInTransactionAsync` tương tự. Giữ nguyên hành vi commit một lần (kể cả observer Speech → Vocabulary của D8). |
| E4 | `I<X>DbContext` → `<X>DbContext` (scoped). `IModuleDbContextFactory<T>` → tạo `<X>DbContext` với connection riêng (như factory cũ). Repository Infrastructure chuyển từ `AppDbContext` sang context module của nó. |
| E5 | `AppDbContext` giữ lại: (a) chủ của **21 migration cũ — đóng băng**, (b) seeder, (c) idempotency, (d) test InMemory hiện có. Model của nó dùng cùng configuration nên cũng mang schema mới; snapshot cũ không cập nhật (cảnh báo pending model đã bị bỏ qua sẵn). Test chặn: thư mục `Migrations/` của `AppDbContext` không được có migration mới; test so sánh mapping bảng/cột của `AppDbContext` = hợp các context module (+ `IdempotencyRecords`). |
| E6 | Migration mỗi module (thư mục `Infrastructure/Persistence/Modules/Migrations/<X>/`): **một** migration `MoveToModuleSchema_<X>`: `EnsureSchema("<x>")` + `RenameTable(name, schema: "dbo", newName: name, newSchema: "<x>")` cho từng bảng (SQL Server sinh `ALTER SCHEMA … TRANSFER`, chỉ metadata); `Down()` chuyển ngược. Snapshot = model module ở schema mới. Không có CreateTable/DropTable/AlterColumn. |
| E7 | Thứ tự khởi động trong `DatabaseSeeder.EnsureSchemaAndSeedAsync`: `AppDbContext.MigrateAsync()` (chuỗi cũ: DB trống → tạo bảng ở `dbo`; DB cũ → không làm gì) → `<X>DbContext.MigrateAsync()` theo thứ tự Identity, Catalog, Speech, Lessons, Grammar, Vocabulary, LiveTutor → seed. Không đổi cờ `MigrateOnStartup`. |
| E8 | Outbox: **không thêm** ở P4 — chưa có use case cần hiệu ứng phụ trễ; observer D8 cần nhất quán ngay (điểm hiển thị trên flashcard). Ghi vào ADR. |

## Gói

| Gói | Nội dung |
| --- | --- |
| P4a | E1–E5: schema trong configuration, 7 context, connection scoped dùng chung, UoW nhiều context, DI, repository, factory, test mapping/guard. Chưa có migration module. |
| P4b | E6–E7: 7 migration chuyển schema, thứ tự migrate khi khởi động, kiểm bằng `dotnet ef migrations script --idempotent` cho từng context (đọc SQL: chỉ `CREATE SCHEMA` + `ALTER SCHEMA … TRANSFER` + insert history), `has-pending-model-changes` = không đổi cho cả 7 context. |
| P4c | Orchestrator: ADR (E8, trạng thái), skill, report. |

Không kiểm chứng tĩnh được: chạy migration trên SQL Server thật (người dùng tự chạy); khóa/thời gian `ALTER SCHEMA TRANSFER` (metadata, nhanh); quyền `ALTER` schema của tài khoản SQL.
