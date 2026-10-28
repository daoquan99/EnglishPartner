# Việc giao cho AGY: B4 — Conversation có chủ sở hữu, ownership cho SignalR hub, vòng đời phiên live (backend)

## Mục tiêu

Mỗi hội thoại thuộc về đúng một user; REST và 3 SignalR hub chỉ cho chủ sở hữu thao tác; phiên live được dọn khi ngắt kết nối hoặc nhàn rỗi; bỏ lock toàn cục khi ghi transcript. Liên quan review S2, S3, P2, P7, S9 (phía server), ADR-0002, ADR-0003, ADR-0004. Batch B1–B3 đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, Luật 1, 2, 4, 8, 12), `EnglishPartner.BE/AGENTS.md`, `EnglishPartner.BE/.agents/architecture.md` (mục "Streaming conversation exception")
- ADR: `D:/Projects/EnglishPartner/docs/adr/0002-delete-ownerless-conversations.md`, `0003-byo-api-key.md`, `0004-redis-scale-out.md`
- Review mục S2, S3, P2, P7, S9: `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`
- Skill: `.agents/skills/realtime-session/SKILL.md`, `.agents/skills/security-audit/SKILL.md`, `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`, `EnglishPartner.BE/.agents/skills/ef-performance/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
- Code:
  - `src/EnglishLiveTutor.Domain/Conversations/ConversationEntity.cs` (không có owner), `Domain/Conversations/Repositories/IConversationRepository.cs`
  - `src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs` (Singleton, `SemaphoreSlim _writeLock` toàn cục, không lọc theo user)
  - `src/EnglishLiveTutor.Infrastructure/Persistence/Configurations/ConversationConfigurations.cs`
  - `src/EnglishLiveTutor.Application/Features/Conversations/**` (command/query handlers, `Ports/ILiveSessionService.cs`)
  - `src/EnglishLiveTutor.Api/Endpoints/ConversationEndpoints.cs`
  - `src/EnglishLiveTutor.Api/Hubs/{TutorConversationHub,TranscribeLiveHub,TranslateLiveHub}.cs` (`Groups.AddToGroupAsync` trước mọi kiểm tra)
  - `src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveConnectionManager.cs` (`_bridges`, `_sessionApiKeys`, `_turnStates` theo `conversationId`, không dọn khi disconnect), `GeminiLiveSessionBridge.cs`

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Domain/Conversations/**`, `src/EnglishLiveTutor.Application/Features/Conversations/**`, `src/EnglishLiveTutor.Application/Common/Models/LiveSessionDtos.cs` (chỉ thêm, không đổi field hiện có), `src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs`, `src/EnglishLiveTutor.Infrastructure/Persistence/Configurations/ConversationConfigurations.cs`, `src/EnglishLiveTutor.Infrastructure/Realtime/**`, `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs`, `src/EnglishLiveTutor.Infrastructure/Migrations/**` (chỉ thêm migration mới + snapshot), `src/EnglishLiveTutor.Api/Hubs/**`, `src/EnglishLiveTutor.Api/Endpoints/ConversationEndpoints.cs`, `src/EnglishLiveTutor.Api/Realtime/**`, `appsettings.json` (chỉ thêm section `LiveSession`), `tests/**`.

Không được sửa: migration cũ, FE, tên hub method/event, tham số và payload hub hiện có, route/JSON thành công của REST.

## Yêu cầu

1. **Domain.** `ConversationEntity` có `UserId` (string, bắt buộc, max 64). Tạo hội thoại phải truyền userId (thiếu → `ArgumentException`).
2. **Migration (ADR-0002).** Tạo 2 migration mới, theo thứ tự:
   - `DeleteOwnerlessConversations`: `migrationBuilder.Sql` xóa toàn bộ `Corrections`, `Messages`, `Conversations` hiện có (kiểm tra tên bảng thật trong configuration/snapshot; xóa bảng con trước). Comment đầu file ghi: không hoàn tác được, phải backup trước khi chạy trên DB thật. `Down` để trống kèm comment.
   - `AddConversationUserId`: cột `UserId` non-null + index `(UserId, UpdatedAtUtc)`.
   Sinh bằng `dotnet ef migrations add <Tên> --project src/EnglishLiveTutor.Infrastructure --startup-project src/EnglishLiveTutor.Api` với biến môi trường `ASPNETCORE_ENVIRONMENT=Development` (migration rỗng cho bước xóa rồi điền SQL). **Không** chạy `dotnet ef database update`.
3. **REST.** Mọi query/command Conversation lấy user từ `ICurrentUser`; repository nhận `userId` tường minh và lọc `c.UserId == userId` ngay trong truy vấn. Không phải chủ → not found (404 như hiện tại). Tạo hội thoại gán owner là user hiện tại.
4. **Hub ownership (S3).** Lấy user id từ `Context.User` (claim `sub`/`NameIdentifier`); không có → `HubException`. Đổi port `ILiveSessionService` để mọi method nhận `userId` (và `connectionId` khi khởi tạo). Manager:
   - Khi Start (tutor/transcribe/translate): nếu conversation đã tồn tại trong DB với owner khác → từ chối (`HubException` "Không tìm thấy hội thoại."); nếu chưa có → tạo với owner là user hiện tại (giữ hành vi tạo-hoặc-cập-nhật của tutor). Nếu phiên đang chạy trong bộ nhớ thuộc user khác → từ chối.
   - Hub chỉ gọi `Groups.AddToGroupAsync` **sau khi** manager chấp nhận.
   - `Forward*Audio`, `End*`, `AnalyzeTurnGrammar`, `StopSession`: kiểm tra phiên của `conversationId` thuộc `userId`; không thuộc → `HubException` (audio chunk có thể bỏ qua im lặng để không spam lỗi — chọn một và ghi lý do).
5. **Vòng đời (P7, S9).** Ghi nhận connectionId → (userId, conversationId, loại hub). Mỗi hub override `OnDisconnectedAsync` gọi manager: tutor ngắt → dừng toàn bộ phiên của conversation; transcribe/translate ngắt → đóng bridge tương ứng. Khi conversation không còn bridge nào → xóa API key, turn state, owner. Thêm dọn phiên nhàn rỗi: không có audio/hoạt động quá `LiveSession:IdleTimeoutMinutes` (mặc định 15) thì dừng; kiểm tra mỗi phút, dùng `TimeProvider` để test được. API key không bao giờ được log hay đưa vào message lỗi.
6. **Ghi transcript (P2).** Bỏ `SemaphoreSlim` toàn cục trong `ConversationRepository`. Race khi insert cùng `(ConversationId, TurnId, Speaker)`: bắt `DbUpdateException`, nạp lại và cập nhật một lần. Manager chỉ persist transcript/translation tạm (non-final) tối đa mỗi 2 giây cho mỗi turn; luôn persist khi final và khi phiên dừng (flush phần còn chờ). Broadcast realtime tới client giữ nguyên tần suất như cũ.
7. **Repository Singleton.** Repository không dùng `ICurrentUser` (callback realtime không có HttpContext); actor khi soft delete lấy từ userId truyền vào.
8. **Test (bắt buộc).** REST: user B không list/get/update/delete được hội thoại của user A. Manager/hub (dùng notifier và repository giả hoặc InMemory, không mở WebSocket thật): B không Start được conversation của A; B không forward/stop được phiên của A; disconnect tutor xóa API key và state; phiên nhàn rỗi bị dọn sau timeout (fake `TimeProvider`); persist non-final bị throttle, final luôn được ghi.

## Tiêu chí hoàn thành

- [ ] Yêu cầu 1–8 xong; tên hub method/event và payload không đổi.
- [ ] Build pass; test mới pass; không phát sinh test fail mới ngoài 9 test fail sẵn (`D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`).
- [ ] Hai migration mới có trong `Migrations/`, snapshot cập nhật, `dotnet build` pass.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

## Ràng buộc

- Không chạy `git add`, `git stash`, `git reset`, `git checkout` (orchestrator quản lý index). Không commit, push, merge, tạo branch; không thêm package; không chạy API, `database update`, SQL Server hay Gemini.
- Cần sửa ngoài phạm vi hoặc gặp điều mơ hồ: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt yêu cầu 1–8; lựa chọn ở yêu cầu 4 về audio chunk>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
