# Prompt sửa BE: build, EF Id, phân trang SQL và CQRS

Bạn đang làm việc tại `D:\Projects\EnglishPartner\EnglishPartner.BE`, solution `EnglishLiveTutor.sln`. Hãy thực sự triển khai sửa code BE và xác minh kết quả theo toàn bộ yêu cầu dưới đây, không dừng ở phân tích hay đề xuất.

## 1. Phạm vi và nguyên tắc

- Chỉ sửa BE trong workspace này; không sửa `../EnglishPartner.FE`.
- Đọc AGENTS.md nếu có và `docs/architecture.md`. Kiến trúc là Pragmatic Clean Architecture + DDD + feature-based CQRS, 4 project src và 1 project test. Không thêm generic repository, mediator/framework mới, project hay tầng kiến trúc không cần thiết.
- Có nhiều thay đổi chưa commit, kể cả file untracked của người dùng. Đọc `git status`, giữ nguyên công việc hiện có; không reset, checkout đè, xóa migrations hay tái tạo solution để làm build xanh.
- Giữ target framework hiện tại, các tính năng, routes, HTTP methods, quyền truy cập, auth/cookie, response DTO và semantics nghiệp vụ. Không loại project/file khỏi build, tắt lỗi/analyzer, trả dữ liệu giả, bỏ tính năng hay dùng dynamic để che lỗi.
- Khi cần thay đổi contract BE để sửa đúng nghiệp vụ, cập nhật mọi producer/consumer BE, DI và test liên quan. Giữ JSON tương thích với FE hiện có; ghi rõ thay đổi contract nếu không tránh được.
- Đầu ra build phải có log chỉ chứa lỗi riêng. Không in secrets, connection strings hoặc API keys ra báo cáo.

## 2. Baseline: sửa hết lỗi build, không chỉ hai lỗi đầu tiên

Lần build ngày 2026-10-15 bằng CLI dùng lệnh:

```powershell
dotnet build EnglishLiveTutor.sln --no-incremental --nologo --verbosity minimal '-flp:logfile=build-be.log;verbosity=normal' '-flp1:logfile=build-be-errors.log;errorsonly'
```

Kết quả FAILED ở Domain với 2 CS0246. Các project phụ thuộc chưa biên dịch đầy đủ. Không được kết luận toàn BE chỉ có 2 lỗi. Visual Studio 2026 còn báo 17 lỗi dưới đây (dòng có thể dịch chuyển khi chỉnh sửa):

| Project | File | Dòng | Code | Mô tả |
| --- | --- | --- | --- | --- |
| Domain | Prompts/SpeechSpeedEntity.cs | 8 | CS0246 | Không tìm thấy AggregateRoot<>. |
| Domain | Prompts/SpeechSpeedEntity.cs | 40 | CS1729 | AggregateRoot<Guid> không có constructor nhận 1 argument. |
| Domain | Topics/TopicEntity.cs | 9 | CS0246 | Không tìm thấy AggregateRoot<>. |
| Domain | Topics/TopicEntity.cs | 49 | CS1729 | AggregateRoot<Guid> không có constructor nhận 1 argument. |
| Application | Common/Interfaces/IAiServices.cs | 64 | CS0246 | Thiếu SynthesizePronunciationAudioResponseDto. |
| Application | Common/Interfaces/IAiServices.cs | 65 | CS0246 | Thiếu SynthesizePronunciationAudioRequest. |
| Application | Common/Interfaces/IAppDbContext.cs | 37 | CS0246 | Thiếu SpeechSpeedEntity. |
| Infrastructure | Ai/CefrPromptBuilder.cs | 146 | CS1026 | ) expected. |
| Infrastructure | Ai/CefrPromptBuilder.cs | 146 | CS1002 | ; expected. |
| Api | Endpoints/IdentityEndpoints.cs | 268 | CS1061 | UserEntity thiếu ReplaceRoles. |
| Api | Endpoints/IdentityEndpoints.cs | 284 | CS1061 | UserEntity thiếu MarkDeleted. |
| Api | Endpoints/IdentityEndpoints.cs | 352 | CS1061 | RoleEntity thiếu MarkDeleted. |
| Api | Endpoints/VocabularyEndpoints.cs | 3 | CS0234 | Namespace Application.Common.Mappers không tồn tại. |
| Api | Endpoints/VocabularyEndpoints.cs | 83 | CS0103 | VocabularyFlashcardMapper không tồn tại. |
| Api | Endpoints/VocabularyEndpoints.cs | 92 | CS1061 | VocabularyFlashcardDto thiếu TopicCategory. |
| Api | Endpoints/VocabularyEndpoints.cs | 116 | CS7036 | GenerateVocabularyCommand thiếu argument theo chữ ký (string UserId, GenerateVocabularyRequest Request). |
| Api | Endpoints/VocabularyEndpoints.cs | 150 | CS1503 | Argument 3: string không chuyển được sang Guid?. |

