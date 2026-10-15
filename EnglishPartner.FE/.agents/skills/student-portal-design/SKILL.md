---
name: student-portal-design
description: Thiết kế và cải tiến Student Portal của EnglishPartner FE cho Live Tutor, flashcard, luyện phát âm và lịch sử học. Dùng cho luồng học viên và shell student; không dùng để thiết kế CRUD quản trị.
---

# Student Portal

Đọc [AGENTS.md](../../../AGENTS.md) và [quy tắc thiết kế](../../design-system.md). Entry point: `src/app/layouts/StudentLayout.tsx`, route `/student/*`, feature `tutor` và `vocabulary`.

## Thiết kế theo thao tác học

- Xác định bước chính người học cần làm: nghe, đọc, thu âm, nhận nhận xét, luyện lại hoặc xem lịch sử. Làm rõ một bước chính tại mỗi trạng thái, giữ cấu hình ít dùng trong phần phụ phù hợp.
- Giữ nền ấm/xanh của `.learnerTheme`, nội dung học dễ đọc, tách câu tiếng Anh, IPA và bản dịch. Không dùng cỡ metadata nhỏ cho nội dung phải đọc thành tiếng.
- Bảo toàn API Key duy nhất ở Left Menu và cơ chế focus hiện có; không thêm ô nhập key trong từng form luyện tập.
- Desktop có thể giữ bố cục flashcard/phòng chấm; mobile xếp cột, giữ nút nghe/thu/dừng dễ truy cập. Không cắt câu dài để vừa chiều cao card.

## Đúng đối tượng và lifecycle

- Truy vết vocabularyId, senseId, example.id hoặc conversation ID từ nội dung hiển thị đến request và kết quả. Chọn một đối tượng hiện hành nhất quán; không để mỗi nút tự dùng câu đầu danh sách.
- Nếu có nhiều câu, dùng điều hướng có vị trí i/N; xử lý 0/1 câu, đầu/cuối danh sách và refetch làm thay đổi danh sách.
- Chụp snapshot mục tiêu, model và accent khi bắt đầu bản thu; kiểm tra session sau các await, kể cả chờ quyền microphone.
- Khi chuyển đối tượng/rời trang, xử lý stream, recorder, audio và response cũ. Dùng session/request identity để tránh kết quả hoặc audio cũ xuất hiện ở mục tiêu mới, kể cả quay lại cùng ID.
- Giữ khác biệt giữa TTS mẫu, bản thu người học, assessment và lịch sử persisted. Không đề xuất nghe lại bản thu nếu storage/API chưa cung cấp.

## Feedback và lịch sử

- Hiển thị đang nghe/đang thu/đang chấm/lỗi với hành động tiếp theo rõ ràng; luôn có cách dừng thu âm.
- Không chuyển null score thành 0; phân biệt không chấm được và chưa luyện.
- Lịch sử phải đúng người, nghĩa và câu/từ; lọc ở server trước phân trang. Lịch sử học flashcard không tự trở thành lịch sử ôn riêng của câu.
- Khi BE thiếu filter/identity/pagination, ghi gap cụ thể và trạng thái UI trung thực; không dùng mock/localStorage làm lịch sử thật.
- Catalog model/voice lấy từ contract và capability thực tế. Không khóa số lượng lựa chọn theo con số quảng bá trong label hiện tại.

## Kiểm chứng

Chọn các tình huống liên quan: 0/1/n mục, text dài, chuyển nhanh A/B/A, từ cùng vocabulary khác sense, đang chờ mic/AI, lỗi quyền mic, lịch sử rỗng/lỗi và màn hình hẹp. Dùng test mock khi cần; không gọi provider trả phí để kiểm tra layout. Báo riêng kiểm tra source, build/test và UI/API thực tế.
