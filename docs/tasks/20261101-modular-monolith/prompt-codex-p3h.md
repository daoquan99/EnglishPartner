# P3h — tách module LiveTutor thành project

Áp dụng **đúng "Quy tắc chung tách một module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p3b.md` (đọc lại nếu cần) cho module **LiveTutor**:

| Nguồn | Đích |
| --- | --- |
| `Domain/Conversations` | `Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor/Domain/Conversations` |
| `Features/Conversations` (trừ `Contracts/`) | `.../Modules.LiveTutor/Features/Conversations` |
| `Features/Conversations/Contracts` (nếu có) | `Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Contracts/Features/Conversations/Contracts` |
| `Application/Modules/LiveTutor/ILiveTutorDbContext.cs` | `.../Modules.LiveTutor/Modules/LiveTutor/` |
| Common: `Models/LiveSessionDtos.cs`, `Interfaces/ILiveSessionRealtimeNotifier.cs`, `Interfaces/ICefrPromptBuilder.cs` | module project (Contracts nếu module khác dùng) |

Kiểm chứng và report như P3b.

## Dọn cuối P3

- `src/EnglishLiveTutor.Domain/` chỉ còn `Common/**` (và csproj). `src/EnglishLiveTutor.Application/` chỉ còn composition (`DependencyInjection.cs`, `ApplicationAssemblies`, behaviors nếu chưa ở SharedKernel) — liệt kê file còn lại; file module-specific nào còn sót → chuyển đúng module.
- `ProjectReferenceTests`: đủ 7 module × 2 project theo bảng `plan-p3.md`.
- Không còn thư mục rỗng; `dotnet sln list` liệt kê đủ project.
