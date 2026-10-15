---
name: portal-design-review
description: Kiểm tra UI của một thay đổi trong Student Portal hoặc Admin Portal của EnglishPartner FE về theme, responsive, accessibility, trạng thái dữ liệu và tính nhất quán thao tác. Dùng cho yêu cầu review thiết kế; không tự sửa hoặc audit toàn app khi chỉ được yêu cầu đánh giá.
---

# Kiểm tra thiết kế portal

Đọc [AGENTS.md](../../../AGENTS.md), [quy tắc thiết kế](../../design-system.md) và skill portal tương ứng nếu cần chi tiết. Giới hạn vào trang, diff hoặc luồng được yêu cầu.

## Thu thập bằng chứng

- Đọc component, shell, route, hook và adapter liên quan. Phân biệt hành vi xác nhận từ source với điều chỉ quan sát được qua trình duyệt/API.
- Nếu có công cụ UI, kiểm tra trạng thái thực tế ở viewport hẹp và desktop; dùng tài khoản/quyền được phép, tránh submit hoặc xóa dữ liệu ngoài phạm vi.
- Không suy ra giao diện đã đẹp/không tràn chỉ từ Tailwind class. Nếu không kiểm tra được runtime, ghi rõ giới hạn thay vì kết luận đã verified.

## Câu hỏi cần kiểm tra

| Phần | Bằng chứng cần tìm |
| --- | --- |
| Nhận diện portal | Đúng shell/theme; modal render ra body vẫn có style phù hợp |
| Tác vụ chính | Người dùng biết bước tiếp theo; nhãn pending/disabled đúng thao tác |
| Responsive | Sidebar/main/body cuộn đúng, text dài không mất, bảng/modal dùng được trên màn hình hẹp |
| Accessibility | Keyboard, focus nhìn thấy, tên control, dialog focus/restore, lỗi có text |
| Dữ liệu | Loading/rỗng/lỗi/thiếu quyền tách biệt, không trình bày fallback như kết quả persisted |
| Định danh | Nội dung, request, kết quả và lịch sử cùng user/từ/nghĩa/câu hoặc bản ghi admin |
| Bất đồng bộ | Đổi đối tượng/refetch không để response cũ ghi đè, không submit lặp |
| Phân trang và quyền | Server/client pagination đúng, route/menu/action khớp quyền |

Không cần kiểm tra phần không liên quan tới thay đổi. Ví dụ sửa label không buộc thử microphone hoặc audit toàn bộ authorization.

## Kết quả

Mỗi finding nêu mức độ, file/symbol, thao tác kích hoạt, hậu quả với người dùng, bằng chứng và cách sửa hẹp. Sắp theo ảnh hưởng: sai dữ liệu/thao tác không dùng được trước, vấn đề đọc/scroll tiếp theo, chi tiết thị giác sau cùng. Tách vấn đề mới khỏi hạn chế có sẵn khi review diff.

Nếu chỉ có đề xuất thẩm mỹ, ghi rõ đó là đề xuất. Không tuyên bố đạt accessibility hoặc hoàn tất runtime review nếu chỉ kiểm tra source. Không tự sửa code khi nhiệm vụ chỉ là review.
