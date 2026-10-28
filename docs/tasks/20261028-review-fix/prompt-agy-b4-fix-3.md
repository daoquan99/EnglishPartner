# Việc giao cho AGY: B4 fix 3 — realtime: không chặn vòng nhận, giữ grammar final, dọn semaphore an toàn, rollback/bridge mồ côi (backend)

Các batch trước (gồm B4 fix 2) đã làm trong worktree này — giữ nguyên. Chỉ sửa các điểm dưới đây (từ security review + performance review B4 fix 2). Đọc `.agents/skills/realtime-session/SKILL.md`.

Ký hiệu: M = `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveConnectionManager.cs`; Repo = `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs`; T = `EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/ConversationOwnershipAndRealtimeLifecycleTests.cs`. Số dòng là xấp xỉ.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Infrastructure/Realtime/**`, Repo, `tests/**`. Không migration, không đổi hub method/event client nhìn thấy, không đổi `ILiveSessionService`.

## Yêu cầu

1. **H1 — không chặn vòng nhận của bridge.** Ở M (~550, ~571) `await FinalizeTurnAsync("user", …)` đang chặn tin nhắn tiếp theo của Gemini (audio tutor bị trễ bằng thời gian ghi DB + dịch). Chạy finalize lượt user ở nền (`Task.Run` có try/catch, log `ex.GetType().Name` + conversationId), không `_ =` trần. Thứ tự ghi final vẫn được bảo đảm bởi semaphore theo turn.
2. **H2 — không hủy grammar check final.** `FinalizeTurnAsync` gọi `ScheduleUserGrammarCheck(..., immediate: true)` rồi `CleanupTurnResources` hủy/dispose đúng CTS đó (M ~1520–1530, ~1737). Sửa: CTS của grammar final không bị `CleanupTurnResources` hủy (ví dụ không lưu vào `GrammarDebounceByTurn`, hoặc gỡ entry đó trước khi cleanup mà không cancel; dispose CTS khi task grammar xong). Grammar final chỉ bị hủy khi dọn cả phiên. Sửa test F6 (T ~1158) để chứng minh grammar final **vẫn chạy** (analyzer giả ghi nhận được lời gọi) và dictionary vẫn sạch sau khi xong.
3. **M1 (perf) — dọn phiên chỉ dispose semaphore khi lấy được.** M ~2100/2111: `WaitAsync(200ms)` trả `false` → chỉ `TryRemove`, **không** `Dispose`. Chờ các semaphore song song (`Task.WhenAll`) thay vì tuần tự.
4. **M2 (perf) — `CleanupTurnResources` không dispose semaphore đang có thể được dùng.** Chỉ `TryRemove`, không `Dispose` (SemaphoreSlim không dùng `AvailableWaitHandle` thì không cần dispose). Mọi `WaitAsync` trên semaphore theo turn (M ~1100, ~1162, ~1273, ~1335, ~2178, ~2204) nằm trong try; `ObjectDisposedException`/`OperationCanceledException` → bỏ qua lần ghi đó, không lan ra handler.
5. **M1 (security) — rollback F4 sai.** `wasInitialHub` (M ~360/669/817) tính trước `await` nên hai Start song song đều tưởng mình mở đầu, và rollback của hub lỗi xóa owner/key/turn state/lastActivity của hub kia đang chạy. Sửa: rollback chỉ gỡ owner, key, turn state, `_lastActivityByConversation` khi **sau khi gỡ phần của chính mình** không còn bridge nào, không còn Start khác đang chạy (`IsConversationInitializing` không tính chính nó) và không còn connection hiện hành nào của conversation. Ngược lại chỉ gỡ mapping connection + bridge của chính lần Start đó. Thêm test: tutor Start thành công, transcribe Start song song lỗi → owner/key vẫn còn, audio tutor vẫn được chấp nhận, `StopAllForConversation` của tutor vẫn chạy.
6. **M2 (security) / M3 (perf) — bridge mồ côi và rollback thiếu.**
   - Bọc toàn bộ đoạn Start từ lúc đăng ký owner/key/connection (M ~410, ~712, ~860) tới khi bridge vào `_bridges` trong try/catch gọi rollback (bao gồm `GetOrInitTurnStateAsync`, `GetConversationWithHistoryAsync`, `BuildSystemInstructionAsync`).
   - Sau `ConnectAndSetupAsync`, trước/ngay khi gán `_bridges[bridgeKey]`: kiểm tra connection hiện hành của `(conversationId, hubType)` vẫn là `connectionId` này (và connection còn sống). Không khớp → gỡ bridge khỏi `_bridges` nếu đã gán, dispose bridge, rollback như mục 5, rồi `CheckCleanupIfNoBridgesRemainingAsync`. Thêm test: connection disconnect trong lúc bridge giả đang chặn connect (TCS) → sau khi connect xong, không còn bridge trong `_bridges`, bridge giả đã dispose, owner/key đã gỡ (nếu đó là hub duy nhất).
7. **L1 — key.** Chỉ ghi/ghi đè `_sessionApiKeys` sau khi connect thành công; hoặc lưu key cũ và khôi phục khi rollback.
8. **L2 — đếm "đang khởi tạo".** Chỉ `MarkConversationStartBegin` sau khi kiểm tra owner (RAM và DB) đã qua; `MarkConversationStartEnd` trong `finally` chỉ khi đã Begin.
9. **L3.** `GeminiLiveSessionBridge.DisposeAsync`: xóa `_apiKey` (gán `string.Empty`; nếu field `readonly` thì bỏ `readonly`).
10. **L4.** Repo (~385, ~396, ~430, ~440, ~526): các `catch (DbUpdateException)` retry upsert thêm `when (IsUniqueConstraintViolation(ex))`.
11. **L1 (perf) — turn đã final không tạo lại state.** `inputTranscription`/translation đến sau khi turn đã final (nằm trong `FinalTranscriptTurns`/`FinalTranslationTurns`) → bỏ qua sớm: không tạo lại entry dictionary, không broadcast non-final.
12. **Test race thật.** `FinalAlwaysWins_*` (T ~914): bảo đảm final **đang chờ** semaphore trước khi thả non-final (ví dụ chờ đến khi `semaphore.CurrentCount == 0` và final đã vào hàng, dùng TCS/hook nội bộ `internal`, không `Task.Delay` cố định). Test F3 (T ~1042): thay `Task.Delay(50)` bằng TCS báo "đã bắt đầu connect". Test không truy cập mạng.

## Lệnh kiểm chứng

Chỉ chạy `dotnet build` và `dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~ConversationOwnershipAndRealtimeLifecycleTests"`. Không chạy toàn bộ suite — orchestrator sẽ chạy.

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~ConversationOwnershipAndRealtimeLifecycleTests"
```

Không warning (hiện 0). Test không được truy cập mạng.

## Ràng buộc

Không chạy `git add`/`git stash`/`git reset`/`git checkout`. Không commit/push/merge, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–12>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test, số warning>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
