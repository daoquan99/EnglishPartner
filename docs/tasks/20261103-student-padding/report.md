# Report: bỏ padding lồng nhau ở Student Portal

Worker: AGY. Orchestrator (Claude) lo đo, lập quy ước, kiểm chứng và gọi review.

## Đã làm

Quy ước áp dụng (chi tiết trong `plan.md`):
- Chỉ `main` của `StudentLayout` (`p-4 sm:p-6`) tạo khoảng lề cho trang.
- Wrapper gốc của trang không có padding ngang.
- Card cấp 1 dùng `p-4 sm:p-6` (riêng hero dùng `p-5 sm:p-8`).
- Khối con trong card dùng `p-3 sm:p-4`.

Có 17 file đổi, đều thuộc Student Portal:
- Grammar: Roadmap, Step, Attempt, MyExercises;
- Lessons: list, history, detail, player;
- Progress: page, ActivityChart, StreakCard, SummaryCard;
- Vocabulary games: Setup, History, Play;
- LiveTutorPage;
- PronunciationStudio.

Admin Portal không đổi.

## Đo (padding ngang cộng dồn từ `main` tới phần tử thụt sâu nhất, đơn vị px)

| Trang | 360 trước → sau | 1440 trước → sau |
| --- | --- | --- |
| tutor | 49 → 50 | 58 → 62 |
| vocabulary | 69 → 50 | 77 → 68 |
| falling-words | 74 → 46 | 90 → 66 |
| falling-words/history | 52 → 36 | 60 → 44 |
| lessons | 57 → 36 | 65 → 49 |
| lesson-history | 53 → 33 | 61 → 49 |
| grammar | 72 → 48 | 104 → 72 |
| grammar/A1-01 | 72 → 56 | 100 → 68 |
| grammar/my-exercises | 56 → 40 | 80 → 48 |
| progress | 81 → 57 | 89 → 73 |
| grammar/attempts/:id | 70 → 50 | 106 → 66 |
| lessons/:id | 74 → 50 | 90 → 74 |
| lesson-attempts/:id | 74 → 50 | 90 → 66 |

Wrapper không còn padding thừa: trong chuỗi padding của các trang chỉ còn lớp của `main`. Các trang còn khoảng 50–57px là ở khối lồng tầng 3 bên trong card (ví dụ bảng công thức và ví dụ trong lý thuyết Grammar, ô chọn ngày trong Progress). Đã xem ảnh, nhìn hợp lý.

Tutor tăng nhẹ (49 → 50, 58 → 62) do header khung hội thoại đổi `px-5` sang `px-4 sm:px-5` cộng thêm viền. Không ảnh hưởng.

## Kiểm chứng

- `npm run build`: pass.
- `npx vitest run --maxWorkers=2`: 393/393 pass.
- Audit Chrome headless ở 360px và 1440px (script `%TEMP%/perf/ui-audit/audit.mjs`, phần `nesting`), đã so ảnh trước và sau.
- code-reviewer: xem phần review bên dưới.

## Sau code review

Kết luận lần 1: ĐẠT CÓ ĐIỀU KIỆN. Đã sửa:
- footer Tutor: giữ `px-4`, nút chính có `min-w-0` và nhãn `truncate`;
- `FallingWordsPlayPage` card "Ready": `p-5 sm:p-8`;
- `DailyVocabularyEmptyState`: `p-4 sm:p-8`;
- khối câu ví dụ trong flashcard: `p-3 sm:p-4`.

Review lần 2 xác nhận cả 4 điều kiện đã đạt.

Hạn chế đã biết: Progress (57px) và Grammar Step (56px) vẫn vượt mục tiêu 48px. Nguyên nhân là `EmptyState` (`px-6`) và `TheoryRenderer` (`p-5`) dùng chung với Admin, nên được giữ nguyên.

Chưa có ảnh kiểm chứng cho: Progress khi có dữ liệu, trang play của Từ vựng rơi, nhánh empty/error của Vocabulary.
