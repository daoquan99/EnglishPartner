# Kế hoạch refactor backend

Trạng thái: **đề xuất, chưa triển khai**. Baseline là [review 2026-10-15](backend-architecture-review-2026-10-15.md). Request hiện tại chỉ cho phép review và tạo tài liệu/skills; mọi task dưới đây là công việc tương lai khi có yêu cầu implementation.

Mục tiêu: **Clean Architecture with DDD Aggregates and Feature-Based CQRS**, giữ 4 project và tên EnglishLiveTutor. Chi tiết thiết kế đích nằm tại [architecture decisions](../.agents/architecture.md). Không dùng việc di chuyển file làm bằng chứng đã sửa hành vi.

## 1. Những quyết định cần chốt khi bắt đầu slice liên quan

| Quyết định | Đề xuất mặc định | Cần xác nhận/evidence trước implementation phụ thuộc |
| --- | --- | --- |
| Personal data có anonymous demo không? | Authenticated user là mặc định | Nếu giữ demo, xác định identity/isolation/route rõ; không tiếp tục user chung lẫn dữ liệu đăng nhập |
| Owner hội thoại cũ | Bổ sung owner business field cho dữ liệu mới | Kiểm tra dữ liệu thực; quarantine/explicit assignment cho legacy chưa biết owner; không suy owner từ audit |
| GET today khi chưa có plan | Read-only, client gọi command initialize riêng | Kiểm tra FE hiện gọi gì; rollout/version nếu GET auto-init đang là contract; BE-only không tự sửa FE |
| Review retry vs review lại | Cùng request chỉ đổi state một lần; lần luyện mới có attempt riêng | Chọn idempotency key/attempt identity và trạng thái item; không mặc định cấm mọi lần review tiếp |
| Rubric đã dùng có được sửa trực tiếp? | Publish version mới cho thay đổi có ý nghĩa chấm điểm | Xác định draft/active/history policy, enabled criteria weights và existing API semantics |
| Audio recording retention | Chỉ hứa retention khi thực sự lưu bytes | Chọn metadata-only hoặc storage implementation, TTL và quyền truy xuất |
| Cache audio có cho nhiều bản cùng key? | Cho versioned entries nếu cần regenerate; lookup newest | Nếu muốn unique active entry, audit duplicate + invalid/deleted handling trước index migration |
| Một hay nhiều API instance? | Không xem semaphore là correctness across processes | Chọn SQL concurrency cho data; session routing/affinity/shared state chỉ khi deployment cần |
| Conversation/message boundary | Giữ root tạm thời, ghi finalized turn qua narrow Application port | Đo tần suất/graph growth; tách message/turn aggregate chỉ khi có lifecycle độc lập |

Chưa có câu trả lời không cản trở review/docs hoặc các phần độc lập. Nó cản việc tự thay contract/schema/nghiệp vụ tương ứng.

## 2. Thứ tự và phụ thuộc

| Phase | Nội dung | Findings | Điều kiện bắt đầu |
| --- | --- | --- | --- |
| R0 | Baseline source/contracts, chốt scope từng slice | Tất cả | Có yêu cầu triển khai cụ thể |
| R1 | Identity và resource ownership | P01, P02, P04, A02 | R0; chốt demo/legacy owner |
| R2 | Domain aggregate structure và Learning write consistency | D01, D03, D04, A01–A03, I01 | R0; dùng identity từ R1 hoặc explicit trusted user contract đã chốt |
| R3 | Vocabulary/Pronunciation invariants, authoritative target/cache | D02, D03, A06, I04, I06 | R0; R2 xác lập mẫu UoW; chốt rubric/cache/storage |
| R4 | Feature contracts và read side | A04, A05 | Sau từng write slice tương ứng; có contract baseline |
| R5 | Infrastructure/realtime lifecycle, persistence policy | I02, I03, I05 | R1 cho access; UoW mẫu từ R2; session decision |
| R6 | API adapters, error/paging conventions | P03, A04 | Các use case/ports cần thiết đã có; behavior changes rollout riêng |
| R7 | Hoàn tất structural moves, docs và acceptance | D01, docs/test gaps | Các slice được chọn hoàn tất, checks liên quan pass |

