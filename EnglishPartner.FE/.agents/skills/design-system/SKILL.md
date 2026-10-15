---
name: design-system
description: Thiết kế hoặc chỉnh token, theme và shared UI của EnglishPartner FE để nhất quán giữa Student Portal và Admin Portal. Dùng khi thay đổi nền tảng giao diện hoặc component dùng chung; không áp đặt thiết kế lại các trang ngoài phạm vi.
---

# Design system của EnglishPartner FE

Đọc [AGENTS.md](../../../AGENTS.md) và [quy tắc thiết kế](../../design-system.md). Source hiện tại quyết định component API; tài liệu mô tả hướng áp dụng và các giới hạn cần kiểm tra.

## Quy trình

1. Xác định thay đổi thuộc một portal hay shared UI. Đọc `src/index.css`, shell liên quan và primitive đang dùng; tìm các caller trước khi sửa public props.
2. Tái dùng theme/token, typography, radius và spacing hiện có. Với shared UI, giữ semantic variant chung; tránh tạo hai bộ Button/Dialog chỉ khác màu.
3. Phân biệt token khai báo và style thực sự được tiêu thụ. Không hứa đổi theme toàn hệ thống nếu component vẫn hard-code utility class.
4. Khi liên quan modal, kiểm tra portal render vào body và việc mất CSS variable kế thừa. Chọn cách truyền theme phù hợp với source, không thêm global override ảnh hưởng cả hai cổng.
5. Giữ API props tương thích nếu không có lý do đổi. Không thêm design dependency mới khi primitive hiện có có thể mở rộng nhỏ trong phạm vi.

## Điểm quyết định

- Student ưu tiên nội dung đọc/luyện; Admin ưu tiên nhận diện dữ liệu và hành động. Shared primitive không mang nghiệp vụ học hay quản trị.
- Dùng semantic success/warning/danger có text; không gán ý nghĩa nghiệp vụ chỉ bằng màu.
- Kiểm tra disabled/pending, label/icon, keyboard focus và nội dung dài ở component đang sửa.
- Không cho rằng `Dialog` đã xử lý đầy đủ focus hoặc `DataTable` đã xử lý đúng mọi kiểu phân trang; đọc implementation và kiểm tra tình huống thật.

## Bàn giao

Nêu token/component/caller bị tác động, portal đã kiểm tra và giới hạn. Với tài liệu chỉ kiểm tra link/frontmatter/whitespace. Với source chọn build, test và kiểm tra UI phù hợp; nếu chưa có ảnh/runtime thì không tuyên bố đã xác nhận hình thức bằng trình duyệt.
