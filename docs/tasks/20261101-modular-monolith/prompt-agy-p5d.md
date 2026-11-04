# Việc giao cho AGY: P5d — module Speech thành 5 project

## Bối cảnh (session mới — đọc trước khi sửa)

- Luật: `AGENTS.md` (gốc; luật 13–15), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md` (mục "Cập nhật khi triển khai"), skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` (mục "Project layout" + "Module map").
- Thiết kế: `docs/tasks/20261101-modular-monolith/plan-p5.md`. **Quy tắc chung** cho mỗi module: `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` mục "Quy tắc chung cho module X" (làm đúng 6 bước).
- **Mẫu đã làm xong: module Catalog** — xem `EnglishPartner.BE/src/Modules/Catalog/*` (5 csproj, `CatalogInfrastructureServiceCollectionExtensions.cs`, `CatalogModuleServiceCollectionExtensions.cs`), cách `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs` gọi `AddCatalogInfrastructure`, cách `AppDbContext` apply configuration từ assembly Catalog, cách Api reference `Catalog.Presentation`. Làm module Speech **theo đúng khuôn đó**.
- Phần dùng chung đã ở `EnglishPartner.BE/src/Shared/`: `EnglishLiveTutor.Shared.Domain`, `EnglishLiveTutor.Shared.Application`, `EnglishLiveTutor.Shared.Infrastructure`, `EnglishLiveTutor.Shared.Presentation` (không còn `EnglishLiveTutor.Domain`/`SharedKernel`).
- Trạng thái trước gói: build 0 lỗi; `dotnet test EnglishLiveTutor.sln` toàn bộ pass (641 trước P5c; gói sau cộng thêm test); allowlist kiến trúc 0. Làm việc trên worktree riêng của module này (chạy song song với các module khác; không tạo branch, không commit — orchestrator kiểm chứng rồi merge vào `development`). Chỉ sửa file của module này và file composition bắt buộc.
- Không có mạng: dùng `dotnet build --no-restore`; nếu project mới cần restore, chạy `dotnet restore EnglishLiveTutor.sln --ignore-failed-sources` (package có sẵn trong cache).

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Speech/Domain/**` → `Modules.Speech.Domain` |
| Application | đổi tên → `Modules.Speech.Application` |
| Infrastructure | `Repositories/{AudioRecording,GeneratedPronunciationAudio,PronunciationAssessment,ScoringRubric}Repository.cs`, `Ai/{GeminiLivePronunciationTtsSynthesizer,GeminiPronunciationAiAssessor,PronunciationSynthesisLock}.cs`, `PronunciationConfigurations.cs`, `Persistence/LessonRubricSeeder.cs` (nếu seeder composition gọi nó, giữ lời gọi), `Persistence/Modules/SpeechDbContext.cs`, `Migrations/Speech/**` |
| Presentation | `Api/Endpoints/PronunciationEndpoints.cs`, `Api/Endpoints/LessonRubricEndpoints.cs` |

File Infrastructure/Api của Speech không có trong bảng nhưng `ModuleMap` xếp vào Speech → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.

## Kiểm chứng (bắt buộc, trong EnglishPartner.BE)

```bash
dotnet restore EnglishLiveTutor.sln --ignore-failed-sources
dotnet build EnglishLiveTutor.sln --no-restore
dotnet test EnglishLiveTutor.sln --no-build
dotnet ef migrations has-pending-model-changes --context <X>DbContext --project src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
dotnet ef migrations list --context <X>DbContext --project src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build --no-connect
```

Build lỗi hoặc test fail → sửa trong phạm vi module này; **không** sửa test để cho pass (trừ đường dẫn/tham chiếu kiểu do di chuyển), **không** đổi logic, namespace, route, migration ID. Không tự xử lý được → dừng và ghi rõ trong report.

## Ràng buộc

Không commit/push/merge, không tạo branch, không thêm package mới, không chạy `database update`, không in secret. Chỉ sửa file của module này + file composition bắt buộc (`.sln`, csproj của Application/Infrastructure/Api, `Infrastructure/DependencyInjection.cs`, `AppDbContext.OnModelCreating`, `Application/ApplicationAssemblies.cs`).

## Worker report (bắt buộc ở cuối)

```
### Worker report
- Project mới/đổi tên: <danh sách>
- File chuyển theo layer: Domain <n>, Infrastructure <n>, Presentation <n> (+ danh sách file không có trong bảng nhưng thuộc module)
- Đăng ký DI chuyển vào Add<X>Infrastructure: <danh sách>
- Lệnh đã chạy và kết quả: <lệnh → kết quả, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
