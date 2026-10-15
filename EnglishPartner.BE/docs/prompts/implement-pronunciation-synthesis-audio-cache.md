# Prompt triển khai lưu và tái sử dụng audio phát âm

Bạn đang làm việc trong `D:\Projects\EnglishPartner\EnglishPartner.BE`, solution `EnglishLiveTutor.sln`; FE ở `D:\Projects\EnglishPartner\EnglishPartner.FE`. Hãy triển khai cache bền vững cho `POST /api/pronunciation/synthesize` và cập nhật FE trong đúng luồng phát âm để các lần nghe lại không gọi AI khi audio phù hợp đã có.

## 1. Bối cảnh đã kiểm tra từ source

- `src/EnglishLiveTutor.Api/Endpoints/PronunciationEndpoints.cs` map `/synthesize` qua `ICommandHandler<SynthesizePronunciationAudioCommand, SynthesizePronunciationAudioResponseDto>`.
- `Application/Features/Pronunciation/Commands/SynthesizePronunciationAudio/SynthesizePronunciationAudioCommand.cs` hiện chỉ chuyển tiếp tới `ILivePronunciationTtsSynthesizer.SynthesizeSpeechAsync`, không lookup hay lưu audio.
- `Infrastructure/Ai/GeminiLivePronunciationTtsSynthesizer.cs` tạo Gemini Live bridge, ghép PCM trong MemoryStream, đóng gói WAV và trả AudioBase64. Không có repository/storage bền vững trong luồng này. Hiện có timeout 18 giây và catch OperationCanceledException vẫn có thể trả những chunks đã nhận; cần tránh lưu audio chưa hoàn tất vào cache.
- DTO trong `Application/Common/Models/AdminCrudDtos.cs` hiện nhận ApiKey, Text, TargetType, Accent, VoiceName, SpeechSpeed, ModelCode; chưa có TargetId. Response gồm Text, TargetType, Accent, VoiceName, SpeechSpeed, ModelCodeUsed, SystemPromptUsed, MimeType, SampleRate, DurationMs, AudioBase64.
- `Domain/Pronunciation/AudioRecordingEntity.cs` là metadata audio người học cho assessments; không phải cache audio TTS. `VocabularyPronunciationEntity.AudioUrl` và `VocabularyExampleEntity.AudioUrl` cũng chưa được luồng synthesize cập nhật và không biểu diễn đầy đủ các biến thể voice/speed/model. Không gộp nhầm các dữ liệu này.
- FE `src/features/vocabulary/pages/DailyVocabularyPage.tsx`, helper `handleSynthesizeTeacherSpeech`, hiện chỉ gửi text/kind/accent/voice/speed/model, còn chặn sớm khi thiếu API key. FE giữ kết quả vừa phát trong state nhưng chưa có cache bền vững theo ID.

Đây là kết luận từ source, chưa phải kiểm tra DB hay Google API thật. Đọc lại source hiện tại trước khi sửa vì workspace có nhiều thay đổi chưa commit.

## 2. Phạm vi và kiến trúc

- Đọc AGENTS.md nếu có và `docs/architecture.md`. Giữ Pragmatic Clean Architecture + DDD + feature-based CQRS hiện có; endpoint chỉ bind request, gọi handler, map HTTP response. Command điều phối domain/repository, Infrastructure triển khai EF và Gemini; không đưa EF/provider vào endpoint/Domain.
- Chỉ sửa luồng synthesis/cache và các consumer FE liên quan. Không thay đổi `/api/pronunciation/assessments`, vocabulary generation/import, login, realtime tutor hay cách chấm điểm.
- Giữ các thay đổi đã có của người dùng, kể cả untracked files. Không reset/checkout đè, xóa migrations hoặc tạo framework/generic repository mới.
- Không gọi Google thật, không dùng API key trong chat/log và không chạy migration/seed lên DB thật trong tác vụ này.

## 3. Định danh target và bộ khóa cache

Bộ khóa gốc bắt buộc theo yêu cầu người dùng:

```text
TargetType + VoiceName + SpeechSpeed + TargetId
```

