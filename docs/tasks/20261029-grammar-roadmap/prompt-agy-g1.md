# G1 — Backend nền: Domain, persistence, migration, validator, content pack importer

Đọc trước: `docs/tasks/20261029-grammar-roadmap/plan.md` (mục Thiết kế, Quy tắc nghiệp vụ), `docs/tasks/20261029-grammar-roadmap/curriculum.md` (mục "Khung lý thuyết một step"), `EnglishPartner.BE/AGENTS.md`, `EnglishPartner.BE/.agents/architecture.md`, skill `EnglishPartner.BE/.agents/skills/domain-aggregate-design/SKILL.md`, `infrastructure-persistence/SKILL.md`, `backend-testing/SKILL.md`.

Chỉ làm batch G1. **Không** làm query/command/endpoint cho learner hay admin (G2/G3), không làm FE.

## 1. Domain — `EnglishPartner.BE/src/EnglishLiveTutor.Domain/Grammar/`

Theo phong cách `Domain/Lessons/LessonEntity.cs`: `AggregateRoot<Guid>`, setter private, constructor kiểm tra, method chuyển trạng thái, ném `ArgumentException`/`InvalidOperationException`. Dùng `ExerciseType`, `GradingMode` từ `EnglishLiveTutor.Domain.Lessons.Enums` (không tạo enum trùng).

Enum (`Grammar/Enums/`): `GrammarExerciseSource { Official = 1, UserAi = 2, UserImport = 3 }`, `GrammarExerciseVisibility { Private = 1, Public = 2 }`, `GrammarModerationStatus { None = 0, Pending = 1, Approved = 2, Rejected = 3 }`, `GrammarStepProgressStatus { Unlocked = 1, Passed = 2 }`, `GrammarStepPassedBy { Steps = 1, LevelTest = 2 }`, `GrammarAttemptKind { StepCore = 1, Practice = 2, LevelTest = 3 }`.

### `GrammarStepEntity` (root)
- `Code` (mẫu `^(A1|A2|B1|B2|C1|C2)-\d{2}$`, viết hoa), `CefrLevel` (phải khớp tiền tố Code), `Order` (≥ 1, khớp số trong Code), `TitleVi`, `TitleEn` (≤ 200), `SummaryVi` (≤ 500), `TheoryJson` (không rỗng), `TheoryVersion` (int ≥ 1), `TheoryEditedByAdmin` (bool), `IsLevelReview`, `IsPublished` (mặc định true), `RowVersion`.
- Method: `UpdateMetadata(titleVi, titleEn, summaryVi, isPublished)`, `ApplyPackTheory(theoryJson, theoryVersion)` (chỉ khi `!TheoryEditedByAdmin && theoryVersion > TheoryVersion`, trả bool đã áp dụng), `EditTheoryByAdmin(theoryJson)` (đặt `TheoryEditedByAdmin = true`, tăng `TheoryVersion`).
- `public static readonly string[] Levels = ["A1","A2","B1","B2","C1","C2"]` và helper `PreviousLevel(level)` / `NextLevel(level)`.

### `GrammarExerciseEntity` (root)
- `StepId`, `Key?` (≤ 64; bắt buộc với Official, ví dụ `A1-03-E05`), `Type`, `Difficulty` (1–3), `Title` (≤ 200), `Instructions` (≤ 1000), `ContentJson`, `GradingDefinitionJson?`, `MaxPoints` (> 0, ≤ 100), `GradingMode`, `Source`, `OwnerUserId?` (string, ≤ 128, giống kiểu UserId ở các bảng khác), `Visibility`, `Moderation`, `RejectReason?` (≤ 500), `ReviewedBy?`, `ReviewedAtUtc?`, `IsCore`, `ContentHash` (64 hex), `CreatedAtUtc`, `UpdatedAtUtc`, `RowVersion`.
- Factory: `CreateOfficial(stepId, key, type, difficulty, title, instructions, contentJson, gradingJson, maxPoints, gradingMode, isCore, contentHash, now)` → không owner, `Visibility = Public`, `Moderation = None`. `CreateUserOwned(stepId, ownerUserId, source (UserAi|UserImport), visibility, …, contentHash, now)` → `IsCore = false`; Public → `Pending`, Private → `None`.
- Chuyển trạng thái: `ChangeVisibility(visibility, now)` (chỉ bài user; Private → Moderation None, xóa RejectReason; Public → Pending), `EditContent(…, contentHash, now)` (bài user: nếu Public thì về Pending; bài Official giữ nguyên), `Approve(reviewer, now)` / `Reject(reviewer, reason, now)` (chỉ khi Pending; reason bắt buộc), `PromoteToOfficial(key, isCore, now)` (chỉ khi Approved; thành Official, giữ OwnerUserId = null? → **giữ tác giả ở trường mới `AuthorUserId`**: thêm `AuthorUserId?` lưu người tạo gốc, `OwnerUserId` về null), `SetCore(bool)` (chỉ Official).
- Query helper thuần: `bool IsVisibleTo(string? userId)` = Official || Approved || (OwnerUserId == userId).

