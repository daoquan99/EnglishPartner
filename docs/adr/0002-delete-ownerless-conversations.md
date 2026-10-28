# ADR-0002: Thêm UserId cho Conversation, xóa hội thoại cũ không có chủ

- Trạng thái: Accepted
- Ngày: 2026-10-28
- Liên quan: review S2, S3, A1

## Bối cảnh

`ConversationEntity` không có owner; mọi user đã đăng nhập đọc, sửa, xóa được hội thoại của người khác, và SignalR hub không kiểm tra quyền sở hữu. Dữ liệu cũ không xác định được chủ.

## Quyết định

- Thêm cột `UserId` (bắt buộc, có index) cho `Conversations`.
- Hội thoại hiện có, cùng `Messages` và `Corrections` của chúng, bị **xóa**, không backfill.
- Mọi query/command/hub method lọc và kiểm tra theo `ICurrentUser.UserId`.

## Hệ quả

- Mất toàn bộ lịch sử hội thoại hiện có (không hoàn tác được).
- Đóng được S2 và tạo điều kiện đóng S3.

## Thực hiện an toàn

1. Migration riêng cho việc xóa dữ liệu, tách khỏi migration thêm cột nếu cần thứ tự.
2. Trước khi chạy trên DB thật: backup 3 bảng, báo người dùng số dòng sẽ bị xóa, chờ xác nhận.
3. Migration không tự chạy lúc khởi động ở môi trường thật (xem review P8).

## Việc cần làm

- [ ] Migration thêm `UserId` + xóa dữ liệu không có chủ.
- [ ] Lọc theo owner trong `ConversationRepository` và các handler Conversations.
- [ ] Owner check trong 3 hub và `GeminiLiveConnectionManager`.
- [ ] Test cross-user cho list, get, update, delete và hub.
