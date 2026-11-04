# ADR-0005: Lỗi API theo ProblemDetails, giữ field `error` để tương thích

- Trạng thái: Accepted (người dùng duyệt qua plan `docs/tasks/20261028-review-fix/plan.md`, 2026-10-28)
- Ngày: 2026-10-28
- Liên quan: review S10 (mục Trung bình về `ex.Message`), A4

## Bối cảnh

Mỗi endpoint tự `try/catch` và trả `{ error: ex.Message }`; exception lạ thành 500 không đồng nhất. `EnglishPartner.BE/.agents/architecture.md` mục 5 yêu cầu giữ contract `{ error }` ở giai đoạn đầu. FE `extractApiErrorMessage` đọc được `detail`, `message`, `errors`, `title` nhưng chưa đọc `error`.

## Đề xuất

- Thêm global exception handler (`IExceptionHandler`) + `AddProblemDetails`, map `NotFoundException` → 404, `ForbiddenException` → 403 (hoặc 404 với tài nguyên cá nhân), `ConflictException` → 409, `ValidationException` → 400 kèm `errors`.
- Body là ProblemDetails (RFC 9457) **có thêm extension `error`** cùng nội dung với `detail`, để client cũ vẫn chạy.
- Không trả message của exception không lường trước; log kèm `traceId` và trả `traceId` cho client.
- Gỡ dần `try/catch` lặp ở endpoint.
- FE `extractApiErrorMessage` đọc thêm `error`.

## Hệ quả

- Contract lỗi đồng nhất; endpoint mỏng hơn.
- Là thay đổi contract — làm theo skill `api-contract-sync`, BE và FE cùng task.

## Việc cần làm

- [x] Người dùng xác nhận ADR.
- [x] Global handler + ProblemDetails. — `Api/Exceptions/GlobalExceptionHandler.cs`, body có extension `error`; FE `httpClient.ts` đọc `error`.
- [x] Gỡ try/catch endpoint theo từng nhóm. — còn 12 `catch` cố ý (refresh lỗi xóa cookie → 401; `InvalidOperationException` → 409; `DailyPlanNotFoundException` → 400; media `ForbiddenException` → 404 để không lộ tồn tại): mỗi chỗ trả status khác mặc định của global handler, gỡ sẽ đổi contract. (xác nhận 2026-11-02)