- `TargetType=word`: TargetId chính là `VocabularyEntity.Id`.
- `TargetType=sentence`: TargetId chính là `VocabularyExampleEntity.Id`, không phải VocabularyId hoặc SenseId.
- Bổ sung TargetId vào request DTO; có thể nhận thêm VocabularyId/SenseId làm ngữ cảnh kiểm tra sentence thuộc đúng vocabulary/sense. Giữ tên các fields JSON cũ.
- Với request có TargetId, BE resolve text từ DB. Word lấy Vocabulary.Word; sentence lấy VocabularyExample.SentenceEn qua sense/vocabulary. Nếu context IDs được gửi thì phải match. Không tin Text do client gửi để tạo audio rồi gắn vào ID khác. Không lấy nhầm câu đầu tiên khi ID không tồn tại. Không phục vụ target đã soft-delete hoặc target thuộc vocabulary đã soft-delete.
- Không đoán word/sentence bằng cách kiểm tra text có dấu cách: một vocabulary có thể là cụm từ. TargetType được chuẩn hóa và kiểm tra theo ý nghĩa của TargetId.
- Tương thích caller cũ chỉ gửi Text mà không có TargetId: có thể giữ nhánh sinh theo text như cũ, nhưng nhánh đó không được tạo cache theo ID giả. Cập nhật mọi caller FE trong phạm vi sang ID thật; ghi rõ legacy text-only chưa dùng persistent cache. Request không có ID và cũng không có text phải trả lỗi rõ ràng.

Audio còn phụ thuộc Accent US/UK và effective ModelCode. Giữ nguyên bốn thành phần khóa gốc, đồng thời phân biệt thêm Accent và model khi lookup/lưu; không dùng WAV US cho request UK hoặc tráo model đã chọn. Không unique chỉ trên bốn cột gốc nếu điều đó ngăn lưu các biến thể hợp lệ.

Chuẩn hóa trước khi lookup và trước khi gọi provider theo cùng quy tắc: TargetType, voice mặc định/canonical, accent, effective model, speed. Giữ cách chọn model TTS hiện có; không tự đổi model/provider version. Với speed, giữ default/clamp hợp lý hiện có và dùng decimal có precision rõ ràng để lưu/index; chọn một quy tắc làm tròn, ví dụ 2 chữ số, rồi truyền đúng speed đã chuẩn hóa cho provider. Không so sánh double thô hoặc lưu một speed nhưng generate bằng speed khác.

Lưu text snapshot và hash của text authoritative. Khi text của target đã đổi, bản audio cũ không còn là cache hit dù bốn cột khóa gốc vẫn giống nhau. Không đưa ApiKey/UserId vào cache key: đây là audio của nội dung dùng chung theo biến thể phát âm.

## 4. Lưu audio bền vững trong SQL Server

Hiện chưa thấy storage binary dùng chung cho TTS trong repository. Triển khai một bảng riêng, ví dụ `GeneratedPronunciationAudios`, lưu bytes WAV trong `varbinary(max)` cùng metadata để dùng lại sau restart. Không thêm Blob/S3/Redis hoặc dependency storage mới cho tác vụ này.

Đề xuất layout theo cấu trúc repository hiện có, điều chỉnh tên nếu đã có thành phần tương đương:

- Domain: `Pronunciation/GeneratedPronunciationAudioEntity.cs`, `Pronunciation/Repositories/IGeneratedPronunciationAudioRepository.cs`.
- Application: bổ sung orchestration vào SynthesizePronunciationAudioCommandHandler; thêm query cache-only và abstraction resolve target nếu cần, không tạo bản sao luồng synthesis.
- Infrastructure: repository tương ứng; entity configuration trong Persistence/Configurations; DbSet trong AppDbContext/IAppDbContext nếu abstraction cần; cập nhật DI, migration và snapshot.

Metadata tối thiểu: Id, TargetType, TargetId, VoiceName, SpeechSpeed, Accent, effective ModelCode, TextSnapshot/TextHash, WAV bytes, MimeType, SampleRate, DurationMs, SystemPromptUsed, audit timestamps và trạng thái hợp lệ nếu thiết kế cần. Không lưu API key hay URL có key. Chỉ cache audio hoàn tất, không rỗng/hỏng.

Tạo composite index với đúng bốn cột gốc theo thứ tự người dùng nêu:

```csharp
builder.HasIndex(e => new { e.TargetType, e.VoiceName, e.SpeechSpeed, e.TargetId });
```

