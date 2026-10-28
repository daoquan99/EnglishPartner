# ADR-0001: Giữ MediatR làm cơ chế CQRS duy nhất

- Trạng thái: Accepted
- Ngày: 2026-10-28
- Liên quan: review A2

## Bối cảnh

Application dùng MediatR 14.2 (`EnglishLiveTutor.Application.csproj`). `ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler` trong `Common/Interfaces/ICqrs.cs` là lớp bọc mỏng trên `IRequest`/`IRequestHandler`. 5 pipeline behaviors (Logging, Authorization, Validation, Performance, Transaction) dựa vào MediatR. `Application/DependencyInjection.cs` còn đăng ký thêm handler như service thông thường, cho phép handler inject handler khác và bỏ qua pipeline. Từ v13 MediatR cần license thương mại; dự án đã có license key.

## Quyết định

- Giữ MediatR; mọi request đi qua `ISender.Send`.
- Handler mới dùng interface bọc trong `ICqrs.cs`.
- Không inject handler vào handler; logic dùng chung tách thành Application service.
- License key lấy từ biến môi trường `MEDIATR_LICENSE_KEY` (code đã hỗ trợ), không nằm trong `appsettings.json`.

## Hệ quả

- Authorization/Validation/Transaction luôn được áp dụng.
- Phụ thuộc license thương mại; cần theo dõi điều khoản khi gia hạn.

## Việc cần làm

- [ ] Bỏ vòng đăng ký handler bằng reflection trong `Application/DependencyInjection.cs:47-65` và sửa các handler đang inject handler khác (ví dụ `StartLessonAttemptCommandHandler`).
- [ ] Chuyển 5 handler dùng `IRequestHandler` trực tiếp sang interface bọc.
- [ ] Gỡ `MediatR:LicenseKey` khỏi `appsettings.json` (review S5).
