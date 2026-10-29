# G2 — Backend: lộ trình, làm bài, tiến độ, mở khóa, admin CRUD

Đọc trước: `docs/tasks/20261029-grammar-roadmap/plan.md` (Quy tắc nghiệp vụ, Thiết kế → Application/API), code G1 đã có trong worktree (`Domain/Grammar/**`, `Application/Features/Grammar/**`, `Infrastructure/Persistence/Configurations/GrammarConfigurations.cs`), `EnglishPartner.BE/AGENTS.md`, skill `EnglishPartner.BE/.agents/skills/feature-cqrs/SKILL.md`, `api-realtime/SKILL.md`, `ef-performance/SKILL.md`, `backend-testing/SKILL.md`, `.agents/skills/security-audit/SKILL.md`.

Làm theo mẫu feature Lesson hiện có (MediatR `ICommand`/`IQuery` trong `Common/Interfaces/ICqrs.cs`, FluentValidation nếu feature tương tự dùng, `ICurrentUser`, `IAppDbContext` scoped, `IUnitOfWork`, endpoint mỏng `ISender.Send`, lỗi qua global exception handler/ProblemDetails, paging headers như các endpoint danh sách hiện có). **Không** làm generate AI / moderation / my-exercises (G3), không làm FE.

## Hằng số (`Features/Grammar/GrammarRules.cs` hoặc options)

`StepPassThreshold = 70`, `LevelTestPassThreshold = 80`, `LevelTestMinItems = 20`, `LevelTestMaxItems = 30`, `PracticeMaxItems = 10`.

## Learner — Queries

1. `GetGrammarRoadmapQuery` → `GrammarRoadmapDto { levels: [{ level, isUnlocked, isPassed, canTakeLevelTest, steps: [{ code, order, titleVi, titleEn, summaryVi, isLevelReview, status: "Locked"|"Unlocked"|"Passed", bestScore100?, passedBy? }] }] }`.
   - Chỉ step `IsPublished && !IsDeleted`, sắp theo thứ tự level A1→C2 rồi `Order`.
   - Trạng thái suy ra từ `UserGrammarStepProgress` của user hiện tại + quy tắc: A1-01 luôn Unlocked; step N Unlocked nếu step N-1 cùng level Passed; step 1 của level L Unlocked nếu step cuối (review) của level L-1 Passed. Không ghi DB trong query.
   - `canTakeLevelTest(L)` = L là A1 hoặc mọi step level L-1 Passed (tức step review L-1 Passed).
   - Hai query cố định (steps + progress của user), không N+1. Không trả `TheoryJson`.
2. `GetGrammarStepQuery(code)` → `GrammarStepDetailDto { code, cefrLevel, order, titleVi, titleEn, summaryVi, isLevelReview, theory (JsonElement/raw JSON object), status, bestScore100?, coreExerciseCount, communityExerciseCount, myExerciseCount, prevCode?, nextCode? }`. Step không tồn tại/không published → 404. **Step Locked vẫn xem được lý thuyết** (chỉ làm bài core bị khóa).
3. `ListGrammarExercisesQuery(code, source: core|community|mine, page, pageSize)` → `PagedResult<GrammarExerciseLearnerDto>`; `GrammarExerciseLearnerDto` cùng shape `ExerciseLearnerDto` FE (`id, key, type, title, instructions, order, maxPoints, gradingMode, content`) + `difficulty, focus?, source, visibility, moderation, authorName?`.
   - `core` = Official && IsCore; `community` = Official && !IsCore **hoặc** user-owned Approved; `mine` = OwnerUserId == current user (mọi trạng thái).
   - Content qua `ILessonSanitizer.SanitizeContentForLearner(type, contentJson)` — không bao giờ trả `GradingDefinitionJson`; với ReadingComprehension đáp án nằm trong `content.subQuestions[].grading` → sanitizer phải bỏ (kiểm tra sanitizer hiện có làm vậy; nếu không, thêm test chứng minh và sửa trong một sanitizer riêng cho Grammar, không đổi hành vi Lesson).
   - Phân trang mặc định 20, tối đa 100. Sắp `Difficulty`, rồi `Key`/`CreatedAtUtc`.
