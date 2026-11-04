# ADR-0006: Chuyển backend sang Modular Monolith

- Trạng thái: Accepted (người dùng đồng ý hướng đi, chọn 5 project/module, Shared 4 project, test theo module — 2026-11-01)
- Ngày: 2026-11-01
- Liên quan: review A1; task `docs/tasks/20261101-modular-monolith/`; thay thế mục 1 và 4 của `EnglishPartner.BE/.agents/architecture.md` (hình dạng 4 project, một `AppDbContext`)

## Bối cảnh

Backend có 4 project theo layer (Api, Application, Domain, Infrastructure), 12 feature slice, một `AppDbContext` với 49 `DbSet`, 21 migration trong schema `dbo`. Feature đã chia thư mục khá rõ và phụ thuộc chéo ít; 26 quan hệ FK đều nằm trong cùng một nhóm feature, không bảng nào có FK sang bảng `Users` của module khác (chỉ lưu cột `UserId`). Ranh giới hiện chỉ là quy ước: handler nào cũng đọc/ghi được mọi `DbSet` qua `IAppDbContext`, nên worker AI dễ sửa lan sang feature khác.

Phụ thuộc đo được (số file `using`) ngày 2026-11-01:

| Từ | Đến | Ghi chú |
| --- | --- | --- |
| Lessons | LessonAttempts (6), Pronunciation (8) | gộp LessonAttempts vào Lessons; Pronunciation thành module Speech |
| VocabularyGames | Lessons `SynthesizeExerciseAudio` (2), Pronunciation (1) | tổng hợp audio thuộc Speech |
| Grammar | `Domain.Lessons.Enums` (14), Vocabulary (3) | enum dùng chung (`ExerciseType`, `GradingMode`, `SubQuestionKind`) đưa vào Shared Kernel `Domain/Common/Exercises` |
| Learning ↔ Vocabulary | 5 / 2 | vòng — gộp chung một module |
| Pronunciation, Vocabulary | AiCatalog (2, 2) | |

## Quyết định

### Module

| Module | Gồm (feature hiện tại) | Phụ thuộc Contracts của |
| --- | --- | --- |
| `Identity` | Identity (user, role, permission, refresh token) | — |
| `Catalog` | AiModels/AiCatalog, Prompts (CEFR, mode, speech speed), Voices, Topics | — |
| `Speech` | Pronunciation (assessment, rubric, audio synthesis/cache) | Catalog |
| `Lessons` | Lessons, LessonAttempts, role-play, rubric, media | Speech, Catalog |
| `Grammar` | Grammar | Lessons, Speech, Catalog |
| `Vocabulary` | Vocabulary, Learning (daily plan, progress, review), VocabularyGames | Speech, Catalog |
| `LiveTutor` | Conversations, 3 SignalR hub, Gemini Live | Catalog |
| `Progress` | Tiến độ học tập tổng quan (streak, activity chart, summary) | Identity, Vocabulary, Lessons, Grammar, Speech, LiveTutor (chỉ đọc; 3 project) |
| Shared Kernel | `ICurrentUser`, CQRS wrapper, phân trang, ProblemDetails, `Entity`/`AggregateRoot`, UnitOfWork, outbox | — |

Mọi module được dùng `Identity.Contracts` (ví dụ đọc múi giờ, tên hiển thị). Đồ thị phụ thuộc phải là DAG; module mới hoặc phụ thuộc mới phải sửa bảng này. Sửa 2026-11-01 (P2): Grammar → Vocabulary bỏ (chỉ dùng helper đã chuyển vào Shared Kernel), thêm Grammar → Speech (rubric chấm); Speech không phụ thuộc Vocabulary — Speech khai báo port trong Contracts (`IPronunciationTargetTextSource`, `IPronunciationAssessmentObserver`) và Vocabulary implement. Rubric chấm (kể cả CRUD rubric bài học `/api/lesson-rubrics`) thuộc Speech; `CefrPromptBuilder`/`ICefrPromptBuilder`/`PreviewSystemPrompt` thuộc LiveTutor (đọc prompt Catalog qua `IPromptCatalogReader`); enum bài tập dùng chung thuộc Shared Kernel.

### Ranh giới

