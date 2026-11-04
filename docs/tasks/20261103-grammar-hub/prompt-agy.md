# Việc giao cho AGY: tách hub `/hubs/grammar` cho phần sửa ngữ pháp realtime (BE + FE)

## Bối cảnh

- **Người dùng đã duyệt** `docs/tasks/20261103-grammar-hub/plan.md`. Đọc hết plan. Đây là phạm vi duy nhất được làm.
- Luật: `AGENTS.md` (luật 1, 2, 5, 9, 10, 11, 13), `EnglishPartner.BE/AGENTS.md`, `EnglishPartner.FE/AGENTS.md`, `docs/adr/0004-redis-scale-out.md`.
- Skill:
  - `.agents/skills/realtime-session/SKILL.md`
  - `.agents/skills/api-contract-sync/SKILL.md`
  - `.agents/skills/security-audit/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/api-realtime/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Làm trên `development`, **không commit**. Không cài package. Không có migration.
- Không chạy `dotnet ef database update` và không kết nối SQL Server ngoài lệnh test. Hiện có một BE tạm đang chạy ở `localhost:5000`; **không dừng, không khởi động lại nó**.
- Khi build BE, nếu file DLL bị khóa vì BE tạm đang chạy, build ra thư mục khác. Ví dụ: `dotnet build EnglishPartner.BE/EnglishLiveTutor.slnx -p:BaseOutputPath=$TEMP/agy-hub-build/`. Có thể dùng cách tương đương khác.

## Hiện trạng (đọc code để xác nhận)

- `EnglishPartner.BE/src/Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Presentation/Hubs/TutorConversationHub.cs`: method `AnalyzeTurnGrammar` (dòng 78).
- `.../Presentation/Hubs/TranslateLiveHub.cs`: mẫu hub phụ. `InitializeTranslateSession` kiểm owner rồi `Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}:{normalizedConvId}")`.
- `.../Presentation/Realtime/SignalRLiveSessionRealtimeNotifier.cs:63`: `SendLanguageCorrectionAsync` gửi `ReceiveLanguageCorrection` qua `_tutorHub`.
- `.../Infrastructure/Realtime/GeminiLiveConnectionManager.cs`:
  - `AnalyzeTurnGrammarManualAsync` (có `AssertSessionOwner`);
  - phân tích tự động ở khoảng dòng 1797, 1880 và 1961;
  - `GetGroupTarget`.
- `EnglishPartner.BE/src/EnglishLiveTutor.Api/Program.cs:230-232`: `MapHub`.
- FE:
  - `EnglishPartner.FE/src/features/tutor/hooks/useThreeHubLiveSession.ts`: 3 connection, `analyzeTurnGrammar` (khoảng dòng 340–400), đăng ký `ReceiveLanguageCorrection`;
  - `src/features/tutor/components/TutorSessionHeader.tsx`: trạng thái hub;
  - `src/features/tutor/pages/LiveTutorPage.tsx`.

## Yêu cầu

### BE

1. Tạo `GrammarCorrectionHub` trong `.../Presentation/Hubs/` theo mẫu `TranslateLiveHub`:
   - `[Authorize]`, `userId` lấy từ claims;
   - xử lý lỗi theo cùng mẫu wrapper và log của hub phụ.
2. Method `InitializeGrammarSession(StartSubHubSessionRequest request)`:
   - kiểm owner của conversation/session giống các hub phụ;
   - join group riêng `grammar:{userId}:{conversationId}`;
   - không phải owner thì xử lý giống hub phụ (không join, báo lỗi hoặc NotFound theo mẫu hiện có).
3. Method `AnalyzeTurnGrammar(string conversationId, string turnId, string transcriptText, string cefrLevel)`:
   - chuyển từ tutor hub sang, gọi `_manager.AnalyzeTurnGrammarManualAsync` như cũ;
   - thêm kiểm tra đầu vào: `transcriptText` tối đa 2000 ký tự, `turnId` tối đa 100 ký tự, `cefrLevel` thuộc A1..C2 (không hợp lệ thì từ chối);
   - thêm giới hạn tần suất theo user, ví dụ 20 lần/phút. Hub method không dùng được rate limiter của ASP.NET, nên tự làm bằng `IMemoryCache`/token bucket, hoặc dùng cơ chế tương đương đã có trong repo nếu có.
