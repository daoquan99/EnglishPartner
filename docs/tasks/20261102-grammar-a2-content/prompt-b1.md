# Đợt 2: Soạn bộ bài core cho 18 step B1

Việc này chỉ là soạn nội dung, **không sửa code**. Chỉ tạo đúng một file mới:
`docs/tasks/20261102-grammar-a2-content/b1-exercises.json`

Không sửa `grammar-b1.json` hay bất kỳ file nào khác. Orchestrator sẽ tự gộp và kiểm bằng importer thật.

## Đọc trước

- `docs/tasks/20261029-grammar-roadmap/exercise-prompt.md`: **đặc tả bắt buộc**, gồm phần Template (Quality rules, Grammar scope) và bảng "Schema theo loại bài" (tên field chính xác).
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Infrastructure/Grammar/ContentPacks/grammar-b1.json`: lý thuyết của 18 step B1 (formulas, commonMistakes, examples). Mỗi bài phải kiểm đúng cấu trúc của step chứa nó.
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Infrastructure/Grammar/ContentPacks/grammar-a2.json`: **mẫu đã duyệt**, gồm 200 bài A2 trong `steps[].exercises`, đã qua sửa lỗi. Theo đúng định dạng và văn phong của file này. Câu B1 không được trùng với câu ở `grammar-a1.json` và `grammar-a2.json`.
- `docs/tasks/20261102-grammar-a2-content/review/a2-review.md`, phần "Đã sửa": các lỗi AGY đã mắc ở A2. **Không lặp lại các lỗi này.**
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Grammar/Rules/GrammarExerciseRules.cs` và `EnglishPartner.BE/src/Shared/EnglishLiveTutor.Shared.Application/Common/Exercises/ExerciseContentValidator.cs`: luật validate thật. Mọi bài phải qua được hai validator này.
- `EnglishPartner.BE/src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Application/Features/Grammar/ContentPacks/GrammarContentPackModels.cs`: các field của mỗi bài.

## Định dạng file

```json
{
  "B1-01": [ { "key": "B1-01-E01", "type": "MultipleChoice", "difficulty": 1, "focus": "...", "title": "...", "instructions": "...", "maxPoints": 10, "gradingMode": "Deterministic", "isCore": true, "content": { }, "grading": { } } ],
  "...": [],
  "B1-18": [ ]
}
```

- Đủ 18 khóa, từ `B1-01` đến `B1-18`.
- `key` có dạng `<code>-E<NN>`, từ `E01` đến `E10`, và không trùng.
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

`B1-18` là step ôn tập cả level, gồm 10 bài trộn các chủ đề B1. Mỗi bài ghi chủ đề của nó trong `focus`.

## Chất lượng (bắt buộc; đáp án sai là lỗi nặng nhất)

1. **Mỗi câu chỉ có một đáp án đúng rõ ràng.**
   - MultipleChoice: chỉ một option đúng. Ở B1, các cặp hay bị đúng cả hai là:
     - `have been doing` / `have done`: với `for`/`since` và động từ trạng thái
     - Past Perfect / Past Simple: khi đã có `before`/`after`, cả hai đều đúng
     - `used to` / `would`: `would` không dùng cho trạng thái
     - `will be doing` / `will do`
     - Mệnh đề quan hệ: `who` / `that` / `which`, và có thể lược đại từ
     - Reported speech: có lùi thì hay không khi câu gốc vẫn còn đúng
     - Modal suy luận: `must` / `might` / `can't`
     - gerund / infinitive với `like`, `start`, `begin`: cả hai dạng đều đúng
   - Nếu có hai cách đúng thì đưa **cả hai** vào đáp án được chấp nhận, hoặc sửa câu cho chỉ còn một cách đúng.
   - Liệt kê đủ biến thể trong `acceptedAnswers`, `validSequences` hoặc `referenceCorrections`: dạng rút gọn và dạng đầy đủ, `that` có hay không trong reported speech và mệnh đề quan hệ, các dạng phrasal verb tách hoặc không tách.
2. **Vị trí đáp án đúng của MC phải rải đều.** Trong cả file, mỗi vị trí option (1, 2, 3, 4) có khoảng 1/4 số đáp án đúng. Không được đặt đáp án đúng ở option đầu tiên phần lớn thời gian.
3. **SentenceUnscramble:**
   - Thứ tự token trong `content` không được trùng với đáp án.
   - Nếu có token trùng chữ (ví dụ hai chữ `the`), mọi hoán vị hợp lệ của các token đó phải có trong `validSequences`.
   - Liệt kê mọi trật tự hợp lệ khác, ví dụ trạng từ hoặc mệnh đề `if` đặt đầu hay cuối câu.
4. **Matching:** mỗi mục bên trái chỉ ghép được với đúng một mục bên phải, kể cả khi xét ghép chéo. Nên dùng cặp hỏi–đáp hoặc cặp mà ý nghĩa chỉ khớp duy nhất một cách.
5. **FillInTheBlanks:** câu phải có mốc thời gian hoặc ngữ cảnh đủ rõ để chỉ một thì đúng.
6. **Phạm vi ngữ pháp:** chỉ dùng ngữ pháp của toàn bộ A1–A2, của step đang soạn và của các step B1 **đứng trước** nó, theo thứ tự trong `grammar-b1.json`. Ví dụ: chưa được dùng câu bị động trước `B1-07`, chưa dùng reported speech trước `B1-10`. Từ vựng ở mức A2–B1 thông dụng. Câu tiếng Anh không quá 18 từ.
7. Distractor dựa trên `commonMistakes` của step.
8. Các field `title`, `instructions`, `explanation`, `targetErrors`, `context` viết bằng **tiếng Việt**, ngắn gọn. `explanation` giải thích vì sao đáp án đúng theo quy tắc.
9. Không trùng câu giữa các bài, giữa các step, và với A1, A2. Thỉnh thoảng dùng tên người, địa danh Việt Nam.
10. TrueFalse có 4–6 nhận định, gồm cả nhận định đúng lẫn sai. Matching có 4–6 cặp, và `rightItems` phải được xáo trộn. FillInTheBlanks có 4–8 chỗ trống; khi phải chia hay đổi dạng, `hint` ghi từ gốc, ví dụ "(go)". ErrorCorrection: mỗi câu có đúng 1 lỗi, và lỗi đó thuộc điểm ngữ pháp của step.
11. **Không** đặt key đáp án trong `content`, ví dụ `acceptedAnswers`, `correctOptionIds`, `statementAnswers`, `correctPairs`, `validSequences`, `referenceCorrections`, `explanation`. Các key này chỉ được nằm trong `grading`.

## Tự kiểm trước khi kết thúc

- File là JSON hợp lệ. Lệnh `node -e "const p=JSON.parse(require('fs').readFileSync('docs/tasks/20261102-grammar-a2-content/b1-exercises.json','utf8'));console.log(Object.keys(p).length, Object.values(p).flat().length)"` phải in ra `18 180`. Không có key bài nào bị trùng.
- Đếm vị trí đáp án đúng của các câu MC (yêu cầu 2).
- Đọc lại và tự giải từng bài. Đáp án trong `grading` phải khớp với câu hỏi, và mọi id dùng trong `grading` phải có trong `content`.

## Worker report

```
## Worker report
- File:
- Số step / số bài:
- Phân bố vị trí đáp án đúng MC:
- Bài bạn không chắc đáp án (key + lý do):
- Chưa làm:
```
