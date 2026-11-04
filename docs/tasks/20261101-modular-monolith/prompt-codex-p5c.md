# P5c — module Identity thành 5 project

Áp dụng **đúng "Quy tắc chung cho module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` (đọc lại nếu cần) cho module **Identity**:

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Identity/Domain/**` → `Modules.Identity.Domain` |
| Application | đổi tên → `Modules.Identity.Application` |
| Infrastructure | `Infrastructure/Identity/**` (JwtOptions, validators, seed options, `SecurityServices.cs`), `Repositories/IdentityRepositories.cs`, `IdentityConfigurations.cs`, `Persistence/Modules/IdentityDbContext.cs`, `Migrations/Identity/**` |
| Presentation | `Api/Endpoints/IdentityEndpoints.cs`, `Api/Services/ClaimsPermissionChecker.cs` (nếu chỉ phục vụ Identity; nếu là hạ tầng auth host → giữ ở Api, ghi report) |

File Infrastructure/Api của Identity không có trong bảng nhưng `ModuleMap` xếp vào Identity → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.
