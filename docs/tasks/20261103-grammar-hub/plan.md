# Plan: tách hub riêng `/hubs/grammar` cho "Sửa lỗi Ngữ pháp & Từ vựng Realtime"

Mã task: `20261103-grammar-hub` · Loại: feature full-stack, **đổi contract SignalR** · Cần người dùng duyệt trước khi code.

## Hiện trạng

| Thành phần | Vị trí | Vai trò |
| --- | --- | --- |
| `/hubs/tutor` (`TutorConversationHub`) | `Modules/LiveTutor/...Presentation/Hubs/TutorConversationHub.cs` | Giọng nói AI (Gemini Live), `InitializeTutorSession`, `StreamUserAudioToTutor`, `NotifyUserSpeechEnd`, `StopSession` **và** `AnalyzeTurnGrammar` (phân tích lại một câu khi người dùng bấm). |
| Phân tích tự động | `GeminiLiveConnectionManager` khoảng dòng 1797, 1880 và 1961 | Sau mỗi câu nói của học viên, gọi `GrammarCorrectionAnalyzerService.AnalyzeUserUtteranceAsync` (Gemini qua HTTP) và trích lời sửa có trong câu trả lời của giáo viên. |
| Gửi kết quả | `SignalRLiveSessionRealtimeNotifier.SendLanguageCorrectionAsync` | Gửi `ReceiveLanguageCorrection` tới **`_tutorHub`**, group `{userId}:{conversationId}`. |
| `/hubs/transcribe`, `/hubs/translate` | `TranscribeLiveHub`, `TranslateLiveHub` | Hub phụ: `Initialize…Session` (kiểm owner, sau đó `AddToGroupAsync`), server đẩy sự kiện về group. |
| FE | `features/tutor/hooks/useThreeHubLiveSession.ts` (khoảng dòng 340–400), `GrammarCorrectionPanel` | Gọi `hub.invoke('AnalyzeTurnGrammar', …)` trên tutor hub, nghe `ReceiveLanguageCorrection`. Nếu sau 1,6 giây không có kết quả thì tự gọi Gemini trực tiếp ở client. |

Vấn đề: tutor hub vừa lo luồng giọng nói vừa lo sửa ngữ pháp.
- SignalR mặc định xử lý **tuần tự** từng invocation trên một connection. Vì vậy một `AnalyzeTurnGrammar` chậm (gọi Gemini qua HTTP) có thể làm trễ `StreamUserAudioToTutor` của chính connection đó.
- Event sửa ngữ pháp đi chung kênh với audio 24kHz.

## Thiết kế đề xuất

### Contract mới

| Loại | Tên | Ghi chú |
| --- | --- | --- |
| Route | `/hubs/grammar` → `GrammarCorrectionHub` (`RequireAuthorization`) | Đăng ký trong `Program.cs` cạnh 3 hub hiện có. |
| Method | `InitializeGrammarSession(StartSubHubSessionRequest request)` | Giống `InitializeTranslateSession`: lấy `userId` từ claims, kiểm owner của conversation/session, rồi thêm connection vào group riêng `grammar:{userId}:{conversationId}`. Không phải owner thì báo lỗi theo mẫu của hub phụ. |
| Method | `AnalyzeTurnGrammar(string conversationId, string turnId, string transcriptText, string cefrLevel)` | **Chuyển** từ tutor hub sang. Giữ nguyên logic owner (`AssertSessionOwner`). Thêm giới hạn độ dài `transcriptText` và rate limit theo user, vì method này gọi AI (luật 5). |
| Event | `ReceiveLanguageCorrection` (payload `LanguageCorrectionDto` giữ nguyên) | Gửi qua **grammar hub** thay vì tutor hub. |
| Bỏ khỏi `/hubs/tutor` | method `AnalyzeTurnGrammar`, event `ReceiveLanguageCorrection` | Đổi contract. BE và FE sửa trong cùng task (luật 10). |

### BE

- `SignalRLiveSessionRealtimeNotifier.SendLanguageCorrectionAsync` dùng `IHubContext<GrammarCorrectionHub>` và group có tiền tố `grammar:`. Interface `ILiveSessionRealtimeNotifier` giữ nguyên chữ ký.
- Phân tích tự động vẫn **chạy ở server** trong `GeminiLiveConnectionManager`, vì transcript gốc nằm ở server, không cần FE gửi lên. Chỉ đổi kênh gửi kết quả.
- Có Redis backplane (ADR-0004), nên group của hub mới tự hoạt động khi scale out.
- Test:
  - hub mới: owner thì join được; user khác thì không join được và không nhận event (test chéo user A/B);
  - `AnalyzeTurnGrammar` bị từ chối khi không phải owner;
  - notifier gửi đúng hub và đúng group;
  - tutor hub không còn method `AnalyzeTurnGrammar`;
  - test kiến trúc của module không bị vi phạm.

