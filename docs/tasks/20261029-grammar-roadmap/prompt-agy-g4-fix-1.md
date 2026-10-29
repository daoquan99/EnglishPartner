# G4 fix 1 — sửa sau review (contract/nghiệp vụ + bảo mật/thiết kế)

Worktree đã có G4 (unstaged). Chỉ sửa FE trong phạm vi dưới, không sửa BE, không thêm dependency, không sửa hành vi player Lesson (`ExerciseRenderer`, `GradingResultFeedback`) — chỉ truyền props có sẵn. Đọc lại `prompt-agy-g4.md` và contract BE thật trong worktree trước khi sửa.

## High

1. **Đăng ký route** trong `src/app/router.tsx` dưới khối `/student`: `grammar`, `grammar/my-exercises`, `grammar/attempts/:id`, `grammar/:code` (đặt `my-exercises` và `attempts/:id` trước `:code`). Bọc `RequirePermission` theo cách các route khác đang làm: `grammar.read` (roadmap, step), `grammar.attempt` (attempt), `grammar.generate` (my-exercises). Route step dùng `key` theo `code` (hoặc reset state `practiceSource/practicePage/startError` khi `code` đổi). **Test**: render router thật (MemoryRouter + routes thật hoặc `createMemoryRouter`) và khẳng định `/student/grammar` hiển thị roadmap, không redirect. Xóa test tautology `studentLayoutGrammarRoute.test.tsx` hoặc viết lại để render `StudentLayout` thật.
2. **`aiConfig` khi nộp bài** phải đúng `LessonAiExecutionConfigDto` của BE: `{ apiKey, assessmentModelCode, provider }`. Dựng giống `StudentLessonPlayerPage.tsx` (lấy `assessmentModelCode` từ store, không hardcode model). Sửa type trong `openapi.types.ts` (dùng lại type Lesson nếu đã có) và test `grammarApi.test.ts`. **Không có key thật (`apiKey.trim()` rỗng) → không gửi `aiConfig`**, kể cả provider AGY (BE chỉ chấm AI khi có ApiKey) — ghi gap "BE chưa hỗ trợ AGY cho grammar grading" vào report.
3. **Step ôn tập (`isLevelReview`)**: luôn hiện nút StepCore như mọi step (BE xử lý StepCore bình thường). Nút "Kiểm tra level" là nút phụ, enable theo `canTakeLevelTest` của level (lấy từ roadmap query), **không** theo trạng thái khóa của step.
4. **Practice nộp lại**: với `kind === 'Practice'` thêm state redo theo `exerciseId` và truyền `onStartRedo`/`onCancelRedo`/`isRedoMode` cho `ExerciseRenderer` như Lesson player làm.
5. **StepCore/LevelTest**: confirm (dùng `ConfirmDialog` có sẵn) trước khi nộp mỗi bài ("Mỗi bài chỉ nộp một lần"); sau khi nộp (status khác `FailedGrading`) player ở trạng thái đã nộp, không cho nộp lại; `FailedGrading` cho thử lại.
6. **i18n toàn bộ**: mọi text trong `features/grammar/**` qua `useTranslation` + khóa `grammar.*` ở **cả** `vi.ts` và `en.ts` (kể cả nhãn 9 type bài tập, TheoryRenderer, dialog, trang my-exercises). Thêm `nav.grammar`/`nav.grammarSub` vào `en.ts`, bỏ fallback `|| 'Ngữ pháp'` trong `StudentLayout.tsx`.
7. **Bàn phím/ARIA trên roadmap**: thẻ step → `Link` (hoặc `button`); header accordion → `button` có `aria-expanded`, `aria-controls` trỏ tới vùng danh sách có `id`; nút icon có `aria-label`. Lý do "Kiểm tra level" bị khóa hiển thị bằng text inline (không chỉ tooltip hover).

## Medium

