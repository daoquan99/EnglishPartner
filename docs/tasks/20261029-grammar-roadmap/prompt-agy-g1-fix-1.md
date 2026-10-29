# G1 fix 1 — sửa sau review

Worktree đang chứa G1 (đã stage làm snapshot). Chỉ sửa các mục dưới đây, thêm/sửa test tương ứng. Đọc `docs/tasks/20261029-grammar-roadmap/exercise-prompt.md` mục "Schema theo loại bài" để biết schema bài Grammar.

## High

1. **`Application/Features/Grammar/Rules/GrammarExerciseRules.cs` phải bắt buộc có đáp án chấm được** (không sửa `ExerciseContentValidator`/hành vi Lesson). Đọc `Infrastructure/Lessons/DeterministicExerciseGrader.cs` và kiểm theo đúng field grader đọc:
   - MultipleChoice: `grading.correctOptionIds` không rỗng, mọi id thuộc `content.options`; `selectionMode` Single → đúng 1.
   - TrueFalse: `content.statements` không rỗng; `grading.statementAnswers` (hoặc `answers`) phủ **mọi** statement id, giá trị boolean.
   - Matching: `grading.correctPairs` (hoặc `pairs`) không rỗng, id hợp lệ, mỗi leftId tối đa một cặp.
   - FillInTheBlanks: mọi marker `[blank-n]` trong `textWithBlanks` có trong `content.blanks` và ngược lại; mọi blank id có ≥ 1 accepted answer theo một trong các schema grader hỗ trợ (`blankAnswers`/`correctAnswers`/`blanks`).
   - SentenceUnscramble: `grading.validSequences` không rỗng, mỗi sequence dùng **đủ và đúng** tất cả token id một lần.
   - ErrorCorrection: `content.incorrectSentence` (hoặc `sentence`) và `referenceCorrections` (hoặc `acceptedCorrections`) không rỗng; câu sửa không trùng câu sai (so sánh không phân biệt hoa thường/khoảng trắng).
   - `grading == null` cho type Deterministic → lỗi.
2. **ReadingComprehension**: mỗi subQuestion có `id` (duy nhất), `questionKind` ∈ {SingleChoice, MultipleChoice, TrueFalse, FillBlank} (từ chối ShortAnswer), `maxPoints > 0`, và đáp án theo kind đúng như `GradeComprehension` đọc (`correctOptionIds` khớp `options`; TrueFalse/FillBlank theo field grader). `content.passage` không rỗng.
3. Sửa các test hiện coi bài thiếu đáp án là hợp lệ (`FillInTheBlanks_ValidAndMissingAnswer`, Reading `{"subQuestions":[{"id":"q1"}]}`…); thêm test cho từng rule ở trên. Thêm một test end-to-end: mỗi bài hợp lệ trong `GrammarExerciseRulesTests` chạy qua `DeterministicExerciseGrader.Grade` với câu trả lời đúng → `IsCorrect = true` (với type deterministic/composite).

## Medium (importer `GrammarContentPackImporter.cs`)

4. Preload step và bài Official bằng `IgnoreQueryFilters()`; step/bài đã soft delete: **không tạo lại, không cập nhật**, đếm là skipped (không lỗi). Thêm test: xóa mềm step rồi import lại → không exception, không tạo bản ghi mới.
5. Bọc cả nhánh cập nhật step (metadata + theory) trong try/catch như nhánh tạo; lỗi → ghi `GrammarImportIssue`, bỏ step, các step khác vẫn nhập.
6. Khi cập nhật **không** đổi `IsPublished` hiện có (chỉ đặt khi tạo mới). Test: step đã unpublish → import lại vẫn unpublished.
7. Kiểm `pack.level == prefix(code)` và `order` khớp số trong code **trước** khi rẽ nhánh tạo/cập nhật; sai → lỗi và bỏ step.
8. `StepsUpdated` chỉ tăng khi metadata hoặc theory thực sự thay đổi (sửa test idempotent cho đúng: lần 2 với pack y hệt → `StepsUpdated == 0`).

## Low (Domain)

9. `GrammarExerciseEntity.ChangeVisibility`: visibility không đổi → return, không đụng moderation.
10. `UserGrammarStepProgressEntity.MarkPassedByLevelTest`: nếu đã Passed thì giữ nguyên `PassedBy`/`PassedAtUtc`.
11. Kiểm `Key` ≤ 64 ở constructor/factory và `PromoteToOfficial`; `ContentHash` đúng 64 ký tự hex thường.
12. `GrammarAttemptEntity`: từ chối `ExerciseIds` trùng hoặc `Guid.Empty`; `CefrLevel` ∈ `GrammarStepEntity.Levels`. `SubmitAnswer`: `status` phải thuộc `AllowedStatuses`.

## Bổ sung field

13. `GrammarExerciseEntity.Focus` (string?, ≤ 200, trim, rỗng → null): tham số tùy chọn ở các factory và `EditContent`; model pack `focus` (tùy chọn) được importer lưu. Cấu hình EF `nvarchar(200)`.
14. Migration `AddGrammarRoadmap` **chưa từng được áp dụng** ở đâu: xóa migration này (`dotnet ef migrations remove` trong `EnglishPartner.BE`, cùng project/startup như lúc tạo) rồi tạo lại cùng tên để gồm `Focus`. Kiểm tra snapshot chỉ khác phần Grammar. Không chạy `database update`.

Không sửa các file `Grammar/ContentPacks/*.json` (nội dung của orchestrator) và `tests/.../GrammarBundledContentPackTests.cs` (phải tiếp tục pass).

## Kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```
Không fail mới ngoài 6 baseline `PronunciationSynthesisAudioCacheTests`.

## Worker report

```
## Worker report
- Files changed:
- Migration: <tên mới, xác nhận chỉ Grammar>
- Build:
- Test:
- Chưa làm/không chắc:
```