4. **Xóa** `AnalyzeTurnGrammar` khỏi `TutorConversationHub`.
5. `SignalRLiveSessionRealtimeNotifier.SendLanguageCorrectionAsync`:
   - dùng `IHubContext<GrammarCorrectionHub>`, gửi tới group `grammar:` + target hiện tại;
   - **không** gửi qua tutor hub nữa;
   - interface `ILiveSessionRealtimeNotifier` giữ nguyên.
6. `Program.cs`: `app.MapHub<GrammarCorrectionHub>("/hubs/grammar").RequireAuthorization();`. Cấu hình CORS và JWT qua query string (`access_token`) của hub hiện có phải áp dụng cho route mới. Kiểm đoạn cấu hình JWT `OnMessageReceived`; nếu nó lọc theo đường dẫn `/hubs` thì route mới tự được áp dụng.
7. Test BE trong project test phù hợp (xem `tests/Modules/LiveTutor/...` và `tests/EnglishLiveTutor.Api.IntegrationTests`):
   - `InitializeGrammarSession`: owner join được; user B gọi với conversation của user A thì không join (test chéo user);
   - `AnalyzeTurnGrammar` từ chối khi không phải owner; từ chối input quá dài hoặc CEFR sai; rate limit chặn sau ngưỡng;
   - notifier gửi `ReceiveLanguageCorrection` qua grammar hub, đúng group, không gửi qua tutor hub;
   - `TutorConversationHub` không còn method `AnalyzeTurnGrammar` (test bằng reflection);
   - toàn bộ test hiện có pass, gồm cả architecture tests.

### FE

1. `useThreeHubLiveSession.ts`: thêm connection thứ 4 tới `/hubs/grammar`, theo cùng cách tạo connection, token factory, reconnect và dọn dẹp như 3 hub hiện có.
   - Khi phiên bắt đầu: `start`, sau đó `invoke('InitializeGrammarSession', …)` với payload giống các hub phụ.
   - `on('ReceiveLanguageCorrection', …)` chuyển sang connection grammar. Xử lý kết quả giữ nguyên.
   - `analyzeTurnGrammar` gọi `invoke('AnalyzeTurnGrammar', …)` trên grammar hub. Giữ nguyên phương án dự phòng 1,6 giây ở client.
   - Dừng phiên thì stop connection grammar.
   - Xuất trạng thái kết nối của grammar hub, cùng kiểu với 3 hub kia.
2. `TutorSessionHeader` (và nơi đang hiện "Hub 1/2/3"): thêm "Hub 4: grammar".
3. Copy trong trang Tutor đang nhắc "3 SignalR Hubs": sửa thành 4 hub, kèm Hub 4 (sửa ngữ pháp & từ vựng realtime), cả vi và en. Tìm theo `SignalR Hubs`.
4. Contract: cập nhật `src/shared/api/openapi.types.ts`, hoặc nơi đang khai báo tên method/event của hub (`rg "ReceiveLanguageCorrection|AnalyzeTurnGrammar" EnglishPartner.FE/src`).
5. Test Vitest: mock `@microsoft/signalr` hoặc dùng cách mock hub hiện có trong repo. Kiểm tra:
   - grammar hub được khởi tạo và gọi `InitializeGrammarSession`;
   - `analyzeTurnGrammar` invoke trên grammar hub, không phải tutor hub;
   - `ReceiveLanguageCorrection` từ grammar hub được đưa vào state.

## Kiểm chứng (ghi kết quả vào report)

- BE: build và `dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx` (có thể cần đổi output dir như ghi ở trên).
- FE: `npm run build` và `npx vitest run --maxWorkers=2` trong `EnglishPartner.FE`.
- Chưa kiểm chứng được phiên Gemini Live thật, vì cần API key của người dùng. Ghi điều này vào report.

## Report

Ghi vào `docs/tasks/20261103-grammar-hub/runs/agy-report.md`:
- danh sách file đã sửa;
- contract trước và sau;
- các test đã thêm;
- các lệnh đã chạy và kết quả;
- những điểm chưa làm được.
