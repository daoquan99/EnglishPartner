# G2 fix 1 — sửa sau security + performance review

Worktree chứa G1 (staged) + G2 (unstaged). Chỉ sửa các mục dưới; thêm test cho từng mục có ghi "test". Migration `AddGrammarRoadmap` chưa áp dụng ở đâu: nếu cần đổi schema, `dotnet ef migrations remove` rồi tạo lại cùng tên (chỉ khác phần Grammar).

## Chống gian lận (High)

1. **Khóa câu trả lời trong attempt `StepCore` và `LevelTest`** (`SubmitGrammarAnswerCommand`, `GrammarAttemptEntity.SubmitAnswer`): mỗi bài chỉ nộp **một lần**; lần sau → 409 `grammar_answer_locked`. Ngoại lệ duy nhất: answer hiện tại `FailedGrading` (lỗi provider) được nộp lại. `Practice` giữ hành vi nộp lại thoải mái. Test: nộp lại MC trong StepCore → 409; trong Practice → OK; FailedGrading → nộp lại OK.
2. **Đáp án mẫu AiRubric**: chỉ trả `suggestedAnswer`/`referenceTranslations` khi answer đã bị khóa (StepCore/LevelTest đã nộp, trạng thái khác FailedGrading) hoặc attempt đã complete; với Practice trả sau khi nộp như hiện tại. Không đọc `suggestedAnswer` từ ContentJson (chỉ từ GradingDefinitionJson). Test.
3. **Task context cho AI grader**: dựng `taskContextJson` giống `BuildTaskContextJson` trong `Features/Lessons/Commands/AttemptOperations/SubmitExerciseAnswerCommand.cs` (Translation: `direction`, `sourceText`, `context`, `referenceTranslations`; WritingPractice: `prompt`, `minWords`, `maxWords`, `guidelines`) và truyền vào `ILessonAiGrader.GradeSubmissionAsync`. Test bằng fake grader kiểm context nhận được.
4. **Không để lộ đáp án qua content**: `GrammarExerciseRules` từ chối content (kể cả nested) chứa các key đáp án: `acceptedAnswers`, `answer`, `answers`, `correctAnswer(s)`, `correctOptionIds`, `statementAnswers`, `correctPairs`, `validSequences`, `referenceCorrections`, `acceptedCorrections`, `referenceTranslations`, `suggestedAnswer`, `explanation`, `targetWord` — **ngoại trừ** `content.subQuestions[].grading` của ReadingComprehension (sanitizer đã bỏ). Thêm `GrammarLearnerContentSanitizer` (Application, allowlist theo type cho các field mà player cần: xem `exercise-prompt.md` cột content) dùng trong mọi learner DTO thay cho `ILessonSanitizer` (không đổi `LessonSanitizer`). Test: content có `blanks[].acceptedAnswers` bị validator từ chối; learner DTO của mỗi type chỉ chứa field allowlist.

## Đúng đắn (Medium)

5. `CompleteGrammarAttemptCommand`: bài trong đề đã bị xóa mềm → bỏ khỏi mẫu số. Tái dùng attempt dang dở chỉ khi mọi bài trong đề còn tồn tại. Test.
6. Thêm `RowVersion` cho `GrammarAttemptEntity` (concurrency token). Map `DbUpdateConcurrencyException` và vi phạm unique `(AttemptId, ExerciseId)` / `(UserId, StepId)` thành 409 `grammar_conflict` (theo cách dự án map conflict hiện có). Nhánh LevelTest khi trùng unique progress: nạp lại progress đã có và `MarkPassedByLevelTest`, không throw. Submit vào attempt đã complete → 409. Test race progress (2 context InMemory hoặc mô phỏng exception như test `DailyVocabularyPlanInitializer` đã làm).
7. Bộ core phải có ≥ 1 bài Deterministic/Composite: `StartGrammarAttempt` StepCore với core chỉ toàn AiRubric → 409 `grammar_step_has_no_core_exercises`; `CreateOfficial`/`Update` cho phép nhưng importer/ admin list hiển thị bình thường. Complete: `gradedCount == 0` → không pass.
8. `GetGrammarAttemptQuery` và kết quả Start: lọc lại bài bằng `IsVisibleTo(currentUser)`; bài không còn hiển thị → trả placeholder `{ id, unavailable: true }` (không content). Test.
9. Kiểm trùng hash khi admin tạo/sửa bài Official chỉ so với Official + Approved của step (không tính Private/Pending/Rejected của user). Test.
10. Lỗi provider: `FeedbackJson`/response chỉ chứa mã cố định `ai_grading_failed` + loại exception, không chứa `ex.Message`. `AnswerPayload` > 8 000 ký tự → 400.

## Hiệu năng

11. `ListGrammarExercisesQuery`: luôn phân trang (mặc định 20, tối đa 100) cho mọi `source`, kể cả khi client không gửi tham số. Test.
12. Tra tác giả: parse `OwnerUserId`/`AuthorUserId` sang `Guid` ở client rồi `ids.Contains(u.Id)` (không `u.Id.ToString()` trong LINQ). Id không parse được thì bỏ qua (giữ username nếu khớp `Username`, theo cách các query khác tra user nếu có mẫu).
13. Index `GrammarExercises`: thay `(StepId, Source, Moderation)` bằng `(StepId, Source, Moderation, IsDeleted)` INCLUDE `(IsCore, OwnerUserId, Difficulty, Key, CreatedAtUtc)` (EF `IncludeProperties` cho SQL Server). Giữ các index khác.
14. Practice chọn ngẫu nhiên tại DB: `.OrderBy(_ => EF.Functions.Random())` (hoặc `Guid.NewGuid()`) + `.Take(PracticeMaxItems)`, chỉ select Id. Nếu InMemory không dịch được, tách hàm để test vẫn chạy (ví dụ abstraction nhỏ hoặc fallback có chủ đích) — ghi rõ trong report.
15. `ListGrammarStepsAdminQuery`: project sang DTO, không tải `TheoryJson`.
16. `GetGrammarAttemptQuery`: `AsNoTracking()`.
17. `GetGrammarStepQuery`: thay `GroupBy(e => 1)` bằng `CountAsync` riêng hoặc projection `Sum(cond ? 1 : 0)` dịch chắc chắn trên SQL Server.
18. Gom helper dùng chung: danh sách step đã sắp theo lộ trình (dùng ở GetStep/StartAttempt/Complete), mapping attempt → DTO (Start + Get).
19. Rate limit policy chung hiện có cho `POST /api/grammar/attempts` và `/complete`.

## Kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```
Không fail mới ngoài 6 baseline. Không sửa `Grammar/ContentPacks/*.json`, không sửa Lesson, không commit.

## Worker report

```
## Worker report
- Files changed:
- Migration: <tạo lại? chỉ Grammar?>
- Build:
- Test:
- Quyết định/giả định:
- Chưa làm/không chắc:
```
