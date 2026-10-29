# Plan: Lộ trình ngữ pháp A1 → C2

- Mã task: `20261029-grammar-roadmap`
- Ngày: 2026-10-29
- Liên quan: Luật 1, 2, 4, 5, 6, 8, 9, 10, 11; ADR-0001 (MediatR), ADR-0003 (BYO key), ADR-0005 (lỗi). Khung nội dung: [curriculum.md](curriculum.md).
- **Cần người dùng duyệt:** Có — feature mới, migration thêm bảng, contract HTTP mới, permission mới.
- Trạng thái: Đã duyệt 2026-10-29 — hoàn tất G1–G6, đã áp vào working tree chính, chờ commit (xem [report.md](report.md))

## Quyết định đã chốt với người dùng (2026-10-29)

1. Lộ trình theo level A1 → C2; mỗi level gồm nhiều step; mỗi step có **Lý thuyết** và **Bài tập**. Đề xuất 100 step: A1 20, A2 20, B1 18, B2 16, C1 14, C2 12 (chi tiết ở `curriculum.md`, người dùng có thể sửa).
2. **Độc lập với Lesson** về dữ liệu (bảng, aggregate riêng, không khóa ngoại sang Lesson). Dùng lại `ExerciseType` và "engine" bài tập (schema ContentJson/GradingDefinitionJson, grader, sanitizer, player FE).
3. **Lý thuyết chỉ admin tạo/sửa** (có nút AI soạn nháp cho admin). User chỉ generate thêm bài tập.
4. **Mở khóa tuần tự + test nhảy cóc**: đạt step trước mới mở step sau; có bài kiểm tra level để mở cả level.
5. **Nội dung ban đầu**: seed lý thuyết 20 step A1 + bộ bài core trong repo; level khác admin tạo dần bằng AI trong app.
6. Lý thuyết **tiếng Việt + ví dụ tiếng Anh kèm dịch**.
7. Bài tập user generate: **Private** (chỉ chủ thấy) hoặc **Public** (chờ admin duyệt; Approved → mọi người dùng được; Rejected → chỉ chủ thấy kèm lý do).

## Quy tắc nghiệp vụ (đề xuất, chốt khi duyệt plan)

| Quy tắc | Giá trị |
| --- | --- |
| Step mở sẵn | A1-01 |
| Đạt step | Điểm trung bình **bộ bài core** ≥ 70% và đã nộp ≥ 1 bài mức 3 (nếu step có) |
| Mở step sau | Khi step trước Passed |
| Test level L | Làm được khi L = A1 hoặc level L-1 đã Passed. Đề = 20–30 bài random từ core các step của L (cố định khi bắt đầu attempt) |
| Đạt test level | ≥ 80% → mọi step của L thành Passed (`PassedBy = LevelTest`), mở step đầu level sau |
| Bài cộng đồng/private | Chỉ để **luyện thêm**, không tính điều kiện đạt (tránh tự tạo bài dễ để qua step) |
| Bài mức 3 (Translation/Writing) | Chấm AI rubric `LESSON_TRANSLATION`/`LESSON_WRITING` khi có key; không có key → hiện đáp án mẫu + tự đánh giá, tính là "đã nộp" nhưng không tính điểm |
| Sửa bài Public đã Approved | Quay về Pending, ẩn khỏi người khác tới khi duyệt lại |
| Giới hạn | User: ≤ 10 bài/lần generate, ≤ 40 bài generate/ngày, ≤ 30 bài Pending cùng lúc; rate limit policy AI hiện có |
| Trùng lặp | `ContentHash` (SHA-256 nội dung chuẩn hóa) duy nhất trong phạm vi step cho bài Official/Approved và cho bài của cùng owner |

## Hiện trạng đã xác minh

