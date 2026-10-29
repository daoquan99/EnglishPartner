# G4 fix 2 — phần còn thiếu sau verify fix 1

Worktree có G4 + fix 1 (unstaged). Chỉ sửa FE, các mục dưới. Không sửa BE, không thêm dependency, không sửa player Lesson. Vitest chạy môi trường node (không jsdom): logic test qua hàm thuần, UI qua `renderToStaticMarkup` với khẳng định thuộc tính thật.

## High

1. **Route attempt thiếu `key`**: thêm `GrammarAttemptRoute` render `<GrammarAttemptPage key={id} />` (giống `GrammarStepRoute`) để "Làm lại" sang attempt mới tạo lại component (không giữ `completionResult`, `redoExerciseIds`, `currentIndex`, `pendingSubmitPayload`). Test: router dùng wrapper có key (ví dụ kiểm `GrammarAttemptRoute` được dùng trong bảng route, hoặc test hàm/route element).
2. **i18n còn ~39 chuỗi tiếng Việt cứng** trong `features/grammar/**` (không tính comment/test). Danh sách đã biết (đường dẫn tương đối `src/features/grammar/`):
   - `components/GenerateGrammarExercisesDialog.tsx`: 447 aria-label "Xóa dạng bài", 458, 471 "Thêm dạng bài tập", 626 "Bài #…".
   - `pages/GrammarAttemptPage.tsx`: 190, 363, 386, 401, 446, 502, 518, 531, 588.
   - `pages/GrammarRoadmapPage.tsx`: 87, 128, 141.
   - `pages/GrammarStepPage.tsx`: 127, 301, 365, 507, 510, 520, 571, 582, 598, 608 (và nút "Sau" tương ứng).
   - `pages/MyGrammarExercisesPage.tsx`: 199, 237, 243, 246, 254, 255, 324, 343, 388, 398, 420, 421, 424 (và nút "Sau").
   Sau khi sửa, lệnh sau phải không còn kết quả nào ngoài comment:
   `grep -rnP "[ăâđêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹàáèéìíòóùúýĂÂĐ]" src/features/grammar --include=*.tsx --include=*.ts | grep -v __tests__`
   Hiển thị loại bài bằng `t('grammar.exerciseTypes.<type>')` thay vì `ex.type` thô (`GrammarStepPage.tsx:535`, `MyGrammarExercisesPage.tsx:278`, tiêu đề fallback `Bài tập ${ex.type}`). Bỏ các fallback `t(...) || x` vô tác dụng. Khóa mới thêm ở cả `vi.ts` và `en.ts`, tập khóa giống hệt nhau.

## Medium

3. **Số giới hạn sai**: `grammar.errors.pendingLimit` phải là 30 bài chờ duyệt, `dailyQuotaExceeded` 40 bài/ngày (BE `GrammarRules.MaxPendingExercisesPerUser = 30`, `MaxDailyExercisesPerUser = 40`), cả vi/en.
4. **Đạt/Chưa đạt khi tải lại**: không có `completionResult` (tải lại attempt đã complete) → chỉ hiện điểm, không tự tính Đạt/Chưa đạt ở FE (BE có điều kiện AiRubric mà FE không biết). Ghi gap "GrammarAttemptDto chưa có `passed`" vào report.
5. **Mã lỗi thiếu**: thêm `grammar_step_has_no_exercises`, `grammar_duplicate_content` vào `getGrammarErrorMessage` + i18n.
6. **Model mặc định**: `selectedModel` khởi tạo `''` (hoặc khi catalog tải xong mà model hiện tại không có trong danh sách thì chọn model đầu tiên), để request luôn gửi model đang hiển thị.
7. **Lỗi form sửa bài theo field**: lỗi parse/kiểu của content/grading hiện ngay dưới textarea tương ứng (`aria-invalid`, `aria-describedby`), không chỉ banner chung. Lỗi visibility/delete hiện ở thẻ bài đang thao tác (giữ banner nếu muốn).
8. **404/403**: khi status 404/403 không hiện thêm thông điệp lỗi chung bên dưới tiêu đề (`GrammarStepPage.tsx:145`, `GrammarAttemptPage.tsx:208`).

## Low (ARIA)

9. Accordion: vùng danh sách step luôn render với `hidden={!isExpanded}` (để `aria-controls` có đích); nút chevron thứ hai cũng có `aria-expanded` hoặc bỏ nút trùng.
10. Tab dialog: thêm phần tử `role="tabpanel"` có `id` khớp `aria-controls` (`dialog-tabpanel-direct/manual`) và `aria-labelledby`.
11. `<label htmlFor="ai-model-provider">` đang trỏ vào `div` → đổi thành `span`/heading hoặc gắn đúng control.
12. Xóa biến thừa `isAgy` trong `GrammarAttemptPage.tsx`; dùng lại `LessonAiExecutionConfigDto` từ `lessons.types.ts` (hoặc export chung) thay vì định nghĩa lại trong `openapi.types.ts` nếu không phá type khác.

## Test (sửa test vô nghĩa + thêm test thiếu)

- `buildGrammarSubmitPayload` (hoặc tên hiện có): không key / key rỗng / AGY không key → không `aiConfig`; có key → `{apiKey, assessmentModelCode, provider}` lấy từ input store.
- `getGrammarErrorMessage` nhận lỗi dạng axios `{ response: { status, data: { detail } } }` thật (409 `grammar_pending_limit`, 429 `grammar_daily_quota_exceeded`, prefix `grammar_ai_provider_error_503`, mã lạ → chung), không truyền chuỗi thẳng.
- Hàm thuần quyết định "cần confirm trước khi nộp" (StepCore/LevelTest chưa nộp → true; FailedGrading → true/cho nộp lại; Practice → false) và "có cho nộp lại" (Practice → redo).
- Hàm/route: attempt route dùng wrapper có key.
- Dialog: hàm validate tổng > 10 bị chặn; trạng thái ban đầu không key → render chế độ thủ công (khẳng định có ô dán JSON/nút copy prompt); Public → render cảnh báo kiểm duyệt; hàm đọc `rejected` từ lỗi 422 trả danh sách.
- `parseJsonObjectField`: `null`, `[]`, `"x"`, `5` → lỗi; object → ok.
- Xóa hoặc sửa test tautology: `grammarApi.test.ts` payload aiConfig tự truyền, test "redirect unauthenticated" chỉ kiểm chuỗi vắng mặt (khẳng định có Navigate tới `/login` hoặc nội dung trang login), test My exercises "ConfirmDialog" với dialog đóng.

## Kiểm chứng

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
```
Toàn bộ pass, lệnh grep ở mục 2 không còn chuỗi tiếng Việt ngoài comment. Không commit.

## Worker report

```
## Worker report
- Files changed:
- Mục đã sửa (1–12):
- Kết quả lệnh grep mục 2:
- Build:
- Test:
- Contract gap:
- Chưa làm/không chắc:
```
