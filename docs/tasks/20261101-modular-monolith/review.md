# Review — 20261101-modular-monolith

## P1a — architecture test (code-reviewer)

Kết luận: **ĐẠT CÓ ĐIỀU KIỆN** → giao `prompt-codex-p1a-fix-1.md`.

- Trung bình: `contracts` bỏ sót entity cùng module, báo nhầm `Domain.Common`.
- Trung bình: `MODULE_ALLOWLIST_WRITE=1` ghi đè toàn bộ allowlist → có thể lén thêm vi phạm.
- Thấp: thiếu test cho global/static/alias using, tên đầy đủ, comment, handler trong Contracts, `ModuleOfFile` Infrastructure/Api; assertion yếu; file Infrastructure/Api mới chưa map sẽ bị bỏ qua; không kiểm comment đầu allowlist.
- Giới hạn ghi nhận cho P2: tham chiếu tới file module-specific trong `Application/Common` (namespace chung) và giữa namespace `Infrastructure.*` chưa được scanner bắt; `/* */` không bị bỏ qua.

## P1a fix 1 — ĐẠT (orchestrator kiểm lại hai lỗi trung bình: contracts báo cả cùng module, chế độ ghi chỉ shrink); 560/6 → sau sửa `DataLength` 566/0.

## P1b — interface DbContext theo module

Orchestrator tự review (diff ~90 dòng): 7 interface đủ 48 `DbSet`, DI cùng instance scoped, factory generic singleton trên `IDbContextFactory<AppDbContext>`; build 0 lỗi, 570/570, `has-pending-model-changes` = không đổi. ĐẠT. `code-reviewer` sẽ review gộp toàn bộ P1 sau P1c.

## P1c-1 — Catalog + Identity: ĐẠT (orchestrator): 20 file chỉ đổi kiểu/using; 2 test chỉ wiring + adapter; allowlist 142→121 (chỉ xóa); 570/570. Worker sửa thêm lỗi map `Application.Modules.Catalog` của P1b trong `ModuleMap.cs`.

## P1c-2 — Speech + Vocabulary: ĐẠT (orchestrator): 10 file chuyển, 5 test chỉ wiring; allowlist 121→112; 570/570. File giữ `IAppDbContext` (đầu vào P2):
- Identity `Users`: `UpdateLearningSettingsCommandHandler`, `GetLearningSettingsQueryHandler`, `DailyVocabularyPlanInitializer`
- Speech `PronunciationAssessments`: `VocabularyProjectionHelper` (và các caller `SubmitVocabularyReview`, `GetDailyVocabularyPlan`, `GetVocabularyFlashcard`, `VocabularyGenerationWriter`), `GetLearningProgressQueryHandler`
- Catalog `AiModelCatalog`: `AssessPronunciationCommandHandler`, `GetPronunciationModelsQueryHandler`, `GenerateVocabularyCommandHandler`, `GetVocabularyGenerationModelsQueryHandler`
- Speech → Vocabulary (`Vocabularies`, `VocabularySenses`, `VocabularyExamples`, `UserVocabularyProgresses`): `AssessPronunciationCommandHandler`, `PronunciationSynthesisResolution` — **ngược DAG** (Speech không được phụ thuộc Vocabulary) → P2 phải đảo hướng: Vocabulary cung cấp dữ liệu qua tham số/Contracts do caller truyền vào, hoặc chuyển logic phụ thuộc Vocabulary sang module Vocabulary.

## P1c-3 — Lessons: ĐẠT (orchestrator): 11 file chuyển, không sửa test; allowlist 112→101; 570/570. Giữ `IAppDbContext` (P2): `RetryAnswerGradingCommand`, `SubmitExerciseAnswerCommand`, `RubricCommands`, `RubricQueries`, `LessonRubricDetailBuilder`, `LessonCandidateValidator` → Speech `ScoringRubrics`; `GenerateLessonExercisesCommand` → Catalog `AiModelCatalog`.

## P1c-4 — Grammar: ĐẠT (orchestrator): 27 file; allowlist 101→74 (appdb 25, dag 5, internal 43); 570/570. Giữ: `SubmitGrammarAnswerCommand` (Speech `ScoringRubrics`), `ListGrammarModerationQueueQueryHandler`, `ListGrammarExercisesQuery` (Identity `Users`).

## P1 gộp — code-reviewer: **ĐẠT**

