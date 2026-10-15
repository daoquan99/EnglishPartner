# Prompt FE: chuyển câu ví dụ, lịch sử và trạng thái thao tác

Trạng thái: prompt triển khai, **chưa thực thi**. Baseline source kiểm tra ngày 2026-10-15; đọc lại contract trước khi làm. [Prompt BE đi kèm](pronunciation-learning-history-be.md).

## Prompt để giao cho agent FE

Làm việc tại `D:\Projects\EnglishPartner\EnglishPartner.FE`.

Triển khai trong trang **Từ Vựng Hằng Ngày & Chấm Phát Âm AI** (menu **Từ Vựng & Chấm Phát Âm**): chọn các câu ví dụ khác nhau trong **Phòng Chấm Phát Âm Audio AI**, xem lịch sử của từ/câu hiện tại, đồng thời sửa loading và logic disable của các button trên trang và các dialog thuộc trang.

### Phạm vi và source

Đọc [AGENTS.md](../../AGENTS.md), skill Student Portal và design system liên quan, cùng `git status --short`. Giữ nguyên thay đổi đang có. Chỉ sửa FE thuộc yêu cầu này; không sửa BE hoặc thực hiện refactor ngoài phạm vi.

Đọc:

- `src/features/vocabulary/pages/DailyVocabularyPage.tsx`
- `src/features/vocabulary/api/vocabularyApi.ts`
- `src/features/vocabulary/hooks/useDailyVocabulary.ts`
- `src/features/vocabulary/types/vocabulary.types.ts`
- `src/shared/api/openapi.types.ts`
- `src/shared/components/ui/Button.tsx`
- `src/shared/components/ui/IconButton.tsx`
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

### 5. Loading và disable theo đúng thao tác

Phần này được bổ sung sau khi kiểm tra code FE theo yêu cầu về nút “nghe câu mẫu” và các button còn lại. Chỉ sửa trong trang/feature này; không biến thành đợt chuẩn hóa button toàn ứng dụng hoặc thay đổi Admin Portal.

#### Bằng chứng source hiện tại

