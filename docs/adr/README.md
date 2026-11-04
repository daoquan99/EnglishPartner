# Architecture Decision Records

Mỗi ADR ghi một quyết định đã chốt, lý do và hệ quả. Trạng thái: `Proposed` → `Accepted` → (`Superseded by ADR-xxxx`). Sửa quyết định bằng ADR mới, không viết lại ADR đã Accepted.

| ADR | Quyết định | Trạng thái | Liên quan review |
| --- | --- | --- | --- |
| [0001](0001-keep-mediatr.md) | Giữ MediatR làm cơ chế CQRS duy nhất | Accepted | A2 |
| [0002](0002-delete-ownerless-conversations.md) | Thêm `UserId` cho Conversation, xóa hội thoại cũ không có chủ | Accepted | S2, S3 |
| [0003](0003-byo-api-key.md) | Giữ BYO API key, siết cách lưu và xử lý | Accepted | S9 |
| [0004](0004-redis-scale-out.md) | Scale ngang bằng Redis backplane + sticky session | Accepted | A5, P7 |
| [0005](0005-problem-details-error-contract.md) | Lỗi API theo ProblemDetails, giữ field `error` để tương thích | Accepted | S10, A4 |
| [0006](0006-modular-monolith.md) | Backend thành Modular Monolith: 7 module × 5 project, `Contracts`, DbContext + schema theo module (outbox hoãn) | Accepted | A1 |

Mẫu: copy một ADR có sẵn, giữ các mục Bối cảnh, Quyết định, Hệ quả, Việc cần làm.