- `Domain/Lessons/Enums/ExerciseType.cs`: 16 loại; `GradingMode` (SelfAssessment/Deterministic/AiRubric/Composite).
- `Infrastructure/Lessons/DeterministicExerciseGrader.cs`: `Grade(type, contentJson, gradingJson, answerJson, maxPoints)` — không phụ thuộc Lesson; hỗ trợ MultipleChoice, FillInTheBlanks, Matching, SentenceUnscramble, ErrorCorrection, TrueFalse, Dictation, Reading/Listening.
- `Application/Features/Lessons/Validation/LessonCandidateValidator.cs:179` `ValidateExerciseTypeSemantics` (private static) kiểm tra schema theo type; **chưa** kiểm ErrorCorrection, Translation chỉ kiểm rubricCode.
- `ILessonAiGrader.GradeSubmissionAsync(type, instructions, cefr, rubric, payload, maxPoints, context, aiConfig)`; `ILessonSanitizer.SanitizeContentForLearner(type, contentJson)` bỏ đáp án khỏi content.
- `LessonPromptTemplateProvider.cs` có mô tả schema từng type cho prompt AI (rubric `LESSON_TRANSLATION`, `LESSON_WRITING`).
- FE `features/lessons/components/player/ExerciseRenderer.tsx` nhận `ExerciseLearnerDto {id,key,type,title,instructions,order,maxPoints,gradingMode,content}` và kết quả dạng `ExerciseAnswerResultDto`.
- Permission seed trong `Infrastructure/Persistence/DatabaseSeeder.cs:1018` (`SeedIdentityAsync`, `EnsurePerm`, role `LEARNER` cấp quyền theo danh sách). Production đặt `SeedOnStartup=false` → nội dung seed không thể chỉ dựa vào seeder lúc khởi động.

## Thiết kế

### Domain (`EnglishLiveTutor.Domain/Grammar/`)

| Aggregate | Trường chính | Bất biến |
| --- | --- | --- |
| `GrammarStepEntity` (root) | `Code` (unique, `A1-03`), `CefrLevel`, `Order` (trong level), `TitleVi`, `TitleEn`, `SummaryVi`, `TheoryJson`, `IsLevelReview`, `IsPublished`, soft delete | Code đúng mẫu `^(A1|A2|B1|B2|C1|C2)-\d{2}$`; `(CefrLevel, Order)` unique; mỗi level đúng 1 step `IsLevelReview` và là step cuối |
| `GrammarExerciseEntity` (root) | `StepId`, `Type`, `Difficulty` (1–3), `Title`, `Instructions`, `ContentJson`, `GradingDefinitionJson`, `MaxPoints`, `GradingMode`, `Source` (Official/UserAi/UserImport), `OwnerUserId?`, `Visibility` (Private/Public), `Moderation` (None/Pending/Approved/Rejected), `RejectReason?`, `ReviewedBy?`, `ReviewedAtUtc?`, `IsCore`, `ContentHash`, soft delete, `RowVersion` | Official ⇒ không owner, Moderation None; `IsCore` chỉ cho Official; chuyển trạng thái qua method (`SubmitForReview`, `Approve`, `Reject`, `MakePrivate`, `EditContent` → về Pending nếu Public) |
| `UserGrammarStepProgressEntity` (root) | `UserId`, `StepId`, `Status` (Unlocked/Passed), `BestScore100`, `PassedBy` (Steps/LevelTest), `PassedAtUtc` | unique `(UserId, StepId)`; Passed không lùi về |
| `GrammarAttemptEntity` (root) + `GrammarAttemptAnswerEntity` (child) | `UserId`, `Kind` (StepCore/Practice/LevelTest), `StepId?`, `CefrLevel`, `ExerciseIdsJson` (đề cố định), `StartedAtUtc`, `CompletedAtUtc?`, `Score100?`; answer: `ExerciseId`, `AnswerPayloadJson`, `Status`, `Score100`, `FeedbackJson`, `ContentHash` (snapshot) | Chỉ nộp bài thuộc đề; một answer hiện hành mỗi bài (nộp lại thay thế); complete một lần |

Không tạo khóa ngoại sang `Lessons`. Dùng `ExerciseType`/`GradingMode` từ `Domain.Lessons.Enums` (nợ nhỏ: có thể chuyển sang `Domain.Common` sau, ngoài phạm vi).

### Application (`Features/Grammar/`)

