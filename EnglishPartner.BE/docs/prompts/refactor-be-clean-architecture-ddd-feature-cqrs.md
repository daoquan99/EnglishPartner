# Prompt: Refactor BE theo Clean Architecture with DDD Aggregates and Feature-Based CQRS

## Cách dùng

Mở workspace `D:\Projects\EnglishPartner\EnglishPartner.BE`, yêu cầu coding agent **thực thi file prompt này**. Khi được yêu cầu thực thi, đây là nhiệm vụ implementation: chỉnh source trong phạm vi dưới đây, không chỉ tạo thêm review/plan. Việc tạo file prompt này không đồng nghĩa refactor đã được thực hiện.

---

Bạn là senior .NET backend engineer. Hãy refactor toàn bộ backend hiện tại theo:

**Pragmatic Clean Architecture + DDD + CQRS + Aggregate-Centric Domain Structure + Feature-Based CQRS**, gọi thống nhất là **Clean Architecture with DDD Aggregates and Feature-Based CQRS**.

Đi từ Domain -> Application -> Infrastructure -> API về mặt thiết kế; triển khai từng slice có đủ callers/DI/mapping để build được sau mỗi phần. Tiếp tục đến khi hoàn tất cấu trúc và ranh giới của cả 4 layer trong phạm vi này. Không dừng sau khi đưa ra kế hoạch, chuyển vài folder hoặc sửa compiler errors đầu tiên.

## 1. Đọc trước khi sửa

Đọc và kiểm tra lại với source hiện tại:

- [AGENTS.md](../../AGENTS.md).
- [Kiến trúc đích và aggregate map](../../.agents/architecture.md).
- [Báo cáo 20 findings](../backend-architecture-review-2026-10-15.md).
- [Plan R0–R7 và acceptance gates](../backend-refactor-plan.md).
- [Skill architecture-refactor](../../.agents/skills/architecture-refactor/SKILL.md).
- Theo phần đang làm, đọc skill [Domain](../../.agents/skills/domain-aggregate-design/SKILL.md), [CQRS](../../.agents/skills/feature-cqrs/SKILL.md), [Infrastructure](../../.agents/skills/infrastructure-persistence/SKILL.md), [API/realtime](../../.agents/skills/api-realtime/SKILL.md).

Các file trên được viết trong lần review chỉ đọc. Khi người dùng yêu cầu thực thi prompt này, yêu cầu implementation mới cho phép chỉnh source theo phạm vi ở đây; không hiểu câu “review/docs only” lịch sử là lệnh cấm implementation vĩnh viễn. Source mới nhất là sự thật về trạng thái hiện tại; không coi finding cũ là chắc chắn vẫn tồn tại.

## 2. Phạm vi, nguyên tắc và compatibility

### Phạm vi được làm

- Cả 4 production projects `EnglishLiveTutor.Domain`, `.Application`, `.Infrastructure`, `.Api` cùng tests/docs/guidance liên quan trong BE.
- Refactor folder/namespace, aggregate ownership/behavior, write/read abstractions, command/query handlers, contracts/mappings, DI, repository semantics và realtime boundaries.
- Sửa các lỗi code xác định được trong quá trình refactor nếu không cần tự quyết định sản phẩm hoặc thay data/HTTP contract chưa được chốt. Liệt kê riêng behavior fixes so với mechanical moves.
- Giữ solution/project names, target framework hiện tại và tên `*Entity` ở lần refactor này; không đổi branding EnglishLiveTutor thành EnglishPartner.

### Giới hạn