Chỉ di chuyển (50 field + 14 factory đổi kiểu, không đổi LINQ/thứ tự/message), DI cùng instance scoped (test `Assert.Same` + `ValidateScopes`), `AudioData.Length` → `CAST(DATALENGTH(...) AS int)` trên SQL Server, không né test bằng ép kiểu, `CqrsDependencyInjectionTests` resolve mọi handler.
Thấp (gộp vào P2b): `ModuleDbContextFactory<T>` `(TDb)(object)context` không dispose khi cast lỗi → kiểm `is TDb`, dispose rồi throw; generic mở cho phép `IModuleDbContextFactory<IAppDbContext>` (scanner vẫn bắt; P3 cân nhắc đăng ký đóng); thứ tự `using` lệch alphabet ở vài test.
Chưa kiểm chứng: SQL thật sinh ra cho `AudioData.Length` (InMemory).

## P2a — D1–D4: ĐẠT (orchestrator): 576/576, has-pending-model-changes không đổi; 2 route giữ nguyên prefix + `RequireAuthorization()` (không có rate limit gốc); `AiModelCatalogReader` cùng query; allowlist 74→45 (internal 43→19). Orchestrator tự sửa ADR/skill DAG (worker không có quyền ghi ngoài BE).

## P2b — D5, D6 + factory: ĐẠT (orchestrator): 587/587; `IUserDirectory`, `IPronunciationScoreReader`; factory kiểm `is TDb` + dispose; assertion cũ duy nhất đổi là test reflection kiểu tham số `VocabularyGenerationWriter` (IAppDbContext → IVocabularyDbContext, cùng ý nghĩa); allowlist 45→33 (appdb 9, dag 5, internal 19).

## P2c — D7, D8: ĐẠT (orchestrator): 589/589; port `IPronunciationTargetTextSource` + `IPronunciationAssessmentObserver` (impl Vocabulary), observer gọi trước commit duy nhất, chỉ cập nhật entity tracked; khác biệt vô hại: query progress chỉ chạy khi có điểm (trước chạy cả khi null nhưng không ghi). `AssessPronunciation` → `ISpeechDbContext`. Allowlist 33→31; Speech→Vocabulary = 0. Session được compact 320k→125k.

## P2d — D9, D10: ĐẠT CÓ ĐIỀU KIỆN (orchestrator): 594/594; appdb → 0; route rubric giữ nguyên; snapshot đủ member. **Lỗi trung bình:** worker tạo `LessonRubricExerciseTypes` chép giá trị `ExerciseType` + tự viết TryParse/Format (trùng lặp, có thể khác `Enum.TryParse`) để né Speech→Lessons → sửa ở P2e bước 0: chuyển 3 enum vào Shared Kernel `Domain/Common/Exercises`, trả lại biểu thức gốc.

## P2e — D11, D12 + bước 0: ĐẠT (orchestrator): 598/598; allowlist **0 vi phạm**; enum về `Domain/Common/Exercises`, bỏ bản chép `LessonRubricExerciseTypes`; record VocabularyGames giống hệt shape cũ; `IPromptCatalogReader` singleton qua factory, `ICefrPromptBuilder` giữ singleton; route `/api/prompts/preview` giữ nguyên.

## P2 gộp — security-reviewer: **ĐẠT** (không mất owner/auth/rate limit). Thấp: `IAudioRecordingStore.GetByIdAsync` không lọc owner; observer nhận `userId` tự do; thiếu test cross-user `PronunciationScoreReader`; `ApiKey` trong record có thể lộ qua `ToString()` → `prompt-codex-p2-fix-1.md`.

## P2 gộp — code-reviewer: **ĐẠT CÓ ĐIỀU KIỆN**
- Trung bình: `CefrPromptBuilder` (LiveTutor) dùng entity Catalog qua `Infrastructure.Prompts.PromptCatalogDefaults` — scanner không quét namespace `Infrastructure.*` → fix-1 (fallback dạng string trong Catalog Contracts + mở rộng scanner).
- Trung bình: skill/ADR lệch `ModuleMap` → orchestrator đã sửa (rubric → Speech, CefrPromptBuilder/PreviewSystemPrompt → LiveTutor, enum → Shared, `ModuleMap.cs` là nguồn chuẩn).
- Thấp: base64 nhánh tổng hợp mới đổi cách mã hóa (fix-1 trả lại `Convert.ToBase64String`); dead `IScoringRubricProvider.GetByIdAsync` (fix-1); đăng ký `PromptCatalogReader` nên ở Application (fix-1); khi DB lỗi giữa 2 query prompt thì mất cả guidance DB (chấp nhận, ghi report); `LessonRubricSeeder` mới đổi map, chưa di chuyển file (P3); rubric query đổi tracked → `AsNoTracking` (không sửa rubric, vô hại).

