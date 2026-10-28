# Review EnglishPartner: Bảo mật, Performance, Kiến trúc

- Ngày: 2026-10-28
- Phạm vi: `EnglishPartner.BE` (.NET 10, EnglishLiveTutor.*) và `EnglishPartner.FE` (React 19 + Vite)
- Phương pháp: đọc source tĩnh. **Không** sửa code, không build/test, không chạy API/SQL/SignalR/provider AI. Mọi kết luận cần được xác nhận lại khi sửa.
- Trạng thái: **Chưa xử lý**. Đánh dấu `[x]` khi một mục đã được sửa và kiểm chứng.

## Tóm tắt

Nền tảng kiến trúc tốt (Clean Architecture, feature-based CQRS, pipeline behaviors, idempotency). Code mới (Lessons, Media, VocabularyGames) kiểm tra quyền sở hữu đúng. Code cũ (Conversations, Pronunciation history, Flashcard) có lỗ hổng phân quyền nghiêm trọng; cấu hình bảo mật chưa sẵn sàng cho production.

---

## 1. Bảo mật

### 🔴 Nghiêm trọng — sửa trước khi deploy

- [ ] **S1. JWT secret có giá trị mặc định cứng trong code**
  - Vị trí: `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Identity/SecurityServices.cs:75-76`
  - Kích hoạt: thiếu `Jwt:SecretKey` (hiện `appsettings.json` không có key này) → service lặng lẽ dùng chuỗi mặc định.
  - Hậu quả: ai đọc được repo đều tự ký JWT với `roles: ["ADMIN"]` → chiếm toàn quyền.
  - Đề xuất: bắt buộc cấu hình, fail-fast khi khởi động nếu thiếu hoặc < 32 byte; lấy từ user-secrets/biến môi trường.

- [ ] **S2. Hội thoại không gắn với người dùng**
  - Vị trí: `Domain/Conversations/ConversationEntity.cs` (không có `UserId`), `Api/Endpoints/ConversationEndpoints.cs`, `Infrastructure/Repositories/ConversationRepository.cs:41`
  - Hậu quả: mọi user đã đăng nhập đọc/sửa/xóa được hội thoại và transcript của tất cả người khác.
  - Đề xuất: thêm `UserId` (migration + backfill), lọc mọi query/command theo user từ claims.

- [ ] **S3. SignalR hub không kiểm tra quyền sở hữu `conversationId`**
  - Vị trí: `Api/Hubs/TutorConversationHub.cs:15`, tương tự `TranscribeLiveHub`, `TranslateLiveHub`
  - Hậu quả: biết ID hội thoại là join được group, nghe lén stream audio/transcript, hoặc `StopSession` phá phiên người khác.
  - Đề xuất: kiểm tra owner trong mọi hub method, gắn connection ↔ user.

- [ ] **S4. IDOR qua tham số `userId` trên query string**
  - Vị trí: `Api/Endpoints/PronunciationEndpoints.cs:191`, `Api/Endpoints/VocabularyEndpoints.cs:57`
  - Ví dụ: `GET /api/pronunciations/assessments/history?userId=<id khác>` trả lịch sử của người khác.
  - Đề xuất: bỏ tham số `userId`, chỉ dùng `ICurrentUser.UserId`.

- [ ] **S5. Secret commit vào git**
  - Vị trí: `Api/appsettings.json` (`MediatR:LicenseKey`)
  - Đề xuất: chuyển sang user-secrets/biến môi trường; cân nhắc rotate license.

- [ ] **S6. Tài khoản seed dùng mật khẩu mặc định cứng**
  - Vị trí: `Infrastructure/Persistence/DatabaseSeeder.cs:1138,1148`. `AdminUserSeedOptions` tồn tại nhưng seeder không dùng.
  - Đề xuất: seed lấy mật khẩu từ config; không seed learner demo ở production; bắt buộc đổi mật khẩu lần đầu.

### 🟠 Cao

- [ ] **S7. Không có rate limiting** — brute-force `/api/auth/login`; spam endpoint AI (generate/assess/roleplay) gây tốn chi phí. Đề xuất: `AddRateLimiter` theo IP cho auth, theo user cho AI.
- [ ] **S8. Refresh token lưu plaintext, 30 ngày, không phát hiện tái sử dụng** — `Domain/Identity/Entities/RefreshTokenEntity.cs`, `RefreshTokenCommandHandler.cs`. Đề xuất: lưu SHA-256 hash; token cũ bị dùng lại → thu hồi cả chuỗi.
- [ ] **S9. Google API key của học viên trong `localStorage`**, gửi qua mọi request/hub, giữ trong RAM server (`_sessionApiKeys`) — `FE features/tutor/store/tutorSessionStore.ts:16,167`, `Infrastructure/Realtime/GeminiLiveConnectionManager.cs:47`. Một lỗ XSS là mất key. Cần quyết định chiến lược key (ADR).
- [ ] **S10. Thiếu hardening HTTP** — không `UseHttpsRedirection`/`UseHsts`, không security headers, `AllowedHosts: "*"`, `SignalR EnableDetailedErrors = true` vô điều kiện (`Api/Program.cs:47`).
- [ ] **S11. `access_token` qua query string cho mọi route** — `Api/Security/JwtAndCsrfMiddleware.cs:32`, `EltAuthenticationHandler.cs:40`. Chỉ chấp nhận cho path `/hubs`.
- [ ] **S12. `/register` công khai** — chưa xác minh kỹ handler: kiểm tra role mặc định được gán, cân nhắc chống bot.