Không cần một PR khổng lồ cho cả phase. Mỗi slice tách mechanical moves, behavior fixes và schema changes khi có thể; dependency khó tách thì mô tả rõ trong cùng change. P1 có thể được sửa bằng một patch hẹp trước khi cải tổ toàn feature.

## 3. R0 — Baseline và compatibility inventory

Thực hiện read-only trước mutation:

1. Kiểm tra ancestor `AGENTS.md`, `git status`, diff và untracked files. Phân biệt BE với FE cùng Git root. Ghi snapshot/hash những file có thay đổi sẵn trước khi chỉnh cùng file.
2. Lập bản đồ use case: route/hub -> command/query/port -> aggregate -> repository/context -> commit -> response/event.
3. Ghi route/method, request/response fields, status/error shape, paging headers/list-vs-envelope; hub URL/method/event/argument order; CLR entity identities, table/column/index names và migration history.
4. Characterize baseline build và test phù hợp với slice; nếu đã lỗi từ trước, báo riêng, không chữa ngoài scope. Không start API như một cách build vì startup hiện chạy migration/seeding.

Deliverable: change scope cụ thể + compatibility map + các quyết định còn mở liên quan. Không gán claims về SQL/provider từ một lần build.

## 4. R1 — Identity và ownership trước khi mở rộng feature

### R1.1 — Một nguồn learner identity

File chính: `Api/Services/HttpContextCurrentUser.cs`, `Api/Security/JwtAndCsrfMiddleware.cs`, `Api/Program.cs`, `Api/Endpoints/DailyLearningEndpoints.cs`, `VocabularyEndpoints.cs`, `PronunciationEndpoints.cs`.

- Thống nhất personal use cases lấy subject từ principal hợp lệ. Không tin `X-User-Id`/query userId để truy cập data người khác; nếu admin query thay người dùng, biểu diễn thành capability/route riêng có permission rõ.
- Route personal bắt buộc authentication theo decision; anonymous/invalid token có kết quả định nghĩa. Giữ public catalogs nếu chúng là dữ liệu công khai.
- Cùng user đọc today, initialize, submit, progress/history và pronunciation phải dùng cùng identity.
- Tách audit system actor khỏi learner identity; background callback mang actor/session identity được capture khi authorize, không dựa vào ambient HttpContext lúc callback chạy.

Acceptance: hai user A/B không thấy/đổi plan, attempt, progress, assessment của nhau; anonymous/invalid token bị xử lý đúng; header spoofing không thay actor. Cookie refresh/CSRF và admin permission behavior được kiểm tra riêng.

### R1.2 — Conversation ownership và realtime access

File chính: `Domain/Conversations/ConversationEntity.cs`, configurations/migration, conversation use cases/repository; ba hubs và manager.

- Thêm owner ở write model/schema qua migration additive được review. Ghi policy xử lý rows cũ trước backfill. Không reset database hay viết lại initial migration.
- Authorize trước history/update/delete và trước join group/start/stream/grammar/stop; connection chỉ thao tác session đã được cấp quyền.
- Tránh join group trước khi validation/session initialization thành công; định nghĩa lỗi và cleanup khi setup fail.

Acceptance: A không join/list/read/change/stop B dù biết conversation ID; disconnect một connection không làm hỏng connection khác được phép chia sẻ session. SQL migration/legacy data gate cần database disposable hoặc môi trường được cho phép rõ.

### R1.3 — Token contract

File chính: `Infrastructure/Identity/SecurityServices.cs`, API authentication integration.

- Validate required expiry/issuer/audience và cấu hình signing material; không dùng production fallback cứng.
- Dùng TimeProvider cho cả issue/validate. Giữ token/cookie names/role-permission claims cho client nếu không có migration contract.

