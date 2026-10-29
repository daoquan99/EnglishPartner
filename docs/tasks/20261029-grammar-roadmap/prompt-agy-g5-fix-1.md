# G5 fix 1 — sửa sau review (contract/nghiệp vụ + bảo mật/thiết kế)

Worktree có G1–G4 (staged) + G5 (unstaged). Chỉ sửa FE trong phạm vi dưới; không sửa BE, không thêm dependency, không commit. Giữ toàn bộ test G4 pass và **không đổi hành vi learner G4** (payload, thông điệp lỗi). Vitest môi trường node (không jsdom): logic test qua hàm thuần, UI qua `renderToStaticMarkup` khẳng định thuộc tính thật. Đọc lại `prompt-agy-g5.md` (kể cả "Ghi chú từ G4") và BE thật.

## High

1. **Promote chỉ cho bài Approved** (BE `PromoteGrammarExerciseCommandHandler` từ chối khác Approved). Trang moderation thêm bộ lọc status `Pending | Approved | Rejected` (BE `ListGrammarModerationQueueQuery.Status`, query param tên đúng như endpoint đọc). Pending: Approve/Reject. Approved: Promote (quyền `grammar.manage`). Rejected: chỉ xem + lý do.
2. **Lỗi 400 form bài tập Official theo field**: BE trả `errors` với key path chữ thường/lồng (`type`, `maxPoints`, `gradingMode`, `content`, `content.statements`, `grading`, `grading.correctOptionIds`, …; xem `GrammarExerciseRules`/`ExerciseContentValidator`/`CreateOfficialGrammarExerciseCommand`). Viết helper thuần `groupIssuesByField(errors, fields)` khớp không phân biệt hoa thường theo tiền tố (`content.*` → content, `grading.*` → grading, `$.x` → x); issue không khớp field nào hiển thị danh sách `path: message`. Không dựa vào `detail` (là chuỗi ghép).
3. **Không ghi đè chỉnh sửa chưa lưu khi refetch**: `AdminGrammarStepEditorPage` chỉ khởi tạo form info/theory khi đổi `code` hoặc khi phần đó không dirty (`isInfoDirty`, `isTheoryDirty`); lưu phần nào chỉ sync lại phần đó. Bản nháp AI đặt `isTheoryDirty = true`. Khi dirty hiển thị banner "Có thay đổi chưa lưu" (không cần chặn router).
4. **Admin generate/import/prompt qua mutation**: thêm `useGenerateOfficialGrammarExercisesMutation`, `useImportOfficialGrammarExercisesMutation`, `useBuildOfficialGrammarPromptMutation` trong `useAdminGrammarQueries.ts`, invalidate `grammarAdminKeys.step(code)` + danh sách steps admin. Dialog chọn mutation theo `isAdminMode`; nút disable theo mutation đang dùng (`isPending`). Kết quả có bài bị loại → **không** tự đóng dialog, hiển thị danh sách rejected; chỉ đóng khi người dùng bấm. Response type admin đúng BE (đọc endpoint admin generate/import trả gì) — sửa type nếu đang gõ sai.
5. **Thiếu khóa i18n** (UI hiện tên khóa): `common.saving`, `common.filters`, `common.pageOf`, `common.prev` (+ `common.next` nếu dùng), `admin.grammar.exercise`, `grammar.difficultyBasic/Medium/Advanced` — thêm vào cả `vi.ts` và `en.ts` (hoặc đổi sang khóa đã có). Viết một test đọc (bằng `fs`) mọi file `.ts/.tsx` trong `src/features/grammar/**` (trừ `__tests__`) + `AdminDashboardPage.tsx` + `AdminSidebar.tsx`, lấy mọi khóa literal trong lời gọi `t(...)` bằng regex, và khẳng định khóa tồn tại trong cả locale vi và en.

## Medium