Đọc thêm `be-errors-summary.md` nếu có. Đây là baseline tối thiểu, không phải danh sách đóng. Làm lần lượt Domain → Application → Infrastructure → Api → toàn solution/test project; build lặp sau mỗi nhóm để tìm và sửa các lỗi tiếp theo đến khi tất cả project biên dịch thành công.

Các điểm đã đối chiếu source, phải xử lý tận gốc:

1. `Domain/Common/Primitives/AggregateRoot.cs` đã có `protected AggregateRoot(TId id)`. Hai entity Topic/SpeechSpeed thiếu import `EnglishLiveTutor.Domain.Common.Primitives`; sửa namespace trước rồi kiểm tra lại CS1729, không tạo constructor/base class trùng lặp. `IAppDbContext.cs` đã import Domain.Prompts, nên kiểm tra lại lỗi kéo theo sau khi Domain build được.
2. TopicEntity và SpeechSpeedEntity đang khai báo lại audit và soft-delete properties có sẵn trong Entity/AggregateRoot. Hợp nhất về trạng thái kế thừa, không để property hiding làm EF, interface và domain đọc/ghi các giá trị khác nhau. Giữ nghiệp vụ deactivate khi xóa nếu hiện có, audit actor/time, query filters và khả năng restore.
3. Hai pronunciation DTO đã có trong `Application/Common/Models/AdminCrudDtos.cs`; dùng đúng namespace, không tạo DTO trùng.
4. Sửa string interpolation sai dấu quote trong CefrPromptBuilder tại `Active Topic`, giữ nguyên nội dung topic và logic prompt/fallback.
5. UserEntity có `SetRoles`, base aggregate có `SoftDelete`. Đồng bộ caller với domain API hiện có, tránh thêm aliases chỉ để qua compiler. Kiểm tra cả IdentityEndpoints, VoiceEndpoints, PromptEndpoints và TopicRepository, không chỉ ba dòng người dùng gửi.
6. `PermissionEntity.UpdateDetails` đang được gọi nhưng chưa tồn tại; triển khai cập nhật theo domain validation hiện có và chuyển nghiệp vụ vào command handler phù hợp.
7. `AuditingSaveChangesInterceptor` gọi `SoftDelete` trên `ISoftDeletable`, trong khi interface hiện chỉ khai báo properties. Thống nhất contract và implementation; giữ interceptor hoạt động đúng, idempotent, bảo toàn child entities khi soft-delete, không làm mất các thao tác xóa child độc lập trong cùng SaveChanges.
8. Mapper flashcard được gọi ở nhiều dòng trong VocabularyEndpoints nhưng namespace/class không tồn tại. Helper hiện hữu là `Application/Features/Vocabulary/VocabularyProjectionHelper.MapToFlashcardDto(...)`. Hợp nhất mapping theo một nguồn dùng chung, truyền đúng tham số/semantics (daily item, sense, progress, score); tránh sao chép mapper hoặc query N+1 trên từng card.
9. Với TopicCategory, đối chiếu DTO và cách FE nhận JSON hiện có ở chế độ read-only; quyết định filter trên entity và/hoặc bổ sung DTO field có ý nghĩa. Không thêm property giả chỉ để compile. Đảm bảo tất cả chỗ tạo DTO được cập nhật nếu chữ ký thay đổi.
10. GenerateVocabularyCommand cần UserId và Request. Dùng nguồn định danh theo cơ chế hiện có (`ICurrentUser`/claims hoặc contract hiện hữu), giữ ownership và việc gán vocabulary vào daily plan của đúng user. Không hardcode một user chung chỉ để lấp argument.
11. Đối chiếu mọi lời gọi AddPronunciation, AddWordFamily, AddRelation, AddSense và các factory/domain methods trong CRUD Vocabulary với chữ ký thực tế. Dùng named arguments đúng nghĩa; không cast/parse string sang Guid để che CS1503. Giữ đúng dữ liệu word family, synonyms/antonyms, examples, grammar, IPA và cập nhật aggregate con.

## 3. EF configurations: Id do ứng dụng cung cấp

Rà toàn bộ `src/EnglishLiveTutor.Infrastructure/Persistence/Configurations/*.cs`. Với mỗi entity có khóa Id, thêm trực tiếp trong Configure, ngay sau HasKey:

