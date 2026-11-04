# Report: tách `/hubs/grammar` và phân tích lời nói theo ngữ cảnh, kèm audio khi bấm

Plan được người dùng duyệt (tách hub ngày 2026-11-03; v2 ngày 2026-11-04 với quyết định lưu DB, audio chỉ cache trong phiên).

Worker: AGY, 4 đợt; lượt chạy đợt 2 bị dừng giữa chừng vì máy thiếu bộ nhớ. Claude tự sửa các điều kiện cuối của review.

## Đã làm

### Hub và contract

- **Hub mới** `/hubs/grammar` (`GrammarCorrectionHub`, `[Authorize]`) gồm các method:
  - `InitializeGrammarSession`: kiểm owner trong bộ nhớ và trong DB, kể cả conversation đã xóa mềm; rate limit 10 lần/phút;
  - `AnalyzeTurnGrammar`: chuyển từ `/hubs/tutor` sang; giới hạn input (`turnId` ≤ 64, transcript ≤ 2000, CEFR thuộc A1..C2); rate limit 20 lần/phút;
  - `RequestFeedbackAudio(conversationId, correctionId, variant)`: rate limit 20 lần/phút.
- **Event:** `ReceiveLanguageCorrection` chỉ còn gửi qua grammar hub, tới group `grammar:{userId}:{conv}`.
- **Bỏ khỏi `/hubs/tutor`:** method `AnalyzeTurnGrammar` và event `ReceiveLanguageCorrection`.
- Rate limit dùng `PartitionedRateLimiter` (sliding window, phân vùng theo user), là tham số bắt buộc trong DI.

### Phân tích lời nói (v2)

- Ngữ cảnh lấy ở server:
  - bộ đệm `RecentFinalizedTurns`, giữ tối đa 12 lượt đã finalize, không bị `CleanupTurnResources` xóa;
  - phần đưa vào prompt gồm khoảng 6 lượt gần nhất, câu gần nhất của giáo viên, CEFR, chế độ luyện và chủ đề.
- Mỗi lượt nói gọi **một** model lấy từ cấu hình (`Ai:LiveTutor:CorrectionModelCode`, mặc định `gemini-3.8-flash`), với `responseMimeType: application/json` + `responseSchema`; timeout 20 giây.
- Kết quả gồm `status` (`has_errors`, `can_improve`, `already_natural` hoặc `analysis_failed`), `correctedText`, `improvedText` và `issues[]` (tối đa 5).
- Không lời sửa nào được phép trùng câu gốc; quy tắc này áp ở cả 3 đường: kết quả AI, lời sửa trích từ câu trả lời của giáo viên, và quy tắc cố định.
- Đã bỏ thẻ dự phòng giả "Speaking & Fluency Feedback" cùng `PolishSpokenSentence`.
- Khi AI lỗi:
  - trả `status = analysis_failed`;
  - không ghi câu vào `LastAnalyzedTextByTurn` và không lưu DB, nhờ vậy "Phân tích lại" chạy được.
- Kiểm và làm sạch output:
  - `OriginalText` luôn lấy từ câu ở server;
  - các field được cắt cho vừa cột DB;
  - dữ liệu đưa vào prompt được bọc giữa `BEGIN/END DATA`, chuỗi `===` bị loại bỏ và độ dài bị cắt (hàm `D()`).

### Audio khi bấm

- `RequestFeedbackAudio` lấy câu từ server, không lấy từ client.
- TTS đi qua `Speech.Contracts.ISpeechSynthesisService`. Adapter `LiveFeedbackAudioSynthesizer` đặt ở Api host, là nơi được phép ghép các module với nhau.
- Giọng, accent và model TTS lấy từ phiên. FE gửi `accent` và `ttsModelCode` khi khởi tạo và khi kết nối lại.
- Cache trong phiên:
  - lưu `byte[]`, tối đa 30 câu (LRU);
  - single-flight (`Lazy<Task>`): nhiều yêu cầu cùng lúc cho một câu chỉ gọi TTS một lần;
  - câu gửi TTS cắt ở 500 ký tự;
  - dọn khi phiên kết thúc.