- Git root là `D:\Projects\EnglishPartner`, FE nằm cạnh BE. Không sửa FE, không format toàn monorepo, không đụng thay đổi không liên quan.
- Working tree có thể đang bẩn: đọc status/diff và cả untracked files trước mutation; preserve nội dung người dùng đã sửa. Không reset, checkout đè, clean hoặc xóa untracked files.
- Không commit/push/deploy; không chạy live SQL migration/seeder hoặc gọi Google/provider có phí trong nhiệm vụ này. Không bật API chỉ để kiểm tra compile vì startup hiện có database side effects.
- Không rewrite/xóa existing migrations hoặc reset database. Structural refactor phải giữ table/column/index names, persisted IDs, mapping semantics và migration history. Giữ migration CLR namespaces/attributes khi namespace runtime entities thay đổi nếu EF discovery cần chúng.
- Giữ HTTP URLs/verbs, JSON field names/types/null behavior, `{ error }`, pagination list/envelope và X-* headers, cookie names, SignalR URLs/method/event names, argument order và payload shapes. Khi domain entity đang serialize trực tiếp, DTO thay thế phải tái tạo contract hiện hành.
- Không thêm generic repository, MediatR, event bus/outbox, microservices, bounded-context subprojects hoặc read database riêng chỉ để đủ pattern. Giữ lightweight ICommand/IQuery handlers hiện có.
- Không log/echo credentials, API keys, tokens, connection strings hoặc provider payload đầy đủ.

### Các quyết định còn mở: xử lý để vẫn tiến triển

Đọc decision table trong plan. Dùng lựa chọn đã được người dùng chốt trong hội thoại nếu có. Khi chưa có quyết định, hoàn tất các phần độc lập và giữ các giới hạn sau:

| Chủ đề | Mặc định cho lần refactor này |
| --- | --- |
| GET today đang auto-init | Tách query read-only khỏi init command. Nếu client phụ thuộc GET auto-init, giữ orchestration cũ qua Application compatibility use case có tên thể hiện side effect, không gọi nó là query. Ghi rõ legacy route còn side effect và bước rollout để gỡ; không âm thầm đổi response/behavior |
| Identity/demo | Không thêm fallback identity mới. Nếu source/client contract chứng minh personal flow đã authenticated, thống nhất current principal cho flow đó và check ownership hiện có. Thay anonymous/demo access policy hoặc bỏ public userId cần một compatibility decision; báo pending nếu chưa có |
| Conversation owner cũ | Giữ Conversation là root và tách Application access/session ports. Không tự gán owner từ CreatedBy; owner schema/backfill và multi-user access migration còn pending nếu chưa được chốt |
| Review retry | Không tự cấm mọi lần review tiếp. Tổ chức atomic write và xác định ranh giới attempt; exactly-once cần request/attempt identity rõ. Không tuyên bố idempotent nếu chưa có cơ chế và verification |
| Rubric lifecycle | Sửa validation/state mutation safety; giữ version/publication contract hiện có. Không tự tạo draft/publish workflow mới |
| Audio recording | Tách metadata/storage responsibility; giữ metadata contract, ghi rõ chưa có retained bytes nếu đúng source. Không tự thêm blob provider/retention worker |
| Audio cache | Giữ multiple versions/newest-valid lookup nếu source hiện hỗ trợ; không thêm unique index chưa audit duplicate data. Best-effort cache failure phải có logging có phân loại |
| Deployment | Không thêm distributed infrastructure khi chưa có nhu cầu; ghi process-local locks đúng giới hạn, không coi chúng là cross-instance correctness |

Những mục cần quyết định schema/product là phần pending được báo riêng, không phải lý do bỏ dở phần refactor kiến trúc độc lập. Không tự đánh dấu cả 20 findings “đã sửa” khi vẫn còn các mục này.

## 3. R0 — Baseline thực tế và bản đồ refactor

1. Đọc ancestor AGENTS nếu có; kiểm tra working tree, project references, framework/packages, toàn bộ root/children, handler registrations, endpoint/hub callers và factory/context lifetimes.
2. Ghi baseline source/build trước thay đổi; nếu build đã lỗi, phân biệt lỗi sẵn với lỗi mới. Sửa lỗi trong scope đủ để tiếp tục, không chữa unrelated feature để làm đẹp báo cáo.
3. Tạo `docs/backend-refactor-execution.md`, ghi status từng phase, quyết định kỹ thuật, old -> new path/namespace, contract baseline, behavior changes và checks thực sự chạy. Cập nhật file này khi triển khai.
4. Lập trace cho Learning review/init, Vocabulary automatic/manual generation, Pronunciation assess/synthesize/cache, Identity auth/admin và Conversations HTTP/realtime.
5. Kiểm tra contract FE ở chế độ read-only nếu cần biết compatibility; không sửa FE. Không đưa dữ liệu nhạy cảm vào execution log.

