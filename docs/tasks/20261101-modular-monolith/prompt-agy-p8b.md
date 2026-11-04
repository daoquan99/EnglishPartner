# Việc giao cho AGY: P8b — SharedDbContext, xóa AppDbContext + 10 migration cũ

## Bối cảnh (session mới — đọc kỹ)

- Luật: `AGENTS.md` (**luật 12**), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`, `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`.
- Thiết kế: `docs/tasks/20261101-modular-monolith/plan-p8.md` — gói này làm **F2, F3, F4, F5**. P8a đã viết lại 7 migration module `MoveToModuleSchema_<X>` thành tự chứa (chặn DB cũ dở dang → chuyển từ `dbo` nếu có → tạo nếu thiếu). Đọc một file migration đó làm mẫu cho F2.
- Solution `EnglishPartner.BE/EnglishLiveTutor.slnx`. Làm trên `development`, không commit. **Không chạy `database update`, không kết nối SQL.** Không mạng: `dotnet restore --ignore-failed-sources`, `--no-restore`.

## Yêu cầu

1. **F2 — `SharedDbContext`** trong `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Persistence/`:
   - Chuyển `IdempotencyRecordEntity` (`src/EnglishLiveTutor.Infrastructure/Persistence/Entities/`) và `IdempotencyConfigurations` vào Shared.Infrastructure (**giữ namespace**). Configuration: `ToTable("IdempotencyRecords", "shared")`; cột/index/khóa giữ nguyên.
   - `SharedDbContext : DbContext` chỉ có `DbSet<IdempotencyRecordEntity> IdempotencyRecords`, `HasDefaultSchema("shared")`. Đăng ký qua `AddModuleDbContext`-tương đương: factory + scoped trên `SqlConnection` chung, history `shared.__EFMigrationsHistory`, `MigrationsAssembly` = Shared.Infrastructure, interceptor auditing, logging, **không** ignore `PendingModelChangesWarning`; tham gia unit of work (`IUnitOfWorkParticipant`) ở vị trí **đầu** (thay `AppDbContext`).
   - Migration `InitialCreate_Shared` (sinh bằng `dotnet ef migrations add … --context SharedDbContext --project src/Shared/EnglishLiveTutor.Shared.Infrastructure --startup-project src/EnglishLiveTutor.Api --output-dir Persistence/Migrations/Shared`), rồi **viết lại `Up()`** theo đúng mẫu P8a: `EnsureSchema("shared")`; chặn DB cũ dở dang (nếu `dbo.IdempotencyRecords` tồn tại mà `dbo.__EFMigrationsHistory` thiếu `20261029040750_AddGrammarRoadmap` → `THROW`); `IF OBJECT_ID(N'[dbo].[IdempotencyRecords]','U') IS NOT NULL AND OBJECT_ID(N'[shared].[IdempotencyRecords]','U') IS NULL ALTER SCHEMA [shared] TRANSFER [dbo].[IdempotencyRecords];`; tạo bảng/index nếu thiếu (SQL tĩnh từ script tạo của model, bọc `IF`). `Down()`: chuyển ngược về `dbo` (không drop).
   - `IdempotencyService` dùng `SharedDbContext` thay `AppDbContext` (logic giữ nguyên).
2. **F3 — xóa legacy khỏi `src`:** `AppDbContext`, `IAppDbContext`, `IAppDbContextFactory`, `AppDbContextFactoryWrapper`, toàn bộ `src/EnglishLiveTutor.Infrastructure/Migrations/` (10 migration, Designer, `AppDbContextModelSnapshot`, file `.sql`), đăng ký DI liên quan.
   - `DatabaseMigrationRunner`: migrate `SharedDbContext` rồi 7 module theo thứ tự hiện tại (Identity, Catalog, Speech, Lessons, Grammar, Vocabulary, LiveTutor); bỏ bước legacy. Giữ log/hủy/ném lỗi như hiện tại.
   - `DatabaseSeeder` (`src/EnglishLiveTutor.Infrastructure/Persistence/DatabaseSeeder.cs`, ~1700 dòng): thay `AppDbContext db` bằng context đúng module cho từng phần (Users/Roles/Permissions → Identity; AiModelCatalog/Prompts/SpeechSpeeds/TeacherVoices/Topics → Catalog; ScoringRubrics → Speech; Vocabularies → Vocabulary; …). **Giữ nguyên dữ liệu seed, thứ tự, điều kiện idempotent và số lần `SaveChangesAsync`** (mỗi lần save cũ → save trên context tương ứng; nếu một lần save cũ gom thay đổi của nhiều module thì save lần lượt các context đó ngay tại vị trí đó — ghi report từng chỗ). Ký hiệu public của seeder (tham số `Program.cs` truyền vào) sửa tối thiểu.
   - `Program.cs`, `Infrastructure/DependencyInjection.cs` sửa theo.
3. **F4:** không xóa `dbo.__EFMigrationsHistory` (không có migration nào drop nó).
4. **F5 — test:**
   - Chuyển `AppDbContext` + `IAppDbContext` (+ `IAppDbContextFactory`/wrapper nếu test dùng) sang `tests/EnglishLiveTutor.Tests.Common/Persistence/` **giữ nguyên tên và namespace** để test hiện có không phải sửa; class này apply configuration từ 7 assembly Infrastructure module + Shared.Infrastructure, có `DbSet<IdempotencyRecordEntity>`. Chỉ dùng cho test (InMemory).
   - `ApiTestFactory`: thay đăng ký InMemory cho `AppDbContext` (nếu có) bằng `SharedDbContext`; mọi context dùng chung một InMemory database như hiện tại.
   - Architecture/guard test: bỏ test "AppDbContext có đúng 10 migration"; test so bảng module với `AppDbContextModelSnapshot` → so với danh sách cố định (48 bảng module + `shared.IdempotencyRecords`); test mapping `AppDbContext` = hợp module → so `AppDbContext` (test) = hợp 7 module + shared; thêm `SharedDbContext` vào test model-vs-snapshot (has differences = false) và test "migration chỉ gồm EnsureSchema + Sql". Test thứ tự migrate: Shared → 7 module. `ModuleMap`: file mới ở Shared.Infrastructure là Shared.
5. Không còn chuỗi `AppDbContext`/`IAppDbContext` trong `src/` (kiểm bằng grep, trừ comment lịch sử nếu có — xóa luôn).

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
dotnet ef migrations has-pending-model-changes --context SharedDbContext --project src/Shared/EnglishLiveTutor.Shared.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
# + 7 module context như P8a
dotnet ef migrations script --idempotent --context SharedDbContext --project src/Shared/EnglishLiveTutor.Shared.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build -o <thư mục tạm>/shared.sql
```

## Không được

Sửa ID/nội dung 7 migration module (P8a đã xong); đổi tên bảng/cột module; chạy `database update`; kết nối SQL; thêm package; commit.

## Worker report

File xóa/chuyển/tạo; bảng ánh xạ phần seed → context; các chỗ một lần save cũ bị tách; đoạn SQL `Up()` của `InitialCreate_Shared`; lệnh + kết quả (số test, 8 has-pending).
