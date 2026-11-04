# P2 — gỡ phụ thuộc chéo bằng Contracts (thiết kế của orchestrator)

Mục tiêu P2: allowlist `dag` = 0, `internal` = 0, `appdb` chỉ còn file đã chuyển được hết. Không đổi route, JSON, hub, schema; mọi thay đổi hành vi = 0 (cùng thứ tự kiểm tra, cùng message lỗi, cùng commit).

## Quyết định thiết kế

| # | Phụ thuộc hiện tại | Cách gỡ |
| --- | --- | --- |
| D1 | Grammar, Vocabulary dùng `VocabularyGenerationWriter.IsUniqueConstraintViolation` (static) | Chuyển sang Shared Kernel `Application/Common/Persistence/DbUpdateExceptionClassifier.cs` |
| D2 | Grammar (và rubric Speech) dùng `Domain.Lessons.Enums` (`ExerciseType`, `GradingMode`, `SubQuestionKind`) | (Sửa ở P2e) Chuyển 3 enum vào Shared Kernel `Domain/Common/Exercises/` namespace `EnglishLiveTutor.Domain.Common.Exercises` (giữ tên + giá trị; snapshot EF không chứa namespace enum). `LessonStatus` ở lại |
| D3 | Speech/Vocabulary query `GetPronunciationModels`, `GetVocabularyGenerationModels` trả `AiModelConfigEntity` (Catalog) | Chuyển 2 query+handler vào `Features/AiModels/Queries/`, chuyển mapping endpoint sang `AiModelEndpoints.cs` **cùng route, method, auth, rate limit, kiểu trả về** |
| D4 | Lessons, Speech, Vocabulary đọc `AiModelCatalog` theo `ModelCode` | `Features/AiModels/Contracts/IAiModelCatalogReader` → `AiModelInfo` DTO chứa đúng các member caller dùng (kể cả giá trị của `GetEffectiveModelCode()`); impl `Features/AiModels/Services/AiModelCatalogReader.cs` qua `ICatalogDbContext` |
| D5 | Learning đọc `Users` (TimeZoneId, PreferredCefrLevel); Grammar (`ListGrammarModerationQueueQueryHandler`, `ListGrammarExercisesQuery`) đọc `Users` để hiện tên tác giả | ADR: mọi module được dùng `Identity.Contracts`. `Features/Identity/Contracts/IUserDirectory`: `GetLearningProfileAsync(userId)` → `UserLearningProfile(TimeZoneId, PreferredCefrLevel)` (giữ điều kiện id-hoặc-username hiện tại); `ListUsersByIdOrUsernameAsync(IReadOnlyCollection<string>)`, `ListUsersByIdsAsync(IReadOnlyCollection<Guid>)`, `ListUsersByUsernamesAsync(IReadOnlyCollection<string>)` → `UserNameInfo(Guid Id, string Username, string? FullName)` với đúng predicate hiện tại; caller tự ghép tên như cũ |
| D6 | Vocabulary/Learning đọc `PronunciationAssessments` | `Features/Pronunciation/Contracts/IPronunciationScoreReader`: điểm mới nhất theo (user, sense), theo danh sách sense, tổng số + trung bình theo user — query giữ nguyên |
| D7 | Speech đọc `Vocabularies/Senses/Examples` để lấy câu tham chiếu (ngược DAG) | **Đảo phụ thuộc**: `Features/Pronunciation/Contracts/IPronunciationTargetTextSource` (port của Speech), impl ở Vocabulary `Features/Vocabulary/Services/PronunciationTargetTextSource.cs`. Giữ nguyên thứ tự kiểm tra và message lỗi |
| D8 | Speech cập nhật `UserVocabularyProgresses` sau khi chấm (ngược DAG) | Port `IPronunciationAssessmentObserver.OnAssessedAsync(userId, senseId, overallScore)` trong Speech.Contracts, impl ở Vocabulary qua `IVocabularyDbContext`. Gọi **trước** `CommitChangesAsync` như hiện tại → cùng một commit (cùng instance `AppDbContext` scoped). P4 đổi sang outbox |
| D9 | Lessons và Grammar (`SubmitGrammarAnswerCommand`) đọc `ScoringRubrics` để chấm | `Features/Pronunciation/Contracts/IScoringRubricProvider` → `ScoringRubricSnapshot` (+ criteria) DTO; `LessonAiGrader`, `LessonCandidateValidator`, các command chấm dùng snapshot |
| D10 | Lessons CRUD rubric (`RubricCommands`, `RubricQueries`, `LessonRubricDetailBuilder`) ghi `ScoringRubricEntity` | Rubric thuộc Speech: chuyển các file này + `LessonRubricEndpoints.cs` + `LessonRubricSeeder.cs` sang module Speech (`Features/Pronunciation/Rubrics/…`), giữ route. Nếu `LessonRubricDetailBuilder` cần dữ liệu Lessons → dừng, báo |
| D11 | Lessons media/role-play dùng `IAudioRecordingRepository`; Lessons/VocabularyGames tổng hợp audio qua `PronunciationSynthesis*`, `IGeneratedPronunciationAudioRepository`; VocabularyGames gọi command `SynthesizeExerciseAudio` của Lessons | Speech.Contracts: `IAudioRecordingStore` (thao tác Lessons đang dùng, DTO thay entity) và `IPronunciationAudioSynthesizer` (logic dùng chung đang nằm trong `SynthesizeExerciseAudioCommand`). VocabularyGames gọi port Speech thay vì command Lessons (Vocabulary → Lessons không có trong DAG) |
| D12 | `ICefrPromptBuilder` (map Catalog) dùng `ChatMessageEntity` (LiveTutor) | Prompt builder là của phiên hội thoại: map `ICefrPromptBuilder`, `CefrPromptBuilder`, `PreviewSystemPromptQueryHandler` về LiveTutor; đọc prompt Catalog qua `Features/Prompts/Contracts/IPromptCatalogReader` |

DAG sau P2 (cập nhật ADR + `ModuleMap.AllowedDependencies`): mọi module → Identity (Contracts); Speech → Catalog; Lessons → Speech, Catalog; Grammar → Lessons, Speech; Vocabulary → Speech, Catalog; LiveTutor → Catalog. **Grammar → Vocabulary bỏ** (D1 gỡ hết).

Contracts đặt ở `Application/Features/<Feature>/Contracts/` (Application) và `Domain/<Area>/Contracts/` (enum). Đăng ký DI impl trong `Application/DependencyInjection.cs` (scoped).

## Gói

| Gói | Nội dung | Session |
| --- | --- | --- |
| P2a | D1, D2, D3, D4 + cập nhật DAG (ADR, skill, `ModuleMap`) | Codex session mới (P2) |
| P2b | D5, D6 | resume |
| P2c | D7, D8 (+ chuyển nốt `AssessPronunciation`, `PronunciationSynthesisResolution` sang `ISpeechDbContext`) | resume |
| P2d | D9, D10 | resume |
| P2e | D11, D12; allowlist `dag`/`internal` = 0 | resume |

Mỗi gói: build 0 lỗi, 570+ pass / 0 fail, `has-pending-model-changes` không đổi, allowlist chỉ giảm (ngoại lệ: dòng mới do **đổi map đúng module** mà orchestrator liệt kê đích danh trong prompt).
