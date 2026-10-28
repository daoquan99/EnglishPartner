# Việc giao cho AGY: B4 fix 1 — realtime: ghi final an toàn, cô lập lỗi, phiên theo connection, group theo user, kiến trúc (backend)

Các batch trước (B1–B4, B1/B2/B3 fix) đã làm trong worktree này — giữ nguyên. Chỉ sửa các điểm dưới đây (từ security review + performance review B4). Đọc lại `.agents/skills/realtime-session/SKILL.md`.

Ký hiệu: Manager = `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveConnectionManager.cs`; Repo = `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs`; Hubs = `EnglishPartner.BE/src/EnglishLiveTutor.Api/Hubs/`.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Infrastructure/Realtime/**`, `src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs`, `src/EnglishLiveTutor.Infrastructure/EnglishLiveTutor.Infrastructure.csproj` (chỉ để **gỡ** `FrameworkReference Microsoft.AspNetCore.App` và thêm `InternalsVisibleTo` cho project test), `src/EnglishLiveTutor.Domain/Conversations/**`, `src/EnglishLiveTutor.Application/Features/Conversations/**`, `src/EnglishLiveTutor.Application/Common/Models/LiveSessionDtos.cs` (không đổi/xóa field hiện có), `src/EnglishLiveTutor.Api/Hubs/**`, `src/EnglishLiveTutor.Api/Realtime/**`, `tests/**`. Không migration. Không đổi tên hub method/event hay payload client nhìn thấy.

## Yêu cầu

**Kiến trúc**
1. Gỡ `FrameworkReference Microsoft.AspNetCore.App` khỏi Infrastructure. Manager ném exception của Application (`NotFoundException` cho "không tìm thấy/không phải chủ", `ForbiddenException` khi không có user); mỗi hub method bọc lời gọi manager và chuyển các exception đó thành `HubException("Không tìm thấy hội thoại.")` (một helper chung trong `Api/Hubs`). Lỗi khác → `HubException` thông điệp chung, không lộ message gốc.

**Hiệu năng — bắt buộc**
2. **Final không bị ghi đè.** Bản non-final không được ghi đè bản ghi đã `IsFinal`: trong domain (`ChatMessageEntity.UpdateTranscript` / `UpdateTranslation`) bỏ qua cập nhật non-final khi bản ghi đã final. Translation chưa có cột `IsFinal` thì xếp hàng ghi theo từng turn (một `SemaphoreSlim` mỗi turn trong `ConversationTurnState` hoặc `Channel`) để lần ghi sau luôn chạy sau lần trước. Thêm test race: non-final chậm hoàn tất sau final → DB giữ bản final.
3. **Cô lập lỗi.** Mọi lời gọi persist/correction/translation trong handler realtime (`OnServerMessage`, task throttle, `FinalizeTurnAsync`, debounce) bọc try/catch + log (loại exception và conversationId; không log API key, không log `ex.Message` có thể chứa URL). Lỗi DB không được thoát ra vòng nhận của `GeminiLiveSessionBridge`. Task fire-and-forget phải quan sát exception.

**Hiệu năng/khả dụng — nên làm**
4. **Phiên theo connection.** Lưu connectionId sở hữu cho từng `(conversationId, hubType)`. `OnDisconnectedAsync` chỉ đóng bridge/phiên nếu connection đó vẫn là connection hiện hành; Start mới cùng loại hub thay mapping của connection cũ (đóng bridge cũ). Một connection chỉ được gắn một conversation: Start conversation khác trên cùng connection → từ chối.
5. **Vòng dọn idle.** Cờ `Interlocked` bỏ qua lần chạy khi lần trước chưa xong; try/catch từng conversation; kiểm tra lại `lastActivity` ngay trước khi dừng; `CloseAsync` của bridge có timeout 5 giây.
6. **Dừng phiên hủy debounce.** Khi dừng/dọn: hủy và dispose CTS trong `TranslationDebounceByTurn`, `GrammarDebounceByTurn` và CTS throttle.
7. **Dọn an toàn.** Xóa `_sessionOwners` đầu tiên; xóa API key, turn state, owner, connection trong `finally` kể cả khi flush lỗi. `Forward*` dùng `TryGetValue` cho turn state (không tạo mới). `StartTutorSessionAsync` lỗi khi connect/setup → dispose bridge và rollback đăng ký owner/key/connection.
8. **Audio sau khi phiên kết thúc.** `Forward*Audio` khi conversation không có phiên → bỏ qua im lặng (log `Debug`); phiên thuộc user khác → vẫn từ chối (hub trả `HubException`). Các method khác giữ nguyên kiểm tra owner.

**Bảo mật**
9. **Chuẩn hóa `conversationId`.** Validate ở hub/manager: bắt buộc, trim, ≤ 64 ký tự, chỉ `[A-Za-z0-9_-]`; sai → từ chối. Dùng **cùng** giá trị đã chuẩn hóa cho DB, dictionary trong RAM và group.
10. **Group theo user.** Tên group SignalR = `{userId}:{conversationId}` (thay vì chỉ conversationId); cập nhật `SignalRLiveSessionRealtimeNotifier` và mọi chỗ gửi tới group; manager biết owner nên dựng được tên group. Khi dừng/dọn phiên, gỡ connection khỏi group (hoặc giải thích trong report vì sao tên group theo user đã loại bỏ rủi ro).
11. **Lỗi thống nhất.** Thua race insert conversation / id đã soft-delete → `NotFoundException` (→ "Không tìm thấy hội thoại.").
12. **Che API key.** Override `ToString()` của `StartTutorSessionRequest` và `StartSubHubSessionRequest` để không in `ApiKey` (thêm phương thức, không đổi field).
13. **API test.** `RegisterActiveSessionForTesting`, `HasApiKey`, `GetTurnState` và các helper chỉ cho test → `internal` + `InternalsVisibleTo("EnglishLiveTutor.Domain.Tests")`.

**Test**
14. Thêm: race final/non-final (mục 2); lỗi repository không làm manager ném ra ngoài (mục 3); reconnect — disconnect của connection cũ không dừng phiên của connection mới (mục 4); connection thứ hai Start conversation khác bị từ chối; cross-user cho `EndUserSpeech`, `EndTranslateSpeech`, `AnalyzeTurnGrammar`; `conversationId` rỗng/khoảng trắng/ký tự lạ bị từ chối; tên group gồm userId; audio khi không có phiên bị bỏ qua không ném lỗi; `ToString()` không chứa key.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

Build không phát sinh warning mới (3 warning NU1510 hiện tại do `FrameworkReference` phải hết). Không phát sinh test fail mới ngoài 9 test fail sẵn trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`.

## Ràng buộc

Không chạy `git add`/`git stash`/`git reset`/`git checkout`. Không commit/push/merge, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–14; lựa chọn ở mục 2 cho translation; lựa chọn ở mục 10>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test, số warning>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