## 4. Domain — Aggregate-Centric Domain Structure

Tổ chức `Domain/<Area>/<Aggregate>/` với root và repository contract cạnh nhau; children trong `Entities/` thuộc đúng root. Với area chỉ một root nhỏ, tránh wrapper folder thừa. Tham chiếu cây đích trong `.agents/architecture.md`; không tạo folder rỗng.

| Area | Các root/record và ownership cần thể hiện |
| --- | --- |
| Vocabulary | Vocabularies: VocabularyEntity -> Senses, Pronunciations, WordFamilies; Sense -> Relations, Collocations, Examples, GrammarPatterns. GenerationRuns giữ audit/run record riêng |
| Learning | DailyVocabularyPlans -> DailyVocabularyItem; UserVocabularyProgress là root riêng; ReviewAttempts là historical record riêng |
| Identity | Users -> UserRole, RefreshToken; Roles -> RolePermission; Permissions là root riêng |
| Conversations | Conversation -> ChatMessage, LanguageCorrection; giữ root này trước khi có bằng chứng cần tách turn/message aggregate |
| Pronunciation | Assessments -> CriterionScores; ScoringRubrics -> Criteria; AudioRecordings và GeneratedAudio có lifecycle riêng |
| Prompts | CefrLevelPrompts, ConversationModePrompts, SpeechSpeeds là các root riêng |
| Catalogs | AiModels, Topics, TeacherVoices giữ các root nhỏ, độc lập |

Yêu cầu về thiết kế:

- Domain chỉ phụ thuộc BCL; không EF/HTTP/SignalR/DI/provider SDK. Private setters/backing collections phải còn có ý nghĩa thực sự.
- Root bảo vệ invariant liên quan nhiều children; dùng root operations để thêm/đổi/complete item, primary sense, criteria. Child có thể giữ local behavior; không cho Application tự persist child để né root rules.
- Không tạo repository cho mỗi bảng hoặc child. Repository contract mô tả aggregate write, không trả anonymous object/UI paging shape.
- Giữ IDs của senses/content đang được Learning/assessment tham chiếu; không xóa/tạo lại root chỉ để update.
- Validate candidate state trước replace/clear collection. Rubric kiểm tra code trùng, range hợp lệ, weight hợp lệ và tổng theo policy hiện hành; invalid update không để object đổi state nửa chừng. Assessment kiểm tra score/weight/state consistency và giữ snapshots.
- Đưa thời điểm business transition thành giá trị được truyền vào từ TimeProvider ở Application. Chuyển timezone/calendar resolution và presentation title formatting ra khỏi Domain khi phù hợp, giữ observable behavior.
- Dùng enum/value object khi loại được divergence thật; giữ mapping với storage/wire strings cũ. Không ép `AdminUpdated` vào enum hiện không chứa nó, không tự đổi provenance semantics.
- Phân biệt AggregateRoot, audit và soft-delete capabilities theo lifecycle. Không đổi schema/base toàn bộ history/cache chỉ để đẹp; ghi lại giới hạn nếu giữ inheritance để tương thích trong lần này.
- Không tải toàn bộ lịch sử conversation mỗi audio packet. Finalized-turn writes đi qua Application writer/use case có parent visibility/identity rules; document targeted-write exception rõ ràng.

Cập nhật mọi usings/callers/mappings/DI/tests bị ảnh hưởng khi chuyển namespace. Namespace runtime entity có thể đổi nhưng SQL mapping/migration compatibility phải giữ.

## 5. Application — CQRS with Feature-Based Folder Organization