### 🟡 Trung bình

- [ ] Cookie refresh `Secure = Request.IsHttps` → sai sau reverse proxy TLS. Luôn `Secure = true` + `ForwardedHeaders`.
- [ ] Không có global exception handler/ProblemDetails; endpoint trả `ex.Message` cho client.
- [ ] Logic ADMIN bypass lặp và lệch nhau: `Api/Security/PermissionAuthorizationExtensions.cs` xét claim `"roles"`, `Api/Services/ClaimsPermissionChecker.cs` thì không.
- [ ] `AllowAllPermissionChecker` chỉ bị vô hiệu nhờ thứ tự DI (`Infrastructure/DependencyInjection.cs:41`). Xóa khỏi DI production.
- [ ] `ListLessonAttemptsQuery` nhận `UserId` tùy ý (`Application/Features/Lessons/Queries/AttemptQueries/AttemptQueries.cs:206`) — chưa khai thác được nhưng là bẫy.
- [ ] Path traversal guard dùng `Replace("..","")` (`Infrastructure/Storage/FileLessonMediaStorage.cs:146`); nên so với `_storageRoot + Path.DirectorySeparatorChar`.
- [ ] FE `RequireAuth` không chờ `isInitialized`; khi refresh lỗi nhưng còn hint `sessionStorage` thì UI vẫn coi là đã đăng nhập (`features/auth/hooks/useAuth.ts`, `features/auth/components/RequireAuth.tsx`).
- [ ] Fallback `"default-learner"` trong Domain entity và Query contract (`UserVocabularyProgressEntity`, `GetVocabularyFlashcardQuery`, …).
- [ ] **S13.** `EnglishPartner.FE/.env` đang được git track (hiện chỉ chứa `VITE_BACKEND_URL`, không nhạy cảm) và `.gitignore` không chặn `.env`. Thêm bất kỳ biến bí mật nào vào đây sẽ bị commit. Đề xuất: bỏ track `.env`, chỉ giữ `.env.example`, thêm `.env`/`.env.*.local` vào `.gitignore`. Lưu ý: mọi biến `VITE_*` đều lộ trong bundle, không bao giờ đặt secret ở FE.

### Điểm tốt

PBKDF2 100k vòng + so sánh fixed-time; access token chỉ trong bộ nhớ; CSRF header cho cookie endpoint; upload có whitelist MIME/extension, giới hạn dung lượng, tên file do server sinh; Lesson attempt và media kiểm tra owner; FE không dùng `dangerouslySetInnerHTML`.

---

## 2. Performance

- [ ] **P1. Danh sách không phân trang khi thiếu `page`/`pageSize`** → trả toàn bộ bảng (`Application/Common/Models/PagedResultDto.cs:37-43`, `ListVocabularyFlashcardsQueryHandler.cs:58-64`, `ConversationRepository.cs:93`). Luôn áp page size mặc định + giới hạn tối đa.
- [ ] **P2. `ConversationRepository` Singleton + một `SemaphoreSlim` toàn cục** cho mọi ghi của mọi user; mỗi chunk transcript mở DbContext và `SaveChanges` (`ConversationRepository.cs:16`, `DependencyInjection.cs:71`). Bỏ lock toàn cục, dựa vào unique index, debounce/batch, chỉ ghi khi `isFinal`.
- [ ] **P3. Worker poll DB mỗi 2 giây** + `LoggingBehavior` ghi 2 dòng Information mỗi lần (~86k dòng/ngày khi rảnh) — `Infrastructure/VocabularyGames/FallingWordRoundExpirationWorker.cs:18`.
- [ ] **P4. Tìm kiếm `ToLower().Contains()`** (`ConversationRepository.cs:46`, `ListUsersQueryHandler.cs:26`, `ListVocabularyFlashcardsQueryHandler.cs:28`). *Đính chính 2026-10-28:* `Contains` sinh `LIKE '%x%'` nên vốn đã không dùng được index seek; `ToLower()` chỉ thêm chút CPU mỗi dòng, tác động thấp. Sửa được ngay: bỏ `ToLower()` trên cột đã chuẩn hóa (`NormalizedWord`, `NormalizedEmail`) và so sánh bằng. Giải pháp thật cho tìm kiếm trên bảng lớn là Full-Text Search hoặc tìm theo tiền tố — cần cấu hình SQL Server, ngoài phạm vi sửa code.
- [ ] **P5. Token được xác thực 2 lần/request** (middleware + handler); login/refresh `Include(RefreshTokens)` toàn bộ, bảng tăng mãi, không có job dọn (`Infrastructure/Repositories/IdentityRepositories.cs:34,44`).
- [ ] **P6. Include sâu 6 nhánh cho mỗi trang flashcard** (`ListVocabularyFlashcardsQueryHandler.cs:67-76`). Danh sách chỉ project field cần.
- [ ] **P7. State live session trong RAM một process** (`_bridges`, `_turnStates`, `_sessionApiKeys`), không dọn khi client rớt kết nối (`GeminiLiveConnectionManager.cs:46-48`). Cần `OnDisconnectedAsync` + TTL.
- [ ] **P8. `MigrateAsync()` + seed mỗi lần khởi động, lỗi bị nuốt** (`Api/Program.cs:62-70`). Tách thành bước deploy.
- [ ] **P9. FE component quá lớn** (`features/vocabulary/pages/DailyVocabularyPage.tsx` 1.942 dòng, 27 `useState`, 0 `useMemo`); mỗi phiên tutor mở 3 WebSocket hub.