Acceptance: expired, missing/invalid exp, wrong audience/issuer, bad signature không thành authenticated principal; token hợp lệ vẫn gọi đúng endpoint/hub. Không log raw tokens.

## 5. R2 — Domain-first thiết kế, Learning làm slice mẫu

### R2.1 — Đặt lại ownership trong Domain

- Tạo thư mục theo target: `Learning/DailyVocabularyPlans`, `Learning/UserVocabularyProgress`, `Learning/ReviewAttempts`; đưa DailyVocabularyItem cạnh root. Làm tương tự Identity theo từng slice, không chuyển cả repo một lượt bằng regex rộng.
- Giữ tên `*Entity` ở bước đầu; cập nhật namespace/usings/EF configs/tests/DI callers trong phạm vi chuyển. Giữ mapping SQL/IDs.
- Bổ sung root operations như `CompleteItem(itemId, now)` và guard transition/completion tương ứng. Child local methods chỉ được dùng qua owning aggregate workflow.
- `ReviewRating`/learning date policy là ứng viên có lợi rõ; không biến mọi catalog string thành abstraction mới.

Acceptance: invalid transition không làm root đổi state một phần; duplicate sense item không sinh thêm item; completion phản ánh item state; mapping không đổi table/column/ID do folder move.

### R2.2 — Thống nhất command persistence

File chính: `Common/Interfaces/IAppDbContext.cs`, Learning commands, `IDailyVocabularyPlanRepository`, `IUserVocabularyProgressRepository`, implementation repositories, DI.

- Tạo Application `IUnitOfWork` với commit scoped trên cùng AppDbContext; command repos stage/load state, không auto-commit riêng trong final design.
- Trong giai đoạn chuyển, xác định rõ path nào còn SaveChanges cũ. Không gọi UoW cho một context còn write thực nằm ở factory context khác.
- Submit review load plan có owner/visibility và items cần thiết, load progress, dedup theo attempt/request policy, transition qua domain, stage immutable attempt, commit cùng plan/progress.
- Xác định optimistic concurrency/retry hoặc SQL isolation phù hợp. Unique `(user, date)`, `(user, sense)`, `(plan, sense)` đã có nhưng không tự giải quyết lost update/double submission. Nếu thêm rowversion/idempotency storage, làm migration additive riêng.
- Initialize dùng root.AddItem, chốt carry-over/due/new selection và time zone nhất quán. Không để plan Completed còn nhận thêm Pending items mà status không được reconcile.

Acceptance: retry cùng request không tăng review lần hai; hai requests hợp lệ không lost update; hai last-item completions không để plan InProgress sai; failed commit không lưu nửa transition; race initialize có kết quả xác định thay vì 500 ngẫu nhiên.

### R2.3 — Read-only today

- Tách GetDailyVocabularyPlanQuery khỏi command handler dependency; query không SaveChanges/update target/assign items.
- Command initialize/ensure trả result cần thiết; dùng read projection sau commit nếu cần.
- Chọn rollout cho client đang dựa vào GET auto-init. Nếu compatibility chưa được cho phép đổi, giữ behavior trong adapter/route được ghi rõ tạm thời, không tiếp tục gọi nó là pure query; ghi ngày/phạm vi gỡ bỏ. Mục tiêu cuối vẫn là GET read-only.

Acceptance: snapshot database không đổi trước/sau query, kể cả chưa có plan và target query khác; response chưa có plan được định nghĩa; initialization command vẫn hoạt động theo identity/timezone đúng.

## 6. R3 — Vocabulary và Pronunciation

### R3.1 — Vocabulary aggregate và generation

File chính: `Domain/Vocabulary`, `VocabularyAdminEditor`, generation writer, create/update/import/generate handlers, vocabulary repositories/configs.