```csharp
builder.HasKey(e => e.Id);
builder.Property(e => e.Id).ValueGeneratedNever();
```

- Hiện có 33 HasKey(e => e.Id), phân bố trong 9 file: AiModelConfigConfiguration.cs, ConversationConfigurations.cs, IdentityConfigurations.cs, LearningConfigurations.cs, PromptConfigurations.cs, PronunciationConfigurations.cs, TeacherVoiceConfiguration.cs, TopicConfiguration.cs, VocabularyConfigurations.cs. Rà lại thực tế khi làm, không hardcode số 33 trong test.
- Áp dụng cho cả aggregate roots và entity con, cả Guid và string Id (ví dụ TeacherVoiceEntity), không chỉ entity mới.
- Không đặt ValueGeneratedNever cho audit dates hay các property ngoài yêu cầu. Giữ column names, indexes, relationships, delete behavior, audit/soft-delete filters.
- Kiểm tra tất cả đường tạo entity/seed đảm bảo Id hợp lệ được cấp trước Add/Save, không Guid.Empty/null/chuỗi rỗng và không trùng Id.
- Thêm kiểm tra EF metadata để các khóa Id được ứng dụng cấp có ValueGenerated.Never. Chỉ đếm dòng code không đủ xác minh.
- So sánh EF model với snapshot/migrations hiện có. Nếu cần migration, tạo migration tăng dần đúng model và đọc lại diff/script; không sửa lịch sử migration đã có để che thay đổi, không drop/recreate database. Không tự chạy `database update` hoặc seeding vào DB thật trong tác vụ này. Báo rõ migration nào chỉ mới tạo, chưa áp dụng.

## 4. Phân trang phải thực thi tại database

Hai chỗ hiện sai:

- `Api/Endpoints/PaginationEndpointExtensions.cs`: nhận IEnumerable, lọc search, ToList rồi Skip/Take.
- `Application/Common/Models/PagedResultDto.cs`: Create(IEnumerable) cũng tải toàn bộ và phân trang trong bộ nhớ.

Phải chuyển dữ liệu truy vấn page/pageSize/search/filter từ endpoint xuống query handler rồi repository/read abstraction thích hợp; thực thi Where, CountAsync, OrderBy, Skip và Take trên IQueryable được provider dịch sang SQL trước khi materialize. Endpoint chỉ bind request, gọi handler và ghi HTTP response/headers từ kết quả đã tính. Đổi tên/thiết kế hoặc bỏ helper cũ khi hết caller; không để helper DTO tiếp tục là đường phân trang danh sách DB trong bộ nhớ.

Phạm vi: tất cả call site `ToPaginatedOrListResult` trong IdentityEndpoints (users/roles/permissions), TopicEndpoints, VoiceEndpoints, PromptEndpoints (speech-speeds/cefr-levels/modes), PronunciationEndpoints (rubrics và /api/ai-models), ConversationEndpoints và VocabularyEndpoints. Rà thêm assessments/history cùng các list có tham số phân trang hiện hữu để không bỏ sót đường tải toàn bộ. Kiểm tra route thật trong source, không bỏ sót danh sách nào chỉ vì tên không có trong ví dụ.

Yêu cầu cụ thể:

