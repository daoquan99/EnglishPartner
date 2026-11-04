# Plan: sửa UI mobile (Student Portal + Admin Portal)

Mã task: `20261103-mobile-ui` · Loại: UI-only FE · Worker: AGY (theo yêu cầu người dùng) · Không đổi contract, không có migration.

## Bằng chứng (audit 2026-11-03)

Audit chạy bằng Chrome headless qua DevTools Protocol:
- Màn hình giả lập 390×844, `mobile: true`, có touch.
- Đăng nhập bằng user test (role ADMIN + LEARNER) trên DB tạm `LingoMint_UiCheck`.
- Đi qua 25 route.
- Script: `%TEMP%/perf/ui-audit/audit.mjs`. Ảnh chụp: `%TEMP%/perf/ui-audit/m390/*.png`.

Lỗi gốc, ảnh hưởng mọi trang sau khi đăng nhập:
- `StudentLayout` và `AdminSidebar` luôn hiện sidebar mở rộng ở mọi kích thước màn hình: `w-64 sm:w-72`, `w-72`, `shrink-0`, nằm trong `flex`.
- Trên màn 390px, sidebar chiếm khoảng 255px, `main` chỉ còn khoảng 130px.
- Hậu quả: chữ bị bẻ từng từ, card bị cắt, badge bị tràn. Ví dụ `student_tutor.png`, `student_vocabulary.png`, `admin.png`.
- Body bị khóa cuộn (`overflow-hidden`). Vì vậy đo `scrollWidth` của document không thấy tràn; nội dung bị cắt bên trong `main`.

## Đợt 1: shell responsive (AGY)

Dưới breakpoint `lg` (1024px):
- Sidebar thành **drawer off-canvas**:
  - Mặc định ẩn.
  - Có một top bar cố định trong `main` gồm: nút mở menu (icon, có `aria-label`, `aria-expanded`, `aria-controls`), logo và tiêu đề portal.
  - Có overlay mờ; bấm overlay thì đóng.
  - Đóng khi nhấn Escape, khi đổi route, và khi bấm một mục menu.
  - Focus chuyển vào drawer khi mở và trả về nút menu khi đóng.
  - Khi drawer mở, `main` phía sau không cuộn được.
- Trên mobile, drawer luôn ở dạng mở rộng (không dùng chế độ icon-only).
- `main` chiếm toàn bộ chiều ngang. Padding `p-4` trên mobile.

Từ `lg` trở lên: **giữ nguyên hành vi desktop hiện tại**, gồm cả nút thu gọn/mở rộng và state `isStudentSidebarCollapsed`/`isAdminSidebarCollapsed` trong store.

File được sửa:
- `src/app/layouts/StudentLayout.tsx`
- `src/app/layouts/AdminLayout.tsx`
- `src/features/admin/components/AdminSidebar.tsx`
- Có thể tách một hook nhỏ (ví dụ `src/shared/hooks/useMobileDrawer.ts`) nếu dùng chung được.
- i18n vi/en cho các nhãn mới.

## Đợt 2: lỗi riêng từng trang

Sau đợt 1, chụp lại 25 route ở 390px và 360px. Mỗi lỗi riêng của từng trang ghi vào `audit-round2.md` rồi giao tiếp cho AGY. Các nhóm lỗi dự kiến:
- Bảng admin trên mobile cần cuộn ngang trong một wrapper, hoặc chuyển sang dạng card.
- Toolbar và bộ lọc cần xuống dòng.
- Header trang có badge dài.
- Dialog cần vừa màn hình.
- Vùng bấm nhỏ hơn 32px.

## Kiểm chứng

- `npm run build`; `npm run test` (bao gồm `src/features/grammar/__tests__/studentLayoutGrammarRoute.test.tsx` và test của layout).
- Chạy lại script audit ở 390px và 360px, xem ảnh chụp từng trang. Desktop 1440px không được thay đổi.
- code-reviewer, có skill `portal-design-review`.
