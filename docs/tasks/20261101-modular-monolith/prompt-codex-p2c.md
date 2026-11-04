# P2c — đảo phụ thuộc Speech ↔ Vocabulary (D7, D8)

Theo `plan-p2.md` D7, D8. Speech **không được** tham chiếu namespace Vocabulary/Learning (DAG). Speech khai báo port trong Contracts; Vocabulary implement. Không đổi hành vi: cùng thứ tự kiểm tra, cùng exception type + message, cùng một lần commit.

## Yêu cầu

1. **Port trong `src/EnglishLiveTutor.Application/Features/Pronunciation/Contracts/`:**
   - `IPronunciationTargetTextSource` với các method đủ để thay **mọi** truy cập `Vocabularies`/`VocabularySenses`/`VocabularyExamples`/`VocabularyPronunciations` trong:
     - `Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommandHandler.cs` (~dòng 68-90: load vocab + pronunciations + senses/examples, chọn câu ví dụ theo `TargetId` hoặc cái đầu tiên, các field khác handler dùng sau đó — đọc toàn file).
     - `Features/Pronunciation/PronunciationSynthesisResolution.cs` (`PronunciationTargetResolver`, ~dòng 60-140: word theo id `!IsDeleted`; example theo id → sense → vocab `!IsDeleted`).
     Trả về DTO record (không entity) chứa đúng dữ liệu handler cần. **Các kiểm tra và message lỗi giữ ở phía Speech** (port chỉ trả dữ liệu hoặc null), để thứ tự kiểm tra không đổi. Nếu một kiểm tra không thể giữ ở Speech mà không đổi thứ tự → dừng, báo.
   - `IPronunciationAssessmentObserver` với `Task OnAssessedAsync(string userId, Guid senseId, decimal overallScore, CancellationToken ct)` (kiểu tham số theo đúng kiểu hiện có trong handler, ~dòng 213-218).
2. **Impl ở Vocabulary:** `Features/Vocabulary/Services/PronunciationTargetTextSource.cs` (dùng `IVocabularyDbContext`, query `AsNoTracking` y như cũ) và `Features/Vocabulary/Services/VocabularyPronunciationProgressObserver.cs` (dùng `IVocabularyDbContext`: tìm `UserVocabularyProgresses` theo `UserId == userId && SenseId == senseId`, nếu có thì `UpdateLatestPronunciationScore(score)` — **không** `SaveChanges`; commit vẫn do `_uow.CommitChangesAsync` của handler Speech, cùng instance `AppDbContext` scoped). Đăng ký scoped trong `Application/DependencyInjection.cs`.
3. **Speech dùng port:** trong `AssessPronunciationCommandHandler`, gọi observer ở **đúng chỗ** đoạn cập nhật progress hiện tại (trước `CommitChangesAsync`), chỉ khi `overallScore.HasValue` như cũ. Handler và `PronunciationTargetResolver` chuyển sang `ISpeechDbContext` nếu sau khi bỏ Vocabulary + Catalog (P2a) chỉ còn `DbSet` của Speech; nếu còn thứ khác (rubric là Speech — được) thì liệt kê.
4. **Test:**
   - Test hiện có cho AssessPronunciation/synthesis resolution phải pass không sửa assertion (chỉ wiring constructor nếu cần — truyền impl Vocabulary thật dùng cùng `AppDbContext` InMemory).
   - Thêm test: điểm phát âm được ghi vào `UserVocabularyProgress` **trong cùng commit** (đếm `SaveChanges` = 1 nếu test hiện có cách đếm commit, xem `AiCommandsUnitOfWorkTests.cs`); provider lỗi → không ghi progress; không có progress → không lỗi.
   - Test ranh giới: file trong `Features/Pronunciation/` không có `using` namespace Vocabulary/Learning (scanner đã bắt `dag`; đảm bảo allowlist không có dòng Speech → Vocabulary).
5. Allowlist chỉ bớt; chạy shrink như các gói trước.

## Kiểm chứng

Như P2a. Baseline: số pass sau P2b, 0 fail. Worker report theo mẫu P2a, thêm mục "Port: method + DTO" và "Vị trí gọi observer (dòng trước/sau)".