- Giữ các query parameter và response contracts hiện tại: page, pageSize, paged, search và các filters riêng. `paged=true` trả envelope Items/TotalCount/Page/PageSize/TotalPages; dạng list cũ tiếp tục nhận được array. Giữ các headers X-Total-Count, X-Page, X-Page-Size, X-Total-Pages đúng với cùng tập dữ liệu.
- Chốt semantics nhất quán: có page/pageSize hoặc paged=true thì phải phân trang ở DB (paged=true không được trả toàn bộ items kèm pageSize giả); khi không yêu cầu phân trang, có thể giữ list theo contract cũ nhưng query vẫn filter/order ở DB. Ghi rõ default/max pageSize, clamp hoặc validation, hành vi total=0 và page vượt cuối. Không đổi response envelope mặc định ngoài ý muốn.
- TotalCount lấy từ tập query đã filter nhưng trước Skip/Take. Loại soft-deleted records theo filters hiện có. Dùng cùng predicates cho count và page.
- OrderBy ổn định theo thứ tự nghiệp vụ hiện tại, thêm ThenBy(Id) hoặc tie-breaker phù hợp; kiểm tra phép tính offset không overflow. Truyền CancellationToken đầy đủ. Không chạy đồng thời hai EF operations trên cùng DbContext.
- Search các fields đã hỗ trợ (ví dụ roles của user) phải thành biểu thức dịch được sang SQL. Không dùng Func<T,string>, AsEnumerable, ToList hay string.Join trên collection để lọc toàn bộ ở bộ nhớ trước phân trang. Kiểm tra SQL translation và semantics collation/case phù hợp provider.
- Với Vocabulary: hiện có Take(200) trước khi lọc senses/bung flashcards, làm sai tổng và thiếu kết quả. Đơn vị trả về là flashcard theo vocabulary/sense, nên filter/count/order/page đúng đơn vị đó ở DB, gồm cefrLevel và topicCategory. Không page theo vocabulary rồi bung senses thành số items vượt pageSize. Có thể query page IDs trước rồi tải graph chỉ của page và map sau, giữ đúng thứ tự, không N+1.
- Mọi mapping phức tạp sau materialization chỉ xử lý page đã giới hạn, không lấy toàn dataset rồi cắt trang. Dùng projection/batch queries cho role/permission/progress và quan hệ con cần thiết.
- Speech-speeds hiện có fallback gọi DatabaseSeeder.BuildDefaultSpeechSpeeds ngay trong GET khi items rỗng. Đưa nguồn defaults về seeding hiện có thay vì lấy catalog tĩnh rồi phân trang trong bộ nhớ. GET không tự seed/ghi DB; khi filter không khớp hoặc page vượt cuối phải trả tập rỗng với total đúng, không thay bằng defaults. Ghi rõ điều chỉnh này trong báo cáo để tránh che trường hợp DB chưa seed.
- Tận dụng CQRS/abstraction hiện có; Application có thể dùng IAppDbContextFactory cho read queries theo docs hiện tại. Với Identity, thực hiện yêu cầu repository ở phần 5. Không đẩy EF SQL hoặc HttpContext vào Domain; không để Domain repository interface tham chiếu Application DTO/PagedResultDto. Nếu cần DTO projection repository, đặt read abstraction tại Application và implement ở Infrastructure.

## 5. IdentityEndpoints phải đồng nhất CQRS → repository

`src/EnglishLiveTutor.Api/Endpoints/IdentityEndpoints.cs` có 6 endpoint CRUD đang dùng IDbContextFactory<AppDbContext> trực tiếp:

- POST /api/users/: tạo user.
- PUT /api/users/{id:guid}: cập nhật user/profile/password/roles.
- DELETE /api/users/{id:guid}: xóa user.
- DELETE /api/roles/{id:guid}: xóa role.
- PUT /api/permissions/{id:guid}: cập nhật permission.
- DELETE /api/permissions/{id:guid}: xóa permission.

Chuyển các endpoint này sang ICommandHandler với command/handler trong `Application/Features/Identity/Commands/...`. Tái sử dụng `IUserRepository`, `IRoleRepository`, `IPermissionRepository` trong `Domain/Identity/Repositories` và `Infrastructure/Repositories/IdentityRepositories.cs`, bổ sung methods vừa đủ theo use case. Các Identity list/detail queries cũng đi qua repository/read abstraction, bao gồm SQL pagination ở phần 4. IdentityEndpoints không còn tạo DbContext, gọi DbSet/EF, hash password, điều khiển domain mutation hay SaveChanges trực tiếp.

- Dùng các CQRS interfaces sẵn có trong Application/Common/Interfaces/ICqrs.cs; cập nhật registrations trong Application/DependencyInjection.cs và Infrastructure/DependencyInjection.cs, không thêm MediatR.
- Endpoint giữ việc bind request, auth/context, cookie và HTTP result/status mapping. Validation, password hashing qua IPasswordHasher, user normalization, kiểm tra trùng username/email, role/permission validation và domain mutation nằm trong application/domain thích hợp.
- Giữ semantics cập nhật: RoleCodes=null nghĩa là không thay đổi; danh sách rỗng phải được xử lý theo contract thay thế roles hiện có. Giữ rules active/soft-delete, không vô tình gán roles không hợp lệ hay làm mất liên kết ngoài yêu cầu.
- Các fields có trên request phải được xử lý rõ ràng; ví dụ AdminUpdateUserRequest có Username/Email, không được âm thầm bỏ qua nếu contract là cho phép sửa. Đối chiếu model/normalization/domain API và xử lý trùng lặp đúng use case.
- Entity đọc để update phải được persist bởi đúng DbContext theo tracking/lifetime hiện tại. Tránh đọc entity ở factory context A rồi SaveChanges bằng scoped repository context B khiến thao tác không được ghi. User và role links được cập nhật nguyên tử trong cùng unit of work phù hợp.
- Giữ quyền users.read/users.manage/roles.manage, auth flow, response DTO, hành vi not-found/bad-request/no-content và cancellation. Không đổi luồng login/register/refresh/token sang giải pháp khác.
- Soft-delete dùng contract thống nhất ở phần 2; lưu audit actor/time, giữ child links khi aggregate bị soft-delete và loại bản ghi xóa khỏi read mặc định.
- Các endpoint ngoài Identity: chuyển list queries bị ảnh hưởng bởi pagination xuống CQRS; sửa compile và mapping CRUD liên quan. Không nhân cơ hội viết lại toàn bộ auth, realtime hoặc AI provider không liên quan.

