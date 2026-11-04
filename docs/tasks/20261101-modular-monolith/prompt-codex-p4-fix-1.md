# P4 fix 1 — sửa theo code review P4

Không tạo/sửa migration; không chạy `database update`. Build `--no-restore`, toàn bộ test pass.

1. **M1 — migrate lỗi phải dừng app** (`src/EnglishLiveTutor.Api/Program.cs` ~159-166): khi `Database:MigrateOnStartup` = true và migrate/seed ném lỗi, log `LogCritical` (không in connection string) rồi **ném lại** ở mọi môi trường (kể cả Development), để không bao giờ chạy với schema nửa vời. Hành vi khi cờ = false giữ nguyên. Nếu code cũ cố ý nuốt lỗi chỉ của **seed** (không phải migrate) ở Development, giữ hành vi đó cho seed và chỉ ném với lỗi migrate — tách hai bước để phân biệt; mô tả trong report.
2. **M2 — logging cho context scoped** (`Infrastructure/DependencyInjection.cs` `CreateScopedContext` ~354): thêm `UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())` và `UseApplicationServiceProvider(sp)` (nếu không gây vòng/khác hành vi), để context request có cùng logging như context từ factory.
3. **M3 — chặn model lệch snapshot:**
   - `ConfigureWarnings(Ignore PendingModelChangesWarning)` chỉ áp cho `AppDbContext` (đóng băng). 7 context module **không** ignore (để `MigrateAsync` báo lỗi nếu quên migration).
   - Test: với 7 context module, `IMigrationsModelDiffer.HasDifferences(snapshot.Model.GetRelationalModel(), designTimeModel.GetRelationalModel())` = false (lấy snapshot qua `IMigrationsAssembly.ModelSnapshot`, model design-time qua `IDesignTimeModel`; build context bằng `UseSqlServer("Server=.;Database=x")`, không mở kết nối).
4. **L3** `Persistence/DatabaseSeeder.cs` ~44: truyền `CancellationToken` vào `MigrateAsync` của chuỗi cũ.
5. **L4** `Persistence/AppDbContext.cs` ~179-188 `ModuleDbContextFactory`: cache delegate/`MethodInfo` theo `TDb` (static readonly), không reflection mỗi lần tạo.
6. **L5** test: (a) `EntityMapping` so thêm key, index (tên + cột + unique), FK (cột, bảng đích, `OnDelete`) giữa `AppDbContext` và context module; (b) danh sách bảng trong 7 migration module = danh sách bảng trong `AppDbContextModelSnapshot` cũ (đọc model snapshot, không phải model hiện tại) trừ `IdempotencyRecords`.
7. **L1** thêm comment ngắn ở `UnitOfWork` về bất biến: mọi context trong scope dùng chung một connection — không chạy song song trên các context scoped; context không enlist không được chạy lệnh trong lúc transaction nhiều context đang mở.

Worker report: từng mục → file:dòng, hành vi trước/sau của M1, lệnh + số test.