- Mỗi module có phần nội bộ (`internal` khi đã tách project) và phần `Contracts` public: interface query/service, DTO, enum dùng chung, integration event.
- Module khác chỉ được dùng `Contracts`. Không dùng entity, repository, handler hay `DbSet` của module khác.
- Đọc đồng bộ xuyên module: gọi interface trong `Contracts` (hoặc query MediatR khai báo trong `Contracts`). Không join bảng của module khác.
- Ghi xuyên module: không có transaction chung. Hiệu ứng phụ chấp nhận trễ đi qua **outbox** (bảng trong cùng DB, ghi chung transaction với dữ liệu chính, hosted service dispatch, handler idempotent). Nếu một use case buộc phải ghi nguyên tử hai module, đó là dấu hiệu ranh giới sai — sửa ranh giới, không thêm transaction chung.
- Event dùng để giảm phụ thuộc, không phải để tăng tốc. Không đưa luồng audio/transcript realtime qua event. Không gọi provider AI trong transaction, kể cả trong handler event.

### Dữ liệu

- Một database SQL Server. Mỗi module một `DbContext` riêng, schema riêng (`identity`, `catalog`, `speech`, `lessons`, `grammar`, `vocabulary`, `livetutor`, `messaging` cho outbox), bảng lịch sử `__EFMigrationsHistory` trong schema của module.
- Trước khi tách context thật, mỗi module khai báo interface `I<Module>DbContext` chỉ chứa `DbSet` của mình; `AppDbContext` hiện tại implement tất cả. Handler chuyển dần từ `IAppDbContext` sang interface của module.
- 21 migration cũ giữ nguyên làm lịch sử. Khi tách context: migration baseline `Up()` rỗng cho mỗi context, rồi migration **riêng** `ALTER SCHEMA <x> TRANSFER dbo.<bảng>` cho từng module (chỉ đổi metadata, không chép dữ liệu). Migration chuyển schema chạy trên DB thật chỉ khi người dùng xác nhận và đã có backup (luật 12).
- Truy vấn báo cáo admin cần nhiều module: `ReportingDbContext` chỉ đọc hoặc SQL view, không phá ranh giới module.

### Thứ tự

| Phase | Nội dung | Đụng DB/contract |
| --- | --- | --- |
| P0 | ADR này, luật/skill; dọn phần còn lại của A1 | Không |
| P1 | Bảng ánh xạ file → module; architecture test (quét source, không thêm package) với allowlist vi phạm hiện có; `I<Module>DbContext` | Không |
| P2 | Gỡ phụ thuộc chéo bằng `Contracts`; giảm allowlist về 0 | Không |
| P3 | Tách từng module thành project `EnglishLiveTutor.Modules.<X>` + `.Contracts`, giữ nguyên namespace, route, DTO, tên bảng; thứ tự: Catalog → Identity → Speech → Vocabulary → Lessons → Grammar → LiveTutor | Không |
| P4 | `DbContext` thật theo module, baseline migration, chuyển schema, outbox | **Có** — cần backup + người dùng xác nhận + kiểm trên SQL Server thật |

Route HTTP, field JSON, header phân trang, hub method/event không đổi trong toàn bộ refactor; FE không phải sửa.

### Cập nhật khi triển khai (2026-11-01)

