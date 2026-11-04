# P2e — audio qua Speech.Contracts, prompt builder về LiveTutor, allowlist = 0 (D11, D12)

## Bước 0 — sửa theo review P2d (làm trước)

`Features/Pronunciation/Rubrics/LessonRubricExerciseTypes.cs` chép lại giá trị `ExerciseType` thành hằng số và tự viết `TryParse`/`Format` → trùng lặp và có thể khác hành vi `Enum.TryParse` (chuỗi số, ignoreCase…). Sửa:

- Chuyển `ExerciseType`, `GradingMode`, `SubQuestionKind` từ `src/EnglishLiveTutor.Domain/Lessons/Contracts/` sang Shared Kernel `src/EnglishLiveTutor.Domain/Common/Exercises/`, namespace `EnglishLiveTutor.Domain.Common.Exercises` (khái niệm dùng chung của Lessons, Grammar và rubric Speech). Giữ tên, giá trị, attribute. Sửa mọi `using`. Xóa thư mục `Domain/Lessons/Contracts/` nếu rỗng.
- Xóa `LessonRubricExerciseTypes.cs`. Trong `RubricCommands.cs`, `RubricQueries.cs`, `LessonRubricDetailBuilder.cs`, `LessonRubricSeeder.cs` trả lại **đúng** biểu thức của bản gốc ở HEAD (`git show HEAD:EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Lessons/Commands/RubricCommands/RubricCommands.cs` v.v. — ví dụ `Enum.TryParse<ExerciseType>(req.ExerciseType, ignoreCase: true, out var parsedType)`, `((ExerciseType)x).ToString()`, `ExerciseType.FillInTheBlanks`), chỉ khác `using`.
- `ModuleMap`: namespace `EnglishLiveTutor.Domain.Common.*` đã là Shared — xác nhận scanner không báo.

Theo `plan-p2.md` D11, D12. Mục tiêu: `module-boundary-allowlist.txt` chỉ còn dòng comment (0 vi phạm). Không đổi hành vi, route, JSON, hub.

## Yêu cầu

1. **D11 — audio:**
   - `Features/Pronunciation/Contracts/IAudioRecordingStore` (+ DTO record) với đúng các thao tác mà Lessons đang gọi trên `IAudioRecordingRepository`/`AudioRecordingEntity` (đọc: `Features/Lessons/Commands/MediaOperations/UploadLessonMediaCommand.cs`, `Features/Lessons/Queries/MediaQueryOperations/GetLessonMediaQuery.cs`, `GetLessonMediaStreamQuery.cs`, `Features/Lessons/Commands/RolePlayOperations/SendRolePlayTurnCommand.cs`, `Features/Lessons/Commands/AttemptOperations/SubmitExerciseAnswerCommand.cs`). Impl `Features/Pronunciation/Services/AudioRecordingStore.cs` bọc repository hiện có (giữ nguyên owner check/điều kiện, cách tạo entity, thời điểm commit — nếu caller hiện commit qua UoW chung thì store **không** tự `SaveChanges`).
   - `Features/Pronunciation/Contracts/IPronunciationAudioSynthesizer`: tách phần dùng chung mà `Features/Lessons/Commands/SynthesizeExerciseAudio/SynthesizeExerciseAudioCommand.cs` đang làm với `PronunciationSynthesis*`, `IGeneratedPronunciationAudioRepository`, `IPronunciationSynthesisLock` (cache lookup, single-flight lock, gọi provider, lưu cache) vào impl `Features/Pronunciation/Services/PronunciationAudioSynthesizer.cs`. `SynthesizeExerciseAudioCommand` (Lessons) gọi port. `Features/VocabularyGames/Commands/SynthesizeFallingWordAudio/*` gọi **port Speech** thay vì type của Lessons (`Lessons.Commands.SynthesizeExerciseAudio`) — giữ nguyên request/response public của command VocabularyGames; nếu nó đang tái dùng record request/response của Lessons làm contract HTTP, tạo record tương đương trong VocabularyGames **cùng tên property, cùng thứ tự** và kiểm JSON không đổi. `Api/Endpoints/VocabularyGamesEndpoints.cs` bỏ tham chiếu Lessons.
   - `Infrastructure/Lessons/LessonAiGrader.cs` nếu còn dùng `Domain.Pronunciation.*` → dùng Contracts/snapshot (P2d).
   - `Application/Common/Interfaces/ILessonServices.cs` (map Lessons) không được tham chiếu `Domain.Pronunciation`.
2. **D12 — prompt builder:**
   - Trong `ModuleMap.cs`, map `Application/Common/Interfaces/ICefrPromptBuilder.cs`, `Infrastructure/Ai/CefrPromptBuilder.cs` về **LiveTutor**. `Features/Prompts/Queries/PreviewSystemPrompt/` (đang thuộc Catalog, dùng `ICefrPromptBuilder` của LiveTutor → ngược DAG): chuyển thư mục query này sang `Features/Conversations/Queries/PreviewSystemPrompt/` (namespace theo thư mục) và mapping endpoint tương ứng giữ nguyên route (nếu endpoint nằm trong `PromptEndpoints.cs`, chuyển sang `ConversationEndpoints.cs` hoặc giữ file và map file endpoint đúng module — chọn cách giữ route y nguyên, liệt kê route trước/sau).
   - `CefrPromptBuilder` đọc bảng Catalog (`CefrLevelPrompts`, `ConversationModePrompts`, `SpeechSpeeds`… qua `IDbContextFactory<AppDbContext>`): thay bằng `Features/Prompts/Contracts/IPromptCatalogReader` (impl `Features/Prompts/Services/PromptCatalogReader.cs`, `IModuleDbContextFactory<ICatalogDbContext>` vì builder là singleton dùng trong realtime — **giữ lifetime singleton của builder**, reader cũng phải an toàn singleton: dùng factory, không giữ context scoped). Query giữ nguyên.
3. Các dòng `internal`/`dag` còn lại khác (đọc allowlist): xử lý theo cùng nguyên tắc (Contracts của module được phép trong DAG, hoặc map lại file về đúng module nếu file vốn thuộc module kia). `appdb` còn lại: chuyển sang interface module nếu đã hết phụ thuộc chéo. Không được xử lý bằng cách sửa scanner/ModuleMap để bỏ qua vi phạm. Vi phạm nào cần quyết định thiết kế khác → dừng, báo (ghi dòng allowlist + lý do).
4. Test hiện có pass không đổi assertion (chỉ wiring/`using`); thêm test cho `AudioRecordingStore` (owner check giữ nguyên), `PronunciationAudioSynthesizer` (cache hit không gọi provider — có thể tái dùng test cache hiện có bằng cách chạy lại), `PromptCatalogReader`.

## Kiểm chứng

Như P2a + `dotnet test --filter "FullyQualifiedName~Architecture"`. Mục tiêu allowlist: 0 dòng vi phạm. Report theo mẫu P2a, thêm "Route trước → sau", "Lifetime DI mới", "Vi phạm còn lại + lý do".
