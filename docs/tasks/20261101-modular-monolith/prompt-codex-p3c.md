# P3c — tách module Identity thành project

Áp dụng **đúng "Quy tắc chung tách một module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p3b.md` (đọc lại nếu cần) cho module **Identity**:

| Nguồn | Đích |
| --- | --- |
| `Domain/Identity` | `Modules/Identity/EnglishLiveTutor.Modules.Identity/Domain/Identity` |
| `Features/Identity` (trừ `Contracts/`) | `.../Modules.Identity/Features/Identity` |
| `Features/Identity/Contracts` | `Modules/Identity/EnglishLiveTutor.Modules.Identity.Contracts/Features/Identity/Contracts` |
| `Application/Modules/Identity/IIdentityDbContext.cs` | `.../Modules.Identity/Modules/Identity/` |
| Common: `Models/IdentityDtos.cs`, phần Identity của `Models/AdminCrudDtos.cs`/`Interfaces/IIdentityServices.cs` nếu chỉ Identity dùng | module project (Contracts nếu module khác dùng) |

Kiểm chứng và report như P3b.
