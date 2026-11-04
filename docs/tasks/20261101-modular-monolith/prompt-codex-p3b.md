# P3b — tách module Catalog thành project (quy tắc chung cho P3b–P3h)

Theo `plan-p3.md`. P3a đã tạo SharedKernel và `ApplicationAssemblies.All`.

## Quy tắc chung tách một module X (các gói sau chỉ nêu bảng file)

1. Tạo `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Contracts/EnglishLiveTutor.Modules.<X>.Contracts.csproj` và `src/Modules/<X>/EnglishLiveTutor.Modules.<X>/EnglishLiveTutor.Modules.<X>.csproj` (net10.0, cùng thuộc tính chung như SharedKernel; chỉ package thật cần, cùng version; `dotnet sln add`). `ProjectReference` đúng bảng trong `plan-p3.md`: Contracts → SharedKernel (+ Domain); module → SharedKernel, Contracts của nó, Contracts của module trong DAG (`ModuleMap.AllowedDependencies` + Identity.Contracts). **Không** reference project module khác hoặc Application/Infrastructure/Api.
2. Di chuyển file, **giữ đường dẫn tương đối dưới gốc project và namespace**:
   - `src/EnglishLiveTutor.Domain/<Area>/**` → `<module project>/Domain/<Area>/**`
   - `src/EnglishLiveTutor.Application/Features/<F>/**` (trừ `Contracts/`) → `<module project>/Features/<F>/**`
   - `src/EnglishLiveTutor.Application/Features/<F>/Contracts/**` → `<Contracts project>/Features/<F>/Contracts/**`
   - `src/EnglishLiveTutor.Application/Modules/<X>/**` → `<module project>/Modules/<X>/**`
   - File module-specific còn trong `Application/Common` hoặc `SharedKernel/Common` của module X (xem bảng trong skill + danh sách P3a) → module project (hoặc Contracts project nếu module khác dùng), giữ namespace.
   - Nếu một type trong module project được module khác dùng (compiler báo) → chuyển type đó sang Contracts project của X nếu nó là DTO/interface/enum thuần; nếu là entity/handler → **dừng và báo** (vi phạm ranh giới chưa được scanner bắt).
3. DI: tạo `<module project>/<X>ModuleServiceCollectionExtensions.cs` với `Add<X>Module(this IServiceCollection)` chứa các đăng ký service của module đang nằm trong `Application/DependencyInjection.cs` (chuyển nguyên văn, giữ lifetime và thứ tự tương đối); Application gọi `Add<X>Module()` đúng vị trí cũ. Thêm assembly module + Contracts vào `ApplicationAssemblies.All`. Application thêm `ProjectReference` tới hai project mới; Infrastructure thêm reference nếu compiler cần.
4. File Infrastructure/Api của module **không** di chuyển trong P3.
5. Architecture: `ModuleMap` đã map `src/Modules/<X>/` (P3a); cập nhật map path cũ nếu cần để file không bị "unclassified". Thêm module X vào `ProjectReferenceTests` (kiểm reference đúng bảng).
6. Kiểm chứng: build `--no-restore` 0 lỗi; `dotnet test --no-build` cùng số pass, 0 fail; `CqrsDependencyInjectionTests` số request không đổi; `has-pending-model-changes` không đổi; allowlist 0. Report: số file di chuyển theo nhóm, type phải chuyển sang Contracts, reference thêm.

## Gói P3b — Catalog

| Nguồn | Đích |
| --- | --- |
| `Domain/AiCatalog`, `Domain/Prompts`, `Domain/Voices`, `Domain/Topics` | `Modules/Catalog/EnglishLiveTutor.Modules.Catalog/Domain/...` |
| `Features/AiModels`, `Features/Prompts`, `Features/Voices`, `Features/Topics` (trừ `Contracts/`) | `.../Modules.Catalog/Features/...` |
| `Features/AiModels/Contracts`, `Features/Prompts/Contracts` (+ Contracts khác của 4 feature nếu có) | `Modules/Catalog/EnglishLiveTutor.Modules.Catalog.Contracts/Features/...` |
| `Application/Modules/Catalog/ICatalogDbContext.cs` | `.../Modules.Catalog/Modules/Catalog/` |
| Common: `Models/PromptAndVoiceDtos.cs`, `Models/TopicDtos.cs` (+ phần Catalog của file mixed nếu tách được) | module project (hoặc Contracts nếu module khác dùng) |

Lưu ý: entity Catalog (`AiModelConfigEntity`…) đang được Infrastructure (configurations, repositories, seeder) dùng → Infrastructure reference module project là hợp lệ. Endpoint `AiModelEndpoints` trả entity → Api reference module project (hợp lệ, Api là host).
