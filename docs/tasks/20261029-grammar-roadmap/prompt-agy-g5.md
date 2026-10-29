# G5 — Frontend Admin Portal: quản lý lộ trình ngữ pháp và kiểm duyệt

Đọc trước: `docs/tasks/20261029-grammar-roadmap/plan.md`, `EnglishPartner.FE/AGENTS.md`, `.agents/design-system.md`, skill `EnglishPartner.FE/.agents/skills/admin-portal-design/SKILL.md`, `design-system/SKILL.md`, `frontend-testing/SKILL.md`, `.agents/skills/api-contract-sync/SKILL.md`. Code G4 (feature `src/features/grammar/**`, type trong `openapi.types.ts`) đã có trong worktree — **tái dùng** type/api/hook, theory renderer và adapter của G4; thêm phần admin vào cùng `grammarApi.ts` hoặc `adminGrammarApi.ts`.

**Contract BE là nguồn sự thật**: `Api/Endpoints/AdminGrammarEndpoints.cs` và DTO trong `Application/Features/Grammar/**`. Không bịa field; thiếu thì ghi report.

## Trang (`/admin/grammar/*`, lazy trong `src/app/router.tsx`, dưới `AdminLayout`)

1. **`/admin/grammar` — `AdminGrammarStepsPage`**: bảng step theo level (tab A1…C2): mã, tiêu đề, số bài core/official/pending, published, "lý thuyết đã sửa tay". Nút **"Nhập nội dung chuẩn"** (gọi content-pack import, confirm trước; hiển thị kết quả số step/bài tạo, cập nhật, bỏ qua, lỗi). Phân trang theo BE.
2. **`/admin/grammar/:code` — `AdminGrammarStepEditorPage`** với tab:
   - **Thông tin**: titleVi, titleEn, summaryVi, published (toggle) → PUT step.
   - **Lý thuyết**: editor có cấu trúc cho `theory` (objectives, sections[heading, explanationVi, formulas[label, pattern], examples[en, vi, highlight], notes], commonMistakes[wrong, right, explanationVi], tips, summary) với thêm/xóa/sắp xếp mục; xem trước bằng theory renderer của G4; chế độ "JSON thô" (textarea, validate parse). Lưu → hiển thị issues 400 theo path. Nút **"AI soạn nháp"** (dùng BYO key/provider như dialog sinh của G4) → nạp nháp vào editor, **không tự lưu**, cảnh báo ghi đè.
   - **Bài tập chính thức**: danh sách bài Official (type, key, độ khó, core), xem trước (dùng `ExerciseRenderer` ở chế độ xem) + xem đáp án; tạo/sửa (form: type, key, difficulty, title, instructions, maxPoints, gradingMode, isCore, content/grading JSON textarea có validate parse; hiển thị issues 400/409), xóa (confirm). Nút "Tạo bằng AI" (dùng lại dialog G4 ở chế độ admin: tạo Official, chọn isCore, tối đa 20; chế độ thủ công khi không có key).
3. **`/admin/grammar/moderation` — `AdminGrammarModerationPage`**: hàng chờ Pending (lọc level/step), mỗi mục: step, tác giả, thời gian, xem trước như learner + đáp án đầy đủ; **Approve**, **Reject** (dialog nhập lý do bắt buộc 1–500 ký tự), **Promote** thành bài chính thức (chọn core) — chỉ hiện khi có quyền tương ứng (đọc cách FE hiện kiểm permission cho admin). Sau thao tác, invalidate hàng chờ.
4. Thẻ **"Ngữ pháp"** trên `AdminDashboardPage` (số step + số bài chờ duyệt, lấy count qua paging giống `COUNT_ONLY_PARAMS`), mục menu trong `AdminLayout` (Steps, Kiểm duyệt).

## Chất lượng

- i18n vi/en (`admin.grammar.*`). Loading/rỗng/lỗi/thiếu quyền rõ ràng; thao tác đang lưu disable nút.
- Không render HTML từ dữ liệu (không `dangerouslySetInnerHTML`).
- Responsive tối thiểu cho tablet; bảng cuộn ngang trong khung.

## Test Vitest

- API adapter admin gửi đúng route/payload (PUT theory, CRUD exercise, approve/reject/promote, content-pack import).
- Theory editor: thêm/xóa section, chuyển JSON thô ↔ form giữ dữ liệu, hiển thị issues theo path.
- AI soạn nháp không tự gọi lưu.
- Reject bắt buộc lý do; Approve/Reject/Promote invalidate hàng chờ.
- Nút theo quyền: thiếu `grammar.moderate` thì không có Approve/Reject.

## Kiểm chứng

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
```
Toàn bộ test phải pass.

## Không được làm

Không sửa BE, không thêm dependency, không sửa hành vi trang student G4 trừ khi tách component dùng chung (giữ test G4 pass), không commit.

## Worker report

```
## Worker report
- Files changed:
- Routes added:
- Build:
- Test:
- Contract gap:
- Chưa kiểm chứng trên trình duyệt:
```

## Ghi chú từ G4 (bắt buộc, lỗi đã gặp)

- **Đăng ký route thật** trong `router.tsx` dưới `AdminLayout` + `RequirePermission` (`grammar.manage` cho steps/editor, `grammar.moderate` cho moderation — xem mã quyền trong `DatabaseSeeder.cs`). Route có param dùng wrapper `key={param}` giống `GrammarStepRoute`/`GrammarAttemptRoute`. Test render router thật (`MemoryRouter` + `AppRoutes`), không tự dựng `Link`.
- Vitest chạy môi trường **node, không jsdom/testing-library**: logic tách thành hàm thuần để test; UI test bằng `renderToStaticMarkup` và khẳng định thuộc tính thật (`disabled`, `aria-*`, `href`). Không viết test tự truyền payload rồi kiểm lại chính nó.
- **i18n 100%**: sau khi xong, lệnh này phải không có kết quả ngoài comment:
  `grep -rnP "[ăâđêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹàáèéìíòóùúýĂÂĐ]" <các file admin grammar mới> | grep -v __tests__`
  Khóa ở cả `vi.ts` và `en.ts` cùng tập khóa; không dùng fallback `t(...) || 'x'`; loại bài hiển thị qua `t('grammar.exerciseTypes.<type>')`.
- Lỗi BE: dùng lại `getGrammarErrorMessage` trong `features/grammar/utils/grammarHelpers.ts` (bổ sung mã admin nếu có, ví dụ approve/reject khi không Pending → 409). Không hiện mã thô; không `catch {}` rỗng; lỗi ≠ rỗng; 404/403 tách biệt.
- Lỗi 400 validation của BE (ProblemDetails `errors`/issues theo path) hiển thị theo field/path.
- `aiConfig` cho AI soạn nháp/generate: dùng đúng shape DTO BE của endpoint đó (đọc command), lấy key/provider/model từ `useTutorSessionStore` như dialog G4; không hardcode model.
- Không định nghĩa lại type đã có; dùng lại type G4 trong `openapi.types.ts`.
- `import` luôn ở đầu file.
