# Việc giao cho AGY: B3 fix 1 — test endpoint thật, owner trong truy vấn, 404 thay 403 (backend)

Các batch trước (B1–B4 và các fix) đã làm trong worktree này — giữ nguyên mọi thay đổi khác. Chỉ làm đúng các điểm dưới đây (từ security review B3).

## Phạm vi

Được sửa: `EnglishPartner.BE/tests/**`, `EnglishPartner.BE/src/EnglishLiveTutor.Api/Endpoints/LessonAttemptEndpoints.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Api/Endpoints/VocabularyGamesEndpoints.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Api/Program.cs` (chỉ thêm `public partial class Program { }` nếu cần cho test), `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Lessons/**`, `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/VocabularyGames/**`, `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Learning/**`, `EnglishPartner.BE/src/EnglishLiveTutor.Application/Features/Vocabulary/VocabularyProjectionHelper.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Infrastructure/Repositories/FallingWordGameRepository.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Domain/VocabularyGames/Repositories/IFallingWordGameRepository.cs`, `EnglishPartner.BE/src/EnglishLiveTutor.Domain/Lessons/Repositories/**`.

Được thêm **duy nhất** package `Microsoft.AspNetCore.Mvc.Testing` vào project test (phiên bản khớp major ASP.NET Core/EF đang dùng — 10.x). Không tạo migration. Không đổi route hay JSON thành công.

## Yêu cầu

1. **M1 — hạ tầng test endpoint.** Tạo `tests/EnglishLiveTutor.Domain.Tests/Infrastructure/ApiTestFactory.cs` dựa trên `WebApplicationFactory<Program>`:
   - Môi trường `Testing`; cấu hình in-memory: `Jwt:SecretKey` (chuỗi test ≥ 32 byte), `Database:MigrateOnStartup=false`, `Database:SeedOnStartup=false`, `ConnectionStrings:DefaultConnection` giả, `AllowedHosts=*`, rate limit đủ cao để không cản test.
   - Thay `IDbContextFactory<AppDbContext>`/`AppDbContext` bằng EF InMemory (tên DB riêng mỗi factory).
   - Helper tạo access token thật cho một user (dùng `IJwtTokenService` của host) và `HttpClient` có header `Authorization: Bearer`.
   - Nếu host không khởi động được vì một service cần cấu hình, thêm cấu hình test thay vì sửa code production.
2. **M1 — test S4 ở tầng HTTP.** Seed dữ liệu cho user A và B. User B gọi `GET /api/pronunciation/assessments/history?userId=<A>` và `GET /api/vocabularies/{id}/flashcard?userId=<A>` → chỉ thấy dữ liệu của B (hoặc rỗng), không bao giờ thấy của A. Không token → 401.
3. **L1, L2 — owner trong truy vấn, trả 404.** Các handler lesson attempt (Resume, Complete, Submit answer, Retry grading, Request dictation AI review, Synthesize exercise audio) và falling word (mọi command/query theo `SessionId`): đưa điều kiện `UserId == currentUser` vào câu truy vấn/repository (ví dụ `GetByIdForUserAsync`); không phải chủ → `NotFoundException`. Endpoint trả 404 (bỏ `Results.Forbid()` cho trường hợp không phải chủ; vẫn 401 khi không có user).
4. **L3.** `Learning/Commands/SubmitVocabularyReview/SubmitVocabularyReviewCommandHandler.cs` (~dòng 55, nhánh idempotency): load `DailyVocabularyItems` phải join `DailyVocabularyPlans` với `p.UserId == userId`.
5. **L4.** `VocabularyProjectionHelper.BuildDailyPlanResponseAsync`: truy vấn plan thêm `p.UserId == userId` (thêm tham số nếu cần, cập nhật nơi gọi).
6. **Test.** Mục 2; thêm test HTTP: user B gọi `GET /api/lesson-attempts/{id của A}` → 404; user B gửi lệnh cho falling word session của A → 404. Test cho L3 (B dùng `ClientRequestId` của mình kèm item của A → không lộ trạng thái item của A).

## Lệnh kiểm chứng

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

Không phát sinh test fail mới ngoài 9 test fail sẵn trong `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`.

## Ràng buộc

Không chạy `git add`/`git stash`/`git reset`/`git checkout`. Không commit/push/merge. Chỉ thêm đúng package nêu trên. Không chạy API thật, SQL Server hay Gemini. Không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa/tạo: <danh sách>
- Đã làm: <yêu cầu 1–6; danh sách handler đã đổi sang owner-trong-truy-vấn>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
