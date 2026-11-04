# P5i — dọn cuối P5

1. `src/EnglishLiveTutor.Infrastructure/` chỉ còn composition: `AppDbContext`, `IAppDbContext`, `UnitOfWork`, `DatabaseSeeder`, `DatabaseMigrationRunner`, `IdempotencyService`, `Persistence/Entities/**`, `IdempotencyConfigurations`, 10 migration cũ + snapshot, `DependencyInjection.cs`. Liệt kê file còn lại khác và chuyển đúng module/Shared nếu thuộc module.
2. `src/EnglishLiveTutor.Api/` chỉ còn host: `Program.cs`, `Configuration/**`, auth/JWT/CORS/rate limit/exception handler, `HttpContextCurrentUser`, appsettings. Liệt kê và chuyển phần còn sót.
3. `ModuleMap.cs`: bỏ các rule path cũ không còn file (Infrastructure/Api theo module) — rule chung `src/Modules/<X>/` bao phủ; test "mọi file được phân loại" vẫn pass.
4. Test `ProjectReferenceTests`: đủ 7 × 5 project, mọi `.Domain` không có PackageReference.
5. `dotnet sln list` đủ project; không còn thư mục rỗng; build 0 warning; toàn bộ test pass; `has-pending-model-changes` 7 context không đổi; `migrations list` 8 context đúng ID.
6. Theo rà soát Catalog: chuyển `BuildSystemPromptPreviewRequest` sang LiveTutor (giữ namespace, JSON); bỏ `PackageReference` không dùng (đặc biệt `System.Security.Cryptography.Pkcs`) ở mọi project module; chuyển `PromptCatalogEntityDefaults` sang `Catalog.Infrastructure`; bỏ `ProjectReference` `Identity.Contracts` ở project không dùng; mỗi module Presentation có `Map<X>Endpoints(this IEndpointRouteBuilder)` gọi các `Map…Endpoints()` hiện có, `Program.cs` gọi 7 hàm gom (giữ thứ tự map route cũ); dọn `using` thừa ở Presentation.