Lookup còn phải lọc Accent, effective ModelCode và text hash. Có thể thêm index phù hợp với query đầy đủ; cân nhắc unique index đầy đủ trên các biến thể + text hash cho các bản đang hiệu lực để ngăn duplicate insert. Không đặt unique constraint chỉ trên bốn cột gốc. Giữ giới hạn độ dài các cột indexed phù hợp SQL Server.

Với khóa Id do ứng dụng cấp, khai báo trực tiếp:

```csharp
builder.HasKey(e => e.Id);
builder.Property(e => e.Id).ValueGeneratedNever();
```

Giữ audit/soft-delete conventions hiện có. Repository lấy bản mới nhất hợp lệ theo UpdatedAtUtc ?? CreatedAtUtc, rồi CreatedAtUtc, rồi Id giảm dần nếu có nhiều bản match. Không cập nhật timestamp của row chỉ vì cache hit. Nếu audio mới nhất bị hỏng/thiếu bytes thì không trả thành công; xử lý bản hợp lệ khác hoặc cache miss rõ ràng.

Tạo migration tăng dần và kiểm tra SQL script, không drop/recreate DB hoặc sửa lịch sử migrations để che thay đổi. Không tự chạy database update/seed vào DB thật; báo rõ migration chỉ được tạo/chưa áp dụng.

## 5. Luồng POST /api/pronunciation/synthesize

1. Normalize request và resolve target/text từ DB đối với đường gọi có TargetId.
2. Lookup cache bền vững theo đủ target/voice/speed/accent/model/text hash.
3. Cache hit: trả cùng DTO response và audio bytes đã lưu, chuyển base64 tại response nếu cần. Không gọi Gemini, không mở GeminiLiveSessionBridge, không tạo audio mới và không bắt buộc người dùng cung cấp API key. Việc kiểm tra ApiKey phải nằm sau lookup cache.
4. Cache miss: kiểm tra API key và các dữ liệu cần cho provider. Thiếu key thì trả lỗi rõ ràng để FE yêu cầu nhập key; không trả audio giả hoặc đánh dấu là cache hit.
5. Gọi provider đúng một lần cho target/profile đã resolve. Chỉ khi generation hoàn tất và WAV hợp lệ mới lưu audio cùng metadata rồi trả kết quả. Provider lỗi, timeout, canceled request, PCM rỗng hoặc turn chưa hoàn thành thì không tạo cache row thành công.
6. Đặc biệt sửa/bao bọc nhánh catch OperationCanceledException hiện có: phân biệt caller cancellation với timeout nội bộ; không nuốt request cancellation để lưu/return như đã hoàn tất. Không đưa audio partial do timeout 18 giây vào persistent cache. Nếu vẫn giữ khả năng nghe partial cho tương thích, phải phân biệt completion nội bộ và không cache partial.
7. Khi có nhiều request đồng thời cùng full key, dùng single-flight/lock phù hợp và lookup lại sau khi lấy lock để trong cùng một host không gọi AI lặp. Không dùng một global lock chặn các target khác nhau. Cancellation/exception phải release lock; tránh xóa lock trong khi còn waiters và tránh lưu dictionary tăng vô hạn. Kết hợp xử lý unique-conflict trong DB nếu thiết kế có unique full key. Nếu ứng dụng nhiều instance, nêu rõ phạm vi bảo đảm hoặc dùng cơ chế DB phù hợp; không tuyên bố process-local SemaphoreSlim bảo đảm một lần gọi AI trên toàn cluster.

Giữ response hiện có: audioBase64, mimeType, sampleRate, durationMs, text, targetType, accent, voiceName, speechSpeed, modelCodeUsed, systemPromptUsed. Trả metadata đúng bản audio đã lưu; không recompute prompt rồi mô tả sai audio cũ. Không cần đổi toàn bộ API sang file URL để hoàn thành tác vụ này.

## 6. API đọc cache riêng

Thêm query CQRS và route cache-only, ví dụ:

```http
GET /api/pronunciation/audio?targetType=word&targetId=<vocabulary-guid>&voiceName=Aoede&speechSpeed=1&accent=US&modelCode=<existing-tts-model>
```

Với sentence, TargetId là example GUID. Các default/normalization/target resolution dùng chung với POST. GET chỉ đọc audio đã lưu, không gọi provider và không nhận API key. Hit trả DTO tương thích; cache miss trả 404. Invalid target trả 400/404 rõ ràng theo conventions hiện có, không trả nhầm target hay dùng Text do client tự nhập.

