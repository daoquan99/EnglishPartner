# P5b — module Catalog thành 5 project (quy tắc chung cho P5b–P5h)

Theo `plan-p5.md`. P5a đã có `Shared.Infrastructure`, `Shared.Presentation` và test reference theo hậu tố.

## Quy tắc chung cho module X

1. **Domain:** tạo `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Domain/` (csproj: reference `EnglishLiveTutor.Shared.Domain` (tên cũ `EnglishLiveTutor.Domain` trước P5j) **duy nhất**, không PackageReference). Chuyển `Domain/**` từ project `EnglishLiveTutor.Modules.<X>` sang (giữ đường dẫn tương đối + namespace). Nếu file domain cần package (EF/MediatR…) → dừng, báo (vi phạm layer).
2. **Application:** đổi tên project `EnglishLiveTutor.Modules.<X>` → `EnglishLiveTutor.Modules.<X>.Application` (thư mục + csproj + `AssemblyName`/`RootNamespace` nếu có; cập nhật mọi `ProjectReference`, `.sln`, `ApplicationAssemblies.All`, `InternalsVisibleTo`). Reference thêm `<X>.Domain`. Namespace file **không đổi**.
3. **Infrastructure:** tạo `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure/` (reference `<X>.Application`, `<X>.Domain`, `<X>.Contracts`, `Shared.Infrastructure`, `Shared.Application` (tên cũ `SharedKernel` trước P5j), Contracts trong DAG; package như file chuyển vào cần, cùng version). Chuyển từ `src/EnglishLiveTutor.Infrastructure/` mọi file thuộc module X theo `ModuleMap` (configurations, repositories, adapter AI/storage/worker/options của X, `Persistence/Modules/<X>DbContext.cs`, `Persistence/Modules/Migrations/<X>/**`), giữ đường dẫn tương đối + namespace. Tạo `<X>InfrastructureServiceCollectionExtensions.Add<X>Infrastructure(this IServiceCollection, IConfiguration?)` chứa **nguyên văn** các đăng ký của module X trong `Infrastructure/DependencyInjection.cs` (DbContext/factory của X, repository, adapter, options, HttpClient) — giữ lifetime + thứ tự tương đối; `Infrastructure/DependencyInjection.cs` gọi extension đúng vị trí cũ. `MigrationsAssembly` của `<X>DbContext` → assembly mới. `AppDbContext.OnModelCreating` apply configuration của assembly X (model không đổi — guard test). `Infrastructure` (composition) reference project mới. `InternalsVisibleTo` tests nếu cần.
4. **Presentation:** tạo `src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Presentation/` (`FrameworkReference Microsoft.AspNetCore.App`; reference `<X>.Application`, `<X>.Contracts`, `Shared.Presentation`, `Shared.Application`). Chuyển file endpoint (và hub/realtime nếu là LiveTutor) của module X từ `src/EnglishLiveTutor.Api/` (giữ đường dẫn + namespace). `Api` reference project mới; `Program.cs` giữ nguyên lời gọi `Map…Endpoints()`/`MapHub`.
5. Không đổi route, policy, rate limit, filter, JSON, migration ID, DB. Không thêm package mới.
6. Kiểm chứng: build 0 lỗi 0 warning; toàn bộ test pass; `has-pending-model-changes` cho `<X>DbContext` = không đổi; `dotnet ef migrations list --context <X>DbContext --no-build` liệt kê đúng migration cũ (ID không đổi); allowlist 0; `ProjectReferenceTests` pass với 5 project của X. Report: file chuyển theo layer, đăng ký DI chuyển, reference thêm, kết quả lệnh.

## Gói P5b — Catalog

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Catalog/Domain/**` → `Modules.Catalog.Domain/Domain/**` |
| Application | đổi tên `Modules.Catalog` → `Modules.Catalog.Application` |
| Infrastructure | từ `EnglishLiveTutor.Infrastructure`: `Repositories/{AiModelCatalog,PromptConfiguration,SpeechSpeed,TeacherVoice,Topic}Repository.cs`, `Prompts/**` (nếu còn), configurations `AiModelConfigConfiguration`, `PromptConfigurations`, `TeacherVoiceConfiguration`, `TopicConfiguration`, `Persistence/Modules/CatalogDbContext.cs`, `Persistence/Modules/Migrations/Catalog/**` |
| Presentation | từ `EnglishLiveTutor.Api/Endpoints`: `AiModelEndpoints.cs`, `PromptEndpoints.cs`, `VoiceEndpoints.cs`, `TopicEndpoints.cs` |
