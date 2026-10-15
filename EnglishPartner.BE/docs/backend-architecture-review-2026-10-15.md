# Review kiến trúc backend — 2026-10-15

## Phạm vi và kết luận

Đã rà soát kiến trúc của toàn bộ 4 production project, project tests, project references, DI, persistence mappings và các đường xử lý HTTP/realtime chính. Đây là **review tĩnh trên working tree**, không phải kết luận hệ thống đã chạy đúng trên SQL Server/Google/HTTP thực tế.

Snapshot: Git root `D:/Projects/EnglishPartner`, HEAD `f430bbd`; BE nằm trong `EnglishPartner.BE`. Working tree đã có nhiều thay đổi tracked/untracked trước khi review, gồm pronunciation cache, manual vocabulary generation và cả FE. Các nhận xét bao gồm source đang sửa dở đó. Không sửa source, tests, project/configuration hoặc migrations trong lần này; giữ nguyên `docs/architecture.md` đang có thay đổi của người dùng.

**Kết luận:** BE có nền tảng Clean Architecture đúng về project references và đã có feature-based CQRS. Domain cũng có hành vi thực sự, không chỉ property bags. Tuy nhiên, ranh giới write model, identity/ownership và transaction chưa nhất quán. Chỉ đổi cây thư mục sẽ không giải quyết được các vấn đề này.

| Layer | Phần đang tốt | Khoảng cách chính |
| --- | --- | --- |
| Domain | Project không có package/framework dependency; private setters, backing collections; `AddSense`, SRS, token rotation | Area folder chưa thể hiện rõ từng aggregate; child write paths bypass root; primitive/status/lifecycle chưa nhất quán |
| Application | Có `Features/*/Commands` và `Queries`, handler interfaces đơn giản | Query gọi command; command dùng nhiều kiểu persistence; DTO/domain/object trộn; common files chứa nhiều feature |
| Infrastructure | Một DbContext, mappings riêng, auditing, specialized repositories, provider ports | Context/commit ownership khác nhau giữa repository; realtime manager gộp trách nhiệm; soft delete và cache error handling có vấn đề |
| API | Phần lớn endpoint dispatch handler, có RBAC cho admin CRUD | Personal identity thiếu thống nhất; hubs gọi concrete Infrastructure; một endpoint còn ghi EF trực tiếp |

Ưu tiên: identity/ownership và tính đúng khi ghi -> query/command semantics và commit -> aggregate boundaries -> folder/contract cleanup. Không cần thay framework hay tách thêm service để đạt mục tiêu.

## Coverage và giới hạn bằng chứng

| Khu vực | Inventory C# | Nội dung kiểm tra |
| --- | ---: | --- |
| Domain | 54 files / 2.887 dòng | Base types; 19 class kế thừa AggregateRoot; entities con; repository contracts; các area Vocabulary, Learning, Identity, Conversations, Pronunciation, Prompts, AiCatalog, Topics, Voices |
| Application | 87 / 5.477 | CQRS contracts, DI, Common DTO/ports; toàn bộ feature inventory/dependencies; đọc sâu review/init, generation/import, pronunciation assess/synthesize, identity và conversation flows |
| Infrastructure | 49 / 19.154 | Bao gồm 7 migration/snapshot files; project refs/DI, repository surfaces, EF config/filter/index, interceptor, seeding orchestration, provider adapters và realtime lifecycle |
| API | 18 / 1.867 | Program, mọi endpoint file, 3 hubs, notifier, middleware/current-user/permission adapters |
| Tests | 8 / 1.856 | Inventory test cases và cách thiết lập; Domain, auditing, CQRS handlers/DI, manual generation, cache, identity/seed coverage |

Inventory/line count loại `bin/obj`; Infrastructure count bao gồm generated migration designers/snapshot. Review kiến trúc quét toàn bộ dependency/mutation surfaces và đọc sâu các đường rủi ro; không phải audit từng dòng seed data/generated migration hoặc kiểm định chất lượng ngôn ngữ của mọi prompt. Không chạy build, test, API, SQL migration/seeder, Google calls hoặc concurrency/load tests. Không xác minh model code nào thực sự tồn tại/hỗ trợ capability ở provider.

Tests đang dùng EF InMemory và nhiều test gọi handler trực tiếp. Tên `EndpointCqrsTests` không chứng minh HTTP routing/auth/payload. Test project reference Domain + Application + Infrastructure, chưa reference API; chưa thấy host-based HTTP/SignalR tests trong snapshot. Không đưa ra số test pass/fail vì chưa chạy.