## P2 fix 1: ĐẠT (orchestrator): 607/607; allowlist 0; scanner quét thêm `Infrastructure.<Area>` → phát hiện + sửa 2 vi phạm cũ (CefrPromptBuilder → `Infrastructure.Prompts`; Speech TTS → `Realtime.GeminiLiveSessionBridge`, bridge chuyển về Shared `Infrastructure/Ai`); 75/75 literal prompt của HEAD còn nguyên; `GetOwnedAsync` lọc owner trong query, giữ exception/message; test cross-user; `ApiKey` che trong `ToString()`; base64 Lessons trả lại như cũ. **P2 hoàn tất.**

## P3a — SharedKernel: ĐẠT (orchestrator): 610/610; 150/150 request; 27 file move-only (hash); `IAppDbContext` → Infrastructure giữ namespace; 22 file Common module-specific ở lại Application (chuyển theo từng module P3b–h; `ExerciseContentValidator` phụ thuộc `ValidationIssueDto` trong `LessonDtos.cs`, `IAiServices` dùng `Domain.Pronunciation`, `IIdentityServices` dùng `Domain.Identity`).

## P3b — Catalog: ĐẠT (orchestrator): 612/612; 2 project; 92/93 move byte-identical (+ tách `BuildSystemPromptPreviewRequest` sang Contracts vì LiveTutor dùng, `UpsertAiModelConfigRequest` tách khỏi `AdminCrudDtos`); reference đúng bảng.

## P3c — Identity: ĐẠT (orchestrator): 614/614; 72 file byte-identical; tách 4 record Identity khỏi `AdminCrudDtos`; Catalog → Identity.Contracts đúng DAG.

## P3d — Speech: ĐẠT (orchestrator): 616/616; 52 file byte-identical; tách type Speech khỏi `IAiServices`, `AdminCrudDtos`, `VocabularyDtos`.

## P3e — Vocabulary: ĐẠT (orchestrator): 618/618; 119 file (118 byte-identical, 1 bỏ using thừa); reference Speech.Contracts cho port.

## P3f — Lessons: ĐẠT (orchestrator): 620/620; 47 file (44 byte-identical, 3 chỉ tách type sang Contracts: grader/deterministic grade vì Grammar dùng); `ExerciseContentValidator` → SharedKernel. Lệch plan có chấp nhận: `Lessons.Contracts` → `Speech.Contracts` (`ILessonAiGrader` dùng `ScoringRubricSnapshot`) — cạnh Contracts→Contracts theo đúng DAG; cập nhật `plan-p3.md`.

## P3g — Grammar: ĐẠT (orchestrator): 622/622; 81/81 byte-identical; Grammar.Contracts chỉ có marker type.

## P3h — LiveTutor + dọn: ĐẠT (orchestrator): 624/624, 0 warning; 29 file byte-identical; 150 request/137 handler không đổi; Domain chỉ còn Common, Application chỉ còn composition; `has-pending-model-changes` = không đổi. **P3 xong** (484 rename trong src).

## P3 gộp — code-reviewer: **ĐẠT**. Namespace: 15 file đổi đều thuộc P2 (đã biết), không entity nào; không type mất/nhân đôi; reference đúng DAG; 16 assembly, 78 validator, behavior đúng thứ tự; resource nhúng Grammar vẫn ở Infrastructure. Thấp → `prompt-codex-p3-fix-1.md`: test folder-map rỗng sau P3, `AiCommandsUnitOfWorkTests` thu hẹp phạm vi, `ApplicationAssemblies.All` viết tay không có test chặn thiếu module, `ProjectReferenceTests` danh sách cố định.

