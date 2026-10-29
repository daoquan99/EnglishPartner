# Việc giao cho AGY: D1 fix 1 — sửa theo review (backend)

D1 đã làm trong worktree này — giữ nguyên, chỉ sửa các điểm dưới. Phạm vi file như `prompt-agy-d1.md` (BE, không FE, không migration, không package). Plan: `D:/Projects/EnglishPartner/docs/tasks/20261029-targeted-vocabulary/plan.md`.

## Yêu cầu

1. **M1 — import thủ công không được cắt ví dụ về 2.** Thêm `int? ExamplesPerSense = null` vào `ImportVocabularyJsonRequest` (1..5 khi có giá trị, validator). Import: có giá trị → cắt về giá trị đó; null → **không cắt theo mặc định 2** (chỉ giới hạn trần 5 như sinh trực tiếp). Đường `ParseResponse`/writer cho import không được dùng mặc định 2 ngầm. Test: JSON có 4 ví dụ, không truyền `ExamplesPerSense` → lưu đủ 4; truyền 3 → lưu 3.
2. **L1 — chỉ xử lý race khi đúng lỗi trùng khóa plan.** Bộ lọc `catch (DbUpdateException) when (...)` trong `VocabularyGenerationWriter` phải kiểm unique violation (SqlException 2601/2627 hoặc InMemory tương đương — tái dùng helper `IsUniqueConstraintViolation` nếu có trong Application, nếu không thì thêm helper nhỏ trong Application không phụ thuộc SqlClient: kiểm theo message/inner type an toàn, hoặc kiểm lại DB "đã có plan (UserId, LearningDate)" trước khi chạy nhánh reload). Lỗi khác ném tiếp như cũ.
3. **L2 — không lãng phí slot.** Khi writer gọi initializer với `reservedQuota`, loại các `VocabularyId`/`SenseId` của từ vừa sinh khỏi ứng viên catalog và carry-over của plan mới (truyền danh sách id loại trừ vào initializer), để từ vừa sinh luôn được gán và plan đủ target.
4. **L3 — khớp term bỏ dấu câu hai đầu.** `NormalizeTerm` (BE) bỏ dấu câu/ngoặc kép (kể cả “ ” ‘ ’ « ») ở hai đầu term trước khi validate và khi so khớp mục AI trả về. Test: term `"“well-being,”"` khớp mục `word: "well-being"`.
5. **L4 — nhánh `SourceSentence`.** Cắt số mục được lưu về tối đa `RequestedCount`; với mỗi mục, nếu không có ví dụ nào trùng câu gốc (so sánh sau chuẩn hóa khoảng trắng, không phân biệt hoa thường) thì chèn câu gốc làm ví dụ đầu tiên (bản dịch `sentenceVi` để trống nếu không có) rồi mới cắt theo `ExamplesPerSense`.
6. **L5 — bỏ default interface method.** Thay overload có thân mặc định trên `IVocabularyAiGenerator` (và overload tương tự trên `IVocabularyGenerationContent`) bằng **một** method nhận record `VocabularyGenerationRequestOptions` (hoặc tên tương đương) gồm các tham số cũ + `ExamplesPerSense`, `TargetTerms`, `SourceSentence`; không có thân mặc định. Cập nhật mọi implementation và fake trong test.
7. **L6 — audit và kết quả rỗng.** Đếm `rejected` đúng theo số term không có mục khớp (không trừ theo `RequestedCount - accepted`). Có `TargetTerms`/`SourceSentence` mà sau lọc không còn mục nào → ném `InvalidOperationException` với message tiếng Việt rõ ("AI không trả về từ vựng khớp với chữ đã chọn. Vui lòng thử lại.") — audit ghi Failed (message đã làm sạch).
8. **L7.** `TargetTerms` rỗng (`[]`) coi như null (hành vi cũ), không 400.

## Lệnh kiểm chứng

`dotnet build EnglishLiveTutor.sln` và `dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~Vocabulary|FullyQualifiedName~DailyVocabulary|FullyQualifiedName~Learning|FullyQualifiedName~AiCommands"`; toàn bộ suite tối đa một lần ở cuối. Không warning mới; không fail mới ngoài 6 baseline `PronunciationSynthesisAudioCacheTests`.

## Ràng buộc

Không `git add`/`stash`/`reset`/`checkout`, không commit/push, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi. UTF-8 đúng dấu.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–8>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test, số warning>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