- **P4 — dữ liệu:** 7 `<X>DbContext` (Infrastructure `Persistence/Modules/`), schema theo module, history `<schema>.__EFMigrationsHistory`. Mỗi module một migration `MoveToModuleSchema_<X>` chỉ gồm `EnsureSchema` + `ALTER SCHEMA … TRANSFER` (48 bảng). `AppDbContext` giữ 10 migration cũ (đóng băng, history `dbo`) để DB trống vẫn tạo bảng; thứ tự khởi động: chuỗi cũ → 7 module → seed. `IdempotencyRecords` ở `dbo`.
- **Transaction:** các context trong một request dùng chung một `SqlConnection` scoped; `UnitOfWork` commit mọi context thay đổi trong một transaction. Đây là cơ chế hạ tầng của unit of work, **không** cho phép code module ghi bảng module khác (vẫn bị chặn bởi interface + scanner + project reference).
- **Outbox: hoãn.** Chưa có use case cần hiệu ứng phụ trễ; port `IPronunciationAssessmentObserver` (Speech → Vocabulary) cần nhất quán ngay (điểm hiển thị trên flashcard) nên chạy đồng bộ trong cùng unit of work. Thêm outbox khi có use case đầu tiên chấp nhận eventual consistency.
- **P8 — bỏ `AppDbContext` (2026-11-01):** 10 migration cũ và `AppDbContext` bị xóa khỏi `src`. 7 migration `MoveToModuleSchema_<X>` giữ nguyên ID nhưng `Up()` tự chứa: chặn DB cũ chưa đủ 10 migration cũ (`THROW`) → chuyển bảng từ `dbo` nếu có → tạo bảng/index/FK nếu thiếu (SQL tĩnh, mỗi lệnh có điều kiện). `IdempotencyRecords` thuộc `SharedDbContext` (schema `shared`, migration `InitialCreate_Shared` cùng mẫu). Thứ tự khởi động: Shared → 7 module → seed. Đã kiểm trên SQL Server: DB trống, DB trước refactor (backup) và bản sao DB hiện tại cho cùng cấu trúc, số dòng không đổi. `dbo.__EFMigrationsHistory` cũ giữ lại (dùng cho kiểm phiên bản).
- **Gộp composition (2026-11-01):** project `EnglishLiveTutor.Application` và `EnglishLiveTutor.Infrastructure` (chỉ còn composition) gộp vào `EnglishLiveTutor.Api` (`Composition/Application`, `Composition/Infrastructure`, giữ namespace). Api là host + composition root duy nhất; solution còn 50 project.
- **Cạnh DAG thêm:** Grammar → Catalog (`IAiModelCatalogReader` cho sinh bài tập AI).
- **P5 — cấu trúc project (người dùng chọn 2026-11-01):** mỗi module 5 project `EnglishLiveTutor.Modules.<X>.{Domain,Application,Infrastructure,Presentation,Contracts}`; Infrastructure và endpoint/hub của module chuyển khỏi project `Infrastructure`/`Api` dùng chung. Domain module không tham chiếu EF/MediatR (compiler chặn).
- **Module Progress (2026-11-02):** Module tổng hợp tiến độ chỉ đọc, phụ thuộc Contracts của 6 module (`Identity`, `Vocabulary`, `Lessons`, `Grammar`, `Speech`, `LiveTutor`) qua interface reader scoped; ngoại lệ có 3 project (`Contracts`, `Application`, `Presentation`), không có Domain/Infrastructure/DbContext riêng do không có dữ liệu bảng độc lập.

## Hệ quả

- Ranh giới được kiểm bằng test (P1–P2) rồi bằng compiler (P3) và quyền truy cập bảng (P4). Mỗi prompt worker chỉ cần một module.
- Diff lớn, kéo dài nhiều task; trong thời gian chuyển đổi tồn tại allowlist vi phạm có hạn.
- Một request có thể mở nhiều `DbContext`; connection pooling xử lý được.
- Eventual consistency cho hiệu ứng phụ qua outbox; cần retry, idempotency và theo dõi bảng outbox.
- EF InMemory không kiểm được schema/migration; P4 cần kiểm trên SQL Server (LocalDB hoặc container) trước khi chạy DB thật.

## Việc cần làm

- [x] P0: cập nhật `AGENTS.md`, `EnglishPartner.BE/AGENTS.md`, skill `modular-monolith`, `code-reviewer`; dọn A1 còn lại.
- [x] P1: architecture test + `I<Module>DbContext`.
- [x] P2: allowlist = 0.
- [x] P3: 7 module × 2 project (P5 tách tiếp thành 5 project).
- [x] P4: DbContext/schema theo module (migration viết sẵn, người dùng tự apply; outbox hoãn).
- [x] P5: 5 project mỗi module; Shared 4 project (`Shared.{Domain,Application,Infrastructure,Presentation}`).
- [x] P6: test theo module (Architecture, Api.IntegrationTests, Tests.Common, `Modules/<X>/<X>.Tests`) + snapshot contract API.
- [x] P7: helper `AddModuleDbContext`, UnitOfWork participants, snapshot shape 7 module.
- [x] P8: bỏ `AppDbContext` + 10 migration cũ; migration module tự chứa; `SharedDbContext`.
