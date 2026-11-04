# Việc giao cho AGY: bỏ padding lồng nhau ở Student Portal

## Bối cảnh

- Đọc `docs/tasks/20261103-student-padding/plan.md`. Bảng bằng chứng, **quy ước padding** và phạm vi file đều nằm trong đó.
- Luật: `AGENTS.md`, `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`.
- Skill: `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`, `EnglishPartner.FE/.agents/skills/design-system/SKILL.md`.
- Làm trên `development`, **không commit**. Không cài package. Chỉ sửa class trình bày. Không đổi logic, nội dung, route hay API. **Không sửa Admin Portal.**

## Yêu cầu

1. Wrapper gốc của từng trang Student trong phạm vi: bỏ `px-*`/`p-*` ngang, giữ `max-w-*` và `mx-auto`. Làm cho **mọi nhánh return**, gồm loading, error và empty.
2. Card cấp 1: `p-4 sm:p-6`. Card hero: `p-5 sm:p-8`.
3. Khối con trong card: `p-3 sm:p-4`.
4. Lớp thứ 3 trở đi: giảm còn `p-2.5 sm:p-3`, hoặc bỏ nền/viền nếu không cần một khung nữa.
5. Header của khung hội thoại Tutor (`px-5`) và thanh nút cuối: dùng cùng nhịp với card (`px-4 sm:px-5`).
6. Không đổi `StudentLayout` `main` (`p-4 sm:p-6`).
7. Nếu một card hay panel trong `src/shared/components/ui` cũng được Admin dùng thì **không sửa component chung**. Truyền `className` ở nơi gọi phía Student.

## Tự kiểm chứng

BE tạm đang chạy ở `localhost:5000`, FE dev ở `localhost:5173`.

1. Trong `EnglishPartner.FE`: `npm run build` và `npx vitest run --maxWorkers=2`.
2. Chạy audit và tóm tắt:

   ```
   node --experimental-websocket C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/audit.mjs C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/pad360-after C:/Users/DELL/AppData/Local/Temp/perf/ui-cred.txt 360 780
   node C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/nesting.js C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/pad360-after/results.json
   ```

   Không đọc hay in nội dung file `ui-cred.txt`.
3. So với số đo trước khi sửa ở `C:/Users/DELL/AppData/Local/Temp/perf/ui-audit/pad360/results.json` (cũng dùng `nesting.js`). Xem ảnh các trang trong bảng bằng chứng.

## Report

Ghi vào `docs/tasks/20261103-student-padding/runs/agy-report.md`, gồm:
- danh sách file đã sửa, mỗi file có class trước và sau;
- bảng `maxPad` trước và sau cho từng trang;
- các lệnh đã chạy và kết quả.