### Điểm tốt

Index khá đầy đủ; `AsNoTracking`, `SplitQuery`, batch role/permission (tránh N+1); HttpClient retry/backoff tôn trọng `Retry-After`; FE lazy-load route, `staleTime`, refresh token single-flight.

---

## 3. Kiến trúc

- [ ] **A1. Hai thế hệ code cùng tồn tại.** Mới: `IAppDbContext` + UnitOfWork + kiểm tra owner. Cũ: `IDbContextFactory` mỗi method, repository Singleton, không owner, không transaction. Đây là gốc của S2, S3, S4, P2.
- [ ] **A2. CQRS dùng MediatR 14.2 nhưng còn lối tắt bỏ qua pipeline.** `ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler` (`Application/Common/Interfaces/ICqrs.cs`) là lớp bọc mỏng trên `IRequest`/`IRequestHandler` — hợp lý. Vấn đề: `Application/DependencyInjection.cs:47-65` đăng ký thêm handler như service để inject trực tiếp, nên handler gọi handler khác (vd `StartLessonAttemptCommandHandler` → `_resumeHandler`) bỏ qua Authorization/Validation/Transaction behaviors; 5 file vẫn dùng `IRequestHandler` trực tiếp thay vì interface bọc. `AGENTS.md` BE còn ghi "không thêm mediator" — lệch với thực tế. Quyết định: giữ MediatR (ADR-001).
- [ ] **A3. Transaction không nhất quán:** có `TransactionBehavior` nhưng nhiều handler tự commit hoặc dùng repository `SaveChangesAsync` riêng.
- [ ] **A4. Business rule ở tầng API:** endpoint tự map exception → HTTP, resolve `userId` trong endpoint, hub gọi thẳng Infrastructure manager.
- [ ] **A5. Realtime không scale ngang:** state trong RAM, không SignalR backplane.
- [ ] **A6. Cấu hình:** không có `appsettings.Production.json`; connection string/secret fallback cứng; options không `ValidateOnStart` (trừ vocabulary).
- [x] **A7. Tài liệu agent lệch thực tế:** `EnglishPartner.BE/AGENTS.md` và 5 skill BE trỏ tới `docs/backend-architecture-review-2026-10-15.md` và `docs/backend-refactor-plan.md` không tồn tại. *Đã sửa 2026-10-28: trỏ về file review này và `docs/adr/`.* Còn lại: `EnglishPartner.FE/.agents/design-system.md` trỏ tới `docs/prompts/*.md` không tồn tại — nội dung gốc đã mất, cần người dùng cho biết còn bản nào không.

### Điểm tốt

Layering rõ; feature-based CQRS nhất quán; pipeline behaviors (Logging, Authorization, Validation, Performance, Transaction); idempotency service có fingerprint + lease; aggregate có invariant; FE chia theo feature với ranh giới `shared/app/features` rõ.

---

## 4. Thứ tự sửa đề xuất

1. **Ngay lập tức:** S1, S5, S6, S4.
2. **Sprint tới:** S2 + S3 (migration `UserId` cho Conversation, lọc theo user, owner check trong hub), S7, S8, P1.
3. **Tiếp theo:** global exception handler + ProblemDetails, security headers/HSTS, refactor `ConversationRepository` (P2, P7), chốt một cơ chế CQRS (A2), giảm log/worker (P3).
