# Report: thu gọn header Tutor và hero Login/Register trên mobile

Worker: AGY. Claude sửa thêm theo code review.

## Đã làm

- **`TutorSessionHeader`** (tách ra từ `LiveTutorPage`):
  - Dòng tóm tắt luôn hiện, gồm: chấm trạng thái hub (có `sr-only`), CEFR, mode, một nút toggle duy nhất luôn được render (`aria-expanded`/`aria-controls`). Khi thu gọn, dòng này có thêm nút icon "Cấu hình" và "Lịch sử".
  - Phần chi tiết luôn có trong DOM, ẩn bằng thuộc tính `hidden`.
  - Mặc định: thu gọn dưới `lg`, mở rộng từ `lg` trở lên. Lựa chọn của người dùng được lưu vào `localStorage` trong `try/catch`.
  - Ở 360px, header giảm từ khoảng 330px xuống khoảng 70px khi thu gọn.
- **Login/Register:** dưới `lg`, hero chỉ còn logo, tag và tiêu đề ngắn (`auth.heroTitleShort`/`auth.registerHeroTitleShort`). Ô nhập đầu tiên nằm trong màn hình đầu tiên. Từ `lg` trở lên giữ nguyên như cũ.
- **Login:** sửa câu phụ (không còn nhắc "tài khoản Seeder mẫu") và placeholder (không còn lộ tên tài khoản mẫu).
- **Sửa theo review:**
  - M1 (mất focus khi toggle): dùng một nút toggle cố định.
  - T1 (`aria-controls` trỏ tới phần tử không tồn tại): phần chi tiết luôn được render.
  - T2: trên desktop, toggle nằm ở dòng tóm tắt.
  - T4: bỏ import `Wifi` không dùng.
  - T5: thêm test cho collapse lưu `'false'`, cho trường hợp `localStorage` ném lỗi, và xác nhận nút toggle là cùng một phần tử.

## Kiểm chứng

- `npm run build`: pass.
- `npx vitest run --maxWorkers=2`: 397/397 pass.
- Ảnh chụp bản cuối: `%TEMP%/perf/ui-audit/fin360`, `fin1440` (`student_tutor`, `login`, `register`).

## Chưa kiểm chứng

- Chưa thử với trình đọc màn hình thật.
- Phiên live đang chạy (có trạng thái hub "connected") chưa được chụp.
