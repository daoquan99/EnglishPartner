# Prompt cho AGY: bổ sung test cho P1 (Progress BE)

Bạn là worker. Đọc `AGENTS.md`, `EnglishPartner.BE/AGENTS.md` và skill `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`. Plan: `docs/tasks/20261102-learning-progress/plan.md`.

Code BE của module Progress đã xong và đã qua review. Phần này **chỉ sửa test**.

## Phạm vi

Chỉ được sửa các file sau:
- `EnglishPartner.BE/tests/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Tests/GrammarProgressReaderTests.cs`
- `EnglishPartner.BE/tests/Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Tests/LessonsProgressReaderTests.cs`
- `EnglishPartner.BE/tests/Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Tests/LiveTutorProgressReaderTests.cs`
- `EnglishPartner.BE/tests/Modules/Speech/EnglishLiveTutor.Modules.Speech.Tests/SpeechProgressReaderTests.cs`
- `EnglishPartner.BE/tests/Modules/Vocabulary/EnglishLiveTutor.Modules.Vocabulary.Tests/VocabularyProgressReaderTests.cs`
- `EnglishPartner.BE/tests/EnglishLiveTutor.Api.IntegrationTests/Progress/ProgressEndpointsIntegrationTests.cs`
- `EnglishPartner.BE/tests/EnglishLiveTutor.Api.IntegrationTests/Progress/GetProgressOverviewQueryHandlerTests.cs`

**Không sửa code trong `src/`.** Nếu một test fail vì code sai, dừng lại và ghi vào báo cáo cuối, không tự sửa `src/`. Không sửa snapshot, migration hay file nào khác. Không commit.

## Việc cần làm

1. **Cross-user cho `GetSummaryAsync`** của Grammar, Lessons, LiveTutor và Speech.
   - Seed thêm dữ liệu của user B, với giá trị khác hẳn user A:
     - Lessons: attempt Completed, điểm khác.
     - Speech: assessment, điểm khác, có bản ghi trong và ngoài khoảng `fromUtc`.
     - LiveTutor: conversation, cả trong và ngoài khoảng.
     - Grammar: `UserGrammarStepProgress` Passed ở một step mà A chưa pass.
   - Assert **bằng `Assert.Equal`** rằng count, average và passed steps của A đúng bằng giá trị chỉ tính từ dữ liệu của A.
   - Vocabulary đã có test này; kiểm lại rằng nó assert bằng `Equal`.
2. **Phần tóm tắt theo khoảng thời gian:**
   - Speech: `AverageScoreSinceFrom` chỉ tính các bản ghi có `CreatedAtUtc >= fromUtc`. Seed một bản ghi cũ có điểm khác để phân biệt được với giá trị trung bình toàn bộ.
   - LiveTutor: `SessionsSinceFrom` chỉ đếm các conversation trong khoảng; `Sessions` đếm tất cả.
3. **Soft-delete:** ít nhất ở LiveTutor và Speech, seed một bản ghi đã xóa mềm của user A, dùng method domain có sẵn (tìm bằng grep `SoftDelete`/`MarkDeleted`/`IsDeleted`). Assert bản ghi đó không được tính, cả trong `GetDailyActivityAsync` lẫn `GetSummaryAsync`. Nếu InMemory không áp dụng query filter thì ghi chú vào báo cáo và bỏ case đó.
4. **Endpoint** (`ProgressEndpointsIntegrationTests`): sửa test cross-user.
   - Seed dữ liệu cho **cả A và B**, với số lượng khác nhau, ở ít nhất 3 nguồn. Ví dụ: Conversation (A: 1, B: 3), PronunciationAssessment, GrammarAttempt đã hoàn thành hoặc VocabularyReviewAttempt. Tìm cách seed trong `ApiTestFactory` hoặc test có sẵn bằng grep.
   - Assert `Equal` chính xác cho từng user: `LiveTutor.Sessions`, `Speech.Assessments`, và tổng activity của hôm nay.
5. **Handler test:** các fake reader và `FakeUserDirectory` ghi lại `userId` nhận được. Assert mọi reader đều nhận đúng `query.UserId`.

## Kiểm chứng

Chạy trong `EnglishPartner.BE`:
```bash
dotnet build EnglishLiveTutor.slnx
dotnet test EnglishLiveTutor.slnx
```
Tất cả phải pass.

## Báo cáo cuối

Liệt kê:
- test đã thêm hoặc sửa (file và tên test);
- kết quả lệnh, gồm tổng số test pass;
- case nào bỏ và lý do.