### FE

- `useThreeHubLiveSession` mở thêm connection thứ 4 tới `/hubs/grammar`:
  - gọi `InitializeGrammarSession` khi phiên bắt đầu;
  - đăng ký `ReceiveLanguageCorrection` trên connection này;
  - `analyzeTurnGrammar` gọi `invoke` trên grammar hub;
  - dừng và dọn connection khi kết thúc phiên.
- Có thể đổi tên hook thành `useLiveSessionHubs`, hoặc giữ tên cũ để diff nhỏ. Đề xuất: **giữ tên**.
- Hiển thị trạng thái "Hub 4: grammar" cùng 3 hub hiện có trong dòng trạng thái (header Tutor).
- Phương án dự phòng 1,6 giây ở client: **giữ nguyên hành vi**.
- `GrammarCorrectionPanel` không đổi giao diện.
- Cập nhật contract trong `openapi.types.ts` hoặc tài liệu hub nếu đang ghi tên method/event.
- Test hook:
  - grammar hub được khởi tạo khi phiên bắt đầu;
  - `analyzeTurnGrammar` gọi đúng hub;
  - sự kiện từ grammar hub được đưa vào panel.

## Rủi ro

- Thêm một kết nối WebSocket cho mỗi phiên (4 thay vì 3). Chi phí nhỏ.
- Nếu grammar hub mất kết nối thì chỉ phần sửa ngữ pháp bị ảnh hưởng; FE còn phương án dự phòng ở client. Luồng giọng nói không bị ảnh hưởng. Đây là lợi ích chính của việc tách hub.
- Không có migration, không đụng dữ liệu.

## Phân công và kiểm chứng

- **Worker:** AGY cho cả BE và FE, theo yêu cầu "phối hợp với agy". Dự phòng: Codex.
- **Kiểm chứng:**
  - `dotnet build`/`dotnet test`, `npm run build`, `npx vitest run --maxWorkers=2`;
  - chạy thật trên BE tạm: kết nối `/hubs/grammar` bằng client SignalR, kiểm tra join theo owner và kiểm tra user khác bị từ chối;
  - phiên Gemini Live thật cần API key của người dùng, nên ghi là phần chưa kiểm chứng.
- **Review:** security-reviewer (hub mới, owner, rate limit) chạy song song performance-reviewer (realtime), sau đó code-reviewer.

---

## Mở rộng v2 (2026-11-04): phân tích theo ngữ cảnh, câu "hoàn hảo" và audio khi bấm

Người dùng đã duyệt ngày 2026-11-04, kèm hai quyết định:
- **Lưu kết quả phân tích vào DB:** thêm cột bằng migration, chỉ thêm cột. Người dùng tự apply migration.
- **Audio:** chỉ cache trong bộ nhớ của phiên, không lưu DB.

### Vấn đề hiện tại (người dùng báo)

1. Thẻ "Speaking & Fluency Feedback" có câu "nâng cấp" trùng y câu gốc.
   - Nguồn: nhánh dự phòng "Guaranteed Speaking Feedback Card" trong `GrammarCorrectionAnalyzerService.AnalyzeUserUtteranceAsync`. Khi bước gọi Gemini không ra kết quả, nhánh này trả `PolishSpokenSentence(cleaned)`. Hàm đó chỉ có vài regex, nên câu thường không đổi.
   - Bước gọi Gemini dễ thất bại: dùng 3 model tự dò từ API key, timeout 10 giây, và khi lỗi không ghi log lý do.
2. Nút nghe câu sửa (`LiveTutorPage.tsx` `speakCorrectedSample`) dùng `window.speechSynthesis` của trình duyệt, không dùng TTS của BE.

### Thiết kế

**Phân tích (server):**
- Dữ liệu vào gồm:
  - câu nói cuối của học viên (transcript ở server);
  - ngữ cảnh: câu hỏi gần nhất của giáo viên AI và khoảng 6 lượt gần nhất, cắt gọn khoảng 2000 ký tự;
  - CEFR, chế độ luyện, chủ đề.
  FE **không** gửi ngữ cảnh lên.
