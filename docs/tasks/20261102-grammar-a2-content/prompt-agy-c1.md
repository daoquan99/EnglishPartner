# C1 — Soạn bộ bài core cho 20 step A2

Đây là việc soạn nội dung, **không sửa code**. Chỉ tạo đúng một file mới:
`docs/tasks/20261102-grammar-a2-content/a2-exercises.json`

Không sửa `grammar-a2.json` hay bất kỳ file nào khác. Orchestrator sẽ tự gộp và kiểm bằng importer thật.

## Đọc trước

- `docs/tasks/20261029-grammar-roadmap/exercise-prompt.md`: **đặc tả bắt buộc**, gồm phần Template (Quality rules, Grammar scope) và bảng "Schema theo loại bài" (tên field chính xác).
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Infrastructure/Grammar/ContentPacks/grammar-a2.json`: lý thuyết của 20 step A2 (formulas, commonMistakes, examples). Mỗi bài phải kiểm đúng cấu trúc của step mà nó thuộc về.
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Infrastructure/Grammar/ContentPacks/grammar-a1.json`: **mẫu đã duyệt**, gồm 200 bài A1 trong `steps[].exercises`. Theo đúng định dạng và văn phong của file này.
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Grammar/Rules/GrammarExerciseRules.cs` và `EnglishPartner.BE/src/Shared/EnglishLiveTutor.Shared.Application/Common/Exercises/ExerciseContentValidator.cs`: luật validate thật (giới hạn số lựa chọn, token, blank, các key bị cấm trong `content`…). Mọi bài phải qua được hai validator này.
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Grammar/ContentPacks/GrammarContentPackModels.cs`: `GrammarContentPackExerciseDto`, tức các field của mỗi bài.

## Định dạng file

```json
{
  "A2-01": [ { "key": "A2-01-E01", "type": "MultipleChoice", "difficulty": 1, "focus": "...", "title": "...", "instructions": "...", "maxPoints": 10, "gradingMode": "Deterministic", "isCore": true, "content": { }, "grading": { } } ],
  "...": [],
  "A2-20": [ ]
}
```

- Đủ 20 khóa, đúng mã step trong `grammar-a2.json`.
- `key` có dạng `<code>-E<NN>` (từ `E01` đến `E10`) và không trùng.
- Mọi bài đều có `isCore: true`, `maxPoints: 10`, `gradingMode: "Deterministic"`.

## Mỗi step đúng 10 bài, **chỉ dùng loại Deterministic**

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

Step cuối của A2 (step có `isLevelReview: true` trong `grammar-a2.json`) là step ôn tập level: 10 bài trộn các chủ đề của A2. Mỗi bài ghi chủ đề của nó trong `focus`.

## Chất lượng (bắt buộc; đáp án sai là lỗi nặng nhất)

1. Mỗi câu có đúng **một** đáp án đúng rõ ràng.
   - MultipleChoice: chỉ một option đúng. Không có option nào "cũng đúng" về ngữ pháp, nhất là các cặp dễ lẫn ở A2: `will`/`going to`, Past Simple/Present Perfect, `some`/`any`, dạng so sánh.
   - Liệt kê đủ các biến thể chấp nhận được trong `acceptedAnswers`, `validSequences` hoặc `referenceCorrections` (dạng rút gọn và dạng đầy đủ: `haven't`/`have not`, `didn't`/`did not`, `I'm`/`I am`…).
   - Câu nào vẫn mơ hồ thì viết lại hoặc bỏ.
2. Chỉ dùng ngữ pháp của toàn bộ A1, của step đang soạn và của các step A2 **đứng trước** nó (theo thứ tự trong `grammar-a2.json`). Không dùng cấu trúc của step sau. Từ vựng ở mức A1–A2 thông dụng. Câu tiếng Anh không quá 15 từ.
3. Distractor xây dựa trên `commonMistakes` của step.
4. Các field `title`, `instructions`, `explanation`, `targetErrors`, `context` viết **tiếng Việt**, ngắn gọn. `explanation` giải thích vì sao đáp án đúng theo quy tắc.
5. Không trùng câu giữa các bài, giữa các step, và với các câu trong `grammar-a1.json`. Thỉnh thoảng dùng tên người, địa danh Việt Nam.
6. SentenceUnscramble: liệt kê mọi trật tự hợp lệ trong `validSequences` (ghi theo id token). Dấu câu cuối câu xử lý nhất quán: hoặc gắn vào từ cuối, hoặc tách thành token riêng.
7. TrueFalse: 4–6 nhận định dạng "câu này đúng ngữ pháp?", có cả nhận định đúng lẫn sai.
8. Matching: 4–6 cặp. `rightItems` phải xáo trộn. Mỗi mục bên trái khớp với đúng một mục bên phải.
9. FillInTheBlanks: 4–8 chỗ trống. Khi cần chia động từ hay đổi dạng, `hint` ghi từ gốc trong ngoặc, ví dụ "(go)".
10. ErrorCorrection: mỗi câu có đúng 1 lỗi, và lỗi đó thuộc điểm ngữ pháp của step.
11. **Không** đặt các key đáp án (`acceptedAnswers`, `correctOptionIds`, `statementAnswers`, `correctPairs`, `validSequences`, `referenceCorrections`, `explanation`, …) trong `content`. Các key này chỉ được nằm trong `grading`.

## Tự kiểm trước khi kết thúc

- File là JSON hợp lệ. Kiểm bằng lệnh: `node -e "const p=JSON.parse(require('fs').readFileSync('docs/tasks/20261102-grammar-a2-content/a2-exercises.json','utf8'));console.log(Object.keys(p).length, Object.values(p).flat().length)"`. Kết quả mong đợi là `20 200`, và mọi key đều không trùng.
- Đọc lại từng bài và tự giải. Đáp án trong `grading` phải khớp đúng câu hỏi. Mọi id dùng trong `grading` phải có trong `content`.

## Worker report

```
## Worker report
- File:
- Số step / số bài:
- Bài bạn không chắc đáp án (key + lý do):
- Chưa làm:
```
