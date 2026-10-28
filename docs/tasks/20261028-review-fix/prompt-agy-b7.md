# Việc giao cho AGY: B7 — MediatR pipeline, transaction và Redis backplane (backend)

## Mục tiêu

Mọi request đi qua pipeline MediatR (không handler gọi handler), command commit nhất quán, và SignalR có thể scale ngang bằng Redis backplane bật theo cấu hình. Liên quan review A2, A3, A5; ADR-0001, ADR-0004. Batch B1–B6 đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, Luật 9), `EnglishPartner.BE/AGENTS.md` (đoạn MediatR)
- ADR: `D:/Projects/EnglishPartner/docs/adr/0001-keep-mediatr.md`, `D:/Projects/EnglishPartner/docs/adr/0004-redis-scale-out.md`
- Review mục A2, A3, A5: `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`
- Skill: `EnglishPartner.BE/.agents/skills/feature-cqrs/SKILL.md`, `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`, `.agents/skills/realtime-session/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
- Code: `src/EnglishLiveTutor.Application/DependencyInjection.cs` (dòng ~47-65 đăng ký handler như service), `Application/Common/Interfaces/ICqrs.cs`, `Application/Common/Behaviors/TransactionBehavior.cs`, `Infrastructure/Persistence/UnitOfWork.cs`; handler inject handler khác (ví dụ `Features/Lessons/Commands/AttemptOperations/StartLessonAttemptCommandHandler` inject `IQueryHandler<ResumeLessonAttemptQuery, ...>`) — tìm bằng `grep -rn "IQueryHandler<\|ICommandHandler<" src --include=*.cs | grep -v "class \| : I"`; 5 file dùng `IRequestHandler` trực tiếp (`grep -rln "IRequestHandler" src/EnglishLiveTutor.Application`); test `tests/EnglishLiveTutor.Domain.Tests/CqrsDependencyInjectionTests.cs`.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Application/**`, `src/EnglishLiveTutor.Infrastructure/Persistence/UnitOfWork.cs`, `src/EnglishLiveTutor.Api/Program.cs`, `src/EnglishLiveTutor.Api/EnglishLiveTutor.Api.csproj` (**được phép thêm duy nhất** package `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, phiên bản khớp major của ASP.NET Core đang dùng), `appsettings.json` (thêm section `Redis` rỗng), `tests/**`.

Không được sửa: route/JSON, migration, FE, hub method/event.

## Yêu cầu

1. **A2.** Bỏ vòng đăng ký handler bằng reflection trong `Application/DependencyInjection.cs`. Handler cần logic của handler khác → tách logic dùng chung ra Application service (ví dụ `LessonAttemptResumeBuilder`) được đăng ký DI và dùng bởi cả hai handler; hoặc gửi qua `ISender` nếu thực sự là một use case riêng. Không handler nào inject `IQueryHandler<>`/`ICommandHandler<>`/`IRequestHandler<>`.
2. **A2.** Chuyển các handler dùng `IRequestHandler` trực tiếp sang interface bọc trong `ICqrs.cs` (request tương ứng chuyển sang `ICommand`/`IQuery`). Không đổi hành vi.
3. **A2.** Cập nhật/viết test DI: mọi request được resolve qua MediatR; không còn service handler đăng ký riêng; một request đi qua `AuthorizationBehavior` và `ValidationBehavior` khi gửi bằng `ISender`.
4. **A3.** Rà các command đánh dấu `ITransactionalCommand`: handler bên trong **không** tự gọi `SaveChanges`/`CommitChangesAsync` trùng với commit của `TransactionBehavior` theo cách gây commit 2 lần ngoài transaction. Command không transactional giữ một lần commit qua `IUnitOfWork`. Không bọc transaction quanh lời gọi provider AI (Luật 8). Chỉ sửa chỗ vi phạm rõ ràng; liệt kê trong report các chỗ đã sửa và chỗ còn nợ kỹ thuật (đặc biệt các handler dùng `IAppDbContextFactory` tạo context riêng để ghi).
5. **A5.** Nếu cấu hình `Redis:ConnectionString` có giá trị → `AddSignalR().AddStackExchangeRedis(connectionString, o => o.Configuration.ChannelPrefix = "englishpartner")`; không có → chạy như hiện tại. Không log connection string. Ghi chú trong `appsettings.json` không được (JSON) — ghi trong report rằng bắt buộc sticky session cho `/hubs/*` khi chạy nhiều instance.
6. **Test.** Mục 3; và một test xác nhận `Program`/DI không đăng ký Redis khi không có cấu hình (nếu khó test host, viết test cho extension method đăng ký).

## Tiêu chí hoàn thành

- [ ] Không còn handler inject handler; không còn đăng ký handler bằng reflection; không còn `IRequestHandler` dùng trực tiếp trong Application.
- [ ] Build pass; test mới pass; không test fail mới ngoài 9 test fail sẵn (`D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`).

## Lệnh kiểm chứng

**Chính sách test để tiết kiệm thời gian:** chỉ chạy `dotnet build` và `dotnet test EnglishLiveTutor.sln --no-build --filter "<tên lớp test mới hoặc liên quan>"` (có thể nhiều điều kiện nối bằng `|`). **Không** chạy toàn bộ suite nhiều lần — orchestrator sẽ chạy toàn bộ một lần khi nghiệm thu. Chạy toàn bộ suite tối đa một lần ở cuối nếu cần.

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

## Ràng buộc

- Không chạy `git add`, `git stash`, `git reset`, `git checkout` (orchestrator quản lý index). Không commit, push, merge, tạo branch; chỉ được thêm đúng package ở mục Phạm vi; không chạy API/SQL/Redis/Gemini.
- Cần sửa ngoài phạm vi hoặc gặp điều mơ hồ: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt yêu cầu 1–6; danh sách handler đã đổi; kết quả rà A3>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
