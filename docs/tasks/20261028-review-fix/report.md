# Report: Sửa toàn bộ review 2026-10-28

- Mã task: `20261028-review-fix` · Plan: [plan.md](plan.md) · Review: [2026-10-28](../../reviews/2026-10-28-security-performance-architecture.md)
- Worker: AGY (`gemini-3.8-flash-high` cho B1–B5, B4 fix, B7; `gemini-3.8-flash-medium` cho B6, B8, B9). Orchestrator: Claude Code (plan, prompt, kiểm chứng, review, một số bản sửa nhỏ).
- Trạng thái mã: đã đưa về working tree chính `D:/Projects/EnglishPartner` dưới dạng **thay đổi chưa commit** (patch từ worktree `ai/20261028-review-fix-agy`, tree cuối `25487fd`, cộng các bản sửa trực tiếp ghi ở mục 3). Chưa commit/push/merge.

## 1. Kết quả kiểm chứng (chạy trên working tree chính)

| Lệnh | Kết quả |
| --- | --- |
| `dotnet build EnglishLiveTutor.sln` | 0 lỗi; 2 warning có sẵn (`LessonAiGrader.cs` CS8604/CS8600), không warning mới |
| `dotnet test EnglishLiveTutor.sln` | 315 pass / 9 fail / 324. 9 fail đúng danh sách baseline (`TeacherVoicesCatalog_*`, 2× `ManualVocabularyGenerationTests`, 6× `PronunciationSynthesisAudioCacheTests` — EF InMemory không hỗ trợ `DataLength`). Baseline trước sửa: 185/194 |
| `npm run build` (FE) | pass |
| `npx vitest run` (FE) | 185/185 pass (baseline 158) |

**Chưa kiểm chứng** (không được phép/không có môi trường): SQL Server thật (migration, `ExecuteUpdateAsync`, unique index), SignalR thật và Redis backplane, microphone/audio trên trình duyệt, provider Gemini, reverse proxy/HTTPS. Test BE dùng EF InMemory.

## 2. Đã làm theo batch

| Batch | Mục review | Nội dung chính |
| --- | --- | --- |
| B1 | S1, S5, S6, S10, A6, P8 | Bỏ JWT secret/connection string/license/mật khẩu cứng; `JwtOptionsValidator` + `ValidateOnStart` (fail-fast ngoài Dev); HTTPS/HSTS, security headers, `AllowedHosts`, `ForwardedHeaders`; `EnableDetailedErrors` chỉ Dev; migrate/seed chỉ theo cấu hình, lỗi thì dừng khởi động |
| B2 | S7, S8, S11, S12, P5, cookie, ADMIN bypass | Rate limit auth (IP) và AI (user); refresh token lưu SHA-256, phát hiện tái sử dụng → thu hồi chuỗi; `access_token` query chỉ cho `/hubs`; bỏ xác thực 2 lần; cookie `Secure` luôn bật; thống nhất ADMIN bypass; `AllowAllPermissionChecker` chỉ còn trong test |
| B3 | S4, `ListLessonAttemptsQuery.UserId`, `default-learner` | Danh tính chỉ từ claims; bỏ tham số `userId`; bỏ fallback `default-learner` |
| B4 + fix 1–3 | S2, S3, P2, P7, S9 (server) | `Conversation.UserId` + 2 migration; owner check ở REST và 3 hub; group SignalR `{userId}:{conversationId}`; phiên theo connection; dọn phiên khi disconnect/idle; final luôn thắng non-final; không chặn vòng nhận audio; rollback đúng phạm vi; không còn bridge mồ côi giữ API key |
| B5 | ADR-0005, A4 | `GlobalExceptionHandler` + ProblemDetails có thêm `error` (tương thích FE); endpoint mỏng |
| B6 | P1, P3, P4, P6 | Trần phân trang (mặc định 20, tối đa 100, không phân trang tối đa 1000, `TotalCount` thật); worker poll 10 s cấu hình được; `LoggingBehavior` xuống Debug, không log `ex`; bỏ `ToLower()` trên cột đã chuẩn hóa |
| B7 | A2, A3, A5 | Bỏ đăng ký handler bằng reflection; tách 4 Application service thay cho handler gọi handler; test DI qua MediatR; Redis backplane bật theo `Redis:ConnectionString` |
| B8 | S9 (client), RequireAuth, S13, P1 (FE) | API key mặc định `sessionStorage` + tùy chọn ghi nhớ; `RequireAuth` chờ khởi tạo; bỏ offline mode, `demo-jwt-`, `FALLBACK_*`; phân trang server cho admin từ vựng/người dùng; bỏ gửi `userId`; `.gitignore` chặn `EnglishPartner.FE/.env` |
| B9 | P9 | `DailyVocabularyPage.tsx` 1.942 → ~345 dòng, tách 6 component + hook `usePronunciationStudio` |