## P4a — context + schema theo module: ĐẠT sau khi orchestrator sửa (629/629)
- Worker: 48 `ToTable` thêm schema; 7 context; `SqlConnection` scoped dùng chung; UoW nhiều context; repository chuyển context module; guard test (mapping, 10 migration legacy đóng băng — prompt ghi nhầm 21, đúng là 10 migration, 21 là số file .cs; DI cùng connection). Cạnh mới Grammar → Catalog (`GeminiGrammarExerciseAiGenerator` dùng `IAiModelCatalogReader`) → cập nhật ADR ở P4c.
- **Orchestrator sửa 2 lỗi chỉ lộ trên SQL Server thật** (`UnitOfWork.cs`): (1) context phụ giữ tham chiếu transaction đã commit/rollback sau `UseTransactionAsync` → query sau commit trong cùng scope lỗi "SqlTransaction has completed" → `finally` gỡ `UseTransactionAsync(null)`; (2) `CommitChangesAsync` bên trong `ExecuteInTransactionAsync` với ≥2 context thay đổi mở transaction thứ hai trên cùng connection (SQL Server không hỗ trợ transaction song song) → nếu đã có `CurrentTransaction` thì chỉ save.
- Chưa kiểm chứng: transaction relational thật (InMemory bỏ qua transaction).

## P3 fix 1 — siết test kiến trúc: ĐẠT (orchestrator): 631/631; chỉ sửa 4 file test; folder map fail khi root thiếu; reflection quét `ApplicationAssemblies.All` (≥ 9 handler như trước P3); test mọi csproj module có trong `All` + reference đúng DAG.

## P4b — migration chuyển schema: ĐẠT (orchestrator): 635/635; 7 migration, 48 bảng (6/6/6/6/5/16/3), chỉ `EnsureSchema` + `RenameTable` (SQL: `ALTER SCHEMA … TRANSFER`); 10 migration cũ + snapshot không đổi; history `AppDbContext` vẫn `dbo.__EFMigrationsHistory` (tường minh), module ở `<schema>.__EFMigrationsHistory`; 8 context đều bỏ qua `PendingModelChangesWarning` (bắt buộc vì `AppDbContext` đóng băng); `DatabaseMigrationRunner` legacy → 7 module → seed.

## P4 gộp — code-reviewer: **ĐẠT CÓ ĐIỀU KIỆN**
So snapshot cũ với 7 snapshot module (bỏ schema): property/kiểu/index/key/FK/OnDelete trùng hoàn toàn. Chuỗi cũ không chạy lại (history `dbo`), DB trống tạo 49 bảng rồi chuyển 48; `ALTER SCHEMA TRANSFER` giữ PK/FK/index/constraint.
- Trung bình: M1 lỗi migrate bị nuốt ở Development (schema nửa vời); M2 context scoped mất `ILoggerFactory`; M3 `PendingModelChangesWarning` bị ignore cả cho module context, không test model-vs-snapshot → `prompt-codex-p4-fix-1.md`.
- Thấp: bất biến connection chung chưa ghi; nguy cơ tự block factory-vs-transaction (có từ trước, chưa xảy ra); token không truyền vào migrate cũ; reflection mỗi lần tạo context; test mapping chưa so key/index/FK.
- Rủi ro DB thật (đưa vào report cho người dùng): bảng phải ở `dbo`; quyền `CREATE SCHEMA`/`ALTER`/`CONTROL`; view/proc/synonym ngoài EF trỏ `dbo.X` sẽ hỏng (view SCHEMABINDING làm TRANSFER fail); branch khác dùng chung DB sẽ lỗi sau khi apply; `[identity]` là từ khóa T-SQL khi viết SQL tay.

## P4 fix 1: ĐẠT (orchestrator): 636/636, 0 warning; migrate lỗi → `LogCritical` + ném lại ở mọi môi trường (seed lỗi ở Development vẫn warning như cũ); context scoped có `ILoggerFactory`; `PendingModelChangesWarning` chỉ ignore cho `AppDbContext`; test model-vs-snapshot 7 context; token truyền vào migrate cũ; factory cache accessor (đọc `Result` sau `await`, không block); fix UoW của orchestrator còn nguyên. **P4 xong.** Orchestrator thêm `preflight-check-lingomint.sql` (chỉ đọc) cho người dùng.

## P5a — Shared.Infrastructure + Shared.Presentation: ĐẠT (orchestrator): 638/638; 8 + 5 file chuyển giữ namespace (`EntityConfigurationExtensions` internal → public, cần cho assembly module); 8 warning là nullable có sẵn từ baseline (`GrammarContentPackImporter`, `LessonAiGrader`) — build incremental trước đó hiện 0 vì không biên dịch lại.

