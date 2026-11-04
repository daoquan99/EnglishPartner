# Việc giao cho AGY: thu gọn header trang Tutor và panel hero của Login/Register trên mobile

Mã task: `20261103-compact-headers` · Loại: UI-only FE · Không đổi API, route hay logic phiên live.

## Bối cảnh

- Luật: `AGENTS.md`, `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`.
- Skill:
  - `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/design-system/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Làm trên `development`, **không commit**. Không cài package.
- Ảnh hiện trạng ở 360px:
  - `C:\Users\DELL\AppData\Local\Temp\perf\ui-audit\padA360\student_tutor.png`
  - `C:\Users\DELL\AppData\Local\Temp\perf\ui-audit\padA360\login.png`
  - `C:\Users\DELL\AppData\Local\Temp\perf\ui-audit\padA360\register.png`

## 1. Header trang `/student/tutor` có thể thu gọn/mở rộng

File: `src/features/tutor/pages/LiveTutorPage.tsx`, card header khoảng dòng 226–285. Card này hiện có:
- nút "Cấu hình đàm thoại" và "Cuộc trò chuyện mới";
- 4 badge: CEFR, mode, voice, speed;
- chip chủ đề;
- trạng thái 3 hub;
- nút "Lịch sử đàm thoại".

Trên mobile, card này chiếm khoảng 330px chiều cao.

Yêu cầu:
- **Dòng tóm tắt luôn hiện:**
  - badge CEFR;
  - tên mode rút gọn (`truncate`, có `title` đầy đủ);
  - chấm trạng thái kết nối: một chấm màu tổng hợp từ 3 hub, có `sr-only` mô tả;
  - nút toggle "Chi tiết" / "Thu gọn": icon `ChevronDown`/`ChevronUp`, `aria-expanded`, `aria-controls`, vùng bấm tối thiểu 44px.
- **Phần mở rộng:** các nút và badge còn lại, giữ nguyên chức năng. Nút "Cấu hình đàm thoại" và "Lịch sử" vẫn truy cập được khi thu gọn: đặt dạng icon trong dòng tóm tắt, có `aria-label`. Nút "Cuộc trò chuyện mới" đã có ở thanh nút cuối khung hội thoại.
- **Mặc định:**
  - thu gọn khi dưới `lg`, mở rộng từ `lg` trở lên (dùng `useIsDesktop` trong `src/shared/hooks/useMobileDrawer.ts`);
  - sau khi người dùng bấm toggle, lưu lựa chọn vào `localStorage` (key `lingomint_tutor_header_expanded`) bên trong `try/catch`, và ưu tiên giá trị đã lưu.
- **Desktop khi mở rộng:** trông giống hiện tại.
- i18n vi/en cho các nhãn mới.

## 2. Panel hero của Login và Register trên mobile

File: `src/features/auth/pages/LoginPage.tsx`, `src/features/auth/pages/RegisterPage.tsx`.

Hiện trạng: panel hero xanh nằm phía trên form. Trên mobile, panel này cao khoảng 300–400px (tiêu đề dài 3–4 dòng, phụ đề và các gạch đầu dòng), đẩy form xuống dưới màn hình.

Yêu cầu dưới `lg`:
- Panel hero thu gọn thành một dải ngắn gồm:
  - logo;
  - tag;
  - tiêu đề rút gọn: thêm key i18n mới `auth.heroTitleShort` / `auth.registerHeroTitleShort`, ví dụ "Luyện nói tiếng Anh 1-1 cùng AI" và "Nói tiếng Anh tự tin cùng AI";
  - cỡ chữ `text-lg`.
- Ẩn phụ đề và danh sách lợi ích (`hidden lg:block`).
- Padding của panel: `p-5 lg:p-8`.

Từ `lg` trở lên: **giữ nguyên** tiêu đề dài, phụ đề và các gạch đầu dòng như hiện tại.

Mục tiêu: trên 360×780, ô nhập đầu tiên của form nằm trong màn hình đầu tiên mà không cần cuộn.

## Test

- Thêm test Vitest cho toggle header Tutor:
  - mock `matchMedia` cho mobile: mặc định thu gọn, bấm thì mở và `aria-expanded` = `true`;
  - mock cho desktop: mặc định mở rộng.
- Có thể render một component con nếu tách được, ví dụ `TutorSessionHeader`. Nên tách thành component riêng trong `src/features/tutor/components/`.
- Không xóa test cũ.

## Kiểm chứng

1. Trong `EnglishPartner.FE`: `npm run build` và `npx vitest run --maxWorkers=2`.
2. Chạy audit:

   ```
   node --experimental-websocket C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/audit.mjs C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/hdr360 C:/Users/DELL/AppData/Local/Temp/perf/ui-cred.txt 360 780
   node --experimental-websocket C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/audit.mjs C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/hdr1440 C:/Users/DELL/AppData/Local/Temp/perf/ui-cred.txt 1440 900
   ```

   Không đọc hay in nội dung file `ui-cred.txt`.
3. Xem ảnh `student_tutor.png`, `login.png`, `register.png` ở cả hai khổ.

## Report

Ghi vào `docs/tasks/20261103-compact-headers/runs/agy-report.md`, gồm:
- danh sách file đã sửa;
- chiều cao header và hero trước và sau ở 360px;
- các lệnh đã chạy và kết quả.