- Tập trung root và nested content theo ownership; đảm bảo primary sense/content replacement không bỏ referenced sense IDs. Không tạo repo riêng cho Sense/Example.
- Thống nhất source provenance và editor metadata, inventory string đã lưu trước conversion; không đổi `AdminUpdated` silent bằng enum cast.
- Tách generation orchestration khỏi mapping/storage mechanics khi nó giảm trách nhiệm thực. Giữ automatic và manual import dùng chung validation/persistence, cache key và transaction semantics đã có.
- Generation graph + plan assignment + success audit dùng một commit; failure audit bằng context riêng như hiện tại để tránh flush partial graph. Phân biệt provider success + readback fail với write failure, tránh báo Failed cho một transaction đã commit mà không ghi rõ kết quả.
- Dùng timezone/user learning date thống nhất với Learning; writer hiện hardcode Asia/Ho_Chi_Minh.
- Đánh giá uniqueness normalized word/language qua dữ liệu thực trước thêm unique constraint; mapping hiện chỉ index thường. Reimport idempotency theo sense và concurrency là hai việc khác nhau.

Acceptance: cập nhật primary sense giữ ID/other senses; import lặp không nhân bản sense/item; invalid batch không lưu nửa graph; simultaneous generation có policy rõ; automatic/manual paths dùng cùng luật và không gọi provider trong test parser/import.

### R3.2 — Rubric và assessment

- Validate candidate criteria trước mutation, code/range/weight và enabled totals. Đưa scoring/state consistency vào domain factory/policy.
- Chốt publish/versioning; giữ rubric snapshots trong assessment và không hard-delete lịch sử tham chiếu.
- Dùng authoritative target resolution để kiểm tra vocabulary/sense/example membership trước provider và trước update progress. Legacy free-text có đường riêng theo contract.
- Gọi provider ngoài SQL transaction dài; validate raw result trước tạo domain assessment; short commit recording metadata + assessment + progress nếu cùng consistency requirement.
- Chốt AudioRecording retention. Metadata-only không mang key/label khiến client hiểu là có bytes đọc lại; storage thật cần port và lifecycle implementation riêng.

Acceptance: negative/out-of-range weight/score, duplicate code, mismatched sense/example bị từ chối; invalid update không làm đổi root; completed assessment ghi score snapshots đúng; NotAssessable không giả overall score hợp lệ.

### R3.3 — Synthesis/cache

- Chuẩn hóa targetType/voice/speed/accent/model/text hash một lần ở Application/domain policy; TTS provider không đổi multi-word word thành sentence.
- Giữ cache hit không đòi API key, miss mới đòi key; partial/timeout audio không persist. GET audio chỉ đọc.
- Phân biệt cache best-effort và required persistence; thay catch mọi DbUpdateException bằng handling đúng policy, có logging không lộ key/payload.
- Chọn duplicate/version/active uniqueness policy trước index; DB concurrency gate nếu deploy nhiều instance. In-memory keyed lock vẫn có ích cho chi phí trong một process.

Acceptance: miss/hit cùng metadata cho multi-word; từng key dimension và text change không trả nhầm audio; failure insert được quan sát đúng; multi-instance guarantee chỉ được ghi khi có SQL-backed check tương ứng.

## 7. R4 — Application feature contracts và read side

- Chuyển DTO theo owner: Identity, Learning, Pronunciation, Vocabulary, AiModels, Prompts, Topics, Voices, Conversations. `PagedResultDto` và identity/CQRS abstractions thực sự chung ở Common.
- Tách 6 use cases trong AdminIdentityCrudCommands thành từng folder; giữ request+handler cùng file cho use case nhỏ nếu tiện, không bắt buộc 5 file boilerplate.
- Domain repositories bỏ anonymous object/paging UI contracts. Query dùng named DTO/read abstraction hoặc feature read port; không tạo service pass-through chỉ để đổi folder.
- Tách read interface/factory khỏi write context capability. Cho phép EF LINQ trong Application; root visibility/owner phải nằm trong query predicates khi chạm child tables.
- Batch ListUsers roles/permissions theo user IDs của page; batch vocabulary card results; stable order có ID tie-break. Chỉ chọn split query/SQL-specific optimization sau khi thấy generated SQL/query count cần thiết.
- Tách EF execution khỏi PagedResult result record nếu record đang gom cả shape và data access. Giữ list/envelope + X-* headers trong compatibility map.