### `UserGrammarStepProgressEntity` (root)
- `UserId`, `StepId`, `Status`, `BestScore100` (decimal 0–100), `PassedBy?`, `UnlockedAtUtc`, `PassedAtUtc?`.
- `RecordScore(score100, passThreshold, now)` (cập nhật best; đạt ngưỡng → Passed/Steps), `MarkPassedByLevelTest(now)`; Passed không bao giờ quay về Unlocked.

### `GrammarAttemptEntity` (root) + `Entities/GrammarAttemptAnswerEntity`
- Attempt: `UserId`, `Kind`, `StepId?` (bắt buộc với StepCore/Practice), `CefrLevel`, `ExerciseIdsJson` (danh sách Guid cố định, 1–40), `StartedAtUtc`, `CompletedAtUtc?`, `Score100?`.
- Answer: `AttemptId`, `ExerciseId`, `AnswerPayloadJson` (≤ 20 000 ký tự), `Status` (string: `Completed`/`PendingGrading`/`FailedGrading`/`NotAssessable`/`SelfAssessed`), `Score100?`, `PointsEarned?`, `MaxPoints`, `FeedbackJson?`, `ContentHash`, `SubmittedAtUtc`.
- `IReadOnlyList<Guid> ExerciseIds`; `SubmitAnswer(exerciseId, …)`: exercise phải thuộc đề, attempt chưa complete; nộp lại thay thế answer cũ của bài đó (một answer hiện hành/bài). `Complete(score100, now)` chỉ một lần.

## 2. Application

1. **Tách validator dùng chung**: chuyển logic `ValidateExerciseTypeSemantics` của `Application/Features/Lessons/Validation/LessonCandidateValidator.cs` sang `Application/Common/Exercises/ExerciseContentValidator.cs` (public static, cùng chữ ký trừ việc trả issue list). `LessonCandidateValidator` gọi lại — **hành vi Lesson không đổi** (mọi test Lesson hiện có phải pass, không sửa test Lesson).
2. `Application/Features/Grammar/Rules/GrammarExerciseRules.cs`:
   - `AllowedTypes`: TrueFalse, MultipleChoice, Matching, FillInTheBlanks, SentenceUnscramble, ErrorCorrection, Translation, WritingPractice, ReadingComprehension.
   - `Validate(type, contentJson, gradingJson, maxPoints, gradingMode) → List<ValidationIssueDto>`: gọi `ExerciseContentValidator`, rồi bổ sung: type ngoài danh sách → lỗi; ContentJson/GradingDefinitionJson ≤ 20 000 ký tự, JSON object hợp lệ; gradingMode khớp type (Translation/WritingPractice → AiRubric với rubricCode `LESSON_TRANSLATION`/`LESSON_WRITING`; ReadingComprehension → Composite; còn lại → Deterministic); ErrorCorrection phải có đáp án đúng (đọc `DeterministicExerciseGrader.GradeErrorCorrection` để biết field đáp án đang dùng); Translation phải có `content.sourceText` và `grading.referenceTranslations` không rỗng; WritingPractice phải có `content.prompt`. Đọc `Infrastructure/Lessons/DeterministicExerciseGrader.cs` và `PromptBuilder/LessonPromptTemplateProvider.cs` để dùng **đúng tên field** hiện có, không bịa schema mới.
