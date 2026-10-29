# G4 — Frontend Student Portal: lộ trình ngữ pháp

Đọc trước: `docs/tasks/20261029-grammar-roadmap/plan.md` (mục FE + Quy tắc nghiệp vụ), `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`, skill `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`, `design-system/SKILL.md`, `frontend-testing/SKILL.md`, `portal-design-review/SKILL.md`, `.agents/skills/api-contract-sync/SKILL.md`.

**Contract BE là nguồn sự thật**: đọc endpoint + DTO thật trong worktree (`EnglishPartner.BE/src/EnglishLiveTutor.Api/Endpoints/GrammarEndpoints.cs`, `Application/Features/Grammar/**` — Queries, Commands, Contracts/DTO). Không bịa field. Nếu thiếu field cần cho UI, ghi vào report, không sửa BE.

Không làm trang admin (G5). Không sửa hành vi feature Lesson (chỉ import component).

## 1. Contract FE

- Thêm type vào `src/shared/api/openapi.types.ts` (phần Grammar) đúng tên field JSON camelCase của BE.
- `src/features/grammar/api/grammarApi.ts` dùng `httpClient` như `features/lessons/api/lessonApi.ts` / `features/vocabulary/api/vocabularyApi.ts` (paging header theo cách adapter hiện có đọc).
- `src/features/grammar/hooks/useGrammarQueries.ts`: query keys `['grammar','roadmap']`, `['grammar','step',code]`, `['grammar','exercises',code,source,page]`, `['grammar','attempt',id]`, `['grammar','my-exercises',params]`; mutation start/submit/complete/generate/import/update/visibility/delete invalidate đúng key (complete → roadmap + step; generate/import/my-exercise → exercises + my-exercises + step).

## 2. Trang và route (`/student/grammar/*`, lazy như các trang khác trong `src/app/router.tsx`)

1. **`/student/grammar` — `GrammarRoadmapPage`**
   - 6 level (A1→C2) dạng section/accordion; mỗi level: tiến độ `x/y step đạt`, thanh tiến độ, nút **"Kiểm tra level"** (enabled theo `canTakeLevelTest`, tooltip lý do khi disabled), trạng thái Locked/Unlocked/Passed.
   - Danh sách step (mã, tiêu đề VI/EN, tóm tắt, điểm tốt nhất, icon trạng thái). Step Locked vẫn click được để xem lý thuyết (hiện nhãn "Chưa mở bài tập").
   - Tự mở level chứa step Unlocked đầu tiên chưa Passed; nút "Tiếp tục học" nhảy tới step đó.
2. **`/student/grammar/:code` — `GrammarStepPage`** với 3 tab (giữ tab trong URL `?tab=`):
   - **Lý thuyết**: render `theory` (objectives, sections → heading, explanationVi, bảng formulas, examples EN/VI có tô `highlight` (chỉ bọc chuỗi con đầu tiên bằng `<mark>`, escape mọi text — **không** `dangerouslySetInnerHTML`), notes, commonMistakes dạng ❌ wrong / ✅ right + giải thích, tips, summary). Nút prev/next step.
   - **Bài tập**: thông tin bộ bài core (số bài, điều kiện đạt 70%), nút "Bắt đầu"/"Làm tiếp" (Locked → disabled + giải thích). Step review (`isLevelReview`) hiển thị nút "Kiểm tra level" thay cho core nếu BE dùng LevelTest cho step này (đọc G2 để xác định).
   - **Luyện thêm**: bộ lọc nguồn `core | community | mine`, danh sách phân trang (badge type, độ khó 1–3, nguồn, trạng thái kiểm duyệt cho bài của tôi), nút "Luyện bộ này" (attempt Practice), nút **"Tạo bài tập bằng AI"**.
