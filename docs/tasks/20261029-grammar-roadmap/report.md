# Report — 20261029-grammar-roadmap

Trạng thái: **Đã áp vào working tree chính, chưa commit.** Base `e4a9cb1`, tree cuối của worktree `ffc5043` (`snapshots.txt`).

## Đã làm

| Batch | Nội dung | Worker | Vòng sửa |
| --- | --- | --- | --- |
| G1 | Domain Grammar (step, exercise, progress, attempt/answer), `ExerciseContentValidator` dùng chung với Lesson, `GrammarExerciseRules`, `GrammarTheoryValidator`, `GrammarContentHasher`, `GrammarContentPackImporter` (idempotent), embedded pack source, permission `grammar.read/attempt/generate/manage/moderate`, migration `20261029040750_AddGrammarRoadmap` (chỉ tạo 5 bảng Grammar) | AGY | 1 |
| G2 | Roadmap/step/exercises/attempt queries, Start/Submit/Complete attempt (khóa câu trả lời StepCore/LevelTest, RowVersion, 409 `grammar_conflict`), admin CRUD step/theory/exercise, import pack, `GrammarLearnerContentSanitizer` | AGY | 1 |
| G3 | Prompt builder theo `exercise-prompt.md`, Gemini adapter (key qua header `x-goog-api-key`, timeout 120s), generate/prompt/import, bài của tôi, kiểm duyệt (approve/reject/promote), admin generate official, AI soạn nháp lý thuyết; quota 10/lần (admin 20), 40/ngày, 30 Pending | AGY + Claude sửa | 1 |
| G4 | FE Student: `/student/grammar`, `/student/grammar/:code` (Lý thuyết/Bài tập/Luyện thêm), `/student/grammar/attempts/:id`, `/student/grammar/my-exercises`, dialog tạo bài bằng AI/thủ công, menu "Ngữ pháp", i18n vi/en | AGY + Claude sửa nhỏ | 2 |
| G5 | FE Admin: `/admin/grammar`, `/admin/grammar/:code` (Thông tin/Lý thuyết có editor cấu trúc + JSON thô + AI nháp/Bài tập chính thức), `/admin/grammar/moderation` (lọc Pending/Approved/Rejected), "Nhập nội dung chuẩn", thẻ dashboard | AGY + Claude sửa | 2 |
| G6 | Lý thuyết 100 step A1→C2 (Claude) trong `Infrastructure/Grammar/ContentPacks/grammar-*.json`; 200 bài core A1 (AGY soạn, Claude review và sửa 41 bài; bản duyệt `g6/a1-review.md`, người dùng đã duyệt) | Claude + AGY | 1 |
| Thêm | `DeterministicExerciseGrader` chuẩn hóa dấu nháy cong `’ ‘ ʼ` → `'` (áp dụng cả Lesson); `scripts/ai/run-worker.sh` tạo junction `node_modules` bằng PowerShell thay `mklink` (lỗi làm G4 lần đầu không chạy) | Claude | |

Diff và prompt từng vòng: `runs/*.diff`, `prompt-agy-*.md`. Patch áp vào main: `runs/final.patch` (158 file code) + `grammar-a1.json` chép từ worktree.

## Kiểm chứng đã chạy (trên working tree chính, sau khi áp patch)

| Lệnh | Kết quả |
| --- | --- |
| `dotnet build EnglishPartner.BE/EnglishLiveTutor.sln` | Thành công, 0 lỗi |
| `dotnet test EnglishPartner.BE/EnglishLiveTutor.sln` | 533/539 pass; 6 fail là baseline `PronunciationSynthesisAudioCacheTests` (EF InMemory không hỗ trợ `DataLength`), không có fail mới |
| `npm run build` (FE) | Thành công |
| `npx vitest run` (FE) | 335/335 pass (baseline trước task 228) |
| So nội dung main ↔ worktree (bỏ CR) | Khớp 100% |
| `GrammarBundledContentPackTests` | 6 pack, 100 step, 200 bài A1 không lỗi; mỗi step A1 có 10 bài Deterministic, có bài mức 3; import lần 2 không tạo trùng |

Review độc lập (subagent chỉ đọc) sau mỗi batch: security, performance (BE), contract/nghiệp vụ, thiết kế/a11y (FE), nội dung đáp án (G6).

## Chưa kiểm chứng

- SQL Server thật: migration, index INCLUDE, `EF.Functions.Random()` cho Practice, RowVersion/unique conflict.
- Provider Gemini thật (generate bài, AI soạn nháp lý thuyết, chấm AiRubric).
- Trình duyệt thật: bố cục 360px/tablet, bàn phím, focus trong Dialog (component `Dialog` dùng chung chưa có focus trap).

## Khoảng trống contract / nợ còn lại

- BE chưa chấm AI cho provider AGY trong ngữ pháp (cần `ApiKey`) — FE coi như không có key → tự đánh giá.
- Practice chưa nhận `source` (lấy ngẫu nhiên mọi nguồn) — FE ghi "Luyện ngẫu nhiên 10 bài".
- `GrammarAttemptDto` chưa có `passed` — tải lại attempt đã hoàn thành chỉ hiện điểm.
- Learner kích hoạt được AGY CLI trên server khi generate (hành vi có sẵn từ luồng từ vựng).
- Gộp `priorSteps` fallback lệch nhẹ spec; `StripCodeFence` đơn giản; `ProblemDetailsGlobalExceptionHandlerTests.cs` bị thêm BOM.
- Bài core A2→C2 chưa có (admin tạo dần bằng AI trong app như plan).
- Nợ cũ ngoài task: `Ai:VocabularyGeneration:TimeoutSeconds=3000`, `GeminiVocabularyAiGenerator` còn `?key=`, lịch sử hội thoại chưa phân trang.

## Việc của người dùng

1. Quyết định commit (chưa commit/push/merge).
2. Backup DB rồi chạy `dotnet ef database update` (migration chỉ thêm 5 bảng Grammar, không đụng dữ liệu cũ).
3. Production (`SeedOnStartup=false`): đăng nhập admin → `/admin/grammar` → "Nhập nội dung chuẩn".
4. Có thể gỡ worktree `D:/Projects/EnglishPartner.worktrees/20261029-grammar-roadmap-agy` sau khi commit.