- Các nút nghe IPA từ, câu preview mặt trước, danh sách ví dụ mặt sau và Mẫu US/UK trong phòng chấm cùng dùng `synthesizeMutation.isPending` để disable. Nhiều nút vẫn giữ icon/label tĩnh; spinner IPA chỉ kiểm tra `ttsAccent === acc`, nên bấm nghe câu US cũng có thể làm nút IPA từ US hiển thị loading sai. Xem [nhóm IPA và preview](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L623), [danh sách ví dụ](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L780), [phòng chấm](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L940).
- `handleSynthesizeTeacherSpeech` không có guard chống gọi lặp ngay đầu handler. Mutation pending chỉ phản ánh HTTP, không theo dõi toàn bộ phát audio. `play()` bị gọi bằng `void`, nhánh `new Audio(...)` không giữ player trong ref dùng chung, fallback speechSynthesis không có lifecycle state. Xem [handler TTS](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L208).
- `startRecording` chỉ guard `assessMutation.isPending`; chưa có pending khi chờ getUserMedia. Sau stop, `isRecording=false` trước khi Blob → Base64 và mutation bắt đầu, tạo khoảng trống có thể bấm thu lại. Lỗi quyền mic/MediaRecorder và chuyển đổi Base64 chưa được bao trọn bởi try/catch/finally. Xem [luồng thu âm](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L407).
- Bốn nút Again/Hard/Good/Easy không dùng `reviewMutation.isPending` để khóa/chỉ báo thao tác. `handleReview` còn đánh dấu hoàn thành trong catch và chuyển thẻ dù request lỗi. Xem [handleReview](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L283), [nhóm rating](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L831).
- Dialog tạo từ có label pending cho generate/import và spinner cho build prompt, nhưng các nút chỉ xét mutation riêng, vẫn đổi mode được khi request chạy. Nút Hủy bị disable cho generate/import trong khi X, Escape và backdrop vẫn gọi onClose không điều kiện. Copy Prompt có trạng thái đã chép nhưng chưa có pending/lỗi rõ ràng. Xem [dialog tạo từ](../../src/features/vocabulary/pages/DailyVocabularyPage.tsx#L1220).
- Shared `Button`/`IconButton` hiện chưa có API loading riêng. Không được dùng prop `isLoading`/`loading` như thể đã tồn tại; nếu cần thêm phải triển khai tương thích trước, chỉ chuyển các caller trong phạm vi.

Các line là mốc baseline, phải đọc lại symbol khi source thay đổi. Phần sửa trạng thái thao tác là công việc FE, không cần đợi API lịch sử mới của BE.

#### Phản hồi đúng nút và đúng request

- Nút khởi tạo thao tác bất đồng bộ có spinner và nhãn phù hợp: “Đang tải audio…”, “Đang mở micro…”, “Đang xử lý bản thu…”, “Đang chấm điểm…”, “Đang lưu…”, “Đang tạo prompt…”, “Đang sao chép…”. Không khẳng định đang tạo audio mới vì response có thể đến từ cache.
- Chỉ nút/trigger khởi tạo request hiển thị loading. Các nút cùng nhóm bị khóa để tránh xung đột không giả vờ đang thực hiện request riêng. Với control nhỏ, giữ accessible name rõ đối tượng/accent và `aria-busy` phù hợp.
- Định danh thao tác TTS bằng source/trigger ID (IPA, preview, example row, studio), vocabularyId, senseId, targetType, targetId/text, accent, voice, model và speed đã chụp tại click. Chỉ accent hoặc index không đủ phân biệt nút.
- Giữ kích thước button ổn định; disabled có style rõ, không còn hiệu ứng hover/nhãn gợi ý bấm được. Không gắn spinner cho nút lật thẻ, mở cấu hình hoặc đóng dialog vốn chỉ là thao tác đồng bộ.
- Guard ngay trong handler bằng trạng thái/lock đồng bộ trước await để chống double-click hoặc hai nút gọi liên tiếp trước khi React render pending. Đừng chỉ dựa vào disabled trong JSX.
- Trạng thái chuẩn bị/request/phát audio/kết thúc là khác nhau. HTTP hoàn thành phải kết thúc loading request; audio đang phát có chỉ báo “Đang phát” riêng, không quay spinner suốt bản audio. Không thêm phần trăm tiến độ nếu API không cung cấp.

#### Ma trận khóa theo nhóm thao tác

| Trạng thái | Loading và khóa cần thiết | Thao tác vẫn dùng được |
| --- | --- | --- |
| Đang tải/tạo audio mẫu | Loading đúng trigger; khóa nhóm nút nghe mẫu trên cả hai cột và bắt đầu thu âm khi request chưa xong | Xem nội dung, lật thẻ, mở lịch sử; đổi mục tiêu được phép qua cleanup/invalidate session, không phát response cũ |
| Audio mẫu đang phát | Chỉ báo đang phát; bấm mẫu khác thì dừng mẫu cũ trước, không phát chồng | Bắt đầu thu được phép sau khi dừng audio và speechSynthesis đang phát |
| Đang chờ quyền mic hoặc đang thu | Khóa bắt đầu bản thu mới, các nút nghe mẫu và nhóm rating có thể hoàn tất/chuyển thẻ | Dừng thu khi recorder sẵn sàng; đổi mục tiêu/rời trang hủy session an toàn; không disable nút dừng/hủy vì cờ busy chung |
| Đang dừng/xử lý Blob/Base64 | Khóa thu lại và rating liên quan từ trước recorder.stop đến khi xử lý xong; không có khoảng trống giữa các phase | Xem nội dung/lịch sử; đổi mục tiêu hủy và bỏ qua kết quả session cũ |
| Đang chấm điểm | Loading đúng thao tác thu/chấm, khóa thu/chấm lặp và rating trên session đang xử lý | Xem nội dung, lịch sử, nghe mẫu khi microphone đã tắt; đổi mục tiêu phải chặn cập nhật từ request cũ |
| Đang lưu rating | Loading đúng rating vừa bấm; khóa cả 4 rating và thay đổi thẻ/mục tiêu cho đến khi lưu thành công/lỗi để tránh chuyển hai lần | Xem nội dung/lật thẻ/xem lịch sử; không khóa toàn trang |
| Đang generate/import/build prompt | Loading đúng action; khóa các action ghi/generate/import/build và mode/input làm đổi bộ dữ liệu request trong cùng dialog | Đóng dialog theo chính sách bên dưới; tác vụ xem không liên quan không bị khóa toàn trang |
| Đang tải lịch sử | Loading trong vùng lịch sử hoặc nút tải thêm; khóa request trang lặp trong cùng query | Đóng lịch sử và các thao tác luyện tập hợp lệ; không khóa nút nghe vì query lịch sử đang chạy |

Không tạo một `isBusy` tổng hợp để disable tất cả button trong trang. Tái dùng state theo nhóm tài nguyên đang xung đột. Nếu cần điều chỉnh ma trận do contract thực tế, giải thích cụ thể ở handoff; giữ nguyên các bất biến không gửi lặp, không phát audio vào mic và không cập nhật sai đối tượng.

#### Lifecycle và lỗi phải mở khóa đúng

- Quản lý một player được giữ trong ref ngay cả khi audio controls trong dialog chưa mount. Theo dõi play/ended/pause/error và xử lý Promise rejection của play (ví dụ autoplay bị chặn). Không coi HTTP thành công là audio đã phát thành công.
- SpeechSynthesis fallback cũng có start/end/error/cancel tương ứng; dừng khi bắt đầu thu/đổi mục tiêu/unmount. Cảnh báo thiếu API Key/backend phải đúng fallback thực tế, không nói đã phát nếu playback thất bại.
- Không disable nghe mẫu chỉ vì thiếu API Key: audio đã cache vẫn có thể nghe không cần key theo contract hiện có. Chỉ yêu cầu key khi luồng cần provider thật; không thêm ô nhập key khác.
- Mic permission denied, thiết bị không có, MediaRecorder lỗi, Base64 lỗi, HTTP lỗi/timeout và audio lỗi đều giải phóng tài nguyên và trả trạng thái về hợp lệ. Pending phải được cleanup bằng finally/event lifecycle phù hợp.
- Cleanup/finally của request cũ chỉ được giải phóng state do chính request đó sở hữu; không xóa spinner/lock/player của request mới. Kiểm tra cả A → B → A và đóng/mở dialog.
- Với rating, lưu snapshot ID/rating và cập nhật theo ID, không chỉ index. Chỉ đánh dấu completed, invalidate lịch sử và chuyển thẻ một lần sau success của request hợp lệ. Lỗi giữ thẻ/rating để thử lại, hiện lỗi thật; bỏ hành vi giả completed trong catch. Phụ thuộc route/DailyItemId ở mục 4 vẫn phải được báo, không che lỗi BE bằng trạng thái thành công FE.
- Dialog tạo từ: thống nhất footer/X/Escape/backdrop là **đóng giao diện**, không tuyên bố hủy request. Khi request còn chạy, giữ trạng thái ở page để mở lại vẫn thấy pending; khóa gửi lại và đổi mode/input của operation đó. Nếu giữ chữ “Hủy”, phải có hủy thật theo contract; nếu không, đổi thành “Đóng”.
- Kết quả/lỗi generate/import đóng rồi mở lại không được ghi đè form mới hoặc đóng một dialog session mới ngoài ý muốn. Nếu nhận kết quả khi dialog đã đóng, báo thành công/lỗi tại page; tránh tự nhảy thẻ đang luyện, cho người dùng chủ động tới bộ từ mới. Không thêm endpoint cancel BE trong task này.
- Copy Prompt có guard pending, phản hồi lỗi clipboard và cleanup timeout “Đã chép” khi unmount/đổi session; không khóa các tác vụ không liên quan.

### 6. Kiểm chứng và bàn giao

Kiểm tra trong phạm vi:

- 0/1/3 câu, nút đầu/cuối, đổi nghĩa/từ và refetch.
- Câu thứ 2/3 gửi đúng exampleId/referenceText và TTS targetId.
- Chuyển đối tượng khi chờ quyền mic, đang thu, đang chấm, đang tạo audio, gồm A → B → A.
- Lịch sử tách Word/Sentence, hai example và hai sense; trạng thái pending/rỗng/lỗi; đổi tài khoản.
- Bấm nghe câu preview/example row/studio US hoặc UK: chỉ đúng trigger hiện spinner; nút IPA cùng accent không loading sai. Các nút nghe xung đột bị disable với style đúng và không phát sinh request thứ hai khi double-click.
- Với HTTP trả chậm, cache hit, lỗi/timeout, audio ended/error/play rejection và fallback trình duyệt: spinner kết thúc đúng phase, state được mở khóa, không có audio chồng hoặc request cũ mở khóa request mới.
- Giữ Promise getUserMedia chưa resolve, rồi click thu lần nữa hoặc đổi câu; chỉ một session hợp lệ, không rò stream. Lỗi mic/Base64 cũng mở khóa. Dừng thu không cho bấm bản thu mới trong khoảng chờ onstop/Base64.
- Khi đang thu, nghe mẫu/rating không chạy; dừng/hủy luôn truy cập được. Khi đã chấm và mic đã tắt, thao tác không xung đột vẫn dùng được theo ma trận.
- Double-click một rating hoặc bấm nhanh hai rating: chỉ một request; spinner đúng rating; success chuyển một lần, lỗi không đánh dấu completed/không chuyển thẻ. Refetch không làm cập nhật nhầm index.
- Generate/import/build prompt không chạy chồng qua đổi mode; footer/X/Escape/backdrop cùng chính sách đóng; mở lại thấy đúng pending, kết quả cũ không làm mất form mới. Copy thất bại có thông báo và không kẹt pending.
- Mobile/desktop, câu dài, keyboard và dialog scroll/focus nếu có công cụ UI.

Thêm/chạy test có ý nghĩa trong feature, mock HTTP/media khi cần; không gọi AI thật hoặc suite ngoài phạm vi. Chạy `npm run build` sau khi sửa source. Các test hiện có có thể chạy bằng:

```powershell
npm run test -- src/features/vocabulary/__tests__/pronunciationAssessment.test.ts src/features/vocabulary/__tests__/pronunciationSynthesisAudioCache.test.ts
```

Chạy thêm file test mới liên quan nếu có. Test adapter hiện có không chứng minh loading/disabled/lifecycle của trang đã đúng; cần kiểm tra tương tác ở mức phù hợp với tooling thực tế, không tự cài thêm framework chỉ để test. Nếu chưa thể chạy UI, ghi các case mới chưa kiểm tra rõ ràng.

Báo cáo tiếng Việt: file đổi, hành vi đã hoàn thành, ma trận loading/disable thực tế, lệnh/kết quả kiểm chứng, phần chờ BE và UI/API thực tế chưa kiểm tra. Không sửa BE, commit hoặc push trong task này.