Chuẩn hóa mọi feature: Conversations, Learning, Vocabulary, Pronunciation, Identity, Prompts, Topics, Voices, AiModels.

```text
Application/
  Common/
    Cqrs/
    Persistence/                 read abstractions, IUnitOfWork
    Identity/                    shared current-user capability
    Models/                      genuinely shared result shapes
  Features/<Feature>/
    Commands/<UseCase>/
    Queries/<UseCase>/
    Contracts/                   shared within feature
    Ports/                       external capabilities owned by feature
    Mapping/                     only where needed
    Policies/                    only where needed
```

Request + handler có thể cùng file nếu nhỏ. Tách nhiều use cases đang gộp trong `AdminIdentityCrudCommands.cs`. Chuyển DTO/ports từ Common theo feature owner; một contract chỉ dùng cho một query có thể nằm cùng query. Không tạo thêm tầng pass-through.

### Write side

- Chuẩn hóa command -> input/access validation -> load roots -> domain transitions -> stage related history -> một commit boundary.
- Application định nghĩa IUnitOfWork; Infrastructure implement bằng scoped AppDbContext. Các write repositories trong cùng operation dùng cùng context, stage changes, không tự commit mỗi repository.
- Migrate đầy đủ các command đang tự mở factory context để ghi: Learning init/review, Vocabulary generation writer, Pronunciation assessment và Identity admin paths. Giữ write context surface ở Infrastructure; Application query không được nhìn thấy SaveChanges/mutable DbSet capability.
- Learning plan + progress + attempt có thể commit trong một local SQL transaction. Không áp luật “một aggregate mỗi transaction” nếu làm mất atomicity.
- Không giữ SQL transaction qua Google HTTP/WebSocket call. Resolve read snapshots, gọi provider, validate output, rồi short write operation với concurrency handling đúng nhu cầu.
- Repository/context ownership phải rõ cả khi command gọi feature service; không gọi command handler lồng nhau khiến commit context khác nhau. Tách reusable operation/policy khi cần.
- Sửa owner check có thể chứng minh từ model hiện có, như item -> plan -> command user; không dùng ownership suy đoán. Reconcile plan status/items trong cùng operation.
- Failure audit context phải độc lập để không flush partial vocabulary graph. Sau commit thành công, readback failure không được vô tình báo toàn bộ write đã rollback.

### Read side

- Query không SaveChanges, không mutate state, không gọi command. Legacy auto-init orchestration xử lý theo compatibility table ở trên.
- Cho phép EF LINQ/projection ở Application qua Application-owned read context/factory chỉ có read/query capability; không cần xóa mọi EF reference để gọi là Clean Architecture.
- Query trả named DTO, không `object`, `dynamic` hoặc Domain entity làm public response. Query services/optimized ports chỉ dùng khi có lợi thực tế.
- Filter user/root visibility khi đọc child tables. Chỉ AsNoTracking không đủ chứng minh ownership hoặc soft-delete propagation.
- Batch ListUsers roles/permissions theo page, batch flashcard projections; tránh N+1 và full graph không cần thiết. Stable paging order có tie-breaker; giữ search/filter/pagination semantics hiện hành.
- Tách EF execution ra khỏi shared PagedResult record nếu cần; giữ pagination wire/header contract.
- Hợp nhất authoritative pronunciation target/profile resolution giữa synthesize và assessment. Validate vocabulary/sense/example membership; adapter không tự đổi multi-word vocabulary từ word sang sentence. Giữ legacy text-only path có chủ đích.

## 6. Infrastructure — persistence và provider adapters

