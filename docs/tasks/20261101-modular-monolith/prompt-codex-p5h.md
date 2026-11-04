# P5h — module LiveTutor thành 5 project

Áp dụng **đúng "Quy tắc chung cho module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` (đọc lại nếu cần) cho module **LiveTutor**:

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.LiveTutor/Domain/**` → `Modules.LiveTutor.Domain` |
| Application | đổi tên → `Modules.LiveTutor.Application` |
| Infrastructure | `Infrastructure/Realtime/**` (connection manager, options, resampler, deduplicator, null notifier), `Repositories/ConversationRepository.cs`, `Ai/{CefrPromptBuilder,GrammarCorrectionAnalyzerService,FastTextTranslatorFallback}.cs`, `ConversationConfigurations.cs`, `Persistence/Modules/LiveTutorDbContext.cs`, `Migrations/LiveTutor/**` — **giữ lifetime singleton** của connection manager/prompt builder |
| Presentation | `Api/Hubs/**` (3 hub — trừ `HubExceptionHelper` đã ở Shared.Presentation), `Api/Realtime/SignalRLiveSessionRealtimeNotifier.cs`, `Api/Endpoints/ConversationEndpoints.cs`; `Program.cs` giữ nguyên `MapHub<...>(route)` |

File Infrastructure/Api của LiveTutor không có trong bảng nhưng `ModuleMap` xếp vào LiveTutor → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.
