# Prompt FE: chuyển câu ví dụ và xem lịch sử luyện tập

Trạng thái: prompt triển khai, **chưa thực thi**. Baseline source kiểm tra ngày 2026-10-15; đọc lại contract trước khi làm. [Prompt BE đi kèm](pronunciation-learning-history-be.md).

## Prompt để giao cho agent FE

Làm việc tại `D:\Projects\EnglishPartner\EnglishPartner.FE`.

Triển khai trong menu **Từ Vựng & Chấm Phát Âm**, khu vực **Phòng Chấm Phát Âm Audio AI**: chọn các câu ví dụ khác nhau để luyện phát âm và nút xem lịch sử của từ/câu hiện tại.

### Phạm vi và source

Đọc [AGENTS.md](../../AGENTS.md), skill Student Portal và design system liên quan, cùng `git status --short`. Giữ nguyên thay đổi đang có. Chỉ sửa FE thuộc yêu cầu này; không sửa BE hoặc thực hiện refactor ngoài phạm vi.

Đọc:

- `src/features/vocabulary/pages/DailyVocabularyPage.tsx`
- `src/features/vocabulary/api/vocabularyApi.ts`
- `src/features/vocabulary/hooks/useDailyVocabulary.ts`
- `src/features/vocabulary/types/vocabulary.types.ts`
- `src/shared/api/openapi.types.ts`
- `src/shared/components/ui/Dialog.tsx`
- `src/features/vocabulary/__tests__/pronunciationAssessment.test.ts`
- `src/features/vocabulary/__tests__/pronunciationSynthesisAudioCache.test.ts`

### 1. Chọn câu ví dụ

Hiện phòng chấm cố định `examples[0]` cho nội dung, TTS US/UK và assessment. Thay bằng một câu đang chọn thống nhất.

- Nhiều hơn một câu: có **Câu i/N**, **Câu trước**, **Câu tiếp**. Disable tại đầu/cuối, không tự quay vòng.
- Câu dài xuống dòng; vùng nội dung có thể cuộn khi cần trên màn hình nhỏ. Không cắt mất phần cần đọc. Hiển thị `sentenceEn` và `translationVi` nếu có.
- Một câu: luyện bình thường, không cần nút chuyển câu.
- Không có câu: báo “Từ này chưa có câu ví dụ”; không âm thầm nghe/chấm từ đơn khi tab Câu ví dụ đang chọn. Disable thao tác đòi hỏi câu.
- Đổi `vocabularyId` hoặc `senseId`: reset về câu đầu. Khi refetch, giữ câu theo ID nếu còn; nếu mất, chọn lại hợp lệ, không để index vượt phạm vi.

Câu đang chọn quyết định đồng thời:

| Thao tác | Giá trị |
| --- | --- |
| Nội dung mẫu | `selectedExample.sentenceEn` |
| TTS | `text=sentenceEn`, `targetType='sentence'`, `targetId=example.id`, đúng `vocabularyId` và `senseId` |
| Assessment qua adapter hiện có | `exampleId=example.id`, `referenceText=sentenceEn`; adapter giữ `targetType='Sentence'` |

Không thay toàn bộ `examples[0]` trong file một cách máy móc; preview flashcard bên trái có thể vẫn dùng câu đầu.

### 2. Thu âm và response đúng đối tượng

- Chụp snapshot `vocabularyId`, `senseId`, target type, example ID, reference text, accent và model khi bắt đầu thu âm.
- Khi đổi từ/nghĩa/tab/câu: dừng và hủy bản thu dở, giải phóng microphone, xóa kết quả/lỗi cũ và dừng audio mẫu đang phát.
- Kiểm tra session sau `await getUserMedia`; nếu đã chuyển đối tượng, giải phóng stream vừa nhận, không bắt đầu thu đối tượng cũ.
- Dùng request/session token để response assessment/TTS cũ không cập nhật kết quả, phát audio hay hiện lỗi trên đối tượng mới, kể cả A → B → A. Ref chỉ lưu vocabularyId và cờ discard dùng chung hiện tại chưa đủ.
- Không làm hỏng cache audio, lựa chọn Teacher Voice, model, accent và tốc độ hiện có.

### 3. Nút Xem lịch sử

Đặt trong phòng chấm. Mở Dialog/Drawer theo UI hiện có, tiêu đề nhận diện đúng đối tượng. Có keyboard/focus và vùng cuộn phù hợp; kiểm tra theme khi Dialog render vào document.body.

