# Việc giao cho AGY: B9 — tách `DailyVocabularyPage` (frontend performance, không đổi hành vi)

## Mục tiêu

Giảm re-render và độ phức tạp của `src/features/vocabulary/pages/DailyVocabularyPage.tsx` (khoảng 1.940 dòng, 27 `useState`, không `useMemo`) bằng cách tách component/hook, **không đổi hành vi, giao diện hay contract**. Liên quan review P9. Batch B1–B8 đã làm trong cùng worktree — giữ nguyên.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc), `EnglishPartner.FE/AGENTS.md` (mục "Thiết kế và tương tác cần giữ": microphone, audio, request đang chờ khi đổi đối tượng luyện tập)
- Skill: `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`, `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`
- Test hiện có: `src/features/vocabulary/__tests__/*` (dailyLearning, pronunciationAssessment, pronunciationExamplesAndHistory, pronunciationSynthesisAudioCache, manualVocabularyGeneration)

## Phạm vi

Được sửa: `EnglishPartner.FE/src/features/vocabulary/**`.

Không được sửa: file ngoài `features/vocabulary`, API adapter contract, i18n key hiện có (được thêm key mới nếu cần), CSS/token, package.

## Yêu cầu

1. Đọc toàn bộ trang, xác định các vùng độc lập (ví dụ: header/tiến độ, thẻ flashcard, khu luyện phát âm + ghi âm, lịch sử, dialog cài đặt/generate). Tách mỗi vùng thành component trong `src/features/vocabulary/components/` và logic trạng thái liên quan thành hook trong `src/features/vocabulary/hooks/` (ví dụ `usePronunciationRecorder`).
2. State chỉ một vùng dùng → chuyển vào component/hook của vùng đó. Giá trị dẫn xuất tính bằng `useMemo`; callback truyền xuống component con dùng `useCallback`; component con thuần hiển thị bọc `React.memo` khi props ổn định.
3. Giữ nguyên: thứ tự thao tác, text, class Tailwind, phím tắt, cách dừng microphone/audio và hủy request khi đổi từ/câu (response cũ không được gắn vào đối tượng mới).
4. `DailyVocabularyPage.tsx` còn lại chủ yếu là composition, mục tiêu dưới ~400 dòng.
5. Toàn bộ test hiện có trong `src/features/vocabulary/__tests__/` phải pass **không sửa assertion**; chỉ được sửa import đường dẫn nếu test import thứ đã bị di chuyển. Thêm test cho hook mới quan trọng nhất (recorder hoặc phần hủy request khi đổi đối tượng).

## Tiêu chí hoàn thành

- [ ] Hành vi và giao diện không đổi; test cũ pass không sửa assertion.
- [ ] `npm run build` và `npx vitest run` pass.

## Lệnh kiểm chứng

```bash
cd EnglishPartner.FE
npm run build
npx vitest run
```

## Ràng buộc

- Không chạy `git add`, `git stash`, `git reset`, `git checkout` (orchestrator quản lý index). Không commit, push, merge, tạo branch; không cài package; không chạy dev server.
- Nếu một phần quá rủi ro để tách mà không kiểm tra được trên trình duyệt, giữ nguyên phần đó và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa/tạo: <danh sách>
- Đã làm: <component/hook đã tách; số dòng trang trước/sau>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Phần giữ nguyên vì rủi ro: <danh sách hoặc "không">
- Chưa kiểm tra trên trình duyệt: <danh sách>
```
