# P5 — 5 project mỗi module (người dùng chọn 2026-11-01)

Mục tiêu: mỗi module tự chứa đủ 4 layer + Contracts, compiler chặn hướng phụ thuộc giữa layer và giữa module. Giữ nguyên namespace (snapshot EF, migration `[DbContext]`, JSON), route, hub, DB.

## Cấu trúc đích

```text
src/
  EnglishLiveTutor.Domain/                       domain kernel (Domain/Common) — BCL only
  EnglishLiveTutor.SharedKernel/                 application kernel (Common, IModuleDbContext*)
  EnglishLiveTutor.Shared.Infrastructure/        (mới) hạ tầng dùng chung: AuditingSaveChangesInterceptor, ModuleSchemas, EntityConfigurationExtensions, Gemini HTTP/API key/AGY runner, GeminiLiveSessionBridge, … (Shared Kernel trong ModuleMap)
  EnglishLiveTutor.Shared.Presentation/          (mới) helper endpoint dùng chung: RequirePermission/endpoint filter, PaginationEndpointExtensions, HubExceptionHelper, … (FrameworkReference Microsoft.AspNetCore.App)
  Modules/<X>/
    EnglishLiveTutor.Modules.<X>.Domain/          Domain/<Area>/**                       → Domain kernel
    EnglishLiveTutor.Modules.<X>.Contracts/       (như P3)                               → SharedKernel, Domain kernel, Contracts trong DAG
    EnglishLiveTutor.Modules.<X>.Application/     Features/**, Modules/<X>/I<X>DbContext, Add<X>Module()  → <X>.Domain, <X>.Contracts, SharedKernel, Contracts trong DAG
    EnglishLiveTutor.Modules.<X>.Infrastructure/  Infrastructure của module: <X>DbContext + Migrations/<X>, Configurations, Repositories, adapter AI/storage/worker, options, seeder riêng; Add<X>Infrastructure()  → <X>.Application, <X>.Domain, Shared.Infrastructure, SharedKernel, Contracts trong DAG
    EnglishLiveTutor.Modules.<X>.Presentation/    Endpoints (+ Hubs/Realtime cho LiveTutor); Map<X>Endpoints()  → <X>.Application, <X>.Contracts, Shared.Presentation, SharedKernel
  EnglishLiveTutor.Application/                  composition: ApplicationAssemblies, gọi Add<X>Module()
  EnglishLiveTutor.Infrastructure/               composition: AppDbContext (legacy migrations + seeder + idempotency), UnitOfWork, DatabaseSeeder, DatabaseMigrationRunner, IdempotencyService, DI gọi Add<X>Infrastructure()
  EnglishLiveTutor.Api/                          host: Program, auth, CORS, rate limit, gọi Map<X>Endpoints(), MapHub
```

Project `EnglishLiveTutor.Modules.<X>` của P3 được **đổi tên** thành `EnglishLiveTutor.Modules.<X>.Application` sau khi tách Domain ra.

## Quy tắc

- Namespace giữ nguyên ở mọi file di chuyển. Migration chuyển assembly: cập nhật `MigrationsAssembly(...)` của `<X>DbContext` sang assembly `Modules.<X>.Infrastructure` (ID migration và history không đổi). `AppDbContext` apply configuration từ mọi assembly Infrastructure module (model phải giống hệt — guard test P4a).
- Domain module: chỉ `EnglishLiveTutor.Domain` (kernel) + BCL; **không** package nào (EF/MediatR/FluentValidation) — test csproj chặn.
- `ProjectReferenceTests` theo bảng trên cho mọi csproj dưới `src/Modules/` (suy layer từ hậu tố tên project). `ModuleMap`: `src/Modules/<X>/` → X (giữ), `Shared.*` → Shared.
- `ApplicationAssemblies.All`: mọi assembly Application + Contracts + Domain module có request/validator; test hiện có giữ đúng.
- `InternalsVisibleTo` tests: thêm cho project module nào có type `internal` mà test dùng.
- Không thêm package mới (dùng package hiện có, cùng version). Không đổi route, JSON, hub, DB, migration ID.

## Gói

| Gói | Nội dung |
| --- | --- |
| P5a | Tạo `Shared.Infrastructure`, `Shared.Presentation`; chuyển file dùng chung; cập nhật test kiến trúc cho cấu trúc 5 layer (rule theo hậu tố) |
| P5b–P5h | Từng module theo thứ tự Catalog, Identity, Speech, Vocabulary, Lessons, Grammar, LiveTutor: tách `.Domain`, đổi tên `.Application`, tạo `.Infrastructure` (chuyển file Infrastructure của module + DbContext + migration), tạo `.Presentation` (chuyển endpoint/hub) |
| P5i | Dọn: `Infrastructure`/`Api` chỉ còn composition/host; ADR/skill/AGENTS cập nhật; review cuối |

Mỗi gói: build 0 lỗi 0 warning, toàn bộ test pass, `has-pending-model-changes` 7 context = không đổi, `migrations list`/script không đổi ID, allowlist 0.

## Bổ sung (người dùng chốt 2026-11-01)

| Gói | Nội dung |
| --- | --- |
| P5j (chạy ngay sau P5c, trước P5d — người dùng yêu cầu làm Shared trước) | Đổi tên phần dùng chung thành 4 project: `EnglishLiveTutor.Domain` → `EnglishLiveTutor.Shared.Domain`, `EnglishLiveTutor.SharedKernel` → `EnglishLiveTutor.Shared.Application`; `Shared.Infrastructure`, `Shared.Presentation` giữ. Giữ namespace; cập nhật reference, `.sln`, test kiến trúc, `ModuleMap`, ADR/skill |
| P6 | Test theo module: `tests/EnglishLiveTutor.ArchitectureTests`, `tests/EnglishLiveTutor.Api.IntegrationTests` (WebApplicationFactory), `tests/EnglishLiveTutor.Tests.Common` (factory, test doubles), `tests/Modules/<X>/EnglishLiveTutor.Modules.<X>.Tests` (domain, handler, infrastructure, cross-user security). Giữ toàn bộ test hành vi hiện có (chỉ di chuyển). Thêm snapshot contract API (route/method/auth/rate limit + schema JSON response) |
