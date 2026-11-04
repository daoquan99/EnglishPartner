# Việc giao cho Codex: P3a — project SharedKernel và composition nhiều assembly

## Bối cảnh (session mới — đọc kỹ)

- `AGENTS.md` (gốc, luật 13–15), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`.
- Thiết kế P3: `docs/tasks/20261101-modular-monolith/plan-p3.md` (đọc toàn bộ). Gói này là **P3a**.
- Trạng thái: P1, P2 xong — allowlist kiến trúc 0 vi phạm; build 0 lỗi; `dotnet test` 607/607; `has-pending-model-changes` = không đổi. Không có mạng: dùng `--no-restore` (package đã có trong cache cục bộ; không thêm package mới).
- Sandbox không ghi được Git index: dùng di chuyển file thường (Move-Item / mv), orchestrator sẽ stage.

## Yêu cầu

1. Tạo `src/EnglishLiveTutor.SharedKernel/EnglishLiveTutor.SharedKernel.csproj` (net10.0, cùng thuộc tính `Nullable`/`ImplicitUsings`/`LangVersion` như `EnglishLiveTutor.Application.csproj`; package MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions, Microsoft.EntityFrameworkCore cùng version Application đang dùng — chỉ những package SharedKernel thật cần; `ProjectReference` Domain). Thêm vào `EnglishLiveTutor.sln` (`dotnet sln add`).
2. Chuyển sang SharedKernel, **giữ đường dẫn tương đối và namespace**: `src/EnglishLiveTutor.Application/Common/**` → `src/EnglishLiveTutor.SharedKernel/Common/**`, `src/EnglishLiveTutor.Application/Modules/IModuleDbContext.cs`, `IModuleDbContextFactory.cs` → `src/EnglishLiveTutor.SharedKernel/Modules/`. **Ngoại lệ** (ở lại Application): file nào tham chiếu type nằm ngoài `Domain/Common` và ngoài SharedKernel (ví dụ DTO dùng entity của module, `IAppDbContext`, `ICefrPromptBuilder` dùng `ChatMessageEntity`…) — xác định bằng cách build; ghi danh sách vào report.
3. `IAppDbContext.cs` (`IAppDbContext`, `IAppDbContextFactory`) chuyển sang `src/EnglishLiveTutor.Infrastructure/Persistence/IAppDbContext.cs`, **giữ namespace** `EnglishLiveTutor.Application.Common.Interfaces`.
4. `EnglishLiveTutor.Application.csproj` thêm `ProjectReference` SharedKernel. Mọi project/test build được (thêm reference nếu compiler đòi, ghi lại).
5. **Composition nhiều assembly** trong `src/EnglishLiveTutor.Application/DependencyInjection.cs`: thay `var assembly = typeof(DependencyInjection).Assembly;` bằng danh sách assembly ứng dụng (hiện: Application + SharedKernel; các gói sau sẽ thêm assembly module). Tạo `public static class ApplicationAssemblies { public static IReadOnlyList<Assembly> All { get; } }` trong Application; `AddValidatorsFromAssemblies(All)`, MediatR `RegisterServicesFromAssemblies(All)`, mọi chỗ khác dùng `assembly` cũng quét `All`. Pipeline behaviors giữ nguyên thứ tự đăng ký.
6. Test: `CqrsDependencyInjectionTests` (và test reflection khác quét `typeof(...Application...).Assembly`) chuyển sang `ApplicationAssemblies.All` — số request/handler được kiểm **không giảm** (ghi số trước/sau). Thêm `tests/.../Architecture/ProjectReferenceTests.cs`: đọc csproj, kiểm SharedKernel chỉ reference Domain; Domain không reference gì.
7. `ModuleMap.cs`: path `EnglishLiveTutor.SharedKernel/` → Shared (file module-specific trong SharedKernel nếu còn — không được còn; nếu scanner báo thì đó là lỗi bước 2). Thêm luật path `Modules/<X>/` → module X (dùng cho P3b+; kiểm bằng test đơn vị `ModuleOfFile("Modules/Lessons/EnglishLiveTutor.Modules.Lessons/Domain/Lessons/LessonEntity.cs") == Lessons`). Đường dẫn file Infrastructure/Api cũ giữ nguyên. Allowlist phải vẫn 0.

## Không được

Đổi namespace, đổi logic, sửa migration/snapshot, đổi route/DTO, thêm package mới, commit.

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet build EnglishLiveTutor.sln --no-restore
dotnet test EnglishLiveTutor.sln --no-build
dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~Architecture"
dotnet ef migrations has-pending-model-changes --project src/EnglishLiveTutor.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
```

## Worker report (bắt buộc)

```
### Worker report
- Project mới / reference đã thêm: <danh sách>
- File chuyển sang SharedKernel: <số lượng + thư mục>
- File Common ở lại Application (lý do): <danh sách>
- Số request/handler CqrsDependencyInjectionTests trước → sau: <n → n>
- Lệnh đã chạy và kết quả: <lệnh → kết quả, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
