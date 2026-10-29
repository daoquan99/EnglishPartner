# Việc giao cho AGY: D2 fix 1 — sửa theo review (frontend)

D2 đã làm trong worktree này — giữ nguyên, chỉ sửa các điểm dưới. Phạm vi file như `prompt-agy-d2.md` (FE; được sửa thêm `src/features/vocabulary/api/vocabularyApi.ts`). Không sửa BE.

## Yêu cầu

1. **M1 — không gửi payload BE chắc chắn từ chối.**
   - `lib/targetedVocabulary.ts`: bỏ dấu câu hai đầu bằng Unicode `\p{P}` (cờ `u`), khớp `char.IsPunctuation` của BE (gồm `…`, `—`, `–`, ngoặc kép cong). Đếm từ giống BE (tách theo khoảng trắng sau chuẩn hóa).
   - `SelectionVocabularyMenu`: chỉ mở menu khi văn bản **sau chuẩn hóa** không rỗng; nếu rỗng → để menu gốc trình duyệt.
   - `TargetedVocabularyDialog`: chế độ từ/cụm mà term > 80 ký tự → hiện thông báo rõ ("Từ/cụm quá dài (tối đa 80 ký tự). Hãy bôi đen ngắn hơn.") và khóa nút gửi; term rỗng → khóa.
   - Thông báo lỗi trong dialog (và `onError` ở `DailyVocabularyPage`) lấy qua helper đọc ProblemDetails sẵn có (`extractApiErrorMessage` trong `shared/api/httpClient.ts` hoặc tương đương: `detail` → `error` → lỗi đầu tiên trong `errors` → `title`).
   - Test: bôi `"..."`, `"…"` → không mở menu, không `preventDefault`; token 81 ký tự một từ → nút gửi bị khóa kèm thông báo; `"word—"` → gửi `targetTerms: ["word"]`.
2. **M2 — không toast đỏ 404 khi Daily chưa khởi tạo.** `getTodayPlan` trong `vocabularyApi.ts`: tắt toast lỗi toàn cục (cơ chế `skipErrorToast` hoặc tương đương của httpClient) cho riêng trường hợp 404 `code === 'daily_plan_not_initialized'`; các lỗi khác vẫn toast. Test.
3. **Low:**
   - Một nguồn type: bỏ bản định nghĩa trùng `GenerateVocabularyRequestDto`/`BuildVocabularyGenerationPromptRequestDto`/`ImportVocabularyJsonRequestDto` (giữ một nơi, nơi kia re-export nếu cần); xóa field `count` khỏi type.
   - `SelectionVocabularyMenu`: dọn `setTimeout` focus khi đóng; trả focus về phần tử trước đó khi đóng; `aria-label` và toast lỗi sao chép qua i18n; chuột phải ngoài vùng bôi đen (con trỏ không nằm trong `range.getClientRects()`) → không mở menu, để menu gốc.
   - `TargetedVocabularyDialog`: chuỗi cứng còn lại qua i18n; tab direct/manual có `role="tablist"`/`role="tab"`/`aria-selected`; effect reset chỉ chạy khi dialog **mở** (chuyển false→true), không xóa JSON đã dán khi settings tải xong; khóa đổi tab khi đang gửi; 429 chỉ hiện **một** thông báo (tắt toast toàn cục cho request trong dialog hoặc không hiện banner trùng).
4. i18n vi/en đủ, UTF-8 đúng dấu.

## Lệnh kiểm chứng

`npx vitest run src/features/vocabulary` trong lúc làm; cuối cùng `npm run build` và `npx vitest run` một lần (trong `EnglishPartner.FE`).

## Ràng buộc

Không `git add`/`stash`/`reset`/`checkout`, không commit/push, không cài package, không sửa BE.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–4>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