## 7. Cập nhật FE đúng luồng phát âm

Kiểm tra và sửa:

- `EnglishPartner.FE/src/shared/api/openapi.types.ts`: SynthesizePronunciationAudioRequestDto và DTO/query liên quan.
- `src/features/vocabulary/api/vocabularyApi.ts` và `hooks/useDailyVocabulary.ts`: request contract đúng BE; bổ sung cache-only API nếu cần dùng ở consumer.
- `src/features/vocabulary/pages/DailyVocabularyPage.tsx`: helper handleSynthesizeTeacherSpeech và tất cả nút US/UK của word, example sentence, Mẫu US/Mẫu UK trong phòng phát âm.

Word phải gửi currentCard.vocabularyId. Mỗi câu ví dụ phải gửi chính ex.id và ngữ cảnh currentCard.vocabularyId/currentCard.senseId nếu contract yêu cầu. Các nút dùng example đầu tiên chỉ được gửi ID của chính example đó; nếu đang fallback về word thì cả targetType và targetId phải là word/vocabulary ID. Không suy ra example ID bằng so khớp text hoặc dùng chung ID của card cho mọi câu.

Không chặn trước request chỉ vì thiếu API key: bản cache có thể nghe được mà không cần key. FE có thể gọi POST với target IDs và key rỗng để BE lookup trước, hoặc GET cache-only rồi POST khi miss; giữ một luồng rõ ràng, không gọi provider hai lần. Chỉ yêu cầu người dùng nhập key khi cần generate mà cache miss. Nếu giữ browser-TTS fallback thì phải ghi rõ là giọng trình duyệt, không lưu nó như audio Gemini và không che lỗi provider thành thông báo cache thành công.

Giữ việc play AudioBase64/MimeType hiện tại. Disable chống click lặp trong lúc pending. Khi chuyển card trong lúc request đang chạy, tránh phát response của target cũ lên card mới. Không đổi trang/modal/flows ngoài mục phát âm.

## 8. Xác minh trong đúng phạm vi

- Build BE solution và FE. Nếu API đang chạy khóa DLL, dùng output/artifacts path riêng thay vì tự dừng tiến trình người dùng. Không coi build thành công là đã xác minh Google/DB production.
- Chỉ thêm/chạy tests cho synthesis/cache và request FE liên quan, không chạy suites ngoài scope. Dùng fake provider và DB test/in-memory phù hợp; không gọi Google thật.
- Test cache miss lưu WAV và metadata đúng; lần hai cùng key không gọi provider; đọc lại từ context/service instance mới vẫn hit.
- Test đổi TargetId, TargetType, voice, speed, US/UK, effective model hoặc text hash thì không dùng sai audio.
- Test word là cụm nhiều từ vẫn dùng word target; sentence thuộc sai vocabulary/sense hoặc target đã xóa bị từ chối.
- Test cached hit không có API key vẫn thành công; miss không có key trả lỗi mà provider chưa được gọi.
- Test chọn bản mới nhất hợp lệ, bytes đọc lại trùng bytes đã lưu, cache GET 404 không gọi AI, lỗi/timeout/cancel/partial audio không được cache.
- Test concurrent cùng key theo phạm vi khóa đã triển khai; different key không bị chặn chung. Không dùng EF InMemory để tuyên bố đã verify unique index/transaction semantics của SQL Server.
- Kiểm tra EF metadata/migration có ValueGenerated.Never, composite index bốn cột gốc và các biến thể bổ sung đúng thiết kế. Đọc lại SQL script nếu tạo unique index; không áp dụng vào DB thật.
- Test FE gửi đúng IDs của word và từng example; không chặn cache hit khi thiếu key; không phát nhầm response sau khi chuyển card.
- `git diff --check`; đọc lại diff để không mất các thay đổi sẵn có.

## 9. Báo cáo cuối bằng tiếng Việt

Nêu audio được lưu ở bảng/cột nào; bộ khóa gốc và bộ lọc biến thể; API POST/GET cùng payload/query mẫu; quy tắc hit/miss/latest/text invalidation; các file chính; migration đã tạo/chưa áp dụng; build/tests thực tế và giới hạn xác minh. Ghi rõ nếu chưa test live Google/SQL/FE runtime hoặc concurrency nhiều instance. Không báo cache đã chạy trên DB thật nếu mới có code/migration/test fixtures.
