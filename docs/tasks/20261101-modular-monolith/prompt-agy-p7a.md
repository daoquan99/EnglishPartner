# Việc giao cho AGY: P7a — gom hạ tầng DbContext/UnitOfWork vào Shared.Infrastructure

## Bối cảnh (session mới)

- Luật: `AGENTS.md` (luật 12–15), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`, `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`.
- Trạng thái (`development`): 7 module × 5 project, Shared 4 project, solution `EnglishPartner.BE/EnglishLiveTutor.slnx`. Build 0 lỗi; `dotnet test EnglishLiveTutor.slnx` 669/669. Migration module **đã apply** trên DB thật — **không được** tạo/sửa migration nào, không đổi model EF. Làm trực tiếp trên `development`, không commit. Không mạng: `dotnet restore --ignore-failed-sources`, `--no-restore`.
- Vấn đề (review cuối, `docs/tasks/20261101-modular-monolith/review.md`): `ConfigureSqlServerOptions`, `ConfigureCommonOptions`, `CreateScopedContext` và đoạn đọc connection string bị chép ở 7 file `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure/<X>InfrastructureServiceCollectionExtensions.cs` + `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs`; `UnitOfWork` (composition) nhận cứng 8 context; `ModuleDbContextFactory<TDb>` cũ trong `src/EnglishLiveTutor.Infrastructure/Persistence/AppDbContext.cs` không còn được dùng (chỉ test).

## Yêu cầu

1. **Helper dùng chung** trong `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Persistence/`:
   - `ModuleDbContextServiceCollectionExtensions.AddModuleDbContext<TContext, TInterface>(this IServiceCollection services, IConfiguration configuration, string schema) where TContext : DbContext, TInterface where TInterface : class, IModuleDbContext` làm **đúng** những gì mỗi module đang làm: đọc connection string (cùng key/fallback/thông báo lỗi), `AddDbContextFactory<TContext>` (SqlServer, `SplitQuery`, `MigrationsAssembly(typeof(TContext).Assembly)`, `MigrationsHistoryTable("__EFMigrationsHistory", schema)`, interceptor auditing, logging), context scoped dùng `SqlConnection` scoped chung (+ `UseLoggerFactory`, `UseApplicationServiceProvider`, **không** ignore `PendingModelChangesWarning`), `AddScoped<TInterface>` → `TContext`, `IModuleDbContextFactory<TInterface>` (singleton, `ModuleDbContextFactory<TInterface, TContext>` hiện có), và đăng ký context tham gia unit of work (bước 2).
   - So từng dòng với 7 bản hiện tại trước khi xóa; khác biệt nào giữa các bản → giữ hành vi của bản đó (tham số tùy chọn) và ghi report. `AppDbContext` (host) giữ cấu hình riêng (ignore `PendingModelChangesWarning`, history `dbo`) — có thể dùng chung helper cấp thấp nếu tách được, không bắt buộc.
2. **UnitOfWork** → `src/Shared/EnglishLiveTutor.Shared.Infrastructure/Persistence/UnitOfWork.cs` (namespace có thể giữ `EnglishLiveTutor.Infrastructure.Persistence`): nhận `IEnumerable<IUnitOfWorkParticipant>` (interface nhỏ trả `DbContext`, đăng ký scoped bởi helper cho mỗi module context, và bởi host cho `AppDbContext`). **Giữ nguyên ký tự logic** `CommitChangesAsync`/`ExecuteInTransactionAsync`/`DetachTransactionAsync` hiện tại (gồm 2 fix: không mở transaction thứ hai khi đã có `CurrentTransaction`; gỡ `UseTransaction` trong `finally`). Thứ tự context: `AppDbContext` trước, rồi module theo thứ tự đăng ký. Đăng ký `IUnitOfWork` scoped như cũ.
3. 7 file `<X>InfrastructureServiceCollectionExtensions.cs` gọi `AddModuleDbContext<…>(configuration, ModuleSchemas.<X>)` thay cho khối chép; giữ nguyên các đăng ký khác (repository, adapter, options, HttpClient, hosted service) và lifetime. Host `DependencyInjection.cs` bỏ helper chép, dọn dòng trống thừa, bỏ `AddHttpClient()` trùng nếu module đã gọi (giữ một lần).
4. Xóa `ModuleDbContextFactory<TDb>` cũ trong `AppDbContext.cs`; test của nó (`tests/EnglishLiveTutor.ArchitectureTests/ModuleDbContextFactoryTests.cs`) chuyển sang kiểm `ModuleDbContextFactory<TInterface, TContext>` của Shared (giữ case: tạo đúng kiểu; context sai kiểu được dispose + ném lỗi — nếu class mới không có nhánh sai kiểu thì kiểm hành vi tương đương và ghi report).
5. Bỏ `InternalsVisibleTo` thừa ở `src/EnglishLiveTutor.Infrastructure/EnglishLiveTutor.Infrastructure.csproj` (chỉ giữ cho assembly test thật sự dùng `internal` của host — kiểm bằng build).
6. Test mới: helper đăng ký đúng (scoped cùng `SqlConnection`, factory resolve, `IUnitOfWorkParticipant` có đủ 8 context: AppDbContext + 7), UoW commit nhiều context (InMemory) như test hiện có.

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
# 7 lần, X = Identity Catalog Speech Lessons Grammar Vocabulary LiveTutor:
dotnet ef migrations has-pending-model-changes --context <X>DbContext --project src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
```

Tất cả phải: 0 lỗi, toàn bộ test pass (≥ 669), 7 context "No changes".

## Không được

Tạo/sửa migration, đổi model/tên bảng, đổi lifetime, đổi route/JSON, thêm package, commit.

## Worker report

File đã sửa/tạo/xóa; khác biệt phát hiện giữa 7 bản cấu hình (nếu có); lệnh + kết quả (số test, 7 has-pending).
