# Việc giao cho AGY: D2 — Daily tự khởi tạo, số câu ví dụ, menu chuột phải tạo từ vựng từ chữ bôi đen (frontend)

Batch D1 (backend) đã làm trong worktree này — giữ nguyên, **không sửa BE**. Đọc trước: `AGENTS.md` (gốc: Luật 1, 10), `EnglishPartner.FE/AGENTS.md`, `EnglishPartner.FE/.agents/design-system.md`, plan `D:/Projects/EnglishPartner/docs/tasks/20261029-targeted-vocabulary/plan.md` (đã duyệt, xem mục "Quyết định đã chốt"), skill `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`, `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`, `.agents/skills/api-contract-sync/SKILL.md`, `.agents/skills/security-audit/SKILL.md`.

Contract BE (đọc để đối chiếu, không sửa): `EnglishPartner.BE/src/EnglishLiveTutor.Application/Common/Models/VocabularyDtos.cs` (`GenerateVocabularyRequest`), `.../ManualVocabularyGenerationDtos.cs` (`BuildVocabularyGenerationPromptRequest`, `ImportVocabularyJsonRequest` — có thêm `examplesPerSense?` 1–5, gửi theo lựa chọn của người dùng khi import), validator trong `.../Features/Vocabulary/**`.

## Phạm vi

Được sửa (dưới `EnglishPartner.FE/src/`): `features/vocabulary/**`, `shared/api/openapi.types.ts` (nếu có type tương ứng), `shared/i18n/locales/{vi,en}.ts`, `app/layouts/StudentLayout.tsx` (chỉ để gắn component menu), test.
Không được sửa: BE, AdminLayout/Admin Portal, `package.json`/lockfile, httpClient, store auth.

## Yêu cầu

1. **Sửa lỗi contract có sẵn.** `GenerateVocabularyRequestDto` FE đang gửi `count`, BE nhận `requestedCount` → số lượng bị bỏ qua. Đổi sang `requestedCount`; thêm `assignToTodayPlan`, `examplesPerSense`, `targetTerms?`, `sourceSentence?` vào DTO generate và prompt thủ công (`BuildVocabularyGenerationPromptRequestDto`), khớp tên/ràng buộc BE (examplesPerSense 1–5; targetTerms ≤ 10, mỗi mục ≤ 80 ký tự, ≤ 6 từ; sourceSentence ≤ 500; hai field loại trừ nhau; khi có sourceSentence thì requestedCount 1–5).
2. **Daily tự khởi tạo.** `DailyVocabularyPage`: khi query today trả 404 với `code: "daily_plan_not_initialized"` → tự gọi `useInitializeDailyPlanMutation` **đúng một lần** cho lần tải đó (ref guard, không lặp khi lỗi); đang khởi tạo hiện loading; lỗi → hiện nút khởi tạo thủ công như hiện tại.
3. **Dialog Generate hiện có** (`AiVocabularyGeneratorDialog`): thêm ô "Số câu ví dụ mỗi nghĩa" (1–5, mặc định 2) cho cả chế độ trực tiếp và thủ công; checkbox "Thêm vào Daily hôm nay" mặc định bật, gửi `assignToTodayPlan` cho **cả** generate trực tiếp (hiện đang không gửi) và import. Sau khi tạo: invalidate query Daily hôm nay và danh sách flashcard.
4. **Menu chuột phải (mới)** — `features/vocabulary/components/SelectionVocabularyMenu.tsx`, gắn một lần trong `StudentLayout`:
   - Lắng nghe `contextmenu` trên document. Chỉ chặn menu gốc (`preventDefault`) khi: có `window.getSelection()` không rỗng sau trim, độ dài 1–500 ký tự, vùng chọn **không** nằm trong `input`/`textarea`/`[contenteditable]`/`select`, và **không** giữ Shift. Ngược lại để menu gốc trình duyệt.
   - Menu gồm: "Tạo từ vựng…", "Tạo và thêm vào Daily hôm nay…", "Sao chép" (Clipboard API, có fallback). Đặt vị trí theo con trỏ, luôn nằm trong viewport; đóng khi Escape, click ngoài, scroll, resize, đổi route. Dùng `role="menu"`/`menuitem`, focus mục đầu, phím mũi tên/Enter.
   - Lưu snapshot chữ đã bôi khi mở menu (không đọc lại selection sau đó).
5. **Dialog tạo từ chữ đã bôi (mới)** — `TargetedVocabularyDialog.tsx`, dùng `Dialog` sẵn có:
   - Chuẩn hóa chữ: trim, gộp khoảng trắng, bỏ dấu câu ở hai đầu. ≤ 6 từ → chế độ "từ/cụm": gửi `targetTerms: [chữ]`. > 6 từ → chế độ "câu": gửi `sourceSentence`, cho chọn N (`requestedCount`, 1–5, mặc định 3). Hiển thị rõ chữ sẽ gửi (escape, không `dangerouslySetInnerHTML`).
   - Tùy chọn: CEFR (mặc định theo learning settings của user), số câu ví dụ (1–5, mặc định 2), topic (tùy chọn), provider/model (tái dùng logic Gemini/AGY và danh sách model như dialog Generate; API key lấy từ store hiện có, **không** thêm ô nhập key mới), checkbox "Thêm vào Daily hôm nay" (bật sẵn nếu mở từ mục "Tạo và thêm vào Daily", tắt nếu mở từ "Tạo từ vựng").
   - Không có key và không dùng AGY → chế độ thủ công: gọi prompt thủ công với cùng tham số → nút Copy prompt → ô dán JSON → import (`assignToTodayPlan` theo checkbox). Có nút mở ô nhập key ở Left Menu (`requestFocusLeftMenuApiKey`).
   - Pending chống bấm lặp; lỗi hiện trong dialog (đọc ProblemDetails `detail`/`error`; 429 → "Bạn đã tạo quá nhiều, thử lại sau ít phút"); thành công → toast "Đã tạo N từ" (+ "và thêm vào Daily" nếu bật), invalidate query Daily + flashcard, đóng dialog.
   - Không log/hiển thị API key.
6. **i18n** vi/en cho mọi nhãn mới (UTF-8 đúng dấu).
7. **Test (Vitest)**:
   - Menu: hiện khi có bôi đen; không hiện khi không bôi đen, khi trong input/textarea/contenteditable, khi giữ Shift (không `preventDefault`); Escape đóng; "Sao chép" gọi clipboard.
   - Dialog: ≤ 6 từ → payload `targetTerms`; câu dài → `sourceSentence` + `requestedCount`; `examplesPerSense`, `assignToTodayPlan` đúng theo mục menu; thiếu key → chế độ thủ công gọi prompt + import với cùng tham số; 429 hiện thông báo.
   - Generate dialog gửi `requestedCount` (không còn `count`) và `assignToTodayPlan`.
   - Daily tự khởi tạo đúng một lần khi 404 `daily_plan_not_initialized`, không gọi khi lỗi khác.

## Lệnh kiểm chứng

Chỉ chạy `npx vitest run <file test liên quan>` trong lúc làm; cuối cùng chạy `npm run build` và `npx vitest run` một lần (trong `EnglishPartner.FE`).

## Ràng buộc

Không `git add`/`stash`/`reset`/`checkout`, không commit/push, không cài package, không chạy BE/API, không sửa ngoài phạm vi, giữ theme/shell hiện có. Ghi rõ phần chưa kiểm chứng trên trình duyệt thật.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–7>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa kiểm tra trên trình duyệt: <danh sách>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