3. `Features/Grammar/Rules/GrammarTheoryValidator.cs`: kiểm `TheoryJson` theo khung trong `curriculum.md` (objectives 1–6, sections 1–8, mỗi section có heading + explanationVi, formulas ≤ 6, examples ≤ 8 mỗi example có `en` + `vi`, commonMistakes 0–8 mỗi mục có wrong/right/explanationVi, tips ≤ 8, summary 1–6; mọi chuỗi ≤ 1 000 ký tự; tổng ≤ 60 000 ký tự). Step `IsLevelReview` cho phép commonMistakes rỗng. Trả danh sách issue có path.
4. `Features/Grammar/Rules/GrammarContentHasher.cs`: SHA-256 hex (lowercase) của chuỗi chuẩn hóa `type|canonical(content)|canonical(grading)` — canonical JSON: sắp xếp key object theo ordinal, bỏ khoảng trắng, chuỗi trim + lowercase + gộp khoảng trắng.
5. `IAppDbContext` + `AppDbContext`: `GrammarSteps`, `GrammarExercises`, `UserGrammarStepProgress`, `GrammarAttempts`, `GrammarAttemptAnswers`.
6. **Content pack importer** (Application service, scoped, DI trong `Application/DependencyInjection.cs`):
   - Port `Features/Grammar/ContentPacks/IGrammarContentPackSource.cs`: `IReadOnlyList<GrammarContentPackDocument> LoadAll()` trả `(string Name, string Json)`.
   - Model pack (record, System.Text.Json, camelCase):
     ```json
     {
       "packVersion": 1,
       "level": "A1",
       "steps": [
         {
           "code": "A1-03", "order": 3, "titleVi": "…", "titleEn": "…", "summaryVi": "…",
           "isLevelReview": false, "theoryVersion": 1,
           "theory": { "objectives": [], "sections": [], "commonMistakes": [], "tips": [], "summary": [] },
           "exercises": [
             { "key": "A1-03-E01", "type": "MultipleChoice", "difficulty": 1, "title": "…", "instructions": "…",
               "maxPoints": 10, "gradingMode": "Deterministic", "isCore": true,
               "content": { }, "grading": { } }
           ]
         }
       ]
     }
     ```
     `exercises` có thể vắng/rỗng. `type`/`gradingMode` là tên enum (PascalCase).
   - `GrammarContentPackImporter.ImportAsync(ct) → GrammarContentPackImportResult` (số step tạo/cập nhật/bỏ qua, số bài tạo/bỏ qua, danh sách lỗi theo `code`/`key`):
     - Mỗi step: validate Code/level/order và theory (`GrammarTheoryValidator`); lỗi → bỏ step đó, ghi lỗi, tiếp tục.
     - Chưa có → tạo. Có rồi → cập nhật title/summary và `ApplyPackTheory` (không ghi đè khi admin đã sửa hoặc version không lớn hơn).
     - Bài: validate bằng `GrammarExerciseRules`; bỏ qua nếu step đã có bài Official cùng `Key` hoặc cùng `ContentHash`.
     - Một `IUnitOfWork.CommitChangesAsync` cho toàn bộ lần import; dùng `IAppDbContext` scoped (không factory).
     - Idempotent: chạy 2 lần liên tiếp lần 2 tạo 0.
   - Infrastructure `Infrastructure/Grammar/EmbeddedGrammarContentPackSource.cs`: đọc embedded resource có tên khớp `*.Grammar.ContentPacks.*.json` trong assembly Infrastructure, sắp xếp theo tên. Thêm vào `EnglishLiveTutor.Infrastructure.csproj`: `<EmbeddedResource Include="Grammar\ContentPacks\*.json" />`. **Không tạo file nào trong `Grammar/ContentPacks/`** (orchestrator sẽ thêm nội dung) — glob rỗng phải build được.

## 3. Infrastructure persistence

