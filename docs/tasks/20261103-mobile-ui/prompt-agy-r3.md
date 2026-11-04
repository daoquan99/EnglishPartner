# Việc giao cho AGY: đợt 3, sửa theo code review

## Bối cảnh

- Đợt 1 và 2 đã xong.
- Code review kết luận **ĐẠT CÓ ĐIỀU KIỆN**. Lần này chỉ sửa các mục dưới đây, không làm gì khác.
- Luật và skill giống đợt 2 (`docs/tasks/20261103-mobile-ui/prompt-agy-r2.md`).
- Làm trên branch `development`, **không commit**. Không cài package mới.

## Cần sửa

1. **(Cao) Drawer mobile và accessibility.** File: `src/app/layouts/StudentLayout.tsx` (aside khoảng dòng 150), `src/features/admin/components/AdminSidebar.tsx` (khoảng dòng 255).
   - Khi drawer **đóng** và đang ở mobile (`!isDesktop && !isOpen`, dùng `useIsDesktop`): `aside` phải không nhận Tab và không bị trình đọc màn hình đọc. Thêm thuộc tính `inert` (React 19 hỗ trợ `inert` dạng boolean). Ở desktop **không** được có `inert`.
   - Khi drawer **mở** trên mobile:
     - `aside` có `role="dialog"`, `aria-modal="true"` và `aria-label` lấy từ i18n;
     - đặt `inert` cho `main` (gồm cả top bar) để focus không thoát ra phía sau overlay.
     - Nút đóng nằm trong `aside` nên vẫn dùng được.
2. **(Trung bình) Khóa cuộn.** File: `StudentLayout.tsx` (khoảng dòng 614), `AdminLayout.tsx` (khoảng dòng 45).
   - `cn()` không gộp class trùng, nên `overflow-hidden` đang thua `overflow-y-auto`.
   - Đổi sang chọn chuỗi class theo trạng thái: `isOpen && !isDesktop ? 'overflow-hidden' : 'overflow-y-auto overflow-x-hidden'`.
   - Khi `useIsDesktop()` chuyển sang `true` thì tự đóng drawer.
3. **(Trung bình) Test của hook.** File: `src/shared/hooks/useMobileDrawer.ts`, `src/shared/hooks/__tests__/useMobileDrawer.test.tsx`.
   - Xóa `createMobileDrawerController`, hoặc cho hook dùng chính controller đó. Không được để code chỉ test dùng.
   - Viết test render thật bằng jsdom, dùng `react-dom/client` + `act` (xem cách các test khác trong repo đang render), cho các hành vi sau:
     - Escape đóng drawer;
     - đổi route đóng drawer (dùng `MemoryRouter` và điều hướng);
     - mở thì focus vào phần tử đầu tiên trong drawer;
     - đóng thì focus trả về nút menu.
   - Thêm test cho `StudentLayout` hoặc `AdminLayout`:
     - mobile (mock `matchMedia` trả `false`) và drawer đóng: `aside` có thuộc tính `inert`;
     - mở drawer: `main` có `inert`, `aside` có `role="dialog"`;
     - desktop (mock `matchMedia` trả `true`): không có `inert`.
4. **(Trung bình) i18n.** Thêm `vocab.prevCard` và `vocab.nextCard` vào cả `vi.ts` và `en.ts`. Hai key này đang được dùng ở `VocabularyFlashcard.tsx:93` và `:112` nhưng chưa được khai báo.
5. **(Trung bình) Header của `LiveTutorPage`** (khoảng dòng 305): khôi phục `sm:flex-wrap` cho container header, để ở 1024–1279px tiêu đề không bị bẻ từng từ và header không bị tràn. **Không** sửa thanh nút cuối (dòng 364–430); orchestrator đã chỉnh phần này.
6. **(Thấp)** `StudentLayout.tsx:153`: đổi `lg:w-64 lg:sm:w-72` thành `lg:w-72`.
7. **(Thấp)** Logo trên top bar (`AdminLayout.tsx:66`, `StudentLayout.tsx:636`) đứng ngay cạnh tên portal, nên đổi thành `alt=""` để trình đọc màn hình không đọc lặp.

## Kiểm chứng

Chạy trong `EnglishPartner.FE`:
- `npm run build`
- `npm run test`

Sau đó chạy audit ở 360px và 1024px:

```
node --experimental-websocket C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/audit.mjs C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/m360-r4 C:/Users/DELL/AppData/Local/Temp/perf/ui-cred.txt 360 780
node --experimental-websocket C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/audit.mjs C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/d1024-r4 C:/Users/DELL/AppData/Local/Temp/perf/ui-cred.txt 1024 768
```

Không đọc hay in nội dung file `ui-cred.txt`. Xem ảnh `student_tutor.png` và `drawer_student.png`.

## Report

Ghi vào `docs/tasks/20261103-mobile-ui/runs/agy-r3-report.md`, gồm:
- mỗi mục đã sửa thế nào;
- các lệnh đã chạy và kết quả.
