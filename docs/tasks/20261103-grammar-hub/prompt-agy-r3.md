# Việc giao cho AGY: grammar hub, đợt 3 (hoàn tất đợt 2 + v2 phân tích theo ngữ cảnh và audio khi bấm)

## Bối cảnh

- Đọc **toàn bộ** `docs/tasks/20261103-grammar-hub/plan.md`, đặc biệt mục **"Mở rộng v2 (2026-11-04)"**. Người dùng đã duyệt, kể cả migration chỉ thêm cột.
- Đọc `docs/tasks/20261103-grammar-hub/prompt-agy-r2.md`. Lượt chạy đợt 2 **bị dừng giữa chừng vì máy thiếu bộ nhớ**. Working tree đang có phần đã làm dở:
  - `GrammarHubRateLimiter` đã dùng `PartitionedRateLimiter`;
  - FE đã có `onreconnected`;
  - có thêm test trong `ConversationOwnershipAndRealtimeLifecycleTests.cs`.
  Kiểm lại từng mục 1–10 của đợt 2: mục nào xong thì xác nhận, mục nào chưa thì làm nốt.
- Luật: `AGENTS.md` (luật 1, 2, 4, 5, 8, 10, 12, 13), `EnglishPartner.BE/AGENTS.md`, `EnglishPartner.FE/AGENTS.md`.
- Skill:
  - `.agents/skills/realtime-session/SKILL.md`
  - `.agents/skills/api-contract-sync/SKILL.md`
  - `.agents/skills/security-audit/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/ai-provider-integration/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Làm trên `development`, **không commit**. Không cài package.

## Ràng buộc môi trường (bắt buộc)

- Máy đang thiếu bộ nhớ:
  - **không** khởi động API;
  - không chạy nhiều lệnh `dotnet` song song;
  - sau khi build/test xong, chạy `dotnet build-server shutdown`.
- **Không** sửa `ModuleBoundaryScanner.cs` hay `ApiContractSnapshotTests.cs` để tìm đường dẫn. Build và test theo cách bình thường; hiện không có BE nào giữ khóa file DLL.
- Không dùng đường dẫn tuyệt đối của máy trong code hay test.
- Migration: tạo bằng `dotnet ef migrations add AddCorrectionFeedbackFields_LiveTutor --context LiveTutorDbContext --project <project Infrastructure của LiveTutor> --startup-project EnglishPartner.BE/src/EnglishLiveTutor.Api --output-dir <thư mục Migrations hiện có của LiveTutorDbContext>`.
  - Migration **chỉ** được có `AddColumn`/`DropColumn`.
  - **Không** chạy `database update`.

## Yêu cầu v2 (chi tiết trong plan)

1. **Phân tích lời nói (BE, `GrammarCorrectionAnalyzerService` và nơi gọi trong `GeminiLiveConnectionManager`):**
   - Truyền ngữ cảnh từ server:
     - câu gần nhất của giáo viên AI;
     - khoảng 6 lượt gần nhất, cắt gọn khoảng 2000 ký tự;
     - CEFR, chế độ luyện, chủ đề.
   - Gọi một model lấy từ cấu hình (`Ai:LiveTutor:CorrectionModelCode`, mặc định `gemini-3.8-flash`, thêm vào `LiveTutorAiOptions` và `appsettings.json`):
     - `responseMimeType: application/json`, `responseSchema`, `temperature` khoảng 0.2;
     - nâng `GrammarCorrectionTimeoutSeconds` mặc định lên 20.
     - Bỏ vòng "tự dò 3 model".
   - Kết quả có thêm `Status`, `ImprovedText`, `Issues` (danh sách `{ original, fix, category, explanationVi }`, tối đa 5 mục). Có quy tắc chống trùng câu gốc.
   - **Bỏ** thẻ "Guaranteed Speaking Feedback Card" và `PolishSpokenSentence`.
   - AI lỗi thì gửi `status = analysis_failed`, không bịa câu sửa. Log lý do lỗi, không log nội dung câu.
   - `TryExtractCorrectionFromTeacherReply` cũng phải áp quy tắc chống trùng câu gốc.
2. **DTO và contract:**
   - `LanguageCorrectionDto` thêm field mới (giữ field cũ).
   - DTO lịch sử hội thoại trả thêm các field này.
   - Đồng bộ `openapi.types.ts` và type ở FE.
3. **DB:**
   - entity và cấu hình EF thêm 3 cột nullable;
   - tạo migration như mô tả ở mục "Ràng buộc môi trường";
   - `SaveCorrectionAsync` lưu các field mới (`IssuesJson` là JSON đã serialize).
4. **Hub method `RequestFeedbackAudio(conversationId, correctionId, variant)`** trên `GrammarCorrectionHub`:
   - `variant` thuộc `corrected` hoặc `improved`;
   - kiểm owner;
   - **lấy text từ server**, không nhận text từ client;
   - TTS dùng `ILivePronunciationTtsSynthesizer` qua một port/Contracts phù hợp. Không được gọi thẳng vào internals của module Speech; nếu cần thêm phụ thuộc giữa module thì đi qua `Contracts` (luật 13). Nếu có vướng mắc kiến trúc thì ghi vào report và đề xuất cách làm;
   - API key, giọng, accent và tốc độ lấy từ phiên;
   - cache trong bộ nhớ, xóa khi phiên kết thúc;
   - rate limit riêng 20 lần/phút/user;
   - trả `{ mimeType, audioBase64, durationMs }`.
   - Khi phiên không còn: ném lỗi có mã rõ ràng (`feedback_audio_session_ended`).
5. **FE:**
   - `GrammarCorrectionPanel` và modal chi tiết trong `LiveTutorPage`:
     - hiện câu gốc, "Câu sửa" (có đánh dấu `issues`), "Câu tự nhiên hơn", nhãn status;
     - có nút "Phân tích lại" khi `analysis_failed`;
     - không hiện khối nào trùng câu gốc.
   - Nút loa:
     - gọi `RequestFeedbackAudio`, có trạng thái đang tải và cache phía client;
     - khi gặp `feedback_audio_session_ended`, chuyển sang `vocabularyApi.synthesizePronunciationAudio` với `text`, kèm API key từ store;
     - lỗi thì báo qua toast;
     - **bỏ** `window.speechSynthesis`.
   - i18n cho cả vi và en.
6. **Test** theo mục "Kiểm chứng bổ sung" trong plan, cộng với các test của đợt 2 nếu còn thiếu.

## Kiểm chứng (chạy tuần tự)

1. `dotnet build EnglishPartner.BE/EnglishLiveTutor.slnx`
2. `dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx`
3. `dotnet build-server shutdown`
4. Trong `EnglishPartner.FE`: `npm run build`, sau đó `npx vitest run --maxWorkers=2`.

## Report

Ghi vào `docs/tasks/20261103-grammar-hub/runs/agy-r3-report.md`:
- trạng thái các mục 1–10 của đợt 2;
- các mục v2;
- contract trước và sau;
- tên migration và các thao tác trong migration;
- các lệnh đã chạy và kết quả;
- những điểm chưa làm hoặc làm khác plan, kèm lý do.
