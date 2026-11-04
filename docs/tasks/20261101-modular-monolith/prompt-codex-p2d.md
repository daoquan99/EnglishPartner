# P2d — rubric thuộc Speech (D9, D10)

Theo `plan-p2.md` D9, D10. Không đổi hành vi, route, JSON. Allowlist chỉ bớt.

## Yêu cầu

1. **D10 — chuyển CRUD rubric bài học sang Speech** (di chuyển file, chỉ đổi namespace/`using`):
   - `Features/Lessons/Commands/RubricCommands/RubricCommands.cs` → `Features/Pronunciation/Rubrics/Commands/RubricCommands.cs`
   - `Features/Lessons/Queries/RubricQueries/RubricQueries.cs` → `Features/Pronunciation/Rubrics/Queries/RubricQueries.cs`
   - `Features/Lessons/Services/LessonRubricDetailBuilder.cs` (+ interface `ILessonRubricDetailBuilder` nếu khai báo ở `Application/Common/Interfaces/ILessonServices.cs` thì chuyển interface đó vào `Features/Pronunciation/Rubrics/`) → `Features/Pronunciation/Rubrics/Services/`
   - DTO rubric bài học trong `Application/Common/Models/LessonRubricDtos.cs` → `Features/Pronunciation/Rubrics/Models/LessonRubricDtos.cs` (giữ tên type, property, thứ tự property — JSON không đổi).
   - Các file này chuyển sang `ISpeechDbContext` (chúng chỉ dùng `ScoringRubrics` + `SaveChangesAsync`).
   - `src/EnglishLiveTutor.Api/Endpoints/LessonRubricEndpoints.cs` giữ nguyên route `/api/lesson-rubrics/*`; cập nhật `using`. Trong `ModuleMap.cs`, map file endpoint này và `Infrastructure/Persistence/LessonRubricSeeder.cs` về **Speech** (thay vì Lessons).
2. **D9 — snapshot rubric cho Lessons/Grammar:** `Features/Pronunciation/Contracts/IScoringRubricProvider` + record `ScoringRubricSnapshot` (+ `ScoringRubricCriterionSnapshot`) chứa đúng các member mà caller đang đọc từ `ScoringRubricEntity`/criteria (đọc code: `LessonAiGrader.cs`, `LessonCandidateValidator.cs`, `SubmitExerciseAnswerCommand.cs` ~217 và ~327, `RetryAnswerGradingCommand.cs`, `SubmitGrammarAnswerCommand.cs`, `ILessonServices.cs` ~65). Method provider = đúng các query hiện có (theo id, theo code/active… — chép predicate, `AsNoTracking`, `Include` criteria nếu đang có). Impl `Features/Pronunciation/Services/ScoringRubricProvider.cs` (`ISpeechDbContext`, scoped, đăng ký DI).
   - `ILessonAiGrader` (trong `ILessonServices.cs`) đổi tham số `ScoringRubricEntity rubric` → `ScoringRubricSnapshot rubric`; `Infrastructure/Lessons/LessonAiGrader.cs` đọc từ snapshot. Logic chấm (thứ tự criteria, trọng số, làm tròn, prompt text) **giữ nguyên ký tự**; nếu entity có method tính toán mà grader gọi, đưa giá trị đã tính vào snapshot hoặc chép method thành static helper trong Contracts (ghi rõ trong report).
   - Caller Lessons/Grammar dùng provider; sau đó file nào chỉ còn `DbSet` module mình → chuyển sang `ILessonsDbContext`/`IGrammarDbContext`.
3. Test: test hiện có cho chấm bài, rubric bài học, endpoint rubric phải pass không đổi assertion (chỉ wiring/`using`). Thêm test `ScoringRubricProvider` (map đủ criteria, thứ tự giống entity, không thấy → null) và một test so sánh: `LessonAiGrader` cho cùng input entity/snapshot tạo cùng prompt/kết quả (nếu đã có test grader, chỉ cần chạy lại).
4. Allowlist shrink.

## Kiểm chứng

Như P2a. Baseline: số pass sau P2c, 0 fail. Report theo mẫu P2a, thêm "Member trong ScoringRubricSnapshot" và "File đã di chuyển (từ → đến)".
