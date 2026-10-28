---
name: security-audit
description: Review hoặc thiết kế thay đổi EnglishPartner (BE và FE) về xác thực, phân quyền, ownership/IDOR, secret, token, rate limit, upload, SignalR và API key BYO. Dùng khi diff đụng auth, dữ liệu cá nhân, hub, upload, AI hoặc cấu hình; không dùng để audit toàn repo khi chỉ được giao một diff.
---

# Security audit

Đọc [AGENTS.md gốc](../../../AGENTS.md) (Luật bất biến 1–5) và các mục S trong [review 2026-10-28](../../../docs/reviews/2026-10-28-security-performance-architecture.md). Review **diff được giao**, đối chiếu code hiện tại; không lặp lại finding cũ khi code đã đổi.

## Checklist BE

- **Danh tính:** user lấy từ `ICurrentUser.UserId` (claims). Không có tham số `userId` trong query/body/hub payload cho dữ liệu cá nhân; không fallback `default-learner`/`anonymous`.
- **Ownership:** mọi query/command/hub method đọc hay ghi dữ liệu cá nhân lọc theo owner ở câu truy vấn, không chỉ kiểm tra sau khi đã load. Bảng con (message, answer, round) đi qua root có owner. Không phải chủ → `NotFoundException`.
- **Phân quyền:** endpoint có `RequireAuthorization()`/`RequirePermission(...)` đúng; command có `IRequirePermission` khi cần. Không thêm đường bypass ADMIN mới; không đăng ký `AllowAllPermissionChecker` ngoài test.
- **Secret/config:** không literal secret, mật khẩu seed hay fallback key; option bắt buộc có `ValidateOnStart`/fail-fast. `Jwt:SecretKey` ≥ 32 byte.
- **Token:** refresh token lưu hash, rotate, phát hiện reuse; cookie `HttpOnly`, `Secure`, `SameSite`; `access_token` query chỉ cho `/hubs`.
- **Rate limit/timeout:** endpoint auth và AI có policy rate limit; HttpClient có timeout; retry có giới hạn.
- **Input/upload:** validator FluentValidation cho request mới; upload whitelist MIME/extension, giới hạn kích thước, tên file do server sinh, path nằm trong storage root.
- **Lỗi:** không trả `ex.Message`/stack trace cho client; dùng ProblemDetails.
- **SignalR:** hub method kiểm tra owner của `conversationId`/session trước `Groups.AddToGroupAsync` và mọi thao tác; dọn state khi disconnect.
- **BYO key** ([ADR-0003](../../../docs/adr/0003-byo-api-key.md)): không log, không lưu DB, không giữ sau phiên.

## Checklist FE

- Access token chỉ trong bộ nhớ; không đưa token/API key vào `localStorage`, URL hay log.
- API key BYO lưu `sessionStorage` (ADR-0003); input dạng password, không in ra console.
- Không `dangerouslySetInnerHTML`/`eval`; nội dung AI render dạng text.
- Route guard chờ khởi tạo phiên; ẩn nút không thay authorization BE.
- Logout xóa cache TanStack Query và state người dùng.

## Cách báo cáo

Mỗi finding: mức độ (Nghiêm trọng/Cao/Trung bình/Thấp), `file:dòng`, điều kiện kích hoạt, hậu quả, đề xuất sửa. Chỉ báo điều đã xác minh trong code; ghi rõ phần chưa kiểm chứng được (runtime, SQL thật). Mô tả lớp lỗi, không viết exploit từng bước. Không sửa code khi chỉ được giao review.