6. **Lỗi theo status trước**: trong `getGrammarErrorMessage` (hoặc helper admin mới) map `response.status` trước: 403 → forbidden, 404 → notFound, 409/400/422/429 → theo mã trong `detail` như hiện có. **Không đổi** kết quả các mã learner G4 (test G4 pass nguyên; thêm test: 404 learner vẫn ra thông điệp cũ). Bỏ dò chuỗi tiếng Anh kiểu `includes('not found')` nếu nó lấn mã learner.
7. **Lỗi theory theo path đầy đủ**: khối "Lỗi khác" liệt kê mọi path chưa có field hiển thị (`$`, `$.commonMistakes[i].*`, `$.tips`, `$.sections[i].formulas`, `…examples[j]`, `$.summary[i]`); chế độ JSON thô hiển thị toàn bộ issues.
8. **Lỗi ≠ rỗng, 403 ≠ 404**: trang steps, moderation, editor đọc `error` của query → banner lỗi có nút thử lại; editor không hiện "Không tìm thấy step" cho 403/500.
9. **Dialog Reject/Promote**: handler cha ném lại lỗi (hoặc trả boolean); dialog chỉ đóng khi thành công, hiển thị lỗi trong dialog, giữ lý do đã nhập. Bỏ mọi `catch {}` rỗng. Không đóng dialog (Escape/X) khi đang lưu.
10. **AI soạn nháp**: không có key thật (`apiKey.trim()` rỗng) → disable nút + cảnh báo `grammar.noApiKeyWarning` (+ cách mở cấu hình key như dialog G4 nếu có helper). Bỏ model hardcode `'gemini-2.5-flash'` ở editor và dialog: dùng model từ store/catalog; danh sách model rỗng → chặn gọi. Hỏi xác nhận ghi đè nếu theory hiện tại có **bất kỳ** nội dung (objectives, sections, commonMistakes, tips, summary).
11. **Admin mode giới hạn 20**: dùng một hằng `maxCount` (10 learner / 20 admin) cho cảnh báo tổng và `max` mỗi loại.
12. **Key bài Official khi sửa**: ô key `disabled` khi edit, không gửi `key` trong payload update. `maxPoints` min 1. Bỏ `overwriteExistingTheory` nếu BE không có field này.
13. **ARIA/label**: tab editor và tab level có `role="tablist"/"tab"/"tabpanel"`, `aria-selected`, `aria-controls`; nút Cấu trúc/JSON thô có `aria-pressed`; select level + ô stepCode ở moderation có label (debounce 300ms cho ô stepCode); input trong `TheoryStructuredEditor` có `aria-label` kèm số thứ tự; nút icon (quay lại, xóa bài, xóa summary) có `aria-label`. Thêm nút lên/xuống cho formulas, examples, notes, commonMistakes, tips, summary (helper thuần `moveItem`).
14. **Trang cuối rỗng sau thao tác**: `page > totalPages && totalPages > 0` → đặt `page = totalPages` (moderation, steps).

## Low

15. Dashboard: chỉ query/hiện số step khi có `grammar.manage`, số chờ duyệt khi có `grammar.moderate`; thẻ link đúng trang theo quyền. Link "Hàng chờ kiểm duyệt" trên trang steps ẩn khi thiếu `grammar.moderate`.
16. Moderation không hiện `authorUserId` (GUID) khi thiếu tên → nhãn i18n "Ẩn danh".
17. i18n còn sót: `Steps` (dashboard), `pts`, `JSON (RFC 8259)`, placeholder `e.g. …`.
18. Invalidate: update bài Official invalidate danh sách steps admin (số core); promote invalidate `grammarAdminKeys.step(stepCode)` + hàng chờ + steps.
19. Sidebar: mục `/admin/grammar` dùng `end` để không sáng cùng "Kiểm duyệt".
20. Type: `type`/`gradingMode` dùng union enum đã có của G4/Lesson nếu có; `validateTheoryJson` kiểm kiểu từng phần tử (objectives/tips/summary là chuỗi; sections/commonMistakes là object) để JSON thô sai kiểu không crash preview.
21. `flex-wrap` cho hàng meta bài và header danh sách bài; bỏ wrapper `overflow-x-auto` thừa quanh `DataTable`.

## Test

- `groupIssuesByField` với key BE thật (`content.statements[0]`, `grading.correctOptionIds`, `maxPoints`, `$.sections[0].heading`, path lạ → nhóm "khác").
- Hàm quyết định nút theo status + quyền (Pending + moderate → approve/reject; Approved + manage → promote; thiếu quyền → không có).
- Invalidation: tách hàm `getInvalidationKeysFor(action, stepCode)` và test (approve/reject/promote → hàng chờ + steps; promote → thêm step).
- AI soạn nháp: handler xử lý kết quả draft chỉ set state, không gọi `updateTheory` (inject fake api).
- Reject: rỗng, toàn khoảng trắng, 501 ký tự → lỗi; 500 → ok.
- Dialog admin mode: tổng 15 hợp lệ ở admin, không hợp lệ ở learner; payload admin có `isCore`.
- Error map: 403/404 theo status; mã learner G4 giữ nguyên.
- Test khóa i18n (mục 5).
- Xóa test tautology trong `adminGrammarPagesAndTheory.test.tsx` (dict vào/dict ra; render trạng thái mà route không cho vào).

## Kiểm chứng

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
grep -rnP "[ăâđêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹàáèéìíòóùúýĂÂĐ]" src/features/grammar --include=*.tsx --include=*.ts | grep -v __tests__
```
Toàn bộ pass, grep không còn chuỗi ngoài comment. Không commit.

## Worker report

```
## Worker report
- Files changed:
- Mục đã sửa (1–21):
- Build:
- Test:
- Contract gap:
- Chưa làm/không chắc:
```