- **Shared engine**: tách `ValidateExerciseTypeSemantics` thành `Common/Exercises/ExerciseContentValidator` (public); `LessonCandidateValidator` gọi lại, hành vi Lesson giữ nguyên (test Lesson hiện có phải pass). Grammar dùng thêm `GrammarExerciseRules` chặt hơn: chỉ 9 type cho phép (TrueFalse, MultipleChoice, Matching, FillInTheBlanks, SentenceUnscramble, ErrorCorrection, Translation, WritingPractice, ReadingComprehension), bắt buộc ErrorCorrection/Translation có đáp án, giới hạn độ dài text, số câu nhỏ.
- **Queries**: `GetGrammarRoadmap` (level → step + trạng thái của user, không kèm theory), `GetGrammarStep` (theory + số bài theo nguồn), `ListGrammarExercises` (step, `source=core|community|mine`, paged, content đã sanitize), `GetGrammarAttempt`, `ListMyGrammarExercises` (paged), admin `ListGrammarModerationQueue` (paged), admin `ListGrammarStepsAdmin`.
- **Commands**: `StartGrammarAttempt` (StepCore/Practice/LevelTest, kiểm tra quyền mở khóa), `SubmitGrammarAnswer` (grader deterministic; AI rubric gọi provider **trước** khi ghi), `CompleteGrammarAttempt` (tính điểm, cập nhật progress/mở khóa trong một commit), `GenerateGrammarExercises` (AI trực tiếp), `BuildGrammarExercisePrompt` (thủ công), `ImportGrammarExercisesJson`, `UpdateMyGrammarExercise`, `ChangeGrammarExerciseVisibility`, `DeleteMyGrammarExercise`; admin: `UpsertGrammarStep`, `UpdateGrammarStepTheory`, `CreateOfficialGrammarExercise`/`Update`/`Delete`, `ApproveGrammarExercise`, `RejectGrammarExercise`, `PromoteGrammarExerciseToOfficial`, `ImportGrammarContentPack`, `DraftGrammarTheoryWithAi` (trả nháp, không lưu).
- Tất cả theo MediatR + `IAppDbContext` + `IUnitOfWork`, owner từ `ICurrentUser`; bài Private/Rejected/Pending của người khác → 404.
- **Prompt chuẩn sinh bài tập** (`GrammarExercisePromptBuilder`): đưa vào step code/level/title, công thức + lỗi thường gặp từ TheoryJson, danh sách step đã học (chỉ tiêu đề), type + số lượng + phân bố độ khó, schema JSON từng type (dùng lại mô tả của `LessonPromptTemplateProvider`). Yêu cầu: một đáp án đúng duy nhất, đáp án nhiễu hợp lý và đúng ngữ pháp ngoài điểm đang kiểm, từ vựng ≤ level, không dùng ngữ pháp chưa học, giải thích tiếng Việt, không lặp câu. Ghi chú người dùng (tùy chọn ≤ 300 ký tự, ví dụ "chủ đề du lịch") JSON-encode trong khối `<<<…>>>` như prompt từ vựng. Cùng một builder cho chế độ trực tiếp và thủ công.
- **Kiểm tra output**: parse → validator → loại bài sai/ trùng hash → lưu bài hợp lệ, trả danh sách lỗi theo bài; vượt số lượng yêu cầu thì cắt.

### Infrastructure

- EF configuration + migration `AddGrammarRoadmap` (chỉ thêm bảng/index, không đụng dữ liệu cũ). Index: `GrammarSteps(Code)` unique, `(CefrLevel, Order)` unique; `GrammarExercises(StepId, Source, Moderation)`, `(OwnerUserId, StepId)`, `(StepId, ContentHash)`; `UserGrammarStepProgress(UserId, StepId)` unique; `GrammarAttempts(UserId, StartedAtUtc)`.
- Adapter AI: dùng lại client Gemini hiện có (key qua header `x-goog-api-key`, timeout ngắn ≤ 120 s), không log key/payload.
- **Content pack**: `Infrastructure/Grammar/ContentPacks/*.json` (embedded resource). `GrammarContentPackImporter` idempotent theo `Step.Code` và `ContentHash`: tạo step chưa có, cập nhật theory chỉ khi step chưa bị admin sửa (`TheoryVersion` của pack > bản đang có và `TheoryEditedByAdmin = false`), thêm bài core chưa có. Chạy từ seeder (dev) **và** từ nút admin "Nhập nội dung chuẩn" (prod, vì `SeedOnStartup=false`).
- Permission mới (seed + `EnsurePerm`): `grammar.read`, `grammar.attempt`, `grammar.generate` (cấp LEARNER), `grammar.manage`, `grammar.moderate` (cấp ADMIN, TEACHER có `grammar.moderate`).

### API (`Endpoints/GrammarEndpoints.cs`, `AdminGrammarEndpoints.cs`)

