# Việc giao cho AGY: P5i — dọn cuối sau khi 7 module thành 5 project

## Bối cảnh (session mới)

- Luật: `AGENTS.md` (luật 13–15), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` (mục "Project layout").
- Trạng thái (nhánh `development`, commit `1ea208b`): 7 module đều có 5 project `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.{Domain,Application,Infrastructure,Presentation,Contracts}`; Shared ở `src/Shared/`; `src/EnglishLiveTutor.Infrastructure` chỉ còn composition (`AppDbContext`, `UnitOfWork`, seeder, migration runner, idempotency, 10 migration cũ, `DependencyInjection.cs`); `src/EnglishLiveTutor.Api` chỉ còn host. Build 0 lỗi; `dotnet test` 659/659; allowlist kiến trúc 0. Không mạng: `--no-restore` (hoặc `dotnet restore --ignore-failed-sources`).
- Làm trực tiếp trên `development`; không commit (orchestrator commit sau khi kiểm chứng).

## Yêu cầu

1. **Package thừa:** bỏ `<PackageReference Include="System.Security.Cryptography.Pkcs" …/>` khỏi **7 project `.Infrastructure` của module** (không có code nào dùng). **Giữ** ở `src/EnglishLiveTutor.Infrastructure/EnglishLiveTutor.Infrastructure.csproj` (pin phiên bản transitive cho app). Rà mọi csproj dưới `src/Modules/**` và `src/Shared/**`: bỏ `PackageReference` không có code dùng (kiểm bằng build sau khi bỏ; nếu bỏ mà build lỗi thì giữ lại). Liệt kê package đã bỏ.
2. **`BuildSystemPromptPreviewRequest`** (`src/Modules/Catalog/EnglishLiveTutor.Modules.Catalog.Contracts/Features/Prompts/Contracts/BuildSystemPromptPreviewRequest.cs`): chỉ LiveTutor dùng → chuyển sang `src/Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Application/` (cùng thư mục với query `PreviewSystemPrompt`, **giữ namespace** và nội dung → JSON không đổi). Sửa reference nếu cần.
3. **`PromptCatalogEntityDefaults`** (`src/Modules/Catalog/EnglishLiveTutor.Modules.Catalog.Application/Features/Prompts/Services/PromptCatalogEntityDefaults.cs`, dữ liệu seed dạng entity) → `EnglishLiveTutor.Modules.Catalog.Infrastructure` (giữ namespace). Nếu Application còn dùng nó ở đâu → dừng, báo.
4. **Reference không dùng:** bỏ `ProjectReference` tới `EnglishLiveTutor.Modules.Identity.Contracts` ở project nào không dùng type nào của nó (kiểm bằng build). Bỏ `using` không dùng trong các file `Presentation` (đặc biệt `using EnglishLiveTutor.Domain.*` thừa).
5. **Hàm gom endpoint:** mỗi project `.Presentation` có `public static class <X>EndpointRouteBuilderExtensions { public static IEndpointRouteBuilder Map<X>Endpoints(this IEndpointRouteBuilder app) }` gọi các `Map…Endpoints()` hiện có của module đó (LiveTutor: kèm `MapHub` nếu hiện đang map hub ở `Program.cs` — chỉ chuyển nếu giữ **đúng route, options, thứ tự**; nếu không chắc thì để `MapHub` ở `Program.cs`). `Program.cs` gọi 7 hàm gom **theo đúng thứ tự map hiện tại** (nếu thứ tự hiện tại xen kẽ giữa các module, giữ nguyên lời gọi rời và ghi report — không đổi thứ tự route).
6. **Test kiến trúc:** `ModuleMap.cs` bỏ rule path cũ không còn file (Infrastructure/Api/Application theo module) — rule chung `Modules/<X>/` và `Shared/` bao phủ; test "mọi file được phân loại" vẫn pass. `ProjectReferenceTests`: có kiểm đủ 7 × 5 project; mọi `.Domain` không có `PackageReference`.
7. Không đổi namespace, logic, route, JSON, migration, DB.

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet restore EnglishLiveTutor.sln --ignore-failed-sources
dotnet build EnglishLiveTutor.sln --no-restore
dotnet test EnglishLiveTutor.sln --no-build
```

## Worker report

```
### Worker report
- Package bỏ (project → package): …
- File chuyển: …
- Reference/using bỏ: …
- Map<X>Endpoints: module nào có, Program.cs trước/sau (thứ tự)
- Test kiến trúc đã sửa: …
- Lệnh đã chạy và kết quả: …
- Chưa làm / cần quyết định: …
```