- Gọi **một** model văn bản lấy từ cấu hình (mục mới `Ai:LiveTutor:CorrectionModelCode`, mặc định `gemini-3.8-flash`). Đặt `responseMimeType = application/json` kèm `responseSchema`, `temperature` khoảng 0.2. Timeout lấy theo `Ai:LiveTutor:GrammarCorrectionTimeoutSeconds`, nên nâng mặc định lên khoảng 20 giây.
- Kết quả (`LanguageCorrectionDto` thêm field mới; giữ các field cũ để tương thích):

  | Field | Ý nghĩa |
  | --- | --- |
  | `status` | `has_errors`, `can_improve` hoặc `already_natural` |
  | `correctedText` | Sửa tối thiểu: chỉ sửa lỗi, giữ nguyên ý. Rỗng nếu không có lỗi |
  | `improvedText` | Câu tốt nhất theo ngữ cảnh: tự nhiên, trả lời đúng câu hỏi của giáo viên, phù hợp CEFR. Rỗng nếu câu đã tự nhiên |
  | `issues` | Danh sách `{ original, fix, category, explanationVi }`, tối đa 5 mục |
  | `explanationVi`, `cefrTip` | Giữ như cũ |

- Kiểm tra sau khi parse: nếu `correctedText` hoặc `improvedText` trùng câu gốc (so sau khi chuẩn hóa khoảng trắng, dấu câu và chữ hoa/thường) thì đặt rỗng. Nếu cả hai rỗng và không có lỗi thì `status = already_natural`.
- **Bỏ** thẻ dự phòng giả "Guaranteed Speaking Feedback Card". Khi AI lỗi:
  - **không** gửi lời sửa bịa ra;
  - gửi `status = analysis_failed`, kèm câu gốc, để FE hiện nút "Phân tích lại".
- Vẫn giữ quy tắc deterministic hiện có cho các lỗi rõ ràng, và chỉ dùng nó **khi AI lỗi**. Ngoài ra, ghi log lý do lỗi (mã HTTP / timeout / parse), không log nội dung.
- Lời sửa trích từ câu trả lời của giáo viên (`TryExtractCorrectionFromTeacherReply`) giữ nguyên, nhưng cũng áp quy tắc "không trùng câu gốc".

**Lưu DB:**
- Bảng `LanguageCorrections` thêm cột: `Status` (nvarchar 32, null), `ImprovedText` (nvarchar(max), null), `IssuesJson` (nvarchar(max), null).
- Migration mới `AddCorrectionFeedbackFields_LiveTutor`, **chỉ AddColumn**, không biến đổi dữ liệu cũ. **Không apply**; người dùng tự apply.
- API lịch sử hội thoại (DTO chi tiết conversation) trả thêm các field mới; FE đồng bộ type.

**Audio khi bấm (hub method mới):**
- `RequestFeedbackAudio(string conversationId, string correctionId, string variant)` trên `GrammarCorrectionHub`, với `variant` thuộc `corrected` hoặc `improved`. Method:
  - kiểm owner;
  - lấy câu tương ứng từ correction đã lưu hoặc đang có trong phiên, **không** nhận text từ client;
  - gọi TTS sẵn có (`ILivePronunciationTtsSynthesizer`) bằng API key của phiên và giọng/accent/tốc độ của phiên;
  - trả về `{ mimeType, audioBase64, durationMs }` qua giá trị return của `invoke`.
- Cache trong bộ nhớ theo `(conversationId, correctionId, variant)`, xóa khi phiên kết thúc. Có rate limit riêng, ví dụ 20 lần/phút/user. Timeout lấy từ `Ai:PronunciationTts`.
- Phiên đã kết thúc (mở từ lịch sử, không còn API key trên server): trả lỗi rõ ràng. FE chuyển sang gọi `POST /api/pronunciation/synthesize` với `text` + API key của người dùng (endpoint này đã hỗ trợ câu tùy ý, không lưu cache).

**FE:**
- `GrammarCorrectionPanel` và modal chi tiết:
  - hiện câu gốc;
  - "Câu sửa" (nếu có), có đánh dấu từng `issues`;
  - "Câu tự nhiên hơn" (nếu có);
  - nhãn status;
  - nút "Phân tích lại" khi `analysis_failed`.
  - Không hiện khối nào có nội dung trùng câu gốc.
- Nút loa của "Câu sửa" và "Câu tự nhiên hơn":
  - gọi `RequestFeedbackAudio` (có trạng thái đang tải; bấm lần sau dùng cache phía client theo `correctionId`/`variant`);
  - lỗi thì báo qua toast;
  - **bỏ** `window.speechSynthesis`.
- Audio chỉ phát khi người dùng bấm, không tự phát.

### Kiểm chứng bổ sung

- Test cho các trường hợp sau:
  - parse JSON có `responseSchema`;
  - quy tắc không trùng câu gốc;
  - không còn thẻ dự phòng giả;
  - `analysis_failed` khi provider lỗi;
  - ngữ cảnh được ghép từ các lượt gần nhất;
  - `RequestFeedbackAudio`: owner (test chéo user), cache, rate limit, `variant` không hợp lệ.
- Migration chỉ có AddColumn; snapshot model khớp.
- Chưa kiểm chứng được với Gemini thật vì cần API key của người dùng.
