# Việc giao cho AGY: đợt 2, sửa lỗi UI mobile của từng trang

## Bối cảnh

- Đợt 1 (drawer cho shell) đã xong và đã được kiểm chứng. Không sửa lại phần đó.
- Danh sách lỗi cần sửa: `docs/tasks/20261103-mobile-ui/audit-round2.md`, gồm **14 mục** và phần "Quy tắc sửa chung". Đọc kỹ file này.
- Ảnh chụp minh họa cho từng lỗi nằm ở `C:\Users\DELL\AppData\Local\Temp\perf\ui-audit\m390-r1b\` và `...\m360\`. Nếu xem được ảnh thì nên xem.
- Luật: `AGENTS.md`, `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`.
- Skill cần đọc:
  - `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/admin-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/portal-design-review/SKILL.md`
- Làm trên branch `development`, **không commit**. Không cài package. Không sửa `EnglishPartner.BE`. Không đổi API, route, store hay logic nghiệp vụ; chỉ sửa class/markup trình bày. Dùng `rg` với cột "Tìm theo" để tìm file.

## Yêu cầu

1. Sửa cả 14 mục theo "Quy tắc sửa chung".
   - Mức 1 và 2 bắt buộc phải xong.
   - Mức 3 nên làm. Nếu bỏ qua mục nào thì ghi lý do vào report.
2. Desktop phải giữ nguyên:
   - mọi thay đổi bố cục chỉ áp dụng dưới `sm`/`lg`, dùng class responsive của Tailwind;
   - không đổi màu, font hay khoảng cách ở desktop.
3. Không xóa nội dung hay chức năng. Nhãn quá dài trên mobile thì cho xuống dòng gọn gàng hoặc cuộn ngang, không cắt mất thông tin.
4. Test: cập nhật test có sẵn nếu chúng phụ thuộc vào class hay markup đã đổi. Không xóa test.

## Tự kiểm chứng

BE tạm đang chạy ở `localhost:5000`, FE dev ở `localhost:5173`.

1. Trong `EnglishPartner.FE`: chạy `npm run build` và `npm run test`.
2. Chạy lại audit:

   ```
   node --experimental-websocket C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/audit.mjs C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/m360-r2 C:/Users/DELL/AppData/Local/Temp/perf/ui-cred.txt 360 780
   ```

   Không đọc hay in nội dung file `ui-cred.txt`. Script tự tạo dữ liệu thử (attempt) trong DB tạm.
3. Mở các ảnh chụp mới, đối chiếu với từng mục trong audit.

## Report

Ghi vào `docs/tasks/20261103-mobile-ui/runs/agy-r2-report.md` một bảng 14 dòng, mỗi dòng gồm:
- số thứ tự mục;
- file đã sửa;
- cách sửa;
- kết quả kiểm bằng ảnh chụp.

Kèm theo các lệnh đã chạy và kết quả.