Mức độ: **P1** cần ưu tiên vì sai dữ liệu/quyền truy cập hoặc che lỗi; **P2** làm rõ ranh giới/khả năng bảo trì và hiệu năng; **P3** tổ chức/tài liệu. Mỗi mục phân biệt source fact với hậu quả suy ra cần runtime xác nhận.

## 1. Domain — entity và aggregate

### D01 — P2: Có aggregate behavior nhưng folder hiện là area-centric nhiều hơn aggregate-centric

[Learning](../src/EnglishLiveTutor.Domain/Learning/DailyVocabularyPlanEntity.cs#L6) đặt plan, progress và review attempt cạnh nhau; [Pronunciation](../src/EnglishLiveTutor.Domain/Pronunciation/ScoringRubricEntity.cs#L6) đặt rubric, assessment, recording, generated audio chung một area/Entities. Child folder chung không thể hiện rõ entity thuộc root nào khi một area có nhiều root. Identity có tình trạng tương tự. Vocabulary một root lớn với các children hiện đã tương đối rõ.

Hướng đề xuất: `Domain/<Area>/<Aggregate>/RootEntity.cs`, `Entities/`, repository contract cạnh root; area chỉ có một root đơn giản có thể giữ phẳng. Đây là cải thiện discoverability, không phải lỗi compile hay lý do để tạo repository cho mọi entity. [Target structure](../.agents/architecture.md#2-domain-structure) có bảng ownership cho toàn bộ nhóm hiện tại.

### D02 — P1: Rubric/assessment chưa bảo vệ đầy đủ miền giá trị

[ScoringRubricCriterionEntity:41](../src/EnglishLiveTutor.Domain/Pronunciation/Entities/ScoringRubricCriterionEntity.cs#L41) gán thẳng weight/min/max. [ValidateWeights:99](../src/EnglishLiveTutor.Domain/Pronunciation/ScoringRubricEntity.cs#L99) chỉ kiểm tra có criteria và tổng bằng 100, không loại weight âm, code trùng hoặc min > max. Ví dụ hai weights `-20` và `120` vượt qua tổng 100. [UpdateRubric:89](../src/EnglishLiveTutor.Domain/Pronunciation/ScoringRubricEntity.cs#L89) clear state trước khi validate; lỗi để object trong bộ nhớ ở trạng thái đã thay đổi.

[GeminiPronunciationAiAssessor:145](../src/EnglishLiveTutor.Infrastructure/Ai/GeminiPronunciationAiAssessor.cs#L145) đọc raw score từ JSON; [assessment handler:152](../src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommand.cs#L152) dùng score tính weighted score, còn [score entity](../src/EnglishLiveTutor.Domain/Pronunciation/Entities/PronunciationCriterionScoreEntity.cs#L26) không validate range/consistency. Source cho thấy provider output có thể đưa score ngoài range vào tính tổng; chưa tái hiện bằng provider thật.

Đề xuất: validate toàn bộ bộ criteria trước khi thay state; unique code, weight/range, enabled-weight policy; root/policy tạo assessment kiểm soát score và trạng thái. Chốt version/publication policy riêng: hiện update criteria tại chỗ mà không tăng `Version`; assessment đã giữ description/weight snapshots nên không kết luận lịch sử mất toàn bộ dữ liệu.

### D03 — P2: Các giá trị nghiệp vụ bị biểu diễn và normalize khác nhau

[VocabularySourceType](../src/EnglishLiveTutor.Domain/Vocabulary/Enums/VocabularySourceType.cs#L3) có Seeded/AI/Imported/Manual, nhưng [VocabularyEntity:165](../src/EnglishLiveTutor.Domain/Vocabulary/VocabularyEntity.cs#L165) ghi `AdminUpdated` vào string `SourceType`. Review rating/status/CEFR/target cũng là string; [ApplyReview](../src/EnglishLiveTutor.Domain/Learning/UserVocabularyProgressEntity.cs#L55) nhận giá trị lạ và rơi về Good.

Hệ quả: enum không còn đại diện đủ tập giá trị lưu; lỗi chính tả có thể biến thành hành vi hợp lệ khác. Không đổi ngay dữ liệu cũ. Chốt semantics của source provenance so với last-editor; dùng enum/value object ở chỗ loại bỏ divergence, mapping giữ nguyên wire/storage strings trong phase cấu trúc.

### D04 — P2: Base aggregate đang áp chung audit/deletion cho các lifecycle khác nhau

[AggregateRoot](../src/EnglishLiveTutor.Domain/Common/Primitives/AggregateRoot.cs#L5) luôn `ISoftDeletable`; cả review attempt, generation audit, audio cache đều kế thừa. [Generation audit](../src/EnglishLiveTutor.Domain/Vocabulary/VocabularyGenerationAuditEntity.cs#L5) được mô tả append-only nhưng có `MarkCompleted`, `MarkFailed`, thừa hưởng delete/restore. Cách writer tạo rồi hoàn tất trước insert có thể phù hợp một result record, nhưng base API không thực thi tính bất biến đó.

[Entity:10](../src/EnglishLiveTutor.Domain/Common/Primitives/Entity.cs#L10), [Conversation:46](../src/EnglishLiveTutor.Domain/Conversations/ConversationEntity.cs#L46), [ResolveLocalLearningDate:82](../src/EnglishLiveTutor.Domain/Learning/DailyVocabularyPlanEntity.cs#L82) tự lấy giờ/timezone; interceptor lại lấy giờ qua TimeProvider. Điều này làm business time khó kiểm soát và trộn presentation title với local machine clock.

Đề xuất: phân biệt Entity, AggregateRoot, deletion capability và immutable history theo lifecycle thực; truyền `now`/learning date vào transition. Không cần ép mọi string thành value object hay thêm domain events ngay.

## 2. Application — CQRS và quyền sở hữu write model

### A01 — P1: Query `/today` thực chất thực thi command ghi dữ liệu

[GetDailyVocabularyPlanQuery:25](../src/EnglishLiveTutor.Application/Features/Learning/Queries/GetDailyVocabularyPlan/GetDailyVocabularyPlanQuery.cs#L25) gọi `InitializeDailyVocabularyPlanCommand`. [Initialize handler:51](../src/EnglishLiveTutor.Application/Features/Learning/Commands/InitializeDailyVocabularyPlan/InitializeDailyVocabularyPlanCommand.cs#L51) thêm plan, update target, thêm carry-over/review/new items và gọi SaveChanges ở nhiều bước.

GET có thể thay state khi refresh/prefetch và hai GET đầu tiên có thể tranh unique `(UserId, LearningDate)`. Query name/HTTP verb che khuất side effects. Cần read-only query và command ensure/init riêng; vì client hiện có thể dựa vào auto-init, thay đổi này cần compatibility rollout, không chỉ chuyển file.

### A02 — P1: Submit review không kiểm tra owner và chưa có retry/atomicity boundary đầy đủ

[SubmitVocabularyReview:34](../src/EnglishLiveTutor.Application/Features/Learning/Commands/SubmitVocabularyReview/SubmitVocabularyReviewCommand.cs#L34) chỉ tìm item theo ID; không join plan để xác minh `plan.UserId == command.UserId`/plan còn visible. Sau đó lấy hoặc tạo progress của user command nhưng complete item của plan bất kỳ. Caller có item ID của người khác có thể tạo tiến độ cho một user và đổi item của user khác; đó là kết luận từ source, chưa gửi request khai thác.

Cùng handler luôn [ApplyReview và thêm attempt:55](../src/EnglishLiveTutor.Application/Features/Learning/Commands/SubmitVocabularyReview/SubmitVocabularyReviewCommand.cs#L55), không có request dedup/completed-state gate. Một retry có thể tăng count/mastery lần nữa. [Commit:67](../src/EnglishLiveTutor.Application/Features/Learning/Commands/SubmitVocabularyReview/SubmitVocabularyReviewCommand.cs#L67) lưu item/progress/attempt, rồi truy vấn và commit plan completion lần hai; lỗi giữa hai lần hoặc hai request hoàn tất items đồng thời có thể để plan status lệch items.

Đề xuất: check owner trước mutation; xác định retry key và trường hợp review lại hợp lệ; root hoàn tất item/plan; commit cùng progress/attempt với concurrency strategy. Không tự quy định mỗi item chỉ được review một lần nếu sản phẩm muốn luyện lại.

### A03 — P2: Write model có hai lối truy cập, aggregate methods bị bypass

[InitializeDailyVocabularyPlan:78](../src/EnglishLiveTutor.Application/Features/Learning/Commands/InitializeDailyVocabularyPlan/InitializeDailyVocabularyPlanCommand.cs#L78) tự `new DailyVocabularyItemEntity`/Add DbSet dù [plan có AddItem](../src/EnglishLiveTutor.Domain/Learning/DailyVocabularyPlanEntity.cs#L48). Submit review gọi `item.Complete` trực tiếp. [AssessPronunciation](../src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommand.cs#L16) dùng factory context để ghi recording/assessment/progress, trong khi các catalog command dùng repository.

`IDailyVocabularyPlanRepository`, `IUserVocabularyProgressRepository`, `IPronunciationAssessmentRepository` được đăng ký nhưng các workflow write chính này không dùng chúng. Đây là abstraction chưa trở thành đường chính, không phải lý do để thêm abstraction thứ ba. Chọn repository + UoW cho command; query dùng read projection. EF trong Application **không tự nó là sai** với lựa chọn pragmatic này.

### A04 — P2: Contract boundaries làm mất tính rõ ràng của feature

[IConversationRepository:7](../src/EnglishLiveTutor.Domain/Conversations/Repositories/IConversationRepository.cs#L7) trả `List<object>`/paged object; [GetConversationHistoryQuery:6](../src/EnglishLiveTutor.Application/Features/Conversations/Queries/GetConversationHistory/GetConversationHistoryQuery.cs#L6) trả object nhưng thực tế là domain entity. Nhiều prompt/rubric/model command/query trả Domain entities ra API. Đổi aggregate có thể vô tình đổi JSON/audit fields; object làm contract khó kiểm tra tĩnh.

[Common/Models/AdminCrudDtos](../src/EnglishLiveTutor.Application/Common/Models/AdminCrudDtos.cs#L49) gộp TTS + vocabulary + AI models + identity; [Common/Models/VocabularyDtos](../src/EnglishLiveTutor.Application/Common/Models/VocabularyDtos.cs#L74) còn chứa daily plan, review và pronunciation. [AdminIdentityCrudCommands](../src/EnglishLiveTutor.Application/Features/Identity/Commands/AdminManageIdentity/AdminIdentityCrudCommands.cs#L9) gộp 6 use cases. Common không còn chỉ chứa thứ dùng chung.

Đề xuất named DTO theo feature/use case, explicit mapping giữ JSON cũ, tách multi-command file. Giữ handler interfaces hiện tại; manual DI registrations dài nhưng rõ, chưa có bằng chứng cần MediatR/assembly scanning.

### A05 — P2: Read projections có N+1 và tải graph lớn

[ListUsers:46](../src/EnglishLiveTutor.Application/Features/Identity/Queries/ListUsers/ListUsersQuery.cs#L46) gọi [ResolveUserRolesAndPermissionsAsync](../src/EnglishLiveTutor.Application/Features/Identity/IdentityProjectionHelper.cs#L10) trong vòng lặp. Với user có roles, helper chạy tới 4 query/user; page N users có thêm tới 4N round trips ngoài query count/page. [Generation writer:197](../src/EnglishLiveTutor.Application/Features/Vocabulary/VocabularyGenerationWriter.cs#L197) chiếu flashcard từng sense; [projection helper:19](../src/EnglishLiveTutor.Application/Features/Vocabulary/VocabularyProjectionHelper.cs#L19) tải nhiều collections của toàn vocabulary và query tiến độ/điểm.

Đề xuất batch page data/DTO projection, stable ordering có tie-break ID. Chưa đo execution plan/latency; multiple Includes có nguy cơ join multiplication, chưa kết luận cần AsSplitQuery cho mọi truy vấn.

### A06 — P1: Target semantics khác nhau giữa resolver, assessor và TTS adapter

[PronunciationTargetResolver:79](../src/EnglishLiveTutor.Application/Features/Pronunciation/PronunciationSynthesisResolution.cs#L79) giữ explicit word/sentence, kiểm tra sentence thuộc sense/vocabulary. [Assessor:82](../src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommand.cs#L82) có logic riêng, lấy examples trên mọi sense, cho fallback ReferenceText khi không tìm thấy, và không xác minh `req.SenseId` thuộc vocabulary trước ghi progress.

[TTS adapter:43](../src/EnglishLiveTutor.Infrastructure/Ai/GeminiLivePronunciationTtsSynthesizer.cs#L43) tự đổi bất kỳ text có dấu cách thành sentence. Ví dụ vocabulary `look forward to` targetType=word: provider response trả sentence, nhưng [cache writer](../src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/SynthesizePronunciationAudio/SynthesizePronunciationAudioCommand.cs#L106) lưu resolved word. Miss/hit có thể khác metadata và prompt. Cache test multi-word đang dùng fake synthesizer nên không kiểm tra nhánh adapter thật này.

Đề xuất một authoritative target/profile policy, use-case-specific rules rõ cho text-only legacy path. Provider chỉ chuyển request chuẩn hóa thành protocol, không tự phân loại lại business target.

## 3. Infrastructure — persistence, adapters và vận hành

### I01 — P2: Repository có các semantics context/commit khác nhau

[User/Vocabulary repositories](../src/EnglishLiveTutor.Infrastructure/Repositories/IdentityRepositories.cs#L10) dùng scoped AppDbContext và SaveChanges; [TopicRepository:112](../src/EnglishLiveTutor.Infrastructure/Repositories/TopicRepository.cs#L112) mở/đóng context mỗi method, Upsert lại tìm row và commit; [ConversationRepository:14](../src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs#L14) là singleton, context per operation, vừa tạo/update root vừa upsert children vừa projection UI.

Handler không thể suy ra mọi repository tham gia cùng UoW. Đặc biệt khi chuyển Learning sang repos phải tránh gọi SaveChanges ở từng repo khiến atomicity vẫn sai. Đề xuất scoped write repo + một Application UoW; background/realtime callback mở operation scope riêng. Không đổi singleton sang scoped mà để manager singleton capture nó.

### I02 — P1: Soft-delete interceptor có thể hủy các child deletion không liên quan

[AuditingSaveChangesInterceptor:67](../src/EnglishLiveTutor.Infrastructure/Persistence/Interceptors/AuditingSaveChangesInterceptor.cs#L67) thấy ít nhất một root Deleted thì quét **mọi** tracked entity Deleted không implement ISoftDeletable và đổi thành Unchanged. Nó không kiểm tra child thuộc root nào.

Ví dụ một UoW remove root A và xóa example của vocabulary B: example B cũng bị giữ lại. Cần giới hạn theo quan hệ của đúng root/descendants, hoặc thiết kế explicit root soft-delete tránh blanket state rewrite. Đây là source-confirmed conditional behavior; chưa chạy scenario trên SQL.

Root [query filter](../src/EnglishLiveTutor.Infrastructure/Persistence/Configurations/EntityConfigurationExtensions.cs#L49) chỉ áp trên root; direct child DbSets không tự được ẩn theo parent. Các read/write paths phải kiểm tra parent visibility; A02 là một đường hiện chưa làm vậy.

### I03 — P2: Realtime manager gộp use-case orchestration và thiếu cleanup toàn session

[GeminiLiveConnectionManager](../src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveConnectionManager.cs#L37) dài 926 dòng: websocket setup, turn numbering, transcript merge, debounce translation/grammar, repository writes, broadcasting và giữ API keys. Việc orchestration nằm ở concrete Infrastructure góp phần làm hub không đi qua Application.

[StopAllForConversationAsync:898](../src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveConnectionManager.cs#L898) chỉ remove/dispose bridges; không xóa `_turnStates`, `_sessionApiKeys`, cancel/dispose toàn bộ debounce tokens. Hubs không override disconnect cleanup. Source cho thấy state/key còn được singleton giữ sau stop; chưa đo mức tăng memory. [Bridge.DisposeAsync](../src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveSessionBridge.cs#L260) cũng chưa join receive task trước dispose tài nguyên; cần lifecycle tests trước sửa.

Tách Application session service/control workflow, provider bridge, state adapter, persistence callback và notification port; giữ streaming transport nhanh. Session shutdown/disconnect cần cancellation + bounded ownership/cleanup, không thêm event bus chỉ để tách file.

### I04 — P1: Cache write nuốt toàn bộ DbUpdateException với giả định unique conflict không có trong mapping

[Synthesize handler:130](../src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/SynthesizePronunciationAudio/SynthesizePronunciationAudioCommand.cs#L130) catch mọi DbUpdateException, comment nói unique/concurrent insert conflict, rồi trả response thành công. [Audio lookup index:163](../src/EnglishLiveTutor.Infrastructure/Persistence/Configurations/PronunciationConfigurations.cs#L163) không `.IsUnique()`; in-memory keyed lock chỉ bảo vệ một process.

Trên nhiều instance, duplicate rows vẫn có thể xuất hiện; các lỗi write khác cũng bị che. Repository chọn newest nên duplicates không tự chứng minh trả sai audio; đây là sai guarantee/observability, không phải kết luận cache hiện hỏng. Chốt cache có cho phép nhiều version cùng key hay không; nếu cache best-effort thì log failure có phân loại, nếu unique thì constraint + conflict handling cụ thể. Không thêm unique index trước audit dữ liệu trùng.

### I05 — P1: Startup có thể báo healthy khi migration/seeding thất bại

[DatabaseSeeder:21](../src/EnglishLiveTutor.Infrastructure/Persistence/DatabaseSeeder.cs#L21) chạy `MigrateAsync`; [Program:58](../src/EnglishLiveTutor.Api/Program.cs#L58) catch mọi lỗi, log skipped/deferred rồi tiếp tục start. [Health endpoint](../src/EnglishLiveTutor.Api/Endpoints/HealthEndpoints.cs#L7) luôn trả healthy, không kiểm tra readiness SQL/schema. [DI:42](../src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs#L42) còn bỏ qua PendingModelChangesWarning.

Nếu schema chưa dùng được, process vẫn serve với health green. Cần tách liveness/readiness và migration/seed failure policy. Seeder 1.465 dòng trộn schema startup và catalog/identity/sample data; nhiều catalog seed theo Any() toàn bảng nên database partially seeded cần được xét riêng. Không suy ra database thực đang lỗi vì chưa kết nối.

### I06 — P2: AudioRecording mô tả retained recording nhưng chỉ lưu metadata

[Assess handler:119](../src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommand.cs#L119) tạo `StorageKey = mem://recordings/...`, ExpiresAt +30 ngày, Retained, nhưng chỉ dùng audioBytes để lấy size và gửi provider. Không thấy audio storage write/retention worker trong source inventory.

Vì vậy không có bằng chứng có thể đọc lại recording theo key. Chốt recording cần giữ bytes hay chỉ metadata; nếu cần giữ, bổ sung Application storage port/Infrastructure adapter và lifecycle. Không tự thêm blob provider vào một lần đổi cấu trúc. Generated TTS audio là luồng khác, hiện thực sự lưu bytes trong SQL.

## 4. API — transport, identity và contracts

### P01 — P1: Learner identity đọc/ghi không thống nhất

[DailyLearningEndpoints:24](../src/EnglishLiveTutor.Api/Endpoints/DailyLearningEndpoints.cs#L24) dùng currentUser cho GET today, nhưng [initialize/review/progress/history](../src/EnglishLiveTutor.Api/Endpoints/DailyLearningEndpoints.cs#L38) hardcode `default-learner`. [HttpContextCurrentUser:27](../src/EnglishLiveTutor.Api/Services/HttpContextCurrentUser.cs#L27) còn fallback `X-User-Id` rồi default learner; middleware chỉ gắn principal khi token valid chứ không bảo vệ mọi personal route.

Ví dụ user U tạo plan qua GET today rồi review: item U bị complete nhưng progress/attempt ghi cho default learner; lần đọc U không thấy tiến độ đúng. Pronunciation history và flashcard còn nhận userId query từ caller. Cần claims-based identity nhất quán, authentication cho personal operations và ownership trong use case. Anonymous demo nếu cần phải là mode có isolation được định nghĩa riêng.

### P02 — P1: Conversations và SignalR group/session thiếu resource access boundary

[ConversationEntity](../src/EnglishLiveTutor.Domain/Conversations/ConversationEntity.cs#L6) chưa có owner business field; [list/history repository](../src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs#L34) không lọc user. [Tutor hub:15](../src/EnglishLiveTutor.Api/Hubs/TutorConversationHub.cs#L15), Transcribe và Translate join group theo ID do caller gửi và gọi manager; không thấy authorize/ownership check. Stream/stop cũng nhận conversation ID, không đối chiếu connection membership/owner.

Với deployment có thể truy cập các routes/hubs, biết ID có thể đọc/thay/stop session của người khác; chưa thực nghiệm. Cần ownership model + connection/session authorization trước join/start/stream/stop. Dữ liệu cũ không được tự gán owner từ CreatedBy vì audit không chắc là chủ sở hữu. Nếu app chủ ý shared single-user, phải ghi rõ product constraint; hiện source có identity multi-user nhưng conversation chưa theo cùng mô hình.

### P03 — P2: API vẫn có EF write và concrete provider orchestration

[VoiceEndpoints:87](../src/EnglishLiveTutor.Api/Endpoints/VoiceEndpoints.cs#L87) inject factory, query voice, SoftDelete và SaveChanges ngay endpoint; chưa có DeleteTeacherVoice command. Ba hubs inject concrete GeminiLiveConnectionManager. `PronunciationEndpoints` còn map AI model CRUD bằng permission rubrics.manage.

Đề xuất đưa voice deletion vào command và hubs dùng Application port; tách endpoint AI models theo feature. Việc Api project reference Infrastructure tại composition root là đúng; điểm sai là business dispatch trong transport phụ thuộc concrete adapter. Rà lại permission mapping AI-model admin theo quyền sản phẩm thực sự cần, không tự đổi claim names trong folder move.

### P04 — P1: Custom token validation chưa kiểm tra đầy đủ token contract

[HmacJwtTokenService:128](../src/EnglishLiveTutor.Infrastructure/Identity/SecurityServices.cs#L128) kiểm tra signature và chỉ check exp khi claim tồn tại/parse được; không yêu cầu exp hợp lệ, không validate issuer/audience dù lúc phát có ghi chúng. Constructor còn có signing-key fallback cứng; không chép giá trị đó vào tài liệu này. Đây là behavior của validator, không có bằng chứng token/key bị khai thác.

Đề xuất một authentication boundary kiểm tra đầy đủ token contract và configuration hợp lệ; dùng TimeProvider nhất quán. Chọn framework auth handler trong slice identity nếu phù hợp, giữ wire/cookie/permission behavior có kiểm chứng. Không gộp thay JWT implementation với chuyển namespace Domain.

## 5. Những phần nên giữ và tránh làm quá

- Giữ 4 projects, lightweight CQRS interfaces, Minimal API, một SQL database và các provider ports đã có.
- Giữ encapsulation/backing-field mappings, SRS behavior, token transitions và explicit pronunciation target checks đang có; sửa đường gọi vượt ranh giới.
- Một transaction ghi nhiều aggregate trong cùng SQL là lựa chọn pragmatic hợp lệ khi cần atomicity; không áp luật một aggregate/transaction một cách máy móc.
- EF Core projection ở Application query là quyết định có thể chấp nhận. Tách capability đọc/ghi quan trọng hơn xóa sạch mọi `using Microsoft.EntityFrameworkCore`.
- Không thêm generic repository, mediator/event bus, outbox, database đọc riêng hoặc bounded-context projects chỉ để đủ tên pattern.
- `docs/architecture.md` hiện mô tả trạng thái tốt hơn source: nói commands đi qua repositories, API mỏng; còn liệt kê migrations không trùng inventory hiện tại. Chưa sửa file vì có thay đổi sẵn của người dùng. Khi refactor thực hiện xong mới cập nhật nó từ evidence.

## 6. Hướng xử lý

[Kế hoạch refactor](backend-refactor-plan.md) chia implementation thành các slice có preconditions, file targets, contract/schema risks và acceptance gates. [Architecture decisions](../.agents/architecture.md) giải thích aggregate map và layout đích. Tài liệu này là baseline review, không phải chứng nhận đã sửa các findings.

## 7. Kiểm tra artifact thực tế trong lần review

- `quick_validate.py`: cả 6 project skills hợp lệ.
- Kiểm tra 10 Markdown artifacts mới: 93 local links tồn tại, line references trong phạm vi file; code fences cân bằng, không trailing whitespace hay ký tự replacement lỗi encoding.
- `git diff --check -- .`: không có whitespace errors; Git có cảnh báo LF/CRLF trên các file đã thay đổi sẵn, không chuyển line endings trong lần này.
- SHA-256 trước/sau: 224 files trong `src`/`tests` ngoài `bin/obj` không có file thêm/xóa/đổi nội dung; hash `docs/architecture.md` cũ cũng không đổi.
- Build/tests/runtime/SQL/provider: không chạy. Các checks trên xác nhận artifact hướng dẫn và phạm vi không sửa source, không xác nhận implementation tương lai đã đạt acceptance.