4. `GetGrammarAttemptQuery(id)` → `GrammarAttemptDto { id, kind, stepCode?, cefrLevel, startedAtUtc, completedAtUtc?, score100?, exercises: GrammarExerciseLearnerDto[] (theo thứ tự đề), answers: [{ exerciseId, status, score100?, pointsEarned?, maxPoints, feedback (explanation, isCorrect, criteria, strengths, improvements, suggestedAnswer…), answerPayload }] }`. Không phải của user → 404.

## Learner — Commands

5. `StartGrammarAttemptCommand { kind, stepCode?, cefrLevel? }` → `GrammarAttemptDto`.
   - `StepCore`: step phải Unlocked hoặc Passed với user (Locked → 403 `grammar_step_locked`); đề = tất cả bài Official IsCore của step (sắp theo Difficulty, Key); 0 bài → 409 `grammar_step_has_no_core_exercises`.
   - `Practice`: step bất kỳ đã published; đề = tối đa `PracticeMaxItems` bài user được xem (`IsVisibleTo`) của step, ưu tiên bài chưa làm gần đây (đơn giản: random có seed từ attempt id; ghi chú trong report).
   - `LevelTest`: `cefrLevel` bắt buộc; `canTakeLevelTest` phải true (không → 403 `grammar_level_test_locked`); đề = random 20–30 bài Official IsCore từ các step của level (ưu tiên trải đều mỗi step ≥ 1 bài). Không đủ 20 → lấy hết nếu ≥ 10, < 10 → 409.
   - Nếu user đang có attempt cùng kind+step/level chưa complete và bắt đầu < 24 giờ → trả lại attempt đó (không tạo mới).
   - Random chọn trong bộ nhớ trên danh sách Id (không `ORDER BY NEWID()` trên bảng lớn — số bài core mỗi level nhỏ, chấp nhận).
6. `SubmitGrammarAnswerCommand { attemptId, exerciseId, answerPayload (JsonElement), aiConfig? }` → `GrammarAnswerResultDto`.
   - Attempt của user, chưa complete, exercise thuộc đề (không → 404/409).
   - Deterministic/Composite: `IDeterministicExerciseGrader.Grade(...)`.
   - AiRubric (Translation/WritingPractice): nếu request có `aiConfig` hợp lệ (cùng cách Lesson nhận BYO key: đọc `SubmitExerciseAnswerCommand` của Lesson và dùng đúng `LessonAiExecutionConfigDto` + `ILessonAiGrader` + rubric `LESSON_TRANSLATION`/`LESSON_WRITING` từ `ScoringRubrics`) → chấm AI **trước** khi mở ghi DB; không có key → `Status = SelfAssessed`, `Score100 = null`, trả `referenceTranslations`/`guidelines` làm `suggestedAnswer`. Provider lỗi → `FailedGrading`, không ném 500.
   - Lưu qua `attempt.SubmitAnswer(...)` + một commit. Không log answer payload hay key.
7. `CompleteGrammarAttemptCommand { attemptId }` → `GrammarAttemptResultDto { score100, passed, unlockedStepCodes[], levelPassed? }`.
   - Điểm = trung bình `Score100` của các bài **có điểm** (bỏ SelfAssessed/NotAssessable/FailedGrading khỏi mẫu số); bài chưa nộp tính 0.
   - `StepCore`: `progress.RecordScore(score, 70)`; điều kiện đạt thêm: nếu đề có bài AiRubric thì phải có ≥ 1 answer AiRubric ở trạng thái khác "chưa nộp". Tạo progress nếu chưa có.
   - `LevelTest`: ≥ 80 → `MarkPassedByLevelTest` cho mọi step published của level (tạo progress còn thiếu).
   - `Practice`: chỉ lưu điểm attempt, không đổi progress.
   - Một commit; xử lý race unique `(UserId, StepId)` bằng cách nạp lại progress khi trùng khóa (tham khảo `IsUniqueConstraintViolation` đã dùng trong `DailyVocabularyPlanInitializer`).

## Admin (permission `grammar.manage`)