## 6. Trình tự xác minh và tiêu chí hoàn tất

1. Ghi baseline, xử lý lỗi gốc rồi build từng tầng để lộ lỗi tiếp theo. Không dừng sau khi sửa 17 dòng đã liệt kê.
2. Chạy build cuối cho cả Debug và Release, tất cả 5 project đều được build, không skip test project:

```powershell
dotnet build EnglishLiveTutor.sln -c Debug --no-incremental --nologo --verbosity minimal '-flp:logfile=build-be-debug.log;verbosity=normal' '-flp1:logfile=build-be-debug-errors.log;errorsonly'
dotnet build EnglishLiveTutor.sln -c Release --no-incremental --nologo --verbosity minimal '-flp:logfile=build-be-release.log;verbosity=normal' '-flp1:logfile=build-be-release-errors.log;errorsonly'
```

3. Không cần chạy test ngoài phạm vi. Chạy các tests phù hợp trong PersistenceAndAuditingTests và IdentityPromptsAndVoicesTests; bổ sung tests có giá trị cho logic thay đổi: metadata ValueGenerated.Never; giữ Id trên insert; soft-delete/audit nhất quán; Identity create/update/delete/roles được persist; phân trang count/filter/order/envelope và biên page/pageSize. Không viết tests chỉ kiểm tra chuỗi source chứa Skip/Take/ValueGeneratedNever.
4. Phân trang SQL phải có bằng chứng relational provider: SQL tạo bởi SQL Server provider qua ToQueryString/command capture hoặc integration test DB test riêng có sẵn. Count và page phải cùng filter; page query có giới hạn thực tế (OFFSET/FETCH hoặc tương đương). EF InMemory tests chỉ chứng minh logic, không chứng minh SQL translation hay việc DB phân trang. Không cần thêm dependency/provider mới nếu có thể kiểm tra SQL từ provider đang dùng. Không truy cập DB production hoặc gọi AI provider thật để xác minh build.
5. Rà registrations và all call sites sau đổi generic result/DTO/query signatures; bảo đảm handlers resolve được theo lifetime hiện có. Không gọi API startup nếu startup tự migrate/seed vào DB thật; ưu tiên kiểm tra DI cô lập.
6. `git diff --check`; đọc lại diff để bảo đảm không có FE changes, edits vào secrets hoặc sửa đổi ngoài phạm vi. Phân biệt các thay đổi vốn có của người dùng với thay đổi trong tác vụ.
7. Nếu Visual Studio còn báo Error (active) sau khi CLI build thành công: đối chiếu lỗi Build/IntelliSense, configuration, SDK và project references thực tế; Rebuild trong VS nếu có quyền truy cập. Không quy mọi lỗi active là cache từ đầu và không khẳng định đã verify VS nếu chỉ chạy CLI.

Chỉ báo hoàn tất khi toàn bộ build errors của BE đã hết và các yêu cầu EF Id, database pagination, Identity CQRS/repository được thực hiện. Nếu bị chặn bởi môi trường/runtime, hoàn thành các bước độc lập còn lại, ghi rõ bước thất bại và bằng chứng; không báo build/test/DB thành công khi chưa chạy.

## 7. Báo cáo cuối bằng tiếng Việt

- Nhóm lỗi gốc đã sửa, file chính và cách khắc phục; nêu các lỗi mới phát hiện sau baseline.
- Lệnh build thực tế, exit code, tổng errors/warnings cho Debug/Release, link tới log errors-only. Log cuối phải chỉ chứa lỗi nếu còn; giữ baseline tách biệt để tránh nhầm lỗi cũ với lỗi hiện tại.
- Bảng endpoint/list đã chuyển sang SQL pagination và Identity commands/queries/repositories đã thêm hoặc sửa.
- Xác nhận coverage ValueGeneratedNever theo EF metadata, tình trạng snapshot/migration nếu có.
- Tests đã chạy, kết quả và giới hạn bằng chứng: source, compile, test logic, SQL translation, live DB/API, Visual Studio. Liệt kê rõ phần chưa xác minh; không đánh đồng các mức này.