## P5b — Catalog 5 project: ĐẠT (orchestrator): 641/641; migration ID `20261101013950_MoveToModuleSchema_Catalog` giữ nguyên ở assembly mới; has-pending = không đổi; DI Catalog chuyển vào `AddCatalogInfrastructure`; thêm `ModuleDbContextFactory<TDb,TContext>` generic ở Shared.Infrastructure.

## P5c — Identity (Codex): DỪNG giữa chừng theo yêu cầu người dùng (quota Codex sắp hết). Worktree trả về mốc P5b (`git restore` + `git clean`), build + 641/641 xác nhận. Identity sẽ làm lại bằng AGY.

## P5j — Shared thành 4 project trong `src/Shared/` (orchestrator tự làm): ĐẠT
- `EnglishLiveTutor.Domain` → `src/Shared/EnglishLiveTutor.Shared.Domain`, `EnglishLiveTutor.SharedKernel` → `src/Shared/EnglishLiveTutor.Shared.Application` (RootNamespace `EnglishLiveTutor.Application`), `Shared.Infrastructure`/`Shared.Presentation` → `src/Shared/`. `git mv` (rename), namespace không đổi.
- 24 csproj + `.sln` cập nhật đường dẫn; test kiến trúc cập nhật đường dẫn/tên (giữ ngữ nghĩa: domain kernel vẫn `null`, Shared khác vẫn `Shared`); 17 mảng expected trong `ProjectReferenceTests` sắp lại theo ordinal (tên mới đổi thứ tự alphabet).
- Build 0 lỗi; 641/641; `has-pending-model-changes` Catalog/Speech = không đổi; allowlist 0. Skill `modular-monolith` thêm mục "Project layout".

## Rà soát Catalog (AGY, chỉ đọc) — CẦN SỬA nhỏ; orchestrator đánh giá
- Nhận, sửa ở P5i: F-01 `BuildSystemPromptPreviewRequest` (Catalog.Contracts, chỉ LiveTutor dùng) → LiveTutor; F-02 package thừa `System.Security.Cryptography.Pkcs` ở Catalog.Infrastructure (kiểm cả module khác); F-03 `PromptCatalogEntityDefaults` (seed dạng entity) Application → Catalog.Infrastructure; F-07 reference `Identity.Contracts` không dùng; F-08 thêm `Map<X>Endpoints()` gom cho mọi module; F-09 `using` Domain thừa ở Presentation.
- Không nhận (giữ hành vi, luật 15): F-04 query dùng `IModuleDbContextFactory` (có từ trước, đổi sang scoped là đổi hành vi); F-05 query trả entity (đổi DTO = đổi JSON contract FE); F-06 reader singleton (cố ý, phục vụ `CefrPromptBuilder` singleton realtime).

## P5c — Identity 5 project (AGY, trên development): ĐẠT (orchestrator): 644/644; 84 rename; migration `20261101013933_MoveToModuleSchema_Identity` giữ ID; has-pending = không đổi.
## P5d — Speech 5 project (AGY, worktree mm-p5d-speech): ĐẠT (orchestrator): 644/644; 65 rename; migration `20261101013953_MoveToModuleSchema_Speech` giữ ID; has-pending = không đổi.

## P5e–P5h (AGY song song, worktree riêng) + merge vào development: ĐẠT (orchestrator)
- Mỗi worktree: build 0 lỗi, toàn bộ test pass, has-pending = không đổi, migration ID giữ nguyên; commit trên `ai/mm-p5{e,f,g,h}-*`. Rename: Vocabulary 137, Lessons 70, Grammar 99, LiveTutor 50.
- Merge tuần tự Speech → Vocabulary → Lessons → Grammar → LiveTutor; xung đột chỉ ở composition (`Api.csproj`, `Infrastructure.csproj`, `ApplicationAssemblies.cs`, `AppDbContext.cs`, `Infrastructure/DependencyInjection.cs`). Orchestrator gộp: csproj/using/ApplyConfigurations giữ cả hai; DI giữ mọi `Add<X>Infrastructure()` và bỏ đăng ký cũ của module đã chuyển (kiểm không trùng `AddDbContextFactory`); bỏ `EmbeddedResource Grammar\ContentPacks\*.json` thừa ở composition (đã ở Grammar.Infrastructure). Mỗi merge build + test lại: 647 → 650 → 653 → 656 → **659/659**.
- Sau merge: 7 context has-pending = không đổi, 7 migration ID giữ nguyên; 7 module extension đều có `UseLoggerFactory`, interceptor, connection chung, không ignore `PendingModelChangesWarning` (fix P4 giữ nguyên). Composition chỉ còn `Add<X>Infrastructure()` ×7 + AppDbContext/idempotency/migration runner.