Acceptance: API JSON giữ field names/types/null behavior; không còn object/dynamic public use-case response cho migrated features; query không phụ thuộc command; round trips không tăng tuyến tính theo số user/cards trong page; SQL translation được kiểm tra trên provider quan hệ khi query thay đổi đáng kể.

## 8. R5 — Infrastructure/realtime và operational behavior

### R5.1 — Persistence và deletion

- Group configs/repos theo feature/aggregate, giữ central migrations path và assembly identities cho tới khi có lý do chuyển.
- Sửa interceptor chỉ preserve descendants của root bị soft-delete; simultaneous delete child thuộc root khác vẫn persist.
- Child read queries kiểm tra parent visible; catalog deactivate khác soft delete; restore cần xét unique key và trạng thái active theo feature.
- Audit identity capture đúng lúc operation được authorize. Khi callback chạy background, ambient HttpContext không phải nguồn actor đáng tin.

Acceptance: root delete giữ đúng children của nó, unrelated child delete vẫn thực hiện, deleted root không lộ qua child query, created audit không bị overwritten. Test SQL delete/cascade/index semantics trên disposable SQL; EF InMemory không đủ.

### R5.2 — Realtime orchestration và lifecycle

- Application định nghĩa session control port và use-case services; Infrastructure giữ provider websocket/connection/session-state implementations. Không tạo command cho từng audio packet.
- Tách start/stop/access, transcript/turn policy, translation/grammar operations, repository callbacks và notifier.
- Singleton transport/state không capture scoped repo/context. Mỗi persisted event/use case có operation scope/commit boundary; cleanup theo session + connection ownership.
- Stop/disconnect/shutdown cancel debounce, await owned tasks an toàn, dispose bridges/tokens, remove state/API keys khi không còn người sở hữu. Handle setup fail/reconnect/late callback có policy rõ.
- Giữ event order, dedup/sequence và 3 hub protocols; lấy baseline trace trước thay cơ chế.

Acceptance: start-fail cleanup, disconnect/stop không còn tasks/session secrets bị giữ vô hạn; late callbacks không ghi vào deleted/unowned conversation; concurrent connections không stop nhầm; streaming/turn events tương thích.

### R5.3 — Migration, seeding, readiness

- Tách schema migration orchestration với seed coordinator/catalog data. Config quyết định startup migration ở development/deployment; không tự chạy DB update từ refactor script.
- Không suppress pending model mismatch để qua gate; kiểm tra model/snapshot/migration diff trước bỏ suppression, ghi nhận lỗi baseline nếu có.
- Readiness phản ánh schema/database dùng được; liveness chỉ process. Fatal migration failure và optional seed failure có xử lý khác nhau.
- Seeders idempotent theo keys; không overwrite admin edits; hỗ trợ partially seeded database. Tách literal data dài ra khỏi orchestration khi có ích.

Acceptance: startup database unavailable/schema mismatch không báo ready giả; optional seed failure quan sát được; run seed hai lần không duplicate, partial seed chỉ thêm phần thiếu theo policy.

## 9. R6 — API adapters và feature organization

- Thêm DeleteTeacherVoice command và bỏ DbContext factory/write khỏi endpoint; move AI-model routes sang AiModelEndpoints, giữ URLs.
- Hubs chỉ bind/authorize/invoke Application port/map events; composition root vẫn đăng ký Infrastructure adapters.
- Central error mapping có typed outcomes/errors để phân biệt invalid input, not found, forbidden, conflict, provider failure. Giữ `{ error }` legacy ở phase đầu; ProblemDetails là contract change riêng.
- Chuẩn hóa `paged=true` behavior sau compatibility review: một số endpoint hiện chuyển effective page=1, số khác chỉ đổi envelope mà chưa áp default paging. Không tự đổi unpaged clients trong mechanical move.
- Serialization-specific lenient GUID converter có thể chuyển sang API contract adapter; preserve hiện hành trước khi cân nhắc strict input. Việc di chuyển DTO khỏi Common không tự cho phép đổi parsing.