## 3. Orchestrator tự sửa sau review (kèm test)

- B5: ánh xạ `BadHttpRequestException`, `JsonException`, `NotSupportedException` → 400.
- B4 fix 3: rollback chỉ gỡ bridge của chính lần Start (test `SupersededStart_RollbackAfterNewerStartSucceeded_KeepsNewerBridge`, đã xác nhận fail khi bỏ bản sửa); kiểm tra connection ngay trước khi công bố bridge; bỏ khôi phục key cũ; không dispose semaphore khi rollback; gỡ CTS grammar final theo tham chiếu.
- B6: 3 danh sách lesson/attempt/rubric giữ mặc định 20 dòng khi không phân trang (test `ListLessons_WithoutPaging_KeepsDefaultPageOf20`); clamp `limit` 1..100 cho lịch sử ôn tập và phát âm.
- B8: đăng xuất / refresh thất bại / 401 xóa API key ở cả `sessionStorage` và `localStorage` (test đã xác nhận fail khi bỏ bản sửa).
- B9: ổn định callback để `React.memo` có tác dụng; dialog sinh từ vựng giữ chế độ người dùng chọn lần trước khi có key.
- Review cuối: bỏ mật khẩu demo `Learner@123`/`Admin@123` khỏi FE (nút chọn tài khoản demo chỉ hiện ở build Dev và không điền mật khẩu); admin tạo user bắt buộc nhập mật khẩu; `Database:MigrateOnStartup=false` trong `appsettings.Development.json` để migration xóa hội thoại không tự chạy.
- Path traversal `FileLessonMediaStorage`: từ chối key tuyệt đối, so tiền tố với `root + separator` (5 test mới).

## 4. Thay đổi hành vi / contract cần biết

- Lỗi HTTP trả ProblemDetails (`title`, `status`, `detail`, `traceId`) **kèm** `error` = `detail`; status code giữ nguyên.
- Bỏ tham số `userId` ở endpoint lesson attempts và pronunciation/learning history; FE đã cập nhật.
- `pageSize` tối đa 100; request không phân trang bị cắt ở 1000 (header `X-Total-Count` vẫn là tổng thật). Lesson/attempt/rubric giữ mặc định 20.
- Hub từ chối `conversationId` không thuộc user (`HubException("Không tìm thấy hội thoại.")`); `conversationId` chỉ `[A-Za-z0-9_-]`, ≤ 64.
- `PUT /api/conversations/{id}/settings` không còn tự tạo hội thoại; không phải chủ → 404.
- Refresh token cũ (plaintext) mất hiệu lực → mọi người phải đăng nhập lại một lần.
- 429 khi vượt rate limit auth/AI.

## 5. Việc người dùng phải làm trước khi chạy/triển khai

