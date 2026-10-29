# Plan: Daily tự khởi tạo, số câu ví dụ, tạo từ vựng từ chữ bôi đen

- Mã task: `20261029-targeted-vocabulary`
- Ngày: 2026-10-29
- Liên quan: `InitializeDailyVocabularyPlan`, `GenerateVocabulary`, `BuildVocabularyGenerationPrompt`, `ImportVocabularyJson`, `VocabularyGenerationWriter`; ADR-0003 (BYO key); Luật 1, 5, 9, 10.
- **Cần người dùng duyệt:** có — đổi contract (thêm field request), thêm UI toàn cục ở Student Portal.
- Worker: AGY. D1 BE `gemini-3.8-flash-high`; D2 FE `gemini-3.8-flash-high` (UI toàn cục + contract). Worktree riêng từ `006f5c2`.

## Quyết định đã chốt với người dùng (2026-10-29)

1. Vào ngày mới chưa có Daily → **tự tạo** Daily theo settings (không cần bấm nút).
2. Generate/import có checkbox "Thêm vào Daily hôm nay", **mặc định bật**. Bật mà chưa có Daily → tạo Daily theo settings rồi thêm từ. Tắt → chỉ tạo từ vựng.
3. Thêm lựa chọn **số câu ví dụ mỗi nghĩa** (hiện prompt cố định 2).
4. Bôi đen chữ bất kỳ đâu trong **Student Portal** → chuột phải → menu của app (chỉ khi có bôi đen; không ở ô nhập liệu; giữ Shift → menu gốc trình duyệt). Menu: "Tạo từ vựng…", "Tạo và thêm vào Daily…", "Sao chép".
5. Bôi ≤ 6 từ → tạo đúng mục đó. Dài hơn (một câu) → AI chọn N từ/cụm đáng học trong câu (người dùng chọn N), câu gốc dùng làm một ví dụ.
6. Không có API key (và không dùng Antigravity local) → chế độ thủ công: copy prompt cho đúng chữ đã bôi → dán JSON trả về để import.

## Contract (thêm field, tương thích ngược)

| Request | Field mới | Ràng buộc |
| --- | --- | --- |
| `GenerateVocabularyRequest`, `BuildVocabularyGenerationPromptRequest` | `ExamplesPerSense` (int, mặc định 2) | 1–5 |
| như trên | `TargetTerms` (string[]?) | ≤ 10 mục, mỗi mục trim, 1–80 ký tự, ≤ 6 từ, không trùng (không phân biệt hoa thường) |
| như trên | `SourceSentence` (string?) | ≤ 500 ký tự; dùng khi không có `TargetTerms`; `RequestedCount` 1–5 là số mục trích ra |

`TargetTerms` và `SourceSentence` loại trừ nhau. Không có cả hai → hành vi cũ (sinh ngẫu nhiên theo topic). Response không đổi. Đồng bộ `openapi.types.ts`, `vocabularyApi.ts`, query key FE.

## Batch

### D1 — Backend
- Tách logic tạo Daily của `InitializeDailyVocabularyPlanCommandHandler` thành Application service `IDailyVocabularyPlanInitializer` (dùng chung, không handler gọi handler). Handler Initialize dùng service.
- `VocabularyGenerationWriter`: `AssignToTodayPlan` bật và chưa có plan → gọi service tạo plan theo settings (cùng context, cùng một commit) rồi gán từ theo quota. Xử lý race unique `(UserId, LearningDate)`: lỗi trùng khóa → nạp lại plan đã có và gán; không mất từ vựng.
- Prompt: `ExamplesPerSense` thay số "2" cố định; nhánh `TargetTerms` (sinh đúng các mục, giữ nguyên chính tả, có thể nhiều nghĩa phổ biến nhất theo CEFR) và nhánh `SourceSentence` (trích N mục, mỗi mục có câu gốc là một ví dụ). Validator theo bảng trên. Áp dụng cho cả generate trực tiếp và prompt thủ công.
- Parser/validator chấp nhận đúng số ví dụ; không tin tuyệt đối provider (cắt về tối đa `ExamplesPerSense`).
- Test: Daily tự tạo khi bật checkbox (đúng target/CEFR settings, có carry-over/review như Initialize), tắt → không tạo; race tạo plan; validator; prompt chứa đúng term/sentence/số ví dụ; manual prompt cùng nội dung.

### D2 — Frontend
- `DailyVocabularyPage`: 404 `daily_plan_not_initialized` → tự gọi initialize một lần (giữ nút thủ công khi lỗi).
- `AiVocabularyGeneratorDialog`: thêm "Số câu ví dụ mỗi nghĩa" (1–5, mặc định 2); checkbox "Thêm vào Daily hôm nay" mặc định bật (đã có) — gửi cho cả direct và manual.
- Mới: `SelectionVocabularyMenu` gắn ở `StudentLayout` (listener `contextmenu` + `Selection`), vị trí trong viewport, đóng bằng Escape/click ngoài/scroll, có thể dùng bàn phím; bỏ qua input/textarea/contenteditable, Shift → menu gốc.
- Mới: `TargetedVocabularyDialog`: hiển thị chữ đã bôi (≤ 6 từ → "Tạo từ: …"; dài hơn → "Trích N từ từ câu"), chọn CEFR (mặc định theo settings), số ví dụ, topic (tùy chọn), N (khi là câu), provider/model như dialog hiện có, checkbox Daily (mặc định bật theo mục menu). Không có key → tab thủ công (copy prompt → dán JSON → import). Sau khi tạo: toast + invalidate query Daily/flashcard.
- i18n vi/en; test Vitest: menu chỉ hiện khi có bôi đen, không hiện trong input, Shift bỏ qua, payload đúng `TargetTerms`/`SourceSentence`/`ExamplesPerSense`/`AssignToTodayPlan`, chế độ thủ công khi thiếu key, Daily tự khởi tạo một lần.

## Rủi ro

- Rate limit AI (B2) áp dụng cho generate → bôi liên tục có thể bị 429; UI phải báo rõ.
- Chữ bôi có thể chứa nội dung lạ: chỉ đưa vào prompt như dữ liệu (trích dẫn, giới hạn độ dài), không ghép làm chỉ dẫn.
- Chưa kiểm chứng trên trình duyệt thật (selection, chuột phải, mobile long-press không áp dụng).

## Kiểm chứng

BE: `dotnet build` 0 warning mới, `dotnet test` không fail mới ngoài 6 baseline. FE: `npm run build`, `npx vitest run`. Review: security (prompt injection, BYO key, rate limit) ‖ performance → code-reviewer.
