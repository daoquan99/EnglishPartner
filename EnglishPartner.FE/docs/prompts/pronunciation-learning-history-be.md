# Prompt BE: lịch sử phát âm và học từ theo đúng đối tượng

Trạng thái: yêu cầu gửi BE, **chưa thực thi**. Baseline source kiểm tra ngày 2026-10-15; phải đọc lại trước khi triển khai. [Prompt FE đi kèm](pronunciation-examples-history-fe.md).

## Prompt để giao cho agent BE

Làm việc tại `D:\Projects\EnglishPartner\EnglishPartner.BE`.

FE cần nút **Xem lịch sử** của từ/câu trong Student Portal: lịch sử phát âm đúng từ/nghĩa/câu và lịch sử học từ. Triển khai phần BE liên quan; không sửa FE hoặc refactor ngoài phạm vi.

Đọc [AGENTS.md của BE](../../../EnglishPartner.BE/AGENTS.md), các skill áp dụng và `git status --short`. Giữ nguyên thay đổi đang có. Root namespace/solution hiện dùng `EnglishLiveTutor`, không đổi tên.

### Source cần đọc

Các đường dẫn dưới đây tính từ `src` của BE:

- `EnglishLiveTutor.Api/Endpoints/PronunciationEndpoints.cs`
- `EnglishLiveTutor.Api/Endpoints/DailyLearningEndpoints.cs`
- `EnglishLiveTutor.Application/Features/Pronunciation/Queries/GetPronunciationHistory/`
- `EnglishLiveTutor.Application/Features/Learning/Queries/GetLearningHistory/`
- `EnglishLiveTutor.Application/Features/Pronunciation/Commands/AssessPronunciation/`
- `EnglishLiveTutor.Application/Features/Learning/Commands/SubmitVocabularyReview/`
- `EnglishLiveTutor.Application/Common/Models/VocabularyDtos.cs`
- `EnglishLiveTutor.Domain/Pronunciation/PronunciationAssessmentEntity.cs`
- `EnglishLiveTutor.Domain/Learning/VocabularyReviewAttemptEntity.cs`
- `ICurrentUser`, authentication middleware và query/pagination convention thực tế.

### Hiện trạng đã xác nhận từ source

1. `GET /api/pronunciation/assessments/history` nhận `userId`, `vocabularyId`, `take`. Query lọc user/từ rồi `Take`, chưa có filter nghĩa/câu và chưa phân trang. DTO đã có `SenseId`, `TargetType`, `TargetId`, `Accent` và chi tiết điểm.
2. `GET /api/learning/flashcards/history` nhận `limit`, lấy lượt gần nhất chung của `default-learner`; chưa lọc từ/nghĩa và chưa phân trang.
3. Review attempt chỉ gắn `VocabularyId`/`SenseId`, không có định danh câu; lịch sử phát âm của câu đã có thể dựa trên assessment được lưu.
4. Metadata audio dùng `mem://recordings/...`; không coi metadata này là bằng chứng có audio persisted phát lại được.

### A. Lịch sử phát âm

- Hỗ trợ filter `vocabularyId`, `senseId`, `targetType=Word|Sentence`, `targetId`.
- Sentence lọc đúng `example.id`; Word tách khỏi các assessment Sentence của cùng từ.
- Xác định rõ semantics targetId của Word: FE hiện gửi Word không có targetId. Truy vấn Word phải bao gồm các lượt hợp lệ cũ `targetId=null`, không tự mất dữ liệu khi thêm filter.
- Lọc ở SQL trước pagination. Phân trang/tải thêm có giới hạn page size; sắp xếp `CreatedAtUtc` giảm dần và ID để thứ tự xác định khi timestamp trùng.
- Kiểm tra quan hệ vocabulary/sense/example và quy tắc visibility phù hợp. Không trả kết quả sai nghĩa hoặc cho phép bộ ID mâu thuẫn.
- Giữ ID assessment, vocabularyId, senseId, targetType, targetId, expected/reference text snapshot, recognized text, accent, model, rubric/version, status/isAssessable, overallScore nullable, criteria, feedback và timestamp.
- Không cần tạo API detail mới nếu response đã đủ chi tiết. Không thêm chức năng phát lại bản thu nếu chưa có storage lifecycle và API audio thật được yêu cầu.