| Method | Route | Quyền |
| --- | --- | --- |
| GET | `/api/grammar/roadmap` | grammar.read |
| GET | `/api/grammar/steps/{code}` | grammar.read |
| GET | `/api/grammar/steps/{code}/exercises?source=&page=&pageSize=` | grammar.read |
| POST | `/api/grammar/attempts` `{kind, stepCode?, cefrLevel?}` | grammar.attempt |
| GET | `/api/grammar/attempts/{id}` | grammar.attempt |
| POST | `/api/grammar/attempts/{id}/answers` `{exerciseId, answerPayload, aiConfig?}` | grammar.attempt |
| POST | `/api/grammar/attempts/{id}/complete` | grammar.attempt |
| POST | `/api/grammar/steps/{code}/exercises/generate` | grammar.generate + rate limit AI |
| POST | `/api/grammar/steps/{code}/exercises/prompt` | grammar.generate |
| POST | `/api/grammar/steps/{code}/exercises/import` | grammar.generate |
| GET | `/api/grammar/my-exercises?status=&page=` | grammar.generate |
| PUT/DELETE | `/api/grammar/my-exercises/{id}` | grammar.generate (owner) |
| POST | `/api/grammar/my-exercises/{id}/visibility` `{visibility}` | grammar.generate (owner) |
| GET/PUT | `/api/admin/grammar/steps`, `/api/admin/grammar/steps/{code}` (+ `/theory`, `/theory/ai-draft`) | grammar.manage |
| CRUD | `/api/admin/grammar/steps/{code}/exercises` | grammar.manage |
| GET | `/api/admin/grammar/moderation?status=Pending&page=` | grammar.moderate |
| POST | `/api/admin/grammar/exercises/{id}/approve` · `/reject {reason}` · `/promote` | grammar.moderate / manage |
| POST | `/api/admin/grammar/content-pack/import` | grammar.manage |

Danh sách đều phân trang (mặc định 20, tối đa 100) theo header phân trang hiện có. Lỗi qua global handler (ADR-0005). Learner DTO bài tập cùng shape `ExerciseLearnerDto` để FE tái dùng player; **không bao giờ** trả `GradingDefinitionJson` cho learner (kể cả bài của chính họ khi làm bài), chỉ trả khi xem/sửa ở "Bài của tôi" hoặc admin.

### FE

- Student (`features/grammar/`): trang `/grammar` (lộ trình theo level, trạng thái khóa/mở/đạt, nút "Kiểm tra level"), `/grammar/:code` với tab **Lý thuyết** (render TheoryJson dạng text an toàn), **Bài tập** (làm bộ core → kết quả, mở step sau), **Luyện thêm** (core/cộng đồng/của tôi), nút "Tạo thêm bài tập bằng AI" (dialog: type, số lượng, độ khó, ghi chú, Private/Public, provider/model; thiếu key → chế độ thủ công copy prompt/dán JSON, giống từ vựng). Trang "Bài tập của tôi" (trạng thái Pending/Approved/Rejected + lý do).
- Tái dùng `ExerciseRenderer` qua adapter mỏng (không sửa hành vi Lesson).
- Admin (`features/admin/grammar/`): danh sách step theo level, editor lý thuyết (form có cấu trúc + xem trước + "AI soạn nháp"), quản lý bài core, hàng chờ kiểm duyệt (xem trước như learner + đáp án, Approve/Reject có lý do/Promote), nút nhập content pack. Thẻ trên Admin dashboard.
- Menu Student Portal thêm "Ngữ pháp". i18n vi/en. Đồng bộ `openapi.types.ts`, API adapter, query keys.

## Phạm vi

| Được sửa | Không được sửa |
| --- | --- |
| `Domain/Grammar/**`, `Application/Features/Grammar/**`, `Application/Common/Exercises/**` (mới) | Bảng/migration Lesson, hành vi chấm Lesson |
| `LessonCandidateValidator.cs` (chỉ gọi validator đã tách) | Contract Lesson hiện có |
| `Infrastructure/Grammar/**`, `Persistence/Configurations/Grammar/**`, `AppDbContext`, `IAppDbContext`, migration mới `AddGrammarRoadmap` | Migration cũ |
| `DatabaseSeeder.cs` (permission + gọi importer) | Dữ liệu identity khác |
| `Api/Endpoints/Grammar*.cs`, đăng ký endpoint | Program.cs ngoài dòng đăng ký |
| FE `features/grammar/**`, `features/admin/grammar/**`, router, nav, i18n, `openapi.types.ts` | Player Lesson (chỉ import) |

## Dữ liệu và migration

Migration `AddGrammarRoadmap` chỉ **thêm** 5 bảng và permission; không sửa/xóa dữ liệu hiện có. Vẫn cần backup trước `dotnet ef database update` trên DB thật; người dùng tự chạy. Nội dung A1 nạp qua importer (dev: seeder; prod: nút admin).

## Phân công (worker AGY, worktree riêng từ HEAD sau khi commit task trước)

