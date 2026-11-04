# Việc giao cho Codex: P5a — project Shared.Infrastructure, Shared.Presentation + test kiến trúc 5 layer

## Bối cảnh (session mới — đọc kỹ)

- `AGENTS.md` (gốc; luật 13–15), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md` (mục "Cập nhật khi triển khai"), skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`.
- Thiết kế P5: `docs/tasks/20261101-modular-monolith/plan-p5.md` (đọc toàn bộ). Gói này là **P5a**.
- Trạng thái: P1–P4 xong. 7 module × 2 project (`Modules.<X>`, `Modules.<X>.Contracts`); Infrastructure/endpoint của module vẫn ở `EnglishLiveTutor.Infrastructure`/`EnglishLiveTutor.Api`; 7 `<X>DbContext` + migration ở `Infrastructure/Persistence/Modules/`. Build 0 lỗi 0 warning; `dotnet test` 636/636; allowlist 0. Không mạng: `--no-restore`. Sandbox không ghi Git index — di chuyển file thường.

## Yêu cầu

1. Tạo `src/EnglishLiveTutor.Shared.Infrastructure/EnglishLiveTutor.Shared.Infrastructure.csproj` (net10.0, thuộc tính chung; package chỉ những gì file chuyển vào cần, cùng version với Infrastructure; reference SharedKernel, Domain). Chuyển vào (giữ đường dẫn tương đối dưới gốc project và **namespace**) các file Infrastructure thuộc **Shared** theo `ModuleMap` mà module Infrastructure sẽ cần dùng: tối thiểu `Persistence/Interceptors/**`, `Persistence/ModuleSchemas.cs`, `Persistence/Configurations/EntityConfigurationExtensions.cs`, `Ai/{GeminiHttpHelper,GeminiApiKeyResolver,AgyCliRunner,AntigravityCliOptions,GeminiLiveSessionBridge}.cs` (nếu tồn tại; file nào phụ thuộc type của module → để lại, ghi report). **Không** chuyển phần composition: `AppDbContext`, `IAppDbContext`, `UnitOfWork`, `DatabaseSeeder`, `DatabaseMigrationRunner`, `IdempotencyService`, `Persistence/Entities/**`, `IdempotencyConfigurations`, `DependencyInjection.cs`.
2. Tạo `src/EnglishLiveTutor.Shared.Presentation/EnglishLiveTutor.Shared.Presentation.csproj` (`<FrameworkReference Include="Microsoft.AspNetCore.App" />`, reference SharedKernel). Chuyển vào các helper mà file endpoint/hub dùng chung: extension `RequirePermission`/endpoint filter, `Endpoints/PaginationEndpointExtensions.cs`, `Hubs/HubExceptionHelper.cs`, và helper khác mà ≥2 file endpoint của module khác nhau dùng (tìm bằng compiler/grep; liệt kê). Phần host (auth/JWT setup, CORS, rate limiter registration, global exception handler, `HttpContextCurrentUser`, `Program.cs`, `Configuration/**`) **ở lại Api**.
3. `EnglishLiveTutor.Infrastructure` reference `Shared.Infrastructure`; `EnglishLiveTutor.Api` reference `Shared.Presentation`. `dotnet sln add` (nếu CLI lỗi solution folder như trước, sửa `.sln` thủ công và kiểm `dotnet sln list`).
4. Test kiến trúc cho cấu trúc 5 layer (chuẩn bị cho P5b+; phải pass cả với cấu trúc hiện tại):
   - `ModuleMap`: `src/EnglishLiveTutor.Shared.Infrastructure/`, `src/EnglishLiveTutor.Shared.Presentation/` → Shared.
   - `ProjectReferenceTests`: rule theo **hậu tố** tên project dưới `src/Modules/<X>/`: `.Domain` → chỉ `EnglishLiveTutor.Domain`, không PackageReference; `.Contracts` → SharedKernel, Domain, Contracts module trong DAG; `.Application` (và tên không hậu tố `EnglishLiveTutor.Modules.<X>` của P3, coi như Application cho đến khi đổi tên) → `<X>.Domain` (nếu có), `<X>.Contracts`, SharedKernel, Domain, Contracts trong DAG; `.Infrastructure` → `<X>.Application`, `<X>.Domain`, `<X>.Contracts`, Shared.Infrastructure, SharedKernel, Domain, Contracts trong DAG; `.Presentation` → `<X>.Application`, `<X>.Contracts`, Shared.Presentation, SharedKernel, Domain. Test hiện có cho cấu trúc P3 cập nhật tương ứng (không giảm độ chặt).
   - Shared.Infrastructure/Shared.Presentation không reference project module nào.
5. Kiểm chứng: build 0 lỗi 0 warning; toàn bộ test pass; `has-pending-model-changes` cho 7 module context = không đổi; allowlist 0.

## Không được

Đổi namespace, logic, route, migration, DB; thêm package mới; commit.

## Worker report

File chuyển (theo project), file Shared để lại (lý do), reference thêm, rule test mới, lệnh + số test.