- Lỗi chỉ trả mã cố định qua `FeedbackAudioException` (`feedback_audio_session_ended`, `feedback_audio_not_found`, `feedback_audio_no_text`). Phiên của người khác trả **cùng mã** với phiên không tồn tại, để không dò được phiên của người khác.

### DB

- Migration `20261103180227_AddCorrectionFeedbackFields_LiveTutor`: chỉ có 3 `AddColumn` nullable (`Status`, `ImprovedText`, `IssuesJson`). **Chưa apply**; người dùng tự apply.
- `ModulePersistenceGuardTests`: `Up` chỉ cho phép thao tác index và `AddColumn` nullable; `DropColumn` chỉ được nằm trong `Down`. Có thêm test riêng cho migration này.
- Truy vấn chi tiết hội thoại được project sang `ConversationDetailRecord`, có `AsSplitQuery()`.

### FE

- `useThreeHubLiveSession`:
  - mở 4 kết nối;
  - grammar hub tự khởi tạo lại sau khi kết nối lại;
  - khởi tạo song song các hub phụ;
  - không gọi Gemini lần hai khi hub đang connected hoặc đang reconnect;
  - tắt trạng thái loading sau khi `invoke` xong.
- `GrammarCorrectionPanel` và modal hiển thị câu gốc, "Câu sửa" (kèm danh sách lỗi), "Câu tự nhiên hơn", nhãn trạng thái và nút "Phân tích lại".
- `useFeedbackAudio`:
  - gọi `RequestFeedbackAudio`; khi phiên đã kết thúc hoặc nhận 404 thì chuyển sang `POST /api/pronunciation/synthesize`;
  - cache dùng chung tối đa 30 câu; dừng audio khi unmount;
  - **không còn dùng `window.speechSynthesis`** cho câu sửa.

## Kiểm chứng

| Hạng mục | Kết quả |
| --- | --- |
| `dotnet test EnglishLiveTutor.slnx -m:1` | 854/854 pass sau lần sửa cuối của Claude (build 0 lỗi; chỉ còn các cảnh báo nullable cũ trong `GrammarContentPackImporter`) |
| `npm run build` + `npx vitest run --maxWorkers=2` | pass, 417/417 |
| Chạy thật bằng client SignalR trên BE tạm (đợt 1) | Không có token thì bị từ chối; user khác bị "không tìm thấy"; input sai bị từ chối; owner join được và nhận `GrammarHubReady`; lần gọi thứ 21 trong một phút bị chặn; `/hubs/tutor` không còn `AnalyzeTurnGrammar` |
| security-reviewer (lần cuối) | ĐẠT; 2 điểm thấp đã sửa: dùng chung mã lỗi cho phiên lạ, lọc delimiter trong prompt |
| performance-reviewer | ĐẠT CÓ ĐIỀU KIỆN; các điều kiện (gọi AI lần hai, giới hạn cache, "Phân tích lại") đã được xử lý ở đợt 4 |
| code-reviewer (lần cuối) | ĐẠT CÓ ĐIỀU KIỆN; M1 (accent/model TTS), M2 (`AsSplitQuery`) và M3 (loading bị kẹt) đã được Claude sửa |

## Chưa kiểm chứng / nợ kỹ thuật

- Chưa chạy với Gemini và Gemini Live TTS thật (cần API key của người dùng), chưa chạy migration trên SQL Server thật, chưa thử Redis backplane khi có nhiều instance.
- Scanner kiến trúc không quét các file host. Hiện adapter sạch, nhưng chưa có test nào chặn file host import nội bộ của module khác trong tương lai.
- `GetConversationWithHistoryAsync` giờ chỉ còn test dùng; có thể xóa ở task sau.
- Hàm `D()` chưa có test riêng. FE chưa có test cho giới hạn cache 30 câu và cho việc dừng audio khi unmount.
- Nút đọc lại lượt hội thoại trong `LiveMessengerStream` vẫn dùng `speechSynthesis`; nằm ngoài phạm vi task này.
