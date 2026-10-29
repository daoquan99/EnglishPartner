# G3 — Backend: sinh bài tập bằng AI, bài của tôi, kiểm duyệt

Đọc trước: `docs/tasks/20261029-grammar-roadmap/plan.md`, **`docs/tasks/20261029-grammar-roadmap/exercise-prompt.md` (đặc tả prompt — làm đúng từng điểm)**, code G1/G2 trong worktree, luồng sinh từ vựng hiện có làm mẫu (`Features/Vocabulary/Commands/GenerateVocabulary/**`, `Queries/BuildVocabularyGenerationPrompt/**`, `Commands/ImportVocabularyJson/**`, `Features/Vocabulary/Services/VocabularyPromptBuilder.cs`, `Infrastructure/Ai/GeminiVocabularyAiGenerator.cs`), skill `EnglishPartner.BE/.agents/skills/ai-provider-integration/SKILL.md`, `.agents/skills/security-audit/SKILL.md`, `backend-testing/SKILL.md`, ADR-0003.

Không làm FE.

## 1. Prompt builder

`Features/Grammar/Services/GrammarExercisePromptBuilder.cs` (Application, thuần, không I/O): `Build(GrammarExercisePromptInput) → string` theo **đúng** template, placeholder, schema bảng và giới hạn trong `exercise-prompt.md`. `{{learnerNote}}` JSON-encode (System.Text.Json) rồi đặt trong `<<<…>>>`, cắt ≤ 300 ký tự trước khi encode. Đầu vào lấy từ DB bởi một service `GrammarExercisePromptContextLoader` (step, theory, tiêu đề step trước/sau theo lộ trình).

## 2. Port AI

`Features/Grammar/Ports/IGrammarExerciseAiGenerator.cs`: `Task<string> GenerateRawAsync(string prompt, GrammarAiExecutionConfig config, CancellationToken ct)` trả raw text. Infrastructure `Infrastructure/Ai/GeminiGrammarExerciseAiGenerator.cs` dùng cùng cách gọi Gemini như generator từ vựng **nhưng gửi key qua header `x-goog-api-key`** (không query `?key=`), `responseMimeType: application/json`, timeout cấu hình `Ai:GrammarGeneration:TimeoutSeconds` mặc định 120 (thêm vào `appsettings.json` không có secret). Hỗ trợ provider/model như generate từ vựng (đọc cách request từ vựng chọn provider/model/BYO key và AGY local — giữ cùng hành vi). Không log prompt đầy đủ, response, hay key.

## 3. Parser + lưu

`Features/Grammar/Services/GrammarExerciseOutputProcessor.cs`: bóc code fence, parse `{ exercises: [...] }`, kiểm theo mục "Kiểm tra phía server" của `exercise-prompt.md` (`GrammarExerciseRules`, cắt dư theo type, hash trùng với Official/Approved của step và bài của owner, difficulty ngoài 1–3 → 2, `focus` ≤ 200 ký tự). Trả `(valid[], rejected[{index, issues[]}])`.

## 4. Use case learner (permission `grammar.generate`)

| Use case | Route | Ghi chú |
| --- | --- | --- |
| `GenerateGrammarExercisesCommand { stepCode, items[{type, count, difficulty}], learnerNote?, visibility, aiConfig }` | POST `/api/grammar/steps/{code}/exercises/generate` | Gọi provider **ngoài transaction**, rồi xử lý output, rồi tạo `GrammarExerciseEntity.CreateUserOwned(source: UserAi)` + một commit. Trả `{ created: GrammarMyExerciseDto[], rejected[] }`; 0 hợp lệ → 422 `grammar_generation_no_valid_exercises` kèm rejected |
| `BuildGrammarExercisePromptQuery { stepCode, items, learnerNote? }` | POST `/api/grammar/steps/{code}/exercises/prompt` | Trả `{ prompt }`, cùng builder, không gọi AI, không ghi DB |
| `ImportGrammarExercisesJsonCommand { stepCode, json, visibility, expectedItems? }` | POST `/api/grammar/steps/{code}/exercises/import` | `source: UserImport`; json ≤ 200 000 ký tự; cùng processor |
| `ListMyGrammarExercisesQuery { stepCode?, moderation?, page, pageSize }` | GET `/api/grammar/my-exercises` | Kèm grading (đây là bài của chính user), `rejectReason` |
| `UpdateMyGrammarExerciseCommand { id, title, instructions, difficulty, content, grading }` | PUT `/api/grammar/my-exercises/{id}` | Owner; validate; `EditContent` (Public → Pending) |
| `ChangeMyGrammarExerciseVisibilityCommand { id, visibility }` | POST `/api/grammar/my-exercises/{id}/visibility` | Owner; `ChangeVisibility` |
| `DeleteMyGrammarExerciseCommand { id }` | DELETE `/api/grammar/my-exercises/{id}` | Owner; soft delete. Bài đã Promote thành Official không còn là của user → 404 |

