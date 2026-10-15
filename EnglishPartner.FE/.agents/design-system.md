# Thiết kế hệ thống: Student Portal và Admin Portal

## Nguồn và trạng thái

Baseline được đọc từ source ngày 2026-10-15. Khi thực hiện công việc mới, kiểm tra lại file thực tế; bảng dưới đây không phải bản sao thay thế source.

| Khu vực | Nguồn cần đối chiếu |
| --- | --- |
| Route, lazy page, guard | [router.tsx](../src/app/router.tsx) |
| Shell học viên | [StudentLayout.tsx](../src/app/layouts/StudentLayout.tsx) |
| Shell quản trị | [AdminLayout.tsx](../src/app/layouts/AdminLayout.tsx), [AdminSidebar.tsx](../src/features/admin/components/AdminSidebar.tsx) |
| Token và scroll toàn cục | [index.css](../src/index.css) |
| UI dùng chung | [ui](../src/shared/components/ui) |
| Ngôn ngữ | [i18n](../src/shared/i18n/index.ts) |
| HTTP và type | [httpClient.ts](../src/shared/api/httpClient.ts), [openapi.types.ts](../src/shared/api/openapi.types.ts) |

## Nhận diện và token

| Thành phần | Student Portal | Admin Portal |
| --- | --- | --- |
| Theme scope | `.learnerTheme` | `.adminTheme` |
| Canvas | `--learner-canvas: #fffdf8` | `--admin-bg: #f8fafc` |
| Surface | `--learner-card-surface: #ffffff` | `--admin-surface: #ffffff` |
| Màu đọc chính | `--learner-reading-text: #26443c` | `--admin-text: #0f172a` |
| Hành động chính | `--learner-primary-action: #26443c` | `--admin-primary: #059669` |
| Surface phụ | `--learner-mint-surface: #e2f3ee` | `--admin-surface-muted: #f1f5f9` |
| Border | `--learner-border: #e5e7eb` | `--admin-border: #e2e8f0` |
| Typography | Be Vietnam Pro; Grandstander cho display/brand theo token | Be Vietnam Pro |
| Nhịp nội dung | Dễ đọc, tập trung vào một thao tác luyện tập | Dễ quét, so sánh và thao tác trên dữ liệu |

Student đã khai báo `--font-size-study-body: 1.125rem` và `--font-size-vocab-display: 2rem`; ưu tiên mức đọc này cho nội dung học khi thiết kế mới, không áp chúng cho mọi metadata. Kiểm tra font có được tải thực tế trước khi khẳng định giao diện dùng đúng font.

Tái dùng token phù hợp cho phần đang sửa, không thêm palette mới theo từng trang. Button/shared UI hiện còn nhiều lớp Tailwind emerald/slate cố định; thay đổi token không tự đổi các lớp này. Việc theme hóa shared primitive phải được giới hạn và kiểm tra cả hai portal.

## Shell, responsive và scroll

- Hai shell dùng viewport đầy màn hình, body/root khóa cuộn; sidebar và main có vùng cuộn riêng. Page đi trong main không cần một viewport riêng nữa.
- Desktop: Student có thể dùng hai cột học từ/luyện phát âm; Admin ưu tiên toolbar và vùng bảng/form. Trên màn hình hẹp, xếp cột theo thứ tự sử dụng.
- Dùng `min-w-0`, wrap và vùng cuộn cục bộ cho bảng/nội dung dài; không chỉ dùng `overflow-x-hidden` để che nội dung bị mất.
- Sidebar đang có trạng thái thu gọn. Khi làm responsive, giữ nút mở/đóng và đường đến các thao tác chính; không coi collapsed desktop là đã giải quyết mobile.
- Kiểm tra khoảng 360–390 px, tablet và desktop, cùng zoom 200% khi liên quan. Không ép một chiều cao cố định làm mất footer, câu dài hoặc nút dừng thu âm.
- Dialog cần header nhận diện đối tượng, body cuộn khi dài, footer hành động luôn truy cập được. Tránh nhiều vùng cuộn lồng nhau không có mục đích.

## Thành phần và giới hạn hiện có