- Tab Từ đơn: lịch sử phát âm của từ trong nghĩa hiện tại; thêm mục **Lịch sử học từ** cho lượt ôn flashcard.
- Tab Câu ví dụ: lịch sử phát âm đúng `example.id` đang chọn. Không có câu thì không gọi lịch sử từ để thay thế.
- Lịch sử mới nhất trước, phân trang/tải thêm từ server.
- Mỗi lượt phát âm: thời gian theo timezone người dùng, nội dung mẫu, accent, model, trạng thái, tổng điểm; mở chi tiết xem transcript, điểm tiêu chí, nhận xét và gợi ý.
- `overallScore=null` hoặc `NotAssessable`: thể hiện không chấm được, không đổi thành 0. Không hiển thị score tổng hợp chưa được API hỗ trợ.
- Lịch sử học từ: rating Again/Hard/Good/Easy, thời gian ôn, lịch ôn tiếp theo.
- Có loading, empty, error và retry riêng. Lỗi request không được hiện như chưa có lịch sử.
- Chỉ fetch khi mở; query key gồm identity hiện tại, vocabularyId, senseId, targetType, targetId và pagination phù hợp. Đổi tài khoản không giữ lịch sử của người trước.
- Sau assessment/review thành công, invalidate lịch sử liên quan. Response cũ không được xuất hiện khi đổi đối tượng trong lúc mở lịch sử.
- Không thêm nghe lại bản thu khi BE chưa có storage/API audio thật.

### 4. Contract và phần cần chờ BE

Baseline BE có:

```text
GET /api/pronunciation/assessments/history?userId=&vocabularyId=&take=
GET /api/learning/flashcards/history?limit=
```

Endpoint phát âm chưa có filter nghĩa/câu hoặc pagination; endpoint học từ chưa lọc từ/nghĩa. Identity chưa phù hợp lịch sử cá nhân: phát âm nhận userId query, học từ cố định default-learner. Kiểm tra contract BE mới trước khi tích hợp chính thức.

- Không tự phát minh endpoint hoặc coi contract đề xuất là đã tồn tại.
- Không tải 20/30 lượt gần nhất rồi lọc ở FE và gọi là lịch sử đầy đủ.
- Không dùng mock/localStorage thay lịch sử persisted.
- DTO lịch sử giữ `vocabularyId`, `senseId`, `targetType`, `targetId`, `accent` và timestamp. DTO/mapping assessment FE hiện bỏ một số field này; bổ sung có chủ đích, giữ tương thích luồng chấm.
- Nếu BE chưa hoàn thiện: hoàn thành chuyển câu và UI lịch sử với trạng thái rõ “Lịch sử đang chờ Backend hỗ trợ”, không gọi contract chưa hỗ trợ; báo blocker, không báo lịch sử đã hoàn thành.

Luồng ghi review hiện còn lệch route: FE POST `/api/learning/flashcards/review`, source BE POST `/api/learning/flashcards/{itemId:guid}/review`. Nếu tích hợp lịch sử học, xác nhận DTO có `dailyItemId` và sửa adapter/flow FE tương ứng theo contract thực; không dùng `vocabularyId`/`senseId` thay `itemId`, không tự sửa BE.

### 5. Kiểm chứng và bàn giao

Kiểm tra trong phạm vi:

- 0/1/3 câu, nút đầu/cuối, đổi nghĩa/từ và refetch.
- Câu thứ 2/3 gửi đúng exampleId/referenceText và TTS targetId.
- Chuyển đối tượng khi chờ quyền mic, đang thu, đang chấm, đang tạo audio, gồm A → B → A.
- Lịch sử tách Word/Sentence, hai example và hai sense; trạng thái pending/rỗng/lỗi; đổi tài khoản.
- Mobile/desktop, câu dài, keyboard và dialog scroll/focus nếu có công cụ UI.

Thêm/chạy test có ý nghĩa trong feature, mock HTTP/media khi cần; không gọi AI thật hoặc suite ngoài phạm vi. Chạy `npm run build` sau khi sửa source. Các test hiện có có thể chạy bằng:

```powershell
npm run test -- src/features/vocabulary/__tests__/pronunciationAssessment.test.ts src/features/vocabulary/__tests__/pronunciationSynthesisAudioCache.test.ts
```

Chạy thêm file test mới liên quan nếu có. Báo cáo tiếng Việt: file đổi, hành vi đã hoàn thành, lệnh/kết quả kiểm chứng, phần chờ BE và UI/API thực tế chưa kiểm tra. Không sửa BE, commit hoặc push trong task này.
