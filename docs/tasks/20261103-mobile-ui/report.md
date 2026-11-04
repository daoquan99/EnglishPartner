# Report: sửa UI mobile (Student Portal + Admin Portal)

Worker: AGY (3 đợt). Orchestrator (Claude) lo audit, kiểm chứng, review và tự sửa thêm một số chỗ.

## Đã làm

| Đợt | Nội dung |
| --- | --- |
| 1 (AGY) | Dưới `lg`, sidebar của Student và Admin chuyển thành drawer off-canvas: có top bar với nút menu, overlay, đóng bằng Escape, khi đổi route hoặc khi bấm mục menu; có chuyển và trả focus. Từ `lg` trở lên giữ nguyên, gồm cả nút thu gọn. Có hook `src/shared/hooks/useMobileDrawer.ts` (`useMobileDrawer`, `useIsDesktop`). |
| 2 (AGY) | Sửa 14 lỗi của từng trang theo `audit-round2.md`: thanh nút Tutor, flashcard, PronunciationStudio, tab và nút trong trình soạn Grammar Step của admin, tiêu đề và dải số câu trong player Lesson/Grammar, header các trang Grammar Step/Lesson detail/Grammar attempt, dialog AI Vocabulary và Tutor setup, hero của roadmap. |
| Orchestrator | Sửa hồi quy desktop mà đợt 2 gây ra: thanh nút Tutor tràn mép card ở 1440px vì `whitespace-nowrap`. Trên mobile, 3 nút xếp dọc làm khung cao vượt chiều cao cố định nên bị card dưới đè. Cách sửa: 2 nút phụ chỉ hiện icon trên mobile (có `aria-label`), nút chính dùng `flex-1`, thanh mic/voice xếp phía trên tới `xl`. Rubric trên `PronunciationStudio` bị chồng chữ ở desktop: đổi sang `flex-wrap` + `break-all`. Key i18n sai `common.conversationTopic` sửa thành `conversationTopic`. |
| 3 (AGY) | Sửa theo code review: `inert` cho drawer đóng trên mobile; `role="dialog"` + `aria-modal` khi mở, kèm `inert` cho `main`; khóa cuộn bằng chuỗi class theo trạng thái (vì `cn` không gộp class), tự đóng khi chuyển sang desktop; test hook bằng render thật (react-dom/client + act); thêm i18n `vocab.prevCard`/`nextCard`; header Tutor có `sm:flex-wrap`; `lg:w-72`; logo trên top bar dùng `alt=""`. |

## Kiểm chứng

| Cách | Kết quả |
| --- | --- |
| Audit bằng Chrome headless qua DevTools Protocol (`%TEMP%/perf/ui-audit/audit.mjs`): đăng nhập user test trên DB tạm `LingoMint_UiCheck`, 25 route, cộng các trang Grammar attempt, Lesson detail, Lesson player, drawer, 4 dialog và 2 tab | Ở 390px, 360px, 1024px và 1440px: không trang nào tràn ngang, `clipped` = 0 (trừ các label `sr-only`). Đã xem ảnh từng trang trước và sau khi sửa. Bảng admin cuộn ngang trong khung `overflow-x-auto`. |
| `npm run build` | pass |
| `npx vitest run --maxWorkers=2` | 393/393 pass. Khi chạy `npm run test` mặc định trong lúc máy quá tải, mỗi lần có 1–5 test khác nhau bị timeout 5000ms (route render, `ActivityChart`, AI dialog). Chạy riêng từng file hoặc dùng 2 worker thì pass. Đây là test chậm, không phải lỗi logic. |
| code-reviewer | ĐẠT CÓ ĐIỀU KIỆN. Các điều kiện (inert/focus, khóa cuộn, test hook, i18n, header 1024px, alt, class) đã được xử lý ở đợt 3. |

## Chưa kiểm chứng

- Chưa thử trên thiết bị thật (iOS Safari, Android Chrome) và với trình đọc màn hình thật.
- Phiên Live Tutor đang chạy (có mic) chưa được chụp trên mobile.
- Test timeout khi máy quá tải: có thể tăng `testTimeout` hoặc giảm số worker trong cấu hình Vitest. Việc này để task riêng.