8. `ListGrammarStepsAdminQuery(level?, page, pageSize)` → step kèm số bài core/official/pending, `theoryEditedByAdmin`, `isPublished`.
9. `GetGrammarStepAdminQuery(code)` → như learner + `theory` + danh sách bài Official đầy đủ (kèm grading).
10. `UpdateGrammarStepCommand(code, titleVi, titleEn, summaryVi, isPublished)`; `UpdateGrammarStepTheoryCommand(code, theory)` → validate `GrammarTheoryValidator` (lỗi → 400 với issues), `EditTheoryByAdmin`.
11. `CreateOfficialGrammarExerciseCommand(code, key?, type, difficulty, title, instructions, maxPoints, gradingMode, content, grading, isCore)`; `UpdateOfficialGrammarExerciseCommand(id, …)`; `DeleteOfficialGrammarExerciseCommand(id)` (soft delete). Validate `GrammarExerciseRules`, hash trùng trong step → 409. Key tự sinh `<code>-E<NN>` nếu trống.
12. `ImportGrammarContentPackCommand` → gọi `GrammarContentPackImporter.ImportAsync`, trả kết quả (permission `grammar.manage`).

## API

- `Api/Endpoints/GrammarEndpoints.cs` (group `/api/grammar`, `RequireAuthorization`): routes theo bảng trong plan (roadmap, steps/{code}, steps/{code}/exercises, attempts, attempts/{id}, attempts/{id}/answers, attempts/{id}/complete). Permission `grammar.read` cho GET; `grammar.attempt` cho attempts.
- `Api/Endpoints/AdminGrammarEndpoints.cs` (group `/api/admin/grammar`): steps list/detail, PUT step, PUT theory, CRUD exercises, POST `content-pack/import`.
- Đăng ký trong nơi các endpoint khác được map (không sửa logic Program.cs khác). Rate limit: dùng policy AI hiện có cho `attempts/{id}/answers` (vì có thể gọi AI).
- Mã lỗi theo `{ error }`/ProblemDetails hiện có của dự án (đọc `ProblemDetailsGlobalExceptionHandler`); dùng exception type đã có cho 404/403/409.

## Test (`tests/EnglishLiveTutor.Domain.Tests/Grammar*Tests.cs`)

- Roadmap: user mới chỉ A1-01 Unlocked; pass A1-01 → A1-02 Unlocked; pass A1-20 → A2-01 Unlocked; `canTakeLevelTest` đúng; đúng 2 truy vấn (hoặc kiểm tra không N+1 bằng số lần gọi như test performance hiện có, nếu có mẫu).
- StartAttempt: step Locked → 403; StepCore không có bài → 409; attempt dang dở được tái dùng; LevelTest khi level trước chưa pass → 403; LevelTest chọn 20–30 bài từ nhiều step.
- Submit: bài ngoài đề → lỗi; chấm deterministic đúng/sai; AiRubric không key → SelfAssessed; AiRubric có key dùng fake `ILessonAiGrader` và provider được gọi trước khi có thay đổi tracked (mẫu `AiCommandsUnitOfWorkTests`); provider lỗi → FailedGrading.
- Complete: 69 → không pass; 70 → pass và mở step sau; LevelTest 80 → mọi step level Passed; Practice không đổi progress; race progress không mất dữ liệu.
- **Bảo mật**: user B không đọc/nộp/complete attempt của user A (404); user B không thấy bài Private/Pending/Rejected của A trong `community`; learner DTO không chứa `grading`/`correctOptionIds`/`acceptedAnswers`/`referenceCorrections` (kể cả Reading subQuestions); learner không gọi được endpoint admin (403).
- Admin: theory sai schema → 400; sửa theory set `TheoryEditedByAdmin`; tạo bài trùng hash → 409.

## Kiểm chứng phải chạy

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```
Không fail mới ngoài 6 baseline `PronunciationSynthesisAudioCacheTests`.

## Không được làm

- Không đổi migration G1 trừ khi bắt buộc (nếu cần, ghi rõ lý do trong report và tạo migration mới, không sửa migration đã tạo).
- Không sửa contract/hành vi Lesson, không làm FE, không chạy API/`database update`, không gọi AI thật, không commit.

## Worker report

```
## Worker report
- Files changed:
- Routes added:
- Build:
- Test: <pass/fail/tổng, liệt kê fail>
- Quyết định/giả định:
- Chưa làm/không chắc:
```