Giới hạn (cấu hình `Grammar:Generation`): tổng item 1–10/lần (admin 1–20), ≤ 40 bài tạo mới/ngày/user (đếm `CreatedAtUtc` trong 24h của bài user-owned), ≤ 30 bài Pending cùng lúc/user (Public vượt → 409 `grammar_pending_limit`; Private vẫn được). Vượt quota ngày → 429 `grammar_daily_quota_exceeded`. Endpoint generate gắn rate limit policy AI hiện có; prompt/import gắn policy chung hiện có.

Không phải owner hoặc không tồn tại → 404 (Luật 2).

## 5. Admin

| Use case | Route | Permission |
| --- | --- | --- |
| `ListGrammarModerationQueueQuery { status=Pending, level?, stepCode?, page }` | GET `/api/admin/grammar/moderation` | `grammar.moderate` |
| `ApproveGrammarExerciseCommand { id }` | POST `/api/admin/grammar/exercises/{id}/approve` | `grammar.moderate` |
| `RejectGrammarExerciseCommand { id, reason }` (1–500 ký tự) | POST `/api/admin/grammar/exercises/{id}/reject` | `grammar.moderate` |
| `PromoteGrammarExerciseCommand { id, isCore }` | POST `/api/admin/grammar/exercises/{id}/promote` | `grammar.manage` |
| `GenerateOfficialGrammarExercisesCommand` (giống learner, tạo Official, `isCore` theo request, key `<code>-E<NN>`) | POST `/api/admin/grammar/steps/{code}/exercises/generate` (+ `/prompt`, `/import`) | `grammar.manage` |
| `DraftGrammarTheoryWithAiCommand { stepCode, aiConfig }` → `{ theory, issues[] }` không lưu | POST `/api/admin/grammar/steps/{code}/theory/ai-draft` | `grammar.manage` |

Hàng chờ trả bài đầy đủ (content + grading) để admin xem đáp án, kèm `authorUserId`/username, `stepCode`, `createdAtUtc`; sắp cũ nhất trước. Prompt soạn nháp lý thuyết: yêu cầu xuất đúng khung `TheoryJson` của `curriculum.md`, tiếng Việt + ví dụ Anh, chỉ dùng ngữ pháp đến step đó; kết quả qua `GrammarTheoryValidator`.

## 6. Test

- Prompt builder: chứa đúng step/level/công thức/lỗi thường gặp/step trước/step sau/số lượng; chỉ chứa schema của type được yêu cầu; learner note có ký tự `"`, `\n`, `<<<`, "Ignore previous instructions" được JSON-encode, nằm trong `<<<…>>>`, bị cắt ≤ 300; prompt manual == prompt gọi trực tiếp với cùng input.
- Processor: code fence; type không yêu cầu bị loại; dư bị cắt; sai schema bị loại với issues; trùng hash Official/Approved/owner bị loại; trùng hash với bài Private của user khác **không** bị loại; 0 hợp lệ → 422 và không ghi gì.
- Generate: provider gọi trước khi có entity tracked (mẫu `AiCommandsUnitOfWorkTests`); provider lỗi → không ghi; key không xuất hiện trong exception message/audit/log (dùng fake logger nếu có mẫu); header `x-goog-api-key` (test adapter với `HttpMessageHandler` giả nếu có mẫu cho generator từ vựng).
- Quota: 41 bài/ngày → 429; Pending 31 → 409 với Public, Private vẫn OK.
- Bài của tôi: owner sửa bài Approved Public → Pending và biến mất khỏi `community` của user khác; đổi Private → Moderation None; user B sửa/xóa/đổi visibility bài của A → 404; xóa bài đã promote → 404.
- Moderation: approve/reject chỉ khi Pending (khác → 409); reject thiếu lý do → 400; learner gọi moderation → 403; approve → user khác thấy trong `community`; reject → chỉ owner thấy kèm lý do; promote giữ `AuthorUserId`, thành Official.
- Admin generate tạo Official có key, `IsCore` đúng.

## Kiểm chứng phải chạy

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```
Không fail mới ngoài 6 baseline.

## Không được làm

- Không gọi Gemini thật, không chạy API/`database update`, không ghi secret vào config, không sửa luồng từ vựng/Lesson (chỉ tham khảo), không commit.

## Worker report

```
## Worker report
- Files changed:
- Routes added:
- Config keys added:
- Build:
- Test:
- Quyết định/giả định:
- Chưa làm/không chắc:
```

## Ghi chú từ G2 (đã có trong code)

- Lỗi nghiệp vụ dùng `NotFoundException`/`ForbiddenException`/`ConflictException` (đã map trong `GlobalExceptionHandler`); mã lỗi đặt làm message (FE đọc `detail`). Quota ngày trả 429: kiểm tra handler có map; nếu chưa có exception phù hợp, thêm `TooManyRequestsException` (hoặc tương đương) + map 429 trong `GlobalExceptionHandler`, có test.
- `GrammarExerciseRules` đã cấm key đáp án trong content; learner DTO dùng `IGrammarLearnerContentSanitizer`. Bài user generate/import cũng qua đúng hai lớp này.
- `GrammarExerciseLearnerDto.Content` là `JsonElement?`; dùng mapper chung trong `Features/Grammar/Common`.
