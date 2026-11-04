# Audit sau đợt 1 (390px và 360px)

Ảnh chụp nằm trong:
- `%TEMP%/perf/ui-audit/m390-r1b/*.png`, khổ 390×844;
- `%TEMP%/perf/ui-audit/m360/*.png`, khổ 360×780, gồm cả drawer, dialog, tab và các trang làm bài.

Trong các ảnh chụp toàn trang, `main` được mở rộng để thấy hết nội dung.

## Đã ổn

- Drawer của Student và Admin: mở/đóng đúng, overlay hoạt động, trên mobile không còn sidebar nằm cố định.
- Không trang nào tràn ngang ra ngoài viewport.
- Các bảng admin cuộn ngang được bên trong khung `overflow-x-auto`.
- Trang Login và Register.

## Lỗi còn lại (đợt 2)

Ký hiệu: `S` = mức độ (1 nặng, 3 nhẹ). Cột "Tìm theo" là đoạn text dùng để `rg` ra file.

| # | S | Trang / thành phần | Lỗi | Tìm theo |
| --- | --- | --- | --- | --- |
| 1 | 1 | `/student/tutor`, thanh nút cuối khung hội thoại | 3 nút "Cuộc trò chuyện mới", "Cấu hình đàm thoại", "Bắt đầu đàm thoại" chia 3 cột. Chữ bị bẻ từng từ, mỗi nút cao 4 dòng, icon chỉ còn khoảng 8px. | `Bắt đầu đàm thoại` |
| 2 | 2 | `/student/tutor`, header khung hội thoại | Badge "Phiên mới" bị xuống 2 dòng. Dòng "0 Số lượt đàm thoại:" bị chèn nút "Đổi thiết lập" vào giữa. | `Phiên mới`, `Đổi thiết lập` |
| 3 | 1 | `/student/vocabulary`, khu thẻ flashcard | Nút "Lật mặt sau" bị bóp còn khoảng 50px, chữ xuống 3 dòng. Mũi tên chuyển thẻ bên phải sát/tràn mép card. Header thẻ có 3 badge cùng nhóm nút trên một hàng không xuống dòng. | `Lật mặt sau` |
| 4 | 2 | `/student/vocabulary`, khối "Phòng Chấm Phát Âm Audio AI" | Hàng "Cổng AI Model hệ thống" có badge "Google Gemini API" và nút "Cấu hình" chen nhau. Dòng Rubric "Accuracy 40% · Clarity…" bị bẻ lộn xộn. | `Phòng Chấm Phát Âm`, `Accuracy` |
| 5 | 1 | `/admin/grammar/:code` (`AdminGrammarStepEditorPage`), tab Thông tin / Lý thuyết / Bài tập chính thức | Segmented tab chia 3 cột bằng nhau. Nhãn bị bẻ thành 2–5 dòng, ví dụ "Bài tập chính thức (10)" thành 5 dòng. | `Bài tập chính thức` |
| 6 | 1 | `/admin/grammar/:code`, card bài tập | Hàng nút "Xem trước", "Xem đáp án", "Chỉnh sửa" bị bẻ chữ 2–3 dòng. | `Xem đáp án` |
| 7 | 1 | `/student/lesson-attempts/:id` (player) | Tiêu đề bài học **không xuống dòng** và bị cắt ("Talking About My Daily Routine and Remo…" tràn khỏi khung). | tiêu đề lesson trong player |
| 8 | 2 | `/student/lesson-attempts/:id` và `/student/grammar/attempts/:id` | Dải số câu (1, 2, 3…) cuộn ngang, nhưng **vòng focus/active của ô số 1 bị cắt mép trái**. | dải số câu trong player |
| 9 | 2 | `/student/grammar/attempts/:id`, header | Nút back, badge mono "Bài tập cốt lõi: A1-01" (bị bẻ 2 dòng) và cảnh báo "Bài thi này chỉ cho phép nộp mỗi câu 1 lần" chen trên một hàng. | `chỉ cho phép` |
| 10 | 2 | `/student/grammar/:code` (`GrammarStepPage`), header | Link "Về Lộ trình" bị bẻ 2 dòng, chen với cụm tiêu đề. | `Về Lộ trình` hoặc key i18n tương ứng |
| 11 | 2 | `/student/lessons/:id` (chi tiết bài học), header | Link "Danh sách bài học" bị bẻ dòng, chen với nút "Bắt đầu làm bài". | `Bắt đầu làm bài` |
| 12 | 3 | Dialog "Tạo Từ Vựng Thông Minh" (`AiVocabularyGeneratorDialog`) | Tiêu đề bị cắt mà không có dấu "…". Tab "Gọi AI Trực Tiếp (API Key)" / "Web AI: Copy Prompt & Nhập JSON" bị bẻ 3 dòng, icon tab chỉ còn khoảng 8px. Badge "Gợi ý từ Database (24) + Tự nhập" bị bẻ 3 dòng. | `Tạo Từ Vựng Thông Minh`, `Web AI` |
| 13 | 3 | Dialog "Thiết lập Phiên Luyện Nói" | Nút footer "Lưu thiết lập" và "Bắt đầu luyện nói" bị bẻ 2 dòng. Chip CEFR chỉ còn "Begin…", "Eleme…". Có thể chấp nhận, nhưng nên cho chip nhóm CEFR xuống 2 hàng (3×2). | `Thiết lập Phiên Luyện Nói` |
| 14 | 3 | `/student/grammar` (roadmap), hero | `section` hero có `scrollWidth` 390 > `clientWidth` 326 (bị `overflow-hidden` cắt). Cần kiểm tra phần tử nào rộng hơn khung (có thể là phần trang trí). | `Lộ trình chuẩn CEFR` |

## Quy tắc sửa chung

- Nút có icon và nhãn ngắn nằm trong một hàng: thêm `whitespace-nowrap`, cho hàng cha `flex-wrap`, hoặc chuyển thành `grid grid-cols-1 sm:grid-cols-3`. Không để nút co lại và bẻ chữ từng từ. Icon giữ `w-4 h-4 shrink-0`.
- Header gồm nút back, tiêu đề và badge/nút: trên mobile xếp thành cột (`flex-col sm:flex-row`). Tiêu đề dùng `min-w-0 break-words`.
- Segmented tab nhiều nhãn: cho cuộn ngang (`overflow-x-auto`, mỗi tab `shrink-0 whitespace-nowrap`), hoặc rút gọn nhãn trên mobile. Không chia đều 3 cột khi nhãn dài.
- Dải nút số cuộn ngang: thêm padding trong (`px-1 py-1`) để vòng `ring` không bị cắt.
- Desktop (từ `lg` trở lên) không được đổi giao diện. Nếu cần khác biệt, dùng breakpoint `sm:`/`lg:`.
