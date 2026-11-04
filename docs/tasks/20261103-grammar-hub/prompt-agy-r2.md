# Việc giao cho AGY: grammar hub, đợt 2 (sửa theo security review và performance review)

## Bối cảnh

- Đợt 1 đã xong. Code review kết luận **ĐẠT CÓ ĐIỀU KIỆN**. Chỉ sửa các mục dưới đây.
- Plan: `docs/tasks/20261103-grammar-hub/plan.md`. Luật và skill giống `prompt-agy.md`.
- Làm trên `development`, **không commit**. Không có migration.
- Đang có một BE tạm chạy ở `localhost:5000`. Không dừng nó. Nếu file DLL bị khóa thì build/test với `-p:ArtifactsPath=<thư mục tạm>`.
- **Không** thêm đường dẫn tuyệt đối của máy (ví dụ `D:\Projects\...`) vào code hay test.

## Cần sửa

### BE

1. **(Điều kiện security T1)** Thêm test chạy trên `GeminiLiveConnectionManager` **thật**, theo mẫu trong `tests/Modules/LiveTutor/.../ConversationOwnershipAndRealtimeLifecycleTests.cs`:
   - (a) user B gọi `StartGrammarHubSessionAsync` với conversation của A **có trong DB**: ném `NotFoundException` và không đăng ký connection nào;
   - (b) như (a), nhưng conversation chỉ có trong bộ nhớ (dùng `RegisterActiveSessionForTesting` hoặc helper tương đương);
   - (c) conversation đã xóa mềm: 404;
   - (d) user B gọi `AnalyzeTurnGrammarManualAsync` qua một connection Grammar: 404;
   - (e) grammar connection ngắt kết nối khi phiên Tutor đang chạy: phiên Tutor vẫn còn hoạt động;
   - (f) notifier nhận `GetGroupTarget` thật và gửi tới `grammar:{owner}:{conv}`.
2. **Rate limiter (security L1/L2, performance L1/L2):**
   - Thay phần cài đặt tự viết bằng `System.Threading.RateLimiting.PartitionedRateLimiter` với `SlidingWindowRateLimiter` (20 lần mỗi 1 phút, phân vùng theo `userId`). Thư viện này có sẵn trong .NET và tự dọn các partition không còn dùng. Có thể giữ interface `IGrammarHubRateLimiter`.
   - Trong constructor của `GrammarCorrectionHub`, `IGrammarHubRateLimiter` phải là **tham số bắt buộc** (bỏ fallback `new`).
   - Sửa test cho phù hợp.
3. **(Security L3)** Áp thêm rate limit cho `InitializeGrammarSession`, phân vùng riêng, ví dụ 10 lần/phút/user, vì method này có thể ghi DB. Giữ nguyên hành vi kiểm owner.
4. **(Performance M2, phía server)** Trong `AnalyzeTurnGrammarManualAsync`: nếu turn đó **đang được phân tích**, hoặc **đã được phân tích với cùng nội dung** (dùng `LastAnalyzedTextByTurn` hoặc cơ chế sẵn có), thì bỏ qua và không gọi Gemini lại. Thêm test cho trường hợp này.
5. **(Performance L5)** Truyền `Context.ConnectionAborted` từ hub vào lời gọi phân tích thủ công, để khi connection rớt thì lời gọi Gemini bị hủy. Nếu cần, mở rộng chữ ký method trong `ILiveSessionService` bằng tham số `CancellationToken ct = default`.
6. `tests/EnglishLiveTutor.Api.IntegrationTests/ApiContractSnapshotTests.cs`: **xóa đường dẫn tuyệt đối** `D:\Projects\...` mà đợt 1 đã thêm. Dùng cách tìm thư mục snapshot như trước đây (theo `AppContext.BaseDirectory`, đi ngược lên thư mục cha). Không đổi file snapshot `endpoint-inventory.txt`, trừ các dòng của `/hubs/grammar`.

### FE (`src/features/tutor/hooks/useThreeHubLiveSession.ts`)

7. **(Performance M1)** Grammar connection:
   - `onreconnected`: gọi lại `InitializeGrammarSession` với conversation hiện tại;
   - `onreconnecting` và `onclose`: cập nhật `hubStates.grammarHub` (ví dụ `reconnecting`/`disconnected`).
   Làm tương tự cho 3 hub cũ nếu chỉ cần thay đổi ít và an toàn (gọi lại `Initialize*Session` tương ứng). Nếu không an toàn thì ghi lại trong report.
8. **(Performance M2, phía client)** Server đã tự phân tích sau mỗi lượt nói, nên **bỏ việc FE tự gọi `AnalyzeTurnGrammar`** sau 1250 ms và 450 ms khi grammar hub đang `connected`.
   - Khi grammar hub **không** connected, giữ phương án dự phòng phía client như cũ.
   - Nút "Phân tích lại" do người dùng bấm vẫn invoke grammar hub và vẫn giữ phương án dự phòng 1,6 giây.
9. **(Performance L4)** Sau khi tutor hub khởi tạo xong, khởi tạo 3 hub phụ (transcribe, translate, grammar) **song song** bằng `Promise.all`. Phải giữ cách xử lý lỗi hiện có.
10. Cập nhật hoặc thêm test Vitest:
    - grammar `onreconnected` gọi lại `InitializeGrammarSession`;
    - hub connected thì không có lời gọi tự động;
    - hub disconnected thì dùng phương án dự phòng.

## Kiểm chứng

- BE: `dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx` (thêm `-p:ArtifactsPath=...` nếu bị khóa file). Kết quả phải pass toàn bộ.
- FE: `npm run build`, `npx vitest run --maxWorkers=2`.

## Report

Ghi vào `docs/tasks/20261103-grammar-hub/runs/agy-r2-report.md`:
- mỗi mục đã sửa ra sao;
- các lệnh đã chạy và kết quả;
- những điểm chưa làm.