1. **Backup DB rồi mới migrate thủ công**: `dotnet ef database update` áp `DeleteOwnerlessConversations` (xóa vĩnh viễn hội thoại không có chủ, ADR-0002, không hoàn tác) rồi `AddConversationUserId`. Dev đã tắt `MigrateOnStartup`; bật lại sau khi migrate nếu muốn.
2. **Cấu hình bắt buộc** (user-secrets ở Dev, biến môi trường/secret store ở production): `Jwt:SecretKey` (≥ 32 byte), `AdminUser:Password`, `DemoLearner:Password` (nếu muốn seed learner demo), connection string, MediatR license. Ví dụ Dev: `dotnet user-secrets set "Jwt:SecretKey" "<giá trị>" --project EnglishPartner.BE/src/EnglishLiveTutor.Api`.
3. **Production**: đặt `AllowedHosts` đúng domain (mặc định chỉ `localhost;127.0.0.1` → host khác bị 400); `ForwardedHeaders:KnownProxies`/`KnownNetworks` cho reverse proxy; nhiều instance → `Redis:ConnectionString` **và** sticky session cho `/hubs/*`; migrate là bước deploy riêng (`MigrateOnStartup=false` ngoài Dev).
4. **Rotate secret đã lộ trong lịch sử git**: MediatR license key, JWT secret cũ. **Đổi mật khẩu** `admin` / `default-learner` trên mọi DB đã seed trước đây (hash của `Admin@123`/`Learner@123` vẫn còn ở đó).
5. **Git**: `git rm --cached EnglishPartner.FE/.env` (file vẫn đang bị track dù `.gitignore` đã chặn) và gỡ `EnglishPartner.FE/node_modules` (~10.129 file đang bị track) bằng `git rm -r --cached EnglishPartner.FE/node_modules`.
6. Commit/merge sau khi tự review diff.

## 6. Chưa làm và lý do

| Hạng mục | Lý do |
| --- | --- |
| A1 (hai thế hệ code) — một phần | Đã chuyển Conversations sang owner check; còn nợ: Identity commands, `GenerateVocabularyCommandHandler`, `VocabularyGenerationWriter`, `AssessPronunciationCommandHandler`, `SynthesizePronunciationAudioCommandHandler` dùng `IAppDbContextFactory` tạo context riêng để ghi (A3 ghi nhận, chưa command nào dùng `ITransactionalCommand`) |
| Gộp 3 hub thành 1 (P9 phần WebSocket) | Đổi contract realtime lớn, cần ADR riêng |
| Bắt buộc đổi mật khẩu lần đầu | Tính năng mới (cột, endpoint, trang FE) |
| Full-Text Search (P4) | Cần cấu hình SQL Server |
| Tối ưu query list conversations / `GetConversationWithHistoryAsync` `Include` toàn bộ messages để lấy 16 lượt cuối | Có từ trước, ngoài phạm vi batch; nên làm tiếp (Luật 7) |
| Link hỏng `docs/prompts/*.md` trong `EnglishPartner.FE/.agents/design-system.md` (A7) | Nội dung gốc không còn; cần người dùng cho biết |
| Admin dashboard đếm bằng cách tải tới 1000 bản ghi | Nên gọi `paged=true&pageSize=1` và đọc `totalCount` |

## 7. Nợ kỹ thuật / Low còn lại (đã review, không chặn)

- `GlobalExceptionHandler` map mọi `InvalidOperationException`/`ArgumentException` → 400 kèm message gốc; nên có `BusinessRuleException` riêng.
- Test seam trong code production: `RegisterActiveSessionForTesting`/hook `internal` ở realtime, nhánh InMemory + lock tĩnh trong `ConversationRepository`, `GeminiLiveSessionBridge` không `sealed`, `setTestStorage` ở `tutorSessionStore`, `CreateAuthRateLimiter`/`GetAdminUserOptions` public.
- ~170 dòng class cấu hình ở cuối `Program.cs` nên tách sang `Api/Configuration/`.
- Realtime: hiếm khi client nhận một transcript non-final sau final (cửa sổ rất hẹp); grammar final có thể gọi AI 2 lần cho một lượt; bản dịch không có cột `IsFinal` nên chỉ được bảo vệ trong một process.
- `useThreeHubLiveSession.ts` gọi Google với `?key=` trên URL (có từ trước) — nên chuyển sang header `x-goog-api-key`.
- `AdminVocabulariesPage`: đổi bộ lọc CEFR không quay về trang 1. `vocabulary/index.ts` export thêm component nội bộ.
- `ListUsers`/`ListLessons` sắp theo `CreatedAtUtc` không có tie-breaker.

## 8. Hồ sơ

- Prompt từng batch: `prompt-agy-b*.md`; snapshot tree sau mỗi batch: [snapshots.txt](snapshots.txt); log AGY và diff: `runs/` (bị `.gitignore`).
- Review độc lập đã chạy: security (B1–B4, B4 fix 2, B8), performance (B4, B4 fix 2, B6, B9), review kết hợp B4 fix 3, code review cuối toàn bộ diff.