- `Persistence/Configurations/GrammarConfigurations.cs` theo phong cách `LessonConfigurations.cs` (bảng `GrammarSteps`, `GrammarExercises`, `UserGrammarStepProgress`, `GrammarAttempts`, `GrammarAttemptAnswers`; enum lưu int; độ dài cột như trên; `nvarchar(max)` cho JSON; RowVersion; query filter soft delete cho root như các aggregate khác).
- Index: `GrammarSteps(Code)` unique, `(CefrLevel, Order)` unique; `GrammarExercises(StepId, Source, Moderation)`, `(OwnerUserId, StepId)`, `(StepId, ContentHash)`, `(StepId, Key)` unique có filter `[Key] IS NOT NULL`; `UserGrammarStepProgress(UserId, StepId)` unique; `GrammarAttempts(UserId, StartedAtUtc)`; `GrammarAttemptAnswers(AttemptId, ExerciseId)` unique. FK: Exercise → Step (Restrict), Progress → Step (Restrict), Answer → Attempt (Cascade). Không FK sang bảng Lesson.
- Migration: `dotnet ef migrations add AddGrammarRoadmap` (chạy trong `EnglishPartner.BE`, project Infrastructure, startup Api — xem cách các migration trước được tạo). Migration chỉ tạo bảng/index. Kiểm tra snapshot không có thay đổi lạ ngoài Grammar. **Không chạy `database update`.**
- `DatabaseSeeder.SeedIdentityAsync`: thêm permission module `"Grammar"` với ID tiếp theo sau `…000018`: `grammar.read` (…019), `grammar.attempt` (…020), `grammar.generate` (…021), `grammar.manage` (…022), `grammar.moderate` (…023) ở cả nhánh tạo mới và nhánh `EnsurePerm`. LEARNER thêm `grammar.read`, `grammar.attempt`, `grammar.generate` ở **cả hai** chỗ danh sách LEARNER. ADMIN có tất cả (đã tự động); TEACHER nhận quyền module khác Identity (đã tự động).
- Seeder gọi `GrammarContentPackImporter` sau seed catalog (trong luồng seed hiện có, chỉ khi seeding được bật như các seeder khác). Log kết quả dạng số đếm, không log nội dung.

## 4. Test (`EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/GrammarFoundationTests.cs`, có thể tách nhiều file `Grammar*Tests.cs`)

- Domain: Code/level/order sai → ném; `ApplyPackTheory` không ghi đè khi `TheoryEditedByAdmin`; moderation: Public → Pending, Approve/Reject chỉ khi Pending, Reject cần lý do, sửa bài Approved Public → Pending, Private → None; `IsVisibleTo` cho owner/người khác/Official/Approved; Promote giữ `AuthorUserId`; progress Passed không lùi; attempt từ chối bài ngoài đề, nộp lại thay thế, Complete hai lần ném.
- `GrammarExerciseRules`: mỗi type cho phép có 1 case hợp lệ (dùng đúng schema field của grader) và 1 case thiếu đáp án; type ngoài danh sách (Flashcards, Dictation) bị từ chối; gradingMode sai bị từ chối.
- `GrammarTheoryValidator`: hợp lệ, thiếu objectives, example thiếu `vi`, chuỗi quá dài.
- `GrammarContentHasher`: khác thứ tự key/khoảng trắng/hoa thường → cùng hash; đổi đáp án → khác hash.
- Importer (EF InMemory, nguồn pack giả trong test): tạo step + bài; chạy lần 2 → 0 tạo mới; theory admin đã sửa không bị ghi đè; version cao hơn được áp dụng khi chưa sửa; step lỗi bị bỏ qua nhưng step khác vẫn nhập; bài trùng hash bị bỏ qua; đúng một lần commit.
- Lesson: chạy lại toàn bộ test Lesson hiện có — phải pass không đổi.

## Kiểm chứng phải chạy

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```
Baseline: chỉ 6 test `PronunciationSynthesisAudioCacheTests` fail (EF InMemory không hỗ trợ `DataLength`). Không được có fail mới.

## Không được làm

- Không sửa migration cũ, bảng Lesson, contract HTTP hiện có, FE.
- Không chạy API, `database update`, không gọi provider AI.
- Không commit/push.

## Worker report

```
## Worker report
- Files changed: <danh sách>
- Migration: <tên, tóm tắt bảng/index>
- Build: <kết quả>
- Test: <pass/fail/tổng, liệt kê fail>
- Quyết định/giả định: <field schema đã dùng cho ErrorCorrection/Translation…>
- Chưa làm/không chắc: <…>
```