3. **`/student/grammar/attempts/:id` — `GrammarAttemptPage`**: hiển thị từng bài bằng `ExerciseRenderer` của Lesson qua adapter mỏng `grammarExerciseAdapter.ts` (map `GrammarExerciseLearnerDto` → `ExerciseLearnerDto`, `GrammarAnswerResultDto` → shape kết quả `ExerciseRenderer` cần). Điều hướng bài trước/sau, chỉ báo đã nộp. **Attempt `StepCore`/`LevelTest`: mỗi bài chỉ nộp một lần** (BE trả 409 `grammar_answer_locked` nếu nộp lại; trừ answer `FailedGrading` được thử lại) — sau khi nộp khóa player, hiện kết quả; cảnh báo trước khi nộp. Attempt `Practice`: nộp lại thoải mái. Mã lỗi BE nằm trong `detail` của ProblemDetails. Bài `unavailable: true` hiển thị "Bài tập không còn khả dụng". Bài AiRubric: dùng BYO key từ store hiện có của Student Portal (không tạo ô nhập key mới); không có key → nộp vẫn được, hiển thị "Tự đánh giá" + đáp án mẫu. Nút **Hoàn thành** (confirm nếu còn bài chưa nộp) → màn kết quả: điểm, đạt/chưa đạt, step vừa mở (link), làm lại.
4. **`/student/grammar/my-exercises` — `MyGrammarExercisesPage`**: lọc theo step/trạng thái; mỗi bài: xem trước, sửa (form JSON có cấu trúc tối thiểu: title, instructions, difficulty, content/grading dạng textarea JSON có validate parse), đổi Private/Public, xóa (confirm). Hiển thị lý do bị từ chối. Cảnh báo "Sửa bài đã duyệt sẽ phải chờ duyệt lại".

## 3. Dialog tạo bài tập — `GenerateGrammarExercisesDialog`

- Chọn loại bài (chỉ 9 type BE cho phép; ReadingComprehension chỉ hiện từ B1), số lượng mỗi loại, độ khó, ghi chú (≤ 300 ký tự, đếm ký tự), **Private/Public** (mặc định Private; giải thích Public cần admin duyệt), provider/model theo cách dialog sinh từ vựng (`AiVocabularyGeneratorDialog`) đang làm.
- Có key → gọi generate; hiển thị kết quả: số bài tạo + danh sách bài bị loại kèm lý do.
- Không có key → **chế độ thủ công** giống từ vựng: gọi `/prompt`, nút copy prompt, textarea dán JSON, import. Giới hạn tổng 10 bài/lần ở UI; lỗi 429/409/422 hiển thị thông điệp rõ (quota ngày, quá nhiều bài chờ duyệt, không có bài hợp lệ).

## 4. Điều hướng và i18n

- Thêm mục **"Ngữ pháp" / "Grammar"** vào menu `StudentLayout` (sau "Bài học & Luyện tập"), icon trong `/assets/icons/navigation/` đang có (chọn icon phù hợp có sẵn, không thêm asset mới), có sub-label như các mục khác.
- Toàn bộ text qua i18n vi/en (`src/shared/i18n/locales`), namespace/khóa `grammar.*`.

## 5. Trạng thái và chất lượng

- Loading/rỗng/lỗi/không quyền tách biệt; lỗi API không biến thành rỗng.
- Đổi step hoặc rời trang giữa chừng: không gắn kết quả cũ vào bài mới (dùng key theo exercise id).
- Responsive: mobile 360px không tràn ngang; bảng formulas cuộn ngang trong khung riêng.
- Keyboard: tab, accordion, dialog có focus trap/Escape theo component dialog hiện có.

## 6. Test Vitest (`src/features/grammar/__tests__/`)

- `grammarApi` gửi đúng route/payload (start attempt, submit answer có/không aiConfig, generate, prompt, import, visibility).
- Roadmap: hiển thị trạng thái, nút kiểm tra level disabled khi `canTakeLevelTest=false`, "Tiếp tục học" trỏ đúng step.
- Theory renderer: highlight chỉ bọc chuỗi con; text chứa `<script>` được hiển thị dạng text (không chèn HTML).
- Adapter map đúng DTO → `ExerciseLearnerDto`.
- Attempt: hoàn thành gọi complete và hiển thị kết quả pass/fail; bài AiRubric không có key → không gửi aiConfig.
- Dialog: không có key → chế độ thủ công; tổng > 10 bị chặn; Public hiển thị cảnh báo kiểm duyệt.
- My exercises: đổi visibility gọi đúng API; xóa có confirm.

## Kiểm chứng phải chạy

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
```
Toàn bộ test hiện có phải pass (baseline 228/228).

## Không được làm

- Không sửa BE, không đổi router/store/UI framework, không thêm dependency, không commit.

## Worker report

```
## Worker report
- Files changed:
- Routes added:
- Build:
- Test:
- Contract gap (nếu có):
- Chưa kiểm chứng trên trình duyệt:
```
