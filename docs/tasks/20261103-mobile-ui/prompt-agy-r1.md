# Việc giao cho AGY: đợt 1, shell Student/Admin responsive trên mobile

## Bối cảnh

- Đọc `docs/tasks/20261103-mobile-ui/plan.md`. Phạm vi lần này **chỉ là "Đợt 1"**.
- Luật: `AGENTS.md`, `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`.
- Skill cần đọc:
  - `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/admin-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/design-system/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Làm trên branch `development`, **không commit**. Không cài package mới. Không sửa `EnglishPartner.BE`.

## Lỗi cần sửa

Trên màn hình 390px, `StudentLayout` (`src/app/layouts/StudentLayout.tsx`) và `AdminSidebar` (`src/features/admin/components/AdminSidebar.tsx`, được render trong `src/app/layouts/AdminLayout.tsx`) luôn hiện sidebar rộng `w-64`/`w-72` với `shrink-0` nằm cạnh `main`. Kết quả là `main` chỉ còn khoảng 130px. Mọi trang trong hai portal đều bị bóp méo.

## Yêu cầu

1. Dưới `lg` (Tailwind, nhỏ hơn 1024px):
   - `aside` thành drawer off-canvas:
     - `fixed inset-y-0 left-0 z-50`, rộng `w-72 max-w-[85vw]`.
     - Ẩn bằng `-translate-x-full`, hiện bằng `translate-x-0`, có transition.
     - Luôn ở dạng mở rộng: bỏ qua cờ collapsed, và **ẩn nút thu gọn** trên mobile.
   - Overlay `fixed inset-0 z-40 bg-slate-900/40`, bấm vào thì đóng drawer.
   - Thêm top bar `lg:hidden`, sticky ở đầu `main`, gồm:
     - nút mở menu: icon `Menu` của lucide-react, `aria-label` lấy từ i18n, `aria-expanded`, `aria-controls` trỏ tới id của `aside`, vùng bấm tối thiểu 44×44;
     - logo `/assets/brand/logo-mark.svg` (**không** dùng `brightness-0`/`invert`);
     - tên portal.
     Student dùng nền sáng theo learner theme; Admin dùng nền `slate-900` giống sidebar.
   - Drawer đóng trong các trường hợp:
     - nhấn Escape;
     - bấm overlay;
     - đổi `location.pathname` (dùng `useLocation`);
     - bấm một mục điều hướng hoặc nút chuyển portal.
   - Khi mở, focus phần tử đầu tiên có thể focus trong drawer. Khi đóng, trả focus về nút menu.
   - Khi drawer mở, thêm `overflow-hidden` vào `main` (hoặc tương đương) để nội dung phía sau không cuộn.
   - `main`: chiếm toàn bộ chiều ngang, padding `p-4 sm:p-6` giữ nguyên.
2. Từ `lg` trở lên: hành vi **giống hệt hiện tại**:
   - sidebar tĩnh, có nút thu gọn;
   - giữ state `isStudentSidebarCollapsed`/`isAdminSidebarCollapsed`;
   - không thấy top bar, không có overlay.
3. Có thể tách logic mở/đóng dùng chung vào `src/shared/hooks/useMobileDrawer.ts`, gồm state, Escape, đóng khi đổi route và trả focus. Không tạo hệ thống component song song.
4. i18n: thêm key vào cả `src/shared/i18n/locales/vi.ts` và `en.ts` (ví dụ `nav.openMenu`, `nav.closeMenu`).
5. Test Vitest:
   - Render `StudentLayout` và `AdminLayout` (xem mẫu ở `src/features/grammar/__tests__/studentLayoutGrammarRoute.test.tsx`). Kiểm tra có nút mở menu với `aria-controls`/`aria-expanded`.
   - Test hook: mở, nhấn Escape thì đóng, đổi route thì đóng.
   - Không xóa test cũ. Test cũ chỉ được sửa khi nó phụ thuộc trực tiếp vào markup đã thay đổi.

## Không làm

- Không sửa nội dung của từng trang trong `src/features/**/pages` (để dành cho đợt 2).
- Không đổi route, API hay store ngoài 2 cờ sidebar.

## Kiểm chứng (chạy trong `EnglishPartner.FE`, ghi kết quả vào report)

- `npm run build`
- `npm run test`

## Report

Ghi vào `docs/tasks/20261103-mobile-ui/runs/agy-r1-report.md`:
- danh sách file đã sửa;
- các lệnh đã chạy và kết quả;
- những điểm chưa làm được.
