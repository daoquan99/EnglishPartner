# Việc giao cho AGY: B6 — giới hạn phân trang, worker nền, log, tìm kiếm (backend)

## Mục tiêu

Không endpoint danh sách nào trả số dòng không giới hạn; worker nền và log không tạo tải thừa. Liên quan review P1, P3, P4 (đọc phần đính chính), P6. Batch B1–B5 đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, Luật 6–8), `EnglishPartner.BE/AGENTS.md`
- Review mục P1, P3, P4, P6: `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`
- Skill: `EnglishPartner.BE/.agents/skills/ef-performance/SKILL.md`, `.agents/skills/api-contract-sync/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
- Code: `src/EnglishLiveTutor.Application/Common/Models/PagedResultDto.cs` (`CreateAsync`, `CreateProjectedAsync`, `FromPage` trả toàn bộ khi không có `page`/`pageSize`; clamp tối đa 500), `src/EnglishLiveTutor.Api/Endpoints/PaginationEndpointExtensions.cs`, `Application/Features/Vocabulary/Queries/ListVocabularyFlashcards/ListVocabularyFlashcardsQueryHandler.cs`, `Infrastructure/Repositories/ConversationRepository.cs` (`ListConversationsPagedAsync`), `Application/Features/Lessons/Queries/AttemptQueries/AttemptQueries.cs` (clamp 500), `Application/Features/Identity/Queries/ListUsers/ListUsersQueryHandler.cs`, `Infrastructure/VocabularyGames/FallingWordRoundExpirationWorker.cs` (poll mỗi 2 giây), `Application/Common/Behaviors/LoggingBehavior.cs` (log Information 2 lần mỗi request).

## Bối cảnh FE (quan trọng)

FE hiện gọi hầu hết danh sách **không** gửi `page`/`pageSize` và tự chia trang ở client (catalog topics, voices, prompts, speech speeds, AI models, roles, permissions, users, admin vocabularies, conversations). Vì vậy không được áp page size mặc định nhỏ cho request không phân trang — sẽ làm mất dữ liệu ở FE. Admin vocabularies và users sẽ được FE chuyển sang phân trang server ở batch sau.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Application/Common/**`, `src/EnglishLiveTutor.Application/Features/**/Queries/**`, `src/EnglishLiveTutor.Infrastructure/Repositories/**`, `src/EnglishLiveTutor.Infrastructure/VocabularyGames/**`, `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs`, `src/EnglishLiveTutor.Api/Endpoints/PaginationEndpointExtensions.cs`, `appsettings.json` (chỉ thêm `VocabularyGames:ExpirationPollSeconds`), `tests/**`.

Không được sửa: route, tên field JSON, header phân trang hiện có, migration, FE.

## Yêu cầu

1. **P1 — trần phân trang.** Định nghĩa hằng dùng chung (ví dụ trong `PagedResultDto`): `DefaultPageSize = 20`, `MaxPageSize = 100`, `MaxUnpagedItems = 1000`.
   - Có `page` hoặc `pageSize`: clamp `pageSize` vào 1..`MaxPageSize`.
   - Không có cả hai: trả tối đa `MaxUnpagedItems` dòng (không phải toàn bộ bảng); `TotalCount` vẫn là tổng thật để header `X-Total-Count` cho biết có bị cắt.
   - Áp dụng cho mọi handler/repository tự tính phân trang riêng (flashcards, conversations, lesson attempts, users…) — tìm bằng `grep -rn "Math.Clamp(.*500\|ToListAsync" src` và kiểm tra từng chỗ trả danh sách.
2. **P6.** Với `ListVocabularyFlashcardsQueryHandler`: sau P1 số vocabulary nạp mỗi lần đã có giới hạn. Kiểm tra `VocabularyProjectionHelper.MapToFlashcardDto` dùng những navigation nào; bỏ `Include` không được dùng. Không đổi shape DTO.
3. **P4.** Bỏ `ToLower()`/`ToUpper()` trên cột đã chuẩn hóa (`NormalizedWord`, `NormalizedEmail`, `Code` đã upper/lower khi lưu) và các so sánh bằng; giữ nguyên `Contains` có hàm chuẩn hóa trên cột tự do (`Title`, `FullName`, `MeaningVi`…) vì EF InMemory phân biệt hoa thường. Không đổi kết quả tìm kiếm.
4. **P3 — worker.** `FallingWordRoundExpirationWorker` đọc chu kỳ từ `VocabularyGames:ExpirationPollSeconds` (mặc định 10, validate 1..300). Kiểm tra trước rằng client tự gọi timeout round (`TimeoutFallingWordRoundCommand`) nên worker chỉ là cơ chế dự phòng — ghi kết luận trong report; nếu worker là cơ chế chính để kết thúc round đúng giờ, giữ mặc định 2 và ghi lý do.
5. **P3 — log.** `LoggingBehavior`: "Handling"/"Handled" xuống `Debug`; lỗi vẫn `Warning` nhưng chỉ log tên request và `ex.GetType().Name` (không truyền `ex` — message có thể chứa URL kèm API key).
6. **Test.** Request không phân trang trên tập > 1000 trả đúng 1000 và `TotalCount` thật; `pageSize=500` bị clamp còn 100; worker đọc cấu hình chu kỳ.

## Tiêu chí hoàn thành

- [ ] Không còn đường nào trả danh sách không giới hạn; header/field phân trang không đổi tên.
- [ ] Build pass; test mới pass; không test fail mới ngoài 9 test fail sẵn (`D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`).

## Lệnh kiểm chứng

**Chính sách test để tiết kiệm thời gian:** chỉ chạy `dotnet build` và `dotnet test EnglishLiveTutor.sln --no-build --filter "<tên lớp test mới hoặc liên quan>"` (có thể nhiều điều kiện nối bằng `|`). **Không** chạy toàn bộ suite nhiều lần — orchestrator sẽ chạy toàn bộ một lần khi nghiệm thu. Chạy toàn bộ suite tối đa một lần ở cuối nếu cần.

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

## Ràng buộc

- Không chạy `git add`, `git stash`, `git reset`, `git checkout` (orchestrator quản lý index). Không commit, push, merge, tạo branch; không thêm package; không chạy API/SQL/Gemini.
- Cần sửa ngoài phạm vi hoặc gặp điều mơ hồ: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt yêu cầu 1–6; danh sách chỗ phân trang đã sửa; kết luận mục 4>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