8. **Mã lỗi BE → thông điệp i18n**: helper `getGrammarErrorMessage(err, t)` đọc `detail` của ProblemDetails và map các mã: `grammar_step_locked`, `grammar_level_test_locked`, `grammar_level_test_not_enough_exercises`, `grammar_step_has_no_core_exercises`, `grammar_answer_locked`, `grammar_conflict`, `grammar_pending_limit`, `grammar_daily_quota_exceeded`, `grammar_generation_no_valid_exercises`, `grammar_ai_timeout`, `grammar_ai_provider_error_*` (prefix); mã khác → thông điệp chung. Không bao giờ hiện mã thô. Dùng ở mọi chỗ hiển thị lỗi.
9. **Không nuốt lỗi**: bỏ `catch {}` rỗng (start level test trên roadmap); visibility/delete ở my-exercises hiển thị lỗi tại chỗ (đặc biệt 409 `grammar_pending_limit` khi chuyển Public).
10. **Lỗi ≠ rỗng**: danh sách Luyện thêm đọc `error` → khối lỗi có nút thử lại; trang step/attempt phân biệt 404 ("không tìm thấy"), 403 ("không có quyền") và lỗi khác (có nút thử lại dùng `refetch`).
11. **SelfAssessed**: khi `status === 'SelfAssessed'` hiển thị nhãn "Tự đánh giá" + đáp án mẫu ngay trong `GrammarAttemptPage` (không sửa player Lesson).
12. **Phần giải thích chấm tự động**: adapter đưa `explanation` từ feedback BE (`{isCorrect, explanation}`) vào field feedback dạng text mà `GradingResultFeedback` hiển thị (không để JSON thô bị ẩn).
13. **Màn kết quả**: thêm nút "Làm lại" (start attempt cùng kind/step/level); Practice không hiện Đạt/Chưa đạt (chỉ điểm); LevelTest hiển thị "Đã đạt toàn bộ level X" thay vì liệt kê từng step "vừa mở".
14. **422 generate/import**: đọc `rejected` trong extensions ProblemDetails (`err.response.data.rejected`) và hiển thị danh sách bài bị loại kèm issues.
15. **Form sửa bài**: sau `JSON.parse` kiểm `typeof v === 'object' && v !== null && !Array.isArray(v)` cho content và grading, lỗi hiện tại field; gửi lại `focus` hiện có (không làm mất); `label` gắn `htmlFor`/`id` (hoặc dùng `FormField` có sẵn). Xóa dùng `ConfirmDialog` có sẵn.
16. **Nhãn "Luyện bộ này"**: BE Practice lấy ngẫu nhiên từ mọi nguồn → đổi nhãn thành "Luyện ngẫu nhiên 10 bài" và ghi gap "Practice chưa nhận source" vào report.
17. **Tab ngữ nghĩa**: tab trang step và tab chế độ trong dialog có `role="tablist"/"tab"`, `aria-selected`; `?tab=` không hợp lệ → `theory`.
18. Form dialog generate: input số lượng, select độ khó, label provider/model gắn với control.

## Low

19. `encodeURIComponent` cho `code`/`id` trong mọi path của `grammarApi.ts`.
20. Thêm `'Locked'` vào type trạng thái progress nếu BE trả.
21. Thêm `flex-wrap` cho thanh lọc nguồn, hàng phân trang, cặp nút prev/next; bỏ `max-h-[70vh] overflow-y-auto` lồng trong body dialog đã tự cuộn.
22. Debounce (~300ms) ô lọc mã step ở my-exercises.

## Test

Repo chạy Vitest ở môi trường node, **không có jsdom/testing-library** và không được thêm dependency. Tách logic thành hàm thuần (ví dụ `buildGrammarSubmitPayload`, `parseJsonObjectField`, `getGrammarErrorMessage`, `getLevelTestButtonState`, `validateGenerateItems`) để test trực tiếp; UI vẫn dùng `renderToStaticMarkup` nhưng phải khẳng định thuộc tính thật (`disabled`, `aria-expanded`, `href`), không chỉ chuỗi tĩnh. Router: render `MemoryRouter` + bảng route thật bằng `renderToStaticMarkup`. Các ý "bấm/confirm" dưới đây test qua hàm thuần hoặc state ban đầu tương ứng.

- Router thật: `/student/grammar` render roadmap.
- Nút "Kiểm tra level" có thuộc tính `disabled` khi `canTakeLevelTest=false`, enabled khi true.
- Attempt: bấm Hoàn thành gọi `complete` và hiện kết quả; AiRubric không có key → payload submit không có `aiConfig`; có key → `aiConfig` có `assessmentModelCode`; StepCore hỏi confirm trước khi nộp; Practice có nút làm lại.
- Dialog: không có key → chế độ thủ công; tổng > 10 bị chặn; Public hiện cảnh báo kiểm duyệt; 422 hiện danh sách rejected.
- My exercises: đổi visibility gọi API; xóa qua confirm; content `null`/mảng bị báo lỗi, không gọi API.
- Error map: `grammar_pending_limit` → thông điệp i18n, không hiện mã thô.

## Kiểm chứng

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
```
Toàn bộ test phải pass. Không commit.

## Worker report

```
## Worker report
- Files changed:
- Mục đã sửa (1–22):
- Build:
- Test:
- Contract gap:
- Chưa làm/không chắc:
```