| Primitive hiện có | Dùng cho | Điểm cần kiểm tra khi dùng |
| --- | --- | --- |
| `Button`, `IconButton` | Hành động, nút icon có nhãn | Button chưa có prop loading riêng; kết hợp disabled và label pending theo API thực tế |
| `Dialog`, `ConfirmDialog` | Xem/sửa chi tiết, xác nhận thao tác xóa | Dialog render vào document.body; kiểm tra theme, focus trap/restore, aria-labelledby thay vì giả định đã đầy đủ |
| `DataTable` | Bảng quản trị | Có logic cắt trang client; đối chiếu khi truyền trang server, đặc biệt trang cuối, tránh cắt lần hai |
| `FormField` | Nhãn và lỗi trường | Kiểm tra label/input ID và mô tả lỗi nối đúng control |
| `Badge`, `FeedbackBanner` | Trạng thái và phản hồi | Có text mô tả; không chỉ dùng màu |
| `LoadingState`, `EmptyState` | Tải và kết quả rỗng | Error cần nhánh riêng, không dùng EmptyState cho lỗi request |
| `TopicCreatablePicker` | Chọn/tạo topic trong luồng hiện có | Không tự cho phép tạo catalog nếu user thiếu quyền/contract |

Theme đặt trên shell không tự truyền qua React Portal vào document.body. Khi sửa dialog theo theme, kiểm tra CSS scope thực tế và bảo đảm giải pháp không làm portal khác đổi màu. Những hạn chế trên là điểm cần kiểm tra trong công việc liên quan, không phải lệnh sửa toàn hệ thống ngay.

## Ngôn ngữ và nội dung

- Dùng “Student Portal / Cổng Học Viên” và “Admin Portal / Cổng Quản Trị”; giữ brand hiện tại nếu không có yêu cầu đổi.
- Luồng học dùng câu chữ ngắn, rõ bước tiếp theo; nội dung tiếng Anh, IPA, bản dịch và nhận xét có vai trò riêng.
- Giao diện quản trị dùng nhãn thao tác cụ thể như “Lưu thay đổi”, “Xóa từ vựng”, “Tải lại”. Không đưa chi tiết lớp, bảng DB hoặc protocol vào copy người dùng nếu không cần thiết.
- Đọc cơ chế i18next hiện có khi thêm nhãn dùng chung. Không coi toàn bộ app đã song ngữ chỉ vì shell có nút đổi ngôn ngữ.
- Không cắt mất nội dung cần học; cho wrap, cuộn hoặc mở chi tiết. Metadata trong bảng có thể truncate nếu có cách xem đầy đủ.

## Trạng thái, dữ liệu và quyền

Thiết kế rõ: tải lần đầu, đang refetch/lưu, thành công, dữ liệu rỗng, không có kết quả lọc, lỗi có retry, thiếu quyền và dữ liệu chưa được BE hỗ trợ. Chỉ thể hiện các trạng thái liên quan tới luồng đang làm.

Pending action chống submit lặp, giữ dữ liệu form khi lỗi và trả thông báo ở nơi người dùng đang thao tác. Query key phải phản ánh filter, pagination và identity khi dữ liệu cá nhân thay đổi. Không lọc một trang server ở FE rồi hiển thị như toàn bộ dữ liệu.

Đối với Student, từ/nghĩa/câu là các định danh khác nhau. Chấm phát âm của câu không được dùng kết quả của từ để thay thế; không tự chuyển null score thành 0. Đối với Admin, quyền đọc/sửa/xóa phải khớp contract thực tế; không suy diễn quyền từ tên role.

## Accessibility và motion

- Control dùng semantic button/link/input; icon-only có accessible name, tab và filter có trạng thái chọn rõ.
- Có focus nhìn thấy, thứ tự Tab hợp lý và đóng/mở dialog bằng keyboard. Với modal, kiểm tra initial focus, giữ focus, Escape và trả focus về trigger.
- Form lỗi có mô tả bằng text, gắn control liên quan; trạng thái bất đồng bộ có thông báo phù hợp mà không đọc lại toàn bộ trang liên tục.
- Nội dung chính đủ tương phản, không truyền đạt trạng thái bằng màu duy nhất; nút thao tác chính dễ chạm trên màn hình nhỏ.
- Motion ngắn, có mục đích; tôn trọng reduced motion khi thêm animation. Không thêm animation trang trí vào lúc thu âm/chấm điểm hoặc xử lý dữ liệu quản trị.

## Khi API chưa đáp ứng thiết kế

Ghi rõ route/DTO đang có, field/filter còn thiếu và contract đề xuất. UI có thể thể hiện phần chưa khả dụng một cách trung thực; không fake lịch sử, phân trang, số liệu thống kê hoặc trạng thái lưu thành công.

Các yêu cầu cụ thể về chuyển câu/lịch sử nằm trong [prompt FE](../docs/prompts/pronunciation-examples-history-fe.md) và [prompt BE](../docs/prompts/pronunciation-learning-history-be.md). Đây là công việc đề xuất, chưa được coi là đã triển khai.