- Group configs/repositories theo feature/aggregate; giữ AppDbContext canonical và migration history. Không tạo DbContext thứ hai để né việc migrate callers.
- Migrate scoped repository + UoW semantics nhất quán, kể cả Topic/Conversation special cases. Tách read projection khỏi domain write repository.
- Singleton realtime/state adapter không capture scoped repository/context. Mỗi persistence callback mở operation scope riêng, mang actor/session identity được capture đúng lúc; không phụ thuộc HttpContext còn sống.
- Sửa soft-delete interceptor: chỉ preserve descendants của đúng root bị soft-delete; child deletion thuộc aggregate khác trong cùng SaveChanges vẫn phải diễn ra. Root filters không tự áp cho direct child query.
- Giữ IsActive/IsEnabled khác IsDeleted; created audit không overwritten; restore/unique-key policy phải rõ.
- Provider HTTP/WebSocket, JSON parsing, reconnect, audio encoding ở Infrastructure; Application giữ workflow và ports. Giữ timeout/cancellation behavior của vocabulary generation và partial TTS response semantics.
- Cache GET chỉ đọc; hit không cần API key, miss mới cần key; audio chưa hoàn tất không persist. Không catch mọi DbUpdateException như unique conflict khi mapping không có unique constraint; log cache failures an toàn theo best-effort policy và không che write failure ngoài cache.
- Tách seed orchestration/data theo trách nhiệm; giữ seed IDs và bảo vệ administrator edits. Tách schema migration failure với optional seed failure. Không dùng suppression PendingModelChangesWarning để che model drift; đối chiếu model/snapshot trước xử lý suppression.
- Liveness và readiness cần semantics khác nhau; giữ existing health contract nếu client phụ thuộc, bổ sung readiness riêng khi thích hợp. Không báo DB/schema ready nếu chưa kiểm tra được.

### Realtime

Tách `GeminiLiveConnectionManager` theo trách nhiệm thực: Application session control/orchestration; provider bridge; session-state storage; transcript/turn policy; translation/grammar operations; persistence writer; notifier port. Không chỉ tách 926 dòng thành partial classes rồi coi là đã tách layer.

Giữ streaming fast path; không tạo command cho từng audio chunk. Preserve turn IDs, sequence ordering, dedup, notification order và transcript/translation semantics. Stop/disconnect/start-fail/shutdown phải cancel owned work, await tasks khi cần, dispose tài nguyên, remove API keys/state khi session hết owners. Không để disconnect một connection dừng nhầm session đang có connection hợp lệ khác.

## 7. API — thin transport adapters

- Endpoints chỉ bind/authorize/invoke Application/map result; thêm DeleteTeacherVoice command thay direct EF deletion.
- Hubs dùng Application session ports, không inject concrete Gemini manager. Api -> Infrastructure reference chỉ phục vụ composition root/adapter registration.
- Đưa AI model routes sang endpoint feature riêng, giữ URLs và permissions cho tới khi policy change được chốt; không tự đổi rubrics.manage thành claim mới trong mechanical move.
- DTO mappings giữ JSON hiện tại kể cả những fields trước đây đến từ entity serialization. Không đổi mọi response sang envelope/ProblemDetails cùng lần di chuyển.
- Central error mapping chỉ dùng khi bảo toàn các error/status cases; phân biệt not found, invalid input, forbidden, conflict và provider errors theo contract đã chốt.
- Serialization-specific GUID converter thuộc transport adapter nếu di chuyển; giữ lenient parsing hiện hành cho tới khi có quyết định strict parsing riêng.
- Session access/group membership được kiểm tra qua Application policy trước join/start/stream/stop. Nếu owner schema chưa có, báo rõ phần authorization còn pending; interface mới không phải bằng chứng access đã an toàn.

## 8. Verification: thực thi phù hợp, không hứa quá evidence

Baseline và kiểm tra compile từ cwd BE:

```powershell
git status --short -- .
dotnet build .\EnglishLiveTutor.sln --no-restore
git diff --check -- .
```

Nếu thiếu restore assets, restore solution trước và ghi nhận. Nếu build bị process lock, xác định đúng process/output, không kill process của người dùng hay đổi target framework để né lỗi. Giữ evidence lỗi baseline riêng.

Chạy các checks có liên quan sau mỗi slice, không chạy toàn monorepo/full suite theo quán tính. Khi path/namespace tests đổi, cập nhật filters theo symbols thực tế:

```powershell
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~DomainLogicTests"
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~PersistenceAndAuditingTests"
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~EndpointCqrsTests|FullyQualifiedName~CqrsDependencyInjectionTests"
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~ManualVocabularyGenerationTests|FullyQualifiedName~PronunciationSynthesisAudioCacheTests"
```

Bổ sung focused regression tests cho hành vi đã sửa: invalid domain update không mutate state, owner mismatch, review/plan atomicity, unrelated child deletion, authoritative target/profile, cache failure policy, session cleanup, DTO/HTTP contract và DI lifetimes. Không viết tests chỉ để assert folder names hoặc mirror implementation. Không xóa tests có sẵn do project mang tên Domain.Tests nhưng đang chứa nhiều categories.

EF InMemory/handler tests không chứng minh SQL constraints/transactions hoặc HTTP auth/SignalR. Nếu không có disposable SQL/provider environment đã được cho phép, để relational/live gates unverified trong báo cáo, không tự kết nối database thật. Việc sửa query phức tạp phải ghi SQL translation gate còn thiếu nếu chưa kiểm tra provider quan hệ.

Kiểm tra dependency/mutation surfaces cuối bằng search trên source production, loại bin/obj/generated code khi phù hợp: Domain không import framework, API không EF writes/concrete manager, Queries không command/SaveChanges, Application không còn public object/domain-entity response ở features đã migrate, write paths không còn competing factory context. Review hits theo ngữ cảnh, không đổi code chỉ để grep trả zero.

## 9. Definition of Done

- [ ] Cả 4 layer được tổ chức theo target, mọi feature/callers/DI đã migrate; không chỉ Learning hoặc Vocabulary.
- [ ] Domain aggregate ownership rõ, child mutations không bypass invariant qua write paths cũ.
- [ ] Application có feature contracts, pure queries và write repository/UoW boundary nhất quán.
- [ ] Các legacy compatibility operations và structural exceptions được đặt tên/mô tả chính xác; không trình bày chúng là đã đạt pure CQRS.
- [ ] API/hubs dùng Application, Infrastructure giữ persistence/provider concerns; singleton/scoped lifetimes đúng.
- [ ] Existing routes/JSON/paging/hub protocols và SQL/migration identities được giữ ngoài behavior changes đã nêu rõ.
- [ ] Build và focused checks phù hợp pass, hoặc lỗi baseline/verification blockers được ghi chính xác; không claim completed nếu còn lỗi do refactor.
- [ ] Mỗi finding trong review có status: resolved / partially addressed / deferred with reason / no longer applicable, kèm files/checks. Architectural completion và business/security debt được báo tách biệt.
- [ ] Cập nhật `docs/architecture.md`, `AGENTS.md`, `.agents/architecture.md` và skills nếu implementation làm hướng dẫn lỗi thời; reconcile user changes trước ghi. Giữ review 2026-10-15 như historical baseline.
- [ ] Không có source stubs, TODO bắt buộc chưa làm, duplicate abstraction mặc định, old namespaces trong active runtime code hoặc migration/schema churn do rename không cần thiết.

## 10. Báo cáo cuối bằng tiếng Việt

Nêu: kết quả Domain/Application/Infrastructure/API; quyết định aggregate và UoW; old -> new layout; behavior fixes và contracts/schema bị ảnh hưởng; lệnh/checks đã chạy cùng kết quả; pending product decisions, SQL/provider/runtime gates còn thiếu.

Dẫn link file thực tế và `docs/backend-refactor-execution.md`. Không trả lời chỉ “đã chuẩn Clean Architecture”; không nói đã sửa cả 20 findings nếu chỉ di chuyển source, không coi build pass là chứng nhận runtime. Không hỏi lại để duyệt lựa chọn kỹ thuật thông thường đã nằm trong phạm vi này; chỉ yêu cầu thông tin khi một quyết định phụ thuộc thực sự không thể xử lý từ source/contract và không thể tiếp tục phần liên quan an toàn.