### B. Lịch sử học từ

- Hỗ trợ `vocabularyId` + `senseId` và phân trang ổn định, lọc trước khi lấy trang.
- Dùng named DTO cho contract mới thay object anonymous.
- Trả ID, vocabularyId, senseId, word, reviewType, rating, reviewedAt, nextReviewAtSnapshot và pagination metadata phù hợp.
- Không biến lượt ôn flashcard của từ thành lượt ôn câu. Hiện không có lịch sử “học câu” độc lập ngoài pronunciation assessment; nêu rõ giới hạn này trong handoff.
- Nếu sản phẩm cần loại sự kiện học câu độc lập, báo riêng yêu cầu lưu sự kiện/schema; không tự thêm hệ thống tracking mới trong task lịch sử hiện có.

### C. Identity, luồng ghi và tương thích

- Lấy learner từ `ICurrentUser`/claims theo cơ chế authentication hiện có. Không tin userId query để chọn chủ sở hữu và không fallback default-learner cho API lịch sử cá nhân.
- Đảm bảo request chưa xác thực không đọc dữ liệu cá nhân. Kiểm tra ownership/visibility theo model hiện có, không coi audit CreatedBy là owner.
- Kiểm tra các command ghi assessment/review tương ứng: dữ liệu ghi và lịch sử đọc phải cùng identity. Sửa hẹp các đường ghi liên quan nếu đang cố định learner; không gán dữ liệu default-learner cũ tùy tiện cho người dùng hiện tại.
- FE hiện POST `/api/learning/flashcards/review`, BE là POST `/api/learning/flashcards/{itemId:guid}/review`. Xác nhận contract và `DailyItemId` cần cho FE; kiểm tra ownership của daily item khi ghi. Không sửa FE trong task này.
- Không âm thầm đổi response array của endpoint cũ thành paged object. Chọn mở rộng tương thích hoặc route/version mới theo convention dự án, ghi quyết định và chuyển tiếp rõ ràng.
- Contract handoff phải chốt route, query, enum casing, required/optional fields, response mẫu, pagination metadata, validation và status code. FE không phải suy đoán route đề xuất.
- Giữ query read-only, không khởi tạo plan hoặc gọi provider khi xem lịch sử.
- Không tạo migration nếu filter/pagination trên dữ liệu hiện có đã đủ; chỉ thêm index khi có lý do truy vấn cụ thể và nêu tác động.

### D. Kiểm chứng

Kiểm tra có ý nghĩa trong phạm vi:

- Hai user không đọc/ghi chéo lịch sử; caller không chọn user khác bằng query.
- Cùng từ: Word/Sentence tách đúng, hai example ID tách đúng, hai sense tách đúng.
- Word cũ với targetId null còn truy vấn được; dữ liệu không chấm được giữ null score và status.
- Filter được áp trước pagination; nhiều trang, trang cuối, timestamp trùng, giới hạn tham số và bộ ID mâu thuẫn.
- Lịch sử rỗng, validation và identity của luồng ghi/đọc liên quan.
- Endpoint cũ giữ tương thích theo quyết định đã nêu.

Chạy build/test chọn lọc phù hợp; không gọi AI trả phí. Không khởi động API, migration hoặc seed chỉ để kiểm tra một query đọc. Nếu cần xác nhận SQL/HTTP thực tế, thực hiện trong môi trường được phép và báo rõ bằng chứng; test InMemory/build không thay thế SQL Server hoặc authentication runtime.

### Bàn giao

Báo cáo tiếng Việt gồm file đổi, contract FE dùng được kèm JSON mẫu, xử lý compatibility/identity, yêu cầu FE về DailyItemId, kiểm chứng thực sự đã chạy và phần SQL/provider/HTTP chưa kiểm tra. Không báo tính năng lịch sử FE đã hoàn tất chỉ từ thay đổi BE. Không commit/push trong task này.
