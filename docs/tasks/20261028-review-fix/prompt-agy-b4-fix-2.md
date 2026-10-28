# Việc giao cho AGY: B4 fix 2 — realtime: final luôn thắng, khởi tạo không bị dọn nhầm, thao tác theo connection (backend)

Các batch trước đã làm trong worktree này — giữ nguyên. Chỉ sửa các điểm dưới đây (từ lượt review lại B4 fix 1). Đọc `.agents/skills/realtime-session/SKILL.md`.

Ký hiệu: M = `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Realtime/GeminiLiveConnectionManager.cs`; Repo = `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs`.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Infrastructure/Realtime/**`, `src/EnglishLiveTutor.Infrastructure/Repositories/ConversationRepository.cs`, `src/EnglishLiveTutor.Infrastructure/Ai/FastTextTranslatorFallback.cs` và `GrammarCorrectionAnalyzerService.cs` (chỉ để cho phép inject `HttpClient`/handler trong test, không đổi hành vi), `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs`, `src/EnglishLiveTutor.Domain/Conversations/**`, `src/EnglishLiveTutor.Application/Features/Conversations/Ports/ILiveSessionService.cs`, `src/EnglishLiveTutor.Api/Hubs/**`, `tests/**`. Không migration, không đổi hub method/event client nhìn thấy.

## Yêu cầu

1. **F1 — bản dịch final luôn thắng.** Trong `ConversationTurnState` thêm tập `FinalTranslationTurns` (và tương tự `FinalTranscriptTurns`). Đánh dấu khi xử lý final. **Bên trong** semaphore theo turn, trước khi ghi DB: bỏ qua non-final nếu turn đã final. `PersistTranslationThrottledAsync` không tạo `PendingTranslation`/lịch ghi trễ mới khi turn đã final. Task ghi trễ kiểm tra lại cờ final sau khi lấy được semaphore.
2. **F2 — transcript tuần tự.** Ghi transcript cũng đi qua semaphore theo turn như translation (cùng cơ chế mục 1). Ngoài ra, ở Repo, cập nhật non-final dùng UPDATE có điều kiện `WHERE IsFinal = 0` (`ExecuteUpdateAsync`; nhánh InMemory tương đương như đã làm ở `IdentityRepositories`) để hai DbContext song song không hạ bản final.
3. **F3 — khởi tạo không bị dọn nhầm.** Trong lúc tutor (hoặc hub nào) đang Start (từ lúc gỡ bridge cũ tới khi bridge mới vào `_bridges`), đánh dấu conversation đang khởi tạo (đếm số Start đang chạy hoặc placeholder). `CheckCleanupIfNoBridgesRemainingAsync` không dọn conversation đang khởi tạo. Disconnect của hub phụ chỉ gỡ mapping/bridge của **chính connection đó**; chỉ dọn toàn bộ state khi không còn bridge **và** không còn Start đang chạy **và** không còn connection hiện hành nào.
4. **F4 — rollback đúng phạm vi.** Start lỗi chỉ rollback phần do chính lần Start đó đăng ký: không xóa owner/key khi hub khác của cùng conversation còn hoạt động; nếu là hub mở đầu và lỗi → gỡ owner, key, turn state, connection ngay.
5. **F5 — thao tác gắn connection.** Thêm `connectionId` vào `Forward*Audio`, `EndUserSpeech`, `EndTranslateSpeech`, `AnalyzeTurnGrammarManual`, `StopAllForConversation` của `ILiveSessionService` (hub truyền `Context.ConnectionId`). Manager chỉ chấp nhận khi connection đó là connection hiện hành của `(conversationId, hubType)` tương ứng (Stop/Analyze: connection tutor hiện hành). Connection cũ đã bị thay → audio bỏ qua im lặng, lệnh khác → `NotFoundException`.
6. **F6 — vòng đời semaphore/CTS.** Khi turn kết thúc bình thường (sau final đã ghi xong), gỡ và dispose semaphore, CTS debounce/throttle, và các entry dictionary theo turn đó. Khi dọn phiên: hủy CTS trước, chờ task đang giữ semaphore xong (có timeout ngắn) rồi mới dispose; sau khi dọn, không cho `GetOrAdd` tạo semaphore mới cho phiên đã dừng (kiểm tra owner/state trước).
7. **F7 — log.** Trong M, `GeminiLiveSessionBridge.cs`, `Api/Hubs/HubExceptionHelper.cs`: không truyền `ex` vào logger; log `ex.GetType().Name`, conversationId, hub type. (Giữ `ex` khi chắc chắn không phải đường có URL chứa key — nếu không chắc, bỏ.)
8. **F8.** Repo: chỉ chuyển `DbUpdateException` thành `NotFoundException` khi là lỗi trùng khóa chính/unique (kiểm tra `SqlException` number 2627/2601 qua `InnerException`, hoặc InMemory tương đương); lỗi khác ném tiếp.
9. **F9.** M (~dòng 499) `_ = translateBridge.SendRealtimeAudioAsync(...)`: quan sát exception (continuation log type) hoặc await trong try/catch. Rà toàn file không còn task `_ =` không được quan sát.
10. `FinalizeTurnAsync` chuyển `internal` nếu chỉ test gọi từ ngoài.
11. **Test (bắt buộc chứng minh hành vi, không gọi mạng thật):**
    - `FastTextTranslatorFallback` và `GrammarCorrectionAnalyzerService` trong test dùng `HttpClient` với handler giả (không request ra ngoài). Sửa test tên group hiện đang gọi mạng thật.
    - Race thật cho translation và transcript: dùng repository giả có độ trễ điều khiển được (`TaskCompletionSource`) để non-final chạy chậm và hoàn tất **sau** final → kết quả cuối là bản final; non-final đến sau final trong cửa sổ throttle không được ghi.
    - F3: Start tutor đang chờ connect (bridge giả chặn bằng `TaskCompletionSource`) + disconnect của transcribe cũ → state không bị dọn; sau khi connect xong, audio được chấp nhận.
    - F5: connection cũ bị thay không gửi được audio/Stop vào phiên mới.
    - F6: sau khi một turn final xong, dictionary theo turn không còn entry của turn đó.

## Lệnh kiểm chứng

**Chính sách test để tiết kiệm thời gian:** chỉ chạy `dotnet build` và `dotnet test EnglishLiveTutor.sln --no-build --filter "<tên lớp test mới hoặc liên quan>"` (có thể nhiều điều kiện nối bằng `|`). **Không** chạy toàn bộ suite nhiều lần — orchestrator sẽ chạy toàn bộ một lần khi nghiệm thu. Chạy toàn bộ suite tối đa một lần ở cuối nếu cần.

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

Không warning mới (hiện 2 warning có sẵn). Không phát sinh test fail mới ngoài 9 test fail sẵn trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`. Test không được truy cập mạng.

## Ràng buộc

Không chạy `git add`/`git stash`/`git reset`/`git checkout`. Không commit/push/merge, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–11>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test, số warning>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
