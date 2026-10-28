# Việc giao cho AGY: B8 — frontend: phiên đăng nhập, API key BYO, phân trang server, dữ liệu giả (frontend)

## Mục tiêu

FE khớp các thay đổi BE của B3, B5, B6 và sửa các finding phía client. Liên quan review S9 (client), S13, mục "FE `RequireAuth` không chờ `isInitialized`", P1 (phía FE); ADR-0003. Batch B1–B7 (backend) đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc), `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`
- ADR: `D:/Projects/EnglishPartner/docs/adr/0003-byo-api-key.md`
- Review: `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`
- Skill: `.agents/skills/security-audit/SKILL.md` (Checklist FE), `.agents/skills/api-contract-sync/SKILL.md`, `EnglishPartner.FE/.agents/skills/admin-portal-design/SKILL.md`, `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Contract BE sau B3/B6 (đọc code BE trong worktree để xác nhận):
  - `GET /api/vocabularies` và `GET /api/users` nhận `page`, `pageSize` (tối đa 100), `search`; khi gửi `paged=true` trả `{ items, totalCount, page, pageSize, totalPages }`; luôn có header `X-Total-Count`, `X-Page`, `X-Page-Size`, `X-Total-Pages`. Request không phân trang bị cắt ở 1000 dòng.
  - Bỏ tham số `userId` ở `GET /api/pronunciation/assessments/history`, `GET /api/vocabularies/{id}/flashcard`, `GET /api/lesson-attempts`.
- Code FE:
  - `src/features/tutor/store/tutorSessionStore.ts` (API key trong `localStorage`, key `lingomint_api_key`), `src/app/layouts/StudentLayout.tsx` (ô nhập key ở Left Menu)
  - `src/features/auth/components/RequireAuth.tsx`, `src/features/auth/hooks/useAuth.ts` (`useBootstrapAuth`: "offline mode" giữ user hint khi refresh lỗi; nhánh `demo-jwt-`), `src/features/auth/store/authStore.ts`
  - `src/features/admin/api/adminApi.ts` (trả `FALLBACK_USERS`, `FALLBACK_VOCABULARY_CARDS`, `FALLBACK_ROLES`, `FALLBACK_PERMISSIONS` khi lỗi hoặc rỗng), `src/features/vocabulary/api/vocabularyApi.ts` (`FALLBACK_FLASHCARDS`), `src/features/admin/pages/AdminVocabulariesPage.tsx`, `src/features/admin/pages/AdminUsersPage.tsx`, `src/features/admin/hooks/useAdminQueries.ts`, `src/shared/components/ui/DataTable.tsx` (có `totalCount`, `onPageChange`)
  - `src/features/lessons/api/lessonApi.ts` (`listAttempts(params?: { userId })`), `src/features/vocabulary/api/vocabularyApi.ts`

## Phạm vi

Được sửa: `EnglishPartner.FE/src/**`, `EnglishPartner.FE/.env.example`, `.gitignore` ở gốc repo (chỉ thêm dòng).

Không được sửa: backend, `package.json`/lockfile (không cài package), `node_modules`, route trong `router.tsx` (trừ khi guard cần), thiết kế lại trang ngoài phạm vi.

## Yêu cầu

1. **API key BYO (S9, ADR-0003).** Lưu key trong `sessionStorage` mặc định. Thêm checkbox "Ghi nhớ trên thiết bị này" cạnh ô nhập key ở Left Menu: bật → lưu `localStorage`, kèm dòng cảnh báo ngắn (key có thể bị đọc nếu trang bị chèn mã độc). Lần đầu chạy bản mới: nếu `localStorage` đang có key cũ → chuyển sang `sessionStorage` và xóa khỏi `localStorage` (trừ khi người dùng đã bật ghi nhớ). Không log key. Nhãn mới qua i18n (`src/shared/i18n/locales/vi.ts`, `en.ts`).
2. **Phiên đăng nhập.** `RequireAuth` hiển thị trạng thái đang tải khi `isInitialized === false` thay vì redirect; chỉ redirect `/login` khi đã khởi tạo xong và không có phiên. Bỏ "offline mode": refresh lỗi → `clearSession()`. Bỏ nhánh `demo-jwt-`. Hint trong `sessionStorage` chỉ dùng để hiển thị nhanh, không được coi là đã xác thực.
3. **Không dữ liệu giả.** Bỏ toàn bộ fallback `FALLBACK_*` trong `adminApi.ts` và `FALLBACK_FLASHCARDS` trong `vocabularyApi.ts` (và export liên quan trong `features/vocabulary/index.ts` nếu không còn dùng): lỗi API → ném lỗi để trang hiển thị trạng thái lỗi có nút thử lại; danh sách rỗng → trạng thái rỗng. Kiểm tra các trang dùng các hàm này hiển thị đúng 3 trạng thái loading / rỗng / lỗi (dùng `LoadingState`, `EmptyState`, `FeedbackBanner` có sẵn). Giữ nguyên `catalog/api/catalogDefaults.ts` (giá trị mặc định catalog, không phải dữ liệu nghiệp vụ giả).
4. **Phân trang server (P1).** `AdminVocabulariesPage` và `AdminUsersPage` gọi API với `page`, `pageSize` (20), `search`, `paged=true`; truyền `totalCount` và `onPageChange` cho `DataTable` để không cắt trang lần hai ở client. Query key gồm page, pageSize và filter. Tìm kiếm debounce ~300 ms và quay về trang 1. Sau tạo/sửa/xóa: invalidate query hiện tại.
5. **Bỏ `userId` gửi đi.** Xóa tham số `userId` khỏi `listAttempts`, payload/params pronunciation và flashcard trong API adapter và type (`openapi.types.ts`, feature types) nếu chỉ dùng để gửi lên.
6. **S13.** Thêm `EnglishPartner.FE/.env` và `EnglishPartner.FE/.env.*.local` vào `.gitignore` gốc; giữ `.env.example` với giá trị mẫu. (Việc `git rm --cached` do orchestrator báo người dùng — không tự chạy.)
7. **Test (Vitest).** `RequireAuth`: chưa khởi tạo → không redirect; đã khởi tạo + không phiên → redirect. API key: mặc định vào `sessionStorage`; bật ghi nhớ → `localStorage`; migrate key cũ. `adminApi.listUsers` lỗi → reject (không trả dữ liệu giả). Admin vocabularies/users: gửi `page`/`pageSize`/`paged=true` và hiển thị `totalCount`. Cập nhật test cũ đang dựa vào fallback hoặc offline mode.

## Tiêu chí hoàn thành

- [ ] `grep -rn "FALLBACK_\|demo-jwt-\|lingomint_api_key" src` chỉ còn chỗ hợp lệ (khóa storage mới/migration), không còn dữ liệu giả.
- [ ] `npm run build` pass; `npx vitest run` pass toàn bộ.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
```

## Ràng buộc

- Không chạy `git add`, `git stash`, `git reset`, `git checkout` (orchestrator quản lý index). Không commit, push, merge, tạo branch; không cài package; không chạy dev server hay BE.
- Giữ theme/shell hiện có; không thiết kế lại trang ngoài phạm vi.
- Cần sửa ngoài phạm vi hoặc gặp điều mơ hồ: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt yêu cầu 1–7>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa kiểm tra trên trình duyệt: <danh sách>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
