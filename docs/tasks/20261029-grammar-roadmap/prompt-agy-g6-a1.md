# G6 — Soạn bộ bài core cho 20 step A1

Việc nội dung, **không sửa code**. Chỉ tạo đúng một file mới:
`docs/tasks/20261029-grammar-roadmap/g6/a1-exercises.json`
Không sửa `grammar-a1.json` hay file nào khác (orchestrator sẽ gộp và kiểm bằng importer thật).

## Đọc trước

- `docs/tasks/20261029-grammar-roadmap/exercise-prompt.md` — **đặc tả bắt buộc**: Template (Quality rules, Grammar scope) và bảng "Schema theo loại bài" (tên field chính xác).
- `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Grammar/ContentPacks/grammar-a1.json` — lý thuyết 20 step A1 (formulas, commonMistakes, examples). Mỗi bài phải kiểm đúng cấu trúc của step đó.
- `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Grammar/Rules/GrammarExerciseRules.cs` và `EnglishPartner.BE/src/EnglishLiveTutor.Application/Common/Exercises/ExerciseContentValidator.cs` — luật validate thật (giới hạn số lựa chọn, token, blank, key bị cấm trong `content`…). Bài phải qua được validator này.
- `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Grammar/ContentPacks/GrammarContentPackModels.cs` — `GrammarContentPackExerciseDto` (field của mỗi bài).

## Định dạng file

```json
{
  "A1-01": [ { "key": "A1-01-E01", "type": "MultipleChoice", "difficulty": 1, "focus": "...", "title": "...", "instructions": "...", "maxPoints": 10, "gradingMode": "Deterministic", "isCore": true, "content": { }, "grading": { } } ],
  "A1-02": [ ... ],
  ...
  "A1-20": [ ... ]
}
```

- Đủ 20 khóa `A1-01` … `A1-20`, đúng mã step trong `grammar-a1.json`.
- `key` = `<code>-E<NN>` (`E01`…`E10`), duy nhất.
- `isCore: true`, `maxPoints: 10`, `gradingMode: "Deterministic"` cho mọi bài.

## Mỗi step đúng 10 bài, **chỉ loại Deterministic**

| # | Type | difficulty |
| --- | --- | --- |
| E01 | MultipleChoice | 1 |
| E02 | MultipleChoice | 1 |
| E03 | MultipleChoice | 2 |
| E04 | TrueFalse | 1 |
| E05 | Matching | 2 |
| E06 | FillInTheBlanks | 2 |
| E07 | FillInTheBlanks | 3 |
| E08 | SentenceUnscramble | 2 |
| E09 | SentenceUnscramble | 3 |
| E10 | ErrorCorrection | 3 |

Không dùng Translation/WritingPractice/ReadingComprehension ở bộ core A1.

`A1-20` là step ôn tập level: 10 bài trộn các chủ đề A1-01…A1-19, mỗi bài ghi chủ đề trong `focus`.

## Chất lượng (bắt buộc — đáp án sai là lỗi nặng nhất)

1. Mỗi câu đúng **một** đáp án đúng rõ ràng (MC: đúng 1 option đúng; không có option "cũng đúng" theo ngữ pháp). Liệt kê đủ biến thể chấp nhận được trong `acceptedAnswers`/`validSequences`/`referenceCorrections` (dạng rút gọn `isn't`/`is not`, `don't`/`do not`, viết hoa đầu câu không quan trọng vì `caseSensitive:false`).
2. Chỉ dùng ngữ pháp của step hiện tại và các step **trước** nó trong A1 (theo thứ tự trong `grammar-a1.json`); không dùng thì/cấu trúc của step sau. Từ vựng A1 thông dụng. Câu tiếng Anh ≤ 12 từ.
3. Distractor dựa trên `commonMistakes` của step.
4. `title`, `instructions`, `explanation`, `targetErrors`, `context` viết **tiếng Việt** ngắn gọn; `explanation` nói vì sao đúng theo quy tắc.
5. Không trùng câu giữa các bài và giữa các step. Thỉnh thoảng dùng tên/địa danh Việt Nam.
6. SentenceUnscramble: liệt kê mọi trật tự hợp lệ trong `validSequences` (theo id token); token chứa dấu câu cuối gắn vào từ cuối hoặc là token riêng nhất quán.
7. TrueFalse: 4–6 nhận định "câu này đúng ngữ pháp?" có cả đúng và sai.
8. Matching: 4–6 cặp, `rightItems` xáo trộn, mỗi trái khớp đúng một phải.
9. FillInTheBlanks: 4–8 chỗ trống; `hint` là từ gốc trong ngoặc khi cần chia, vd. "(be)".
10. ErrorCorrection: đúng 1 lỗi thuộc điểm ngữ pháp của step.
11. **Không** đặt key đáp án (`acceptedAnswers`, `correctOptionIds`, `statementAnswers`, `correctPairs`, `validSequences`, `referenceCorrections`, `explanation`, …) trong `content` — chỉ trong `grading`.

## Tự kiểm trước khi kết thúc

- File là JSON hợp lệ (`node -e "JSON.parse(require('fs').readFileSync('docs/tasks/20261029-grammar-roadmap/g6/a1-exercises.json','utf8'))"`), 20 khóa × 10 bài, key duy nhất.
- Đọc lại từng bài và tự giải: đáp án trong `grading` khớp đúng câu hỏi; id trong `grading` tồn tại trong `content`.

## Worker report

```
## Worker report
- File:
- Số step / số bài:
- Bài bạn không chắc đáp án (key + lý do):
- Chưa làm:
```