| Batch | Nội dung | Model | Skill cần đọc |
| --- | --- | --- | --- |
| G1 BE | Domain + EF config + migration + permission + tách `ExerciseContentValidator` + `GrammarExerciseRules` + content pack importer (pack rỗng/1 step mẫu) + test domain/validator/importer | flash-high | `domain-aggregate-design`, `infrastructure-persistence`, `backend-testing` |
| G2 BE | Query roadmap/step/exercises, attempt (start/answer/complete), progress + mở khóa + test level, admin CRUD step/theory/exercise, endpoint + test cross-user | flash-high | `feature-cqrs`, `api-realtime`, `ef-performance`, `security-audit` |
| G3 BE | Prompt builder, generate/prompt/import, my-exercises, visibility, moderation, AI draft theory, giới hạn/rate limit, test injection + dedupe + moderation 404 | flash-high | `ai-provider-integration`, `security-audit` |
| G4 FE | Student roadmap, step (theory/bài tập/luyện thêm), player adapter, kết quả, test level, generate dialog (trực tiếp/thủ công), my-exercises | flash-high | FE `AGENTS.md`, `api-contract-sync`, `portal-design-review` |
| G5 FE | Admin step list, theory editor + preview + AI draft, bài core, hàng chờ duyệt, import pack, dashboard card | flash-medium | như G4 |
| G6 Nội dung | Claude soạn **lý thuyết cả 100 step** (A1→C2) vào `Infrastructure/Grammar/ContentPacks/grammar-<level>.json` + prompt chuẩn; AGY sinh bài core theo prompt, Claude review | Claude + AGY | `curriculum.md` |

Thứ tự: G1 → G2 → G3 → G4 ‖ (G5 sau G4) → G6 (G6 có thể bắt đầu soạn theory song song sau G1 vì chỉ phụ thuộc schema).

## Tiêu chí hoàn thành

- [ ] Roadmap trả đúng 100 step theo thứ tự; user mới chỉ mở A1-01.
- [ ] Đạt core ≥ 70% mở step sau; không đạt thì không mở; bài private/cộng đồng không tính.
- [ ] Test level ≥ 80% đánh dấu mọi step level Passed và mở level sau; không làm được test level khi level trước chưa đạt.
- [ ] User B không thấy/làm/sửa bài Private, Pending, Rejected của user A và attempt của A (404) — có test.
- [ ] Learner không nhận `GradingDefinitionJson`/đáp án trước khi nộp — có test.
- [ ] Public → Pending; admin Approve → user khác thấy; Reject → chỉ chủ thấy kèm lý do; sửa bài Approved → Pending.
- [ ] Generate trực tiếp và thủ công dùng cùng prompt; output sai schema/trùng bị loại, không lưu một phần; vượt giới hạn → 429/400 rõ ràng.
- [ ] Không gọi AI trong transaction; key không log/không lưu.
- [ ] Lesson: toàn bộ test hiện có vẫn pass sau khi tách validator.
- [ ] Importer idempotent: chạy 2 lần không nhân đôi; không ghi đè theory admin đã sửa.
- [ ] FE build pass, Vitest pass; i18n đủ vi/en.

## Kiểm chứng

```bash
# BE (trong EnglishPartner.BE)
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln            # không fail mới ngoài 6 baseline PronunciationSynthesisAudioCacheTests
dotnet ef migrations script --idempotent    # đọc SQL, không chạy trên DB thật
# FE (trong EnglishPartner.FE)
npm run build
npx vitest run
```

Không kiểm chứng tĩnh được: SQL Server thật (unique index, RowVersion), provider Gemini, chất lượng nội dung AI sinh ra, giao diện trên trình duyệt.

## Review cần chạy

- [ ] security-reviewer (ownership, moderation, rò đáp án, prompt injection, XSS theory, BYO key, rate limit)
- [ ] performance-reviewer (roadmap + progress query, phân trang, chọn đề random, index)
- [ ] code-reviewer

## Rủi ro và câu hỏi mở

- Khối lượng lớn (≈ 5 batch code + nội dung): ước tính mỗi batch AGY 1 vòng + 1 vòng sửa. Có thể dừng sau G4 để dùng thử với vài step A1.
- Chất lượng bài AI: đáp án sai/ambiguous là rủi ro chính → validator chỉ bắt lỗi cấu trúc; nội dung public dựa vào kiểm duyệt admin; bài core do Claude review.
- `ExerciseType` nằm ở `Domain.Lessons.Enums`: Grammar phụ thuộc namespace Lesson (chỉ enum). Chấp nhận, ghi nợ.
- Chưa có: SRS ôn lại bài sai, placement test đầu vào (ngoài test level tuần tự), báo cáo nội dung sai từ learner — đề xuất làm sau.