## P5i — dọn cuối (AGY trên development): ĐẠT (orchestrator): bỏ `System.Security.Cryptography.Pkcs` ở 7 module (giữ pin ở composition), `BuildSystemPromptPreviewRequest` → LiveTutor, `PromptCatalogEntityDefaults` → Catalog.Infrastructure, bỏ `Identity.Contracts` thừa ở 10 project, using thừa; 7 hàm `Map<X>Endpoints()`. `Program.cs` giữ lời gọi rời đúng thứ tự cũ (module xen kẽ); `MapVocabularyEndpoints`/`MapGrammarEndpoints` cũ đổi tên `…CoreEndpoints` để tránh trùng tên hàm gom — route không đổi. **P5 xong.**

## P6a — test theo module (AGY trên development): ĐẠT (orchestrator)
- 659 → 659 test, 0 fail, 9 project: ArchitectureTests 87, Api.IntegrationTests 96, Modules: Catalog 9, Identity 45, Speech 30, Vocabulary 128, Lessons 89, Grammar 139, LiveTutor 36; `Tests.Common` (factory, test doubles). `EnglishLiveTutor.Domain.Tests` đã xóa. 68 rename; `InternalsVisibleTo` cập nhật.
- **Orchestrator sửa lỗi do chính mình (P5i):** bỏ `System.Security.Cryptography.Pkcs` ở 7 project `.Infrastructure` module theo finding F-02 là SAI — đó là pin bảo mật: EF SqlServer kéo Pkcs 9.0.11 có lỗ hổng (NU1902/NU1903 xuất hiện ở 7 project). Runtime vẫn dùng 10.0.13 (pin ở composition) nhưng project module bị cảnh báo. Đã thêm lại pin (kèm comment lý do) ở 7 project; build không còn NU1902/NU1903, chỉ 8 warning nullable baseline.

## P6b — snapshot contract API (AGY): ĐẠT (orchestrator tự đối chiếu với code trước refactor)
- `ApiContractSnapshotTests`: inventory 174 endpoint (168 HTTP + 6 SignalR) dạng `<methods> <route> | auth | rateLimit | permission`, 5 response shape (health, ai-models, topics, voices, ProblemDetails 404); chế độ ghi `API_SNAPSHOT_WRITE=1`.
- Orchestrator tạo worktree tạm tại `8884da1` (trước refactor), chép nguyên file test, sinh snapshot: **endpoint-inventory 174/174 dòng giống hệt, response-shapes.json giống hệt**. Worktree tạm đã xóa.

## Review cuối P5c–P6 (code-reviewer, range `0d2d72c..7004232`): ĐẠT CÓ ĐIỀU KIỆN → đã xử lý điều kiện
- DI composition sau 5 lần gộp: mỗi service/option/HttpClient/hosted service đúng một lần, lifetime giữ (singleton realtime, `FallingWordRoundExpirationWorker`, 9 option `ValidateOnStart`); 35 project đúng hướng; content pack Grammar đọc đúng assembly; test không bị nới.
- **Cao (đã sửa):** `LessonRubricEndpoints.cs:48` chuỗi `"Không tìm thấy rubric."` bị mojibake khi di chuyển → khôi phục UTF-8; thêm `ArchitectureTests/Architecture/SourceEncodingTests.cs` quét `src/` + `tests/` (pattern mojibake UTF-8→Latin-1) + 3 case pattern.
- Trung bình (ghi report): shape snapshot chỉ phủ Catalog/host.
- Thấp: rule `Api/Services/` nới thành thư mục → đã thu hẹp lại 2 file đích danh; các mục còn lại ghi vào report (factory cũ chết, helper SQL chép 8 nơi, dòng trống, `Map<X>Endpoints` không dùng ở Program, `InternalsVisibleTo` thừa).
- Sau sửa: build 0 lỗi, **669/669**.

