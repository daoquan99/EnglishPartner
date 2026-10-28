# Việc giao cho AGY: B3 — ownership dữ liệu cá nhân, bỏ `userId` từ client và fallback `default-learner` (backend)

## Mục tiêu

Danh tính người dùng chỉ lấy từ claims. Đóng IDOR S4 và loại bỏ mọi đường cho phép đọc dữ liệu của user khác qua tham số. Batch B1, B2 đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, Luật bất biến 1–2), `EnglishPartner.BE/AGENTS.md`
- Review mục S4 và dòng "`ListLessonAttemptsQuery` nhận `UserId` tùy ý", "Fallback `default-learner`": `D:/Projects/EnglishPartner/docs/reviews/2026-10-28-security-performance-architecture.md`
- Skill: `.agents/skills/security-audit/SKILL.md`, `EnglishPartner.BE/.agents/skills/feature-cqrs/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`, `.agents/skills/api-contract-sync/SKILL.md`
- Code:
  - `src/EnglishLiveTutor.Api/Endpoints/PronunciationEndpoints.cs` (`GET /assessments/history` nhận `string? userId`)
  - `src/EnglishLiveTutor.Api/Endpoints/VocabularyEndpoints.cs` (`GET /{vocabularyId}/flashcard` nhận `string? userId`)
  - `src/EnglishLiveTutor.Application/Features/Lessons/Queries/AttemptQueries/AttemptQueries.cs` (`ListLessonAttemptsQuery(string? UserId, ...)`, handler dùng `query.UserId ?? _currentUser.UserId ?? "anonymous"`)
  - `src/EnglishLiveTutor.Application/Features/Vocabulary/Queries/GetVocabularyFlashcard/GetVocabularyFlashcardQuery.cs` (`UserId = "default-learner"`), `Application/Features/Vocabulary/VocabularyProjectionHelper.cs`
  - Domain entity có fallback `"default-learner"`: `Domain/Vocabulary/VocabularyGenerationAuditEntity.cs`, `Domain/Pronunciation/PronunciationAssessmentEntity.cs`, `Domain/Pronunciation/AudioRecordingEntity.cs`, `Domain/Learning/UserVocabularyProgressEntity.cs`, `Domain/Learning/DailyVocabularyPlanEntity.cs`, `Domain/Learning/VocabularyReviewAttemptEntity.cs`
  - Tìm thêm: `grep -rn "default-learner\|\"anonymous\"\|X-User-Id" src`

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Api/Endpoints/{PronunciationEndpoints,VocabularyEndpoints,LessonAttemptEndpoints}.cs`, `src/EnglishLiveTutor.Application/Features/{Vocabulary,Pronunciation,Lessons,Learning}/**`, `src/EnglishLiveTutor.Domain/{Vocabulary,Pronunciation,Learning}/**`, `tests/**`.

Không được sửa: migration, `DatabaseSeeder` (username `default-learner` của tài khoản demo là dữ liệu, không phải fallback), FE, Conversation/hub (thuộc B4).

Contract được phép đổi: bỏ query param `userId` ở `GET /api/pronunciation/assessments/history` và `GET /api/vocabularies/{vocabularyId}/flashcard`. Client gửi `userId` sẽ bị bỏ qua (không lỗi). Response giữ nguyên.

## Yêu cầu

1. Hai endpoint trên chỉ dùng `ICurrentUser.UserId`; không có user → 401.
2. `ListLessonAttemptsQuery` bỏ tham số `UserId`; handler dùng user hiện tại, không có user → `ForbiddenException` (không dùng `"anonymous"`).
3. Bỏ giá trị mặc định `"default-learner"` khỏi `GetVocabularyFlashcardQuery`, `VocabularyProjectionHelper` và các query/command khác: `UserId` là tham số bắt buộc.
4. Domain entity: thiếu `userId` → `ArgumentException` thay vì gán `"default-learner"`; property không còn initializer `"default-learner"` (dùng `string.Empty` cho constructor EF).
5. Rà các query handler khác của Pronunciation, Learning, VocabularyGames, Lessons: query đọc dữ liệu cá nhân phải lọc theo user hiện tại ngay trong câu truy vấn. Báo trong report các handler đã kiểm tra và kết luận; sửa nếu thiếu.
6. **Test (bắt buộc):** user B gọi lịch sử phát âm / flashcard / danh sách lesson attempt không thấy dữ liệu của user A, kể cả khi truyền `userId` của A; user A thấy dữ liệu của mình. Test entity ném lỗi khi thiếu userId.

## Tiêu chí hoàn thành

- [ ] `grep -rn "default-learner" src` chỉ còn trong `DatabaseSeeder.cs` (username demo).
- [ ] Không endpoint nào đọc `userId` từ query/body cho dữ liệu cá nhân.
- [ ] Build pass; test mới pass; không phát sinh test fail mới ngoài 9 test fail sẵn (xem `D:/Projects/EnglishPartner/docs/tasks/20261028-review-fix/plan.md`).

## Lệnh kiểm chứng

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
- Đã làm: <tóm tắt yêu cầu 1–6; danh sách handler đã rà ở mục 5>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
