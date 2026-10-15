---
name: admin-portal-design
description: Thiết kế và cải tiến Admin Portal của EnglishPartner FE với bảng, bộ lọc, form CRUD, catalog và thao tác theo quyền. Dùng cho các trang admin và navigation quản trị; không dùng cho luồng luyện tập của học viên.
---

# Admin Portal

Đọc [AGENTS.md](../../../AGENTS.md) và [quy tắc thiết kế](../../design-system.md). Entry point: `/admin/*`, `src/app/layouts/AdminLayout.tsx`, `src/features/admin/components/AdminSidebar.tsx`, `src/features/admin/pages`, API/hooks của feature admin.

## Bố cục và mật độ thông tin

- Giữ `.adminTheme`: canvas slate nhạt, surface trắng, emerald cho hành động chính và rose cho thao tác nguy hiểm. Dùng Be Vietnam Pro, nhãn dữ liệu rõ; không áp phong cách flashcard vào bảng quản trị.
- Trang danh sách có tiêu đề, thông tin tổng hợp thực sự có dữ liệu, toolbar tìm kiếm/lọc và hành động chính phù hợp quyền. Tránh KPI trang trí không có API.
- Tái dùng `DataTable`, `FormField`, `Dialog`, `ConfirmDialog`, `FeedbackBanner`, `EmptyState` và `LoadingState` khi hợp với nhiệm vụ.
- Cột ưu tiên định danh, trạng thái và thao tác; thông tin dài mở chi tiết thay vì kéo bảng quá rộng. Cho bảng cuộn ngang cục bộ, toolbar wrap và form dùng được trên màn hình nhỏ.

## Dữ liệu và form

- Xác nhận API đang phân trang server hay client trước khi thiết kế footer. Không lọc/sắp xếp một trang rồi coi đó là kết quả của toàn bộ tập dữ liệu.
- Đối chiếu logic cắt trang hiện có trong DataTable; kiểm tra trang cuối, đổi page size/filter và xóa bản ghi cuối trang.
- Tạo/sửa có giá trị ban đầu, validation và lỗi tại đúng trường nếu API cung cấp. Pending chống submit lặp; lỗi giữ form và dialog để người dùng sửa/thử lại.
- Xóa hoặc thay đổi nguy hiểm nêu rõ đối tượng và tác động qua ConfirmDialog phù hợp; việc bấm xác nhận trong UI là hành động của người dùng sản phẩm, không thêm vòng hỏi phép tác giả task.
- Sau mutation, invalidate các query bị ảnh hưởng thực sự, kể cả catalog/student khi có dữ liệu dùng chung. Không dùng full page reload thay cho cập nhật có kiểm soát.

## Quyền và contract

- Đọc `src/app/router.tsx`, AdminSidebar, auth guard và contract BE liên quan. Menu, route và action phải dùng cùng quy tắc quyền đúng; source có thể đang lệch, không copy lỗi chỉ để giống nhau.
- Không suy ra quyền sửa/xóa từ quyền đọc hoặc từ tên role. Trạng thái thiếu quyền khác lỗi tải dữ liệu và danh sách rỗng.
- Không bỏ guard để làm màn hình vào được; nếu BE thiếu quyền/endpoint, ghi rõ yêu cầu và giới hạn tích hợp.
- Không đưa API key/token hoặc dữ liệu nhạy cảm vào bảng, toast hay log. Chỉ hiển thị field cần cho tác vụ quản trị.

## Kiểm chứng

Kiểm tra quyền đọc và quản lý riêng, danh sách rỗng/lỗi, pending/lỗi lưu, nội dung dài, modal scroll/focus, phân trang cuối và layout hẹp trong phạm vi thay đổi. Test có ý nghĩa cho mapping/query/permission khi liên quan, không chạy suite ngoài phạm vi. Handoff nêu contract thực tế và phần chưa được BE/runtime xác nhận.