## P7a — gom hạ tầng DbContext/UoW (AGY trên development): ĐẠT (orchestrator)
- `AddModuleDbContext<TContext,TInterface>()` ở Shared.Infrastructure thay 7 khối chép (7 bản giống nhau 100%); `UnitOfWork` → Shared.Infrastructure, nhận `IEnumerable<IUnitOfWorkParticipant>` (AppDbContext + 7) — logic transaction **giống hệt** (diff -w = 0, giữ 2 fix P4); `SqlConnection` scoped vẫn đăng ký một lần ở host; xóa `ModuleDbContextFactory<TDb>` chết; bỏ 7 `InternalsVisibleTo` thừa; 3 test mới.
- Build 0 lỗi (warning nullable baseline); **674/674**; 7 context has-pending = không đổi.

## P7b — shape snapshot 6 module (AGY, worktree mm-p7b-snapshots): ĐẠT (orchestrator)
- 7 test mới (Identity `/api/auth/me`, Speech rubric active, Vocabulary flashcards, Lessons list, Grammar roadmap, LiveTutor conversations + 404). Orchestrator chép test sang worktree tạm `8884da1`: 12 shape + 174 inventory **giống hệt**. Merge không xung đột; **681/681**.

## P8a — 7 migration module tự chứa (AGY): ĐẠT (orchestrator, kiểm trên SQL Server thật)
- ID/Designer/snapshot/`Down()` giữ nguyên; `Up()` = `EnsureSchema` + SQL có điều kiện: chặn DB cũ dở dang (thiếu `20261029040750_AddGrammarRoadmap`) → chuyển từ `dbo` nếu có → tạo nếu thiếu (48 bảng, 71 index, 23 FK, mỗi lệnh bọc `IF`).
- **DB trống tạm `LingoMint_P8Fresh`** (chỉ 7 context module, không chuỗi cũ): `database update` OK; so cấu trúc với `LingoMint`: 916/916 dòng khớp, trừ 6 cột trên `LingoMint` có `DEFAULT` constraint (`livetutor.Conversations.UserId`, `speech.ScoringRubricCriteria.DescriptorLevel0..4`) — sinh từ `AddColumn(defaultValue)` của migration cũ, không có trong model EF; EF luôn ghi giá trị tường minh → chấp nhận. DB tạm đã xóa.

## P8b — SharedDbContext, xóa AppDbContext + 10 migration cũ (AGY): ĐẠT (orchestrator, kiểm trên SQL Server thật)
- `src` không còn `AppDbContext`/`IAppDbContext`; `SharedDbContext` (schema `shared`) + `InitialCreate_Shared` cùng mẫu P8a; `IdempotencyService` dùng Shared; runner Shared → 7 module; seeder 15 lần save đổi sang context module (không phải tách lần save nào); context gộp chỉ cho test chuyển vào `Tests.Common`. Build 0 lỗi; **680/680** (bỏ test "10 migration đóng băng"); 8 context has-pending = không đổi.
- **Kiểm trên 3 DB tạm** (`dotnet ef database update` Shared + 7): DB trống, DB khôi phục từ backup trước refactor, bản sao `LingoMint` hiện tại (backup `COPY_ONLY`) → cấu trúc **giống hệt** nhau (933 dòng cột/index/FK, 0 object ở `dbo` ngoài history cũ), số dòng mọi bảng **không đổi** trước/sau migrate, bản sao = `LingoMint`. DB cũ thiếu migration `AddGrammarRoadmap` → **dừng với THROW**, không bảng nào bị chuyển. DB tạm + backup tạm đã xóa.

## Gộp composition vào Api (orchestrator): ĐẠT
- 6 file (`ApplicationAssemblies`, 2 `DependencyInjection`, `DatabaseMigrationRunner`, `DatabaseSeeder`, `IdempotencyService`) `git mv` vào `src/EnglishLiveTutor.Api/Composition/{Application,Infrastructure}`, namespace giữ nguyên; xóa `EnglishLiveTutor.Application.csproj`, `EnglishLiveTutor.Infrastructure.csproj`. Api.csproj gộp reference/package (giữ pin `System.Security.Cryptography.Pkcs`, `InternalsVisibleTo` test; bỏ `Microsoft.Extensions.Http` vì NU1510 — đã có trong ASP.NET Core). 10 project test đổi reference sang Api; `.slnx` còn 50 project; `ModuleMap` rule `EnglishLiveTutor.Api/Composition/`.
- Build 0 lỗi (8 warning nullable baseline, không NU19xx); **680/680**; EF tools (`--startup-project Api`) has-pending = không đổi.
