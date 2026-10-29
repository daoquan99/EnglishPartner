# Report: Daily tự khởi tạo, số câu ví dụ, tạo từ vựng từ chữ bôi đen

- Mã task: `20261029-targeted-vocabulary` · Plan: [plan.md](plan.md) (người dùng duyệt 2026-10-29)
- Worker: AGY (D1, D1 fix 1, D2 — High; D2 fix 1 — Medium). Trạng thái: trên working tree chính, **chưa commit** (base `006f5c2`).

## Kiểm chứng (working tree chính)

| Lệnh | Kết quả |
| --- | --- |
| BE `dotnet build` | 0 lỗi |
| BE `dotnet test` | 388 pass / 6 fail (baseline `PronunciationSynthesisAudioCacheTests`) |
| FE `npm run build` | pass |
| FE `npx vitest run` | 228/228 (trước 185) |

Chưa kiểm chứng: chuột phải/selection trên trình duyệt thật, gọi Gemini/AGY thật, SQL Server (race unique plan).

## Đã làm

- **BE:** `IDailyVocabularyPlanInitializer` dùng chung cho Initialize và writer; generate/import bật "Thêm vào Daily" mà chưa có plan → tạo plan theo settings cùng một commit, xử lý race unique `(UserId, LearningDate)`; field mới `examplesPerSense` (1–5), `targetTerms` (≤ 10, ≤ 80 ký tự, ≤ 6 từ), `sourceSentence` (≤ 500) cho generate và prompt thủ công; `examplesPerSense?` cho import (không truyền → giữ tối đa 5); prompt 3 nhánh, chữ người dùng JSON-encode trong khối dữ liệu; parser cắt ví dụ, lọc mục không khớp, chế độ câu luôn có câu gốc làm ví dụ, không còn mục nào → lỗi rõ ràng; `IVocabularyAiGenerator` nhận một options record.
- **FE:** sửa lỗi có sẵn gửi `count` thay vì `requestedCount`; Daily tự khởi tạo một lần, không toast 404 giả; dialog Generate có số câu ví dụ, checkbox Daily gửi cho cả direct và manual; `SelectionVocabularyMenu` (chỉ Student Portal, chỉ khi có bôi đen hợp lệ, Shift → menu gốc) và `TargetedVocabularyDialog` (từ/cụm hoặc câu, CEFR, số ví dụ, topic, N, provider/model, Daily, chế độ thủ công khi thiếu key).

## Còn lại

- `Ai:VocabularyGeneration:TimeoutSeconds = 3000` (50 phút) quá dài cho endpoint AI (Luật 5) — nên giảm.
- `GeminiVocabularyAiGenerator` vẫn đặt API key trong query string `?key=`.
- Lỗi unique khác (ví dụ trùng `NormalizedWord`) cũng đi vào nhánh reload plan; không tìm thấy plan thì ném lại lỗi gốc.