Acceptance: route/verb/body/headers và hub events không đổi ngoài behavior fix được liệt kê; transport không query/mutate EF hay tạo domain workflow; admin/public/personal permission matrix khớp design.

## 10. R7 — Hợp nhất và handoff

- Hoàn tất aggregate folders cho các catalog nhỏ nếu còn cần; không tạo `Entities/Services/ValueObjects` rỗng.
- Xóa abstraction cũ chỉ sau khi callers đã migrate. Không giữ cả generic write DbContext và repository như hai lựa chọn ngang hàng cho code mới.
- Có thể tổ chức test theo category trong project hiện tại trước; chỉ tách project nếu có lợi rõ. Không xóa tests hiện hữu vì tên Domain.Tests chưa chính xác.
- Cập nhật `docs/architecture.md` sau khi đọc/reconcile user changes, rồi root guide/skills khi source thực đã đổi. Review baseline giữ nguyên thời điểm và facts.
- Tóm tắt files/behavior/schema/contracts thay đổi; attach actual check outputs và remaining runtime gates. Không commit/push/deploy ngoài scope được yêu cầu.

## 11. Verification matrix và lệnh dự kiến

**Các lệnh sau là hướng dẫn cho implementation sau này, chưa chạy ở lần review này.** Chạy từ thư mục BE, chỉ chọn checks liên quan tới slice; build restore prerequisites nếu máy chưa có assets. Không chạy API để kiểm tra compile.

```powershell
git status --short -- .
dotnet build .\EnglishLiveTutor.sln --no-restore
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~DomainLogicTests"
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~PersistenceAndAuditingTests"
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~EndpointCqrsTests|FullyQualifiedName~CqrsDependencyInjectionTests"
dotnet test .\tests\EnglishLiveTutor.Domain.Tests\EnglishLiveTutor.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~ManualVocabularyGenerationTests|FullyQualifiedName~PronunciationSynthesisAudioCacheTests"
git diff --check -- .
```

| Slice | Kiểm tra cần có | Điều không được suy ra từ test hiện tại |
| --- | --- | --- |
| Folder/namespace/DI | Product build, scoped DI resolution, diff mappings/contracts | Compile không chứng minh SQL schema hay endpoint behavior |
| Domain | Invalid/valid transitions, state unchanged on failure, deterministic time | Pure tests không kiểm tra EF tracking/transaction |
| Review/init | Owner, retries, simultaneous updates, rollback, GET no-write | Cần bổ sung case; chưa thấy tests cho toàn flow này |
| Persistence | SQL Server constraints, uniqueness, concurrency, deletes/migrations | EF InMemory không thay SQL transaction/concurrency proof |
| Contracts/auth | In-process HTTP host/SignalR tests, two users, cookie/error/paging | Handler tests không đi qua middleware/routes/serialization |
| AI/cache | Fake provider output contracts + provider-adapter parsing/metadata | Cache fake không kiểm tra live websocket/TTS target rewrite |
| Realtime | Lifecycle tests, ordered event traces, cleanup and late callback | Build/DTO tests không chứng minh stream hoạt động |
| Live provider | Chỉ smoke test khi được yêu cầu, dùng credentials an toàn | Seed catalog/model-code string không chứng minh capability thật |

Thay source thì chạy checks thích hợp; tài liệu-only thì kiểm tra links/frontmatter/whitespace và xác minh source hash không đổi. Không thêm broad suites hay live DB/provider calls chỉ để hoàn tất một review.
