# Plan: bỏ padding lồng nhau ở Student Portal

Mã task: `20261103-student-padding` · Loại: UI-only FE · Worker: AGY · Không đổi contract.

## Bằng chứng (đo ngày 2026-11-03)

Đo bằng script audit (`%TEMP%/perf/ui-audit/audit.mjs`, phần `nesting`): với mỗi phần tử chữ trong `main`, cộng dồn `padding-left` + `border-left` của các lớp cha tính từ `main`.

Ở khổ 360px, trước khi sửa, chữ trong card đầu tiên cách mép màn hình khoảng 57–75px. Vùng nội dung chỉ còn khoảng 210–245px.

| Trang | Chuỗi padding trước khi vào nội dung |
| --- | --- |
| `/student/vocabulary-games/falling-words` | `main p-4` (16) + `max-w-3xl mx-auto px-4` (16) + card `p-6 sm:p-8` (24) |
| `/student/vocabulary-games/falling-words/history` | `main p-4` + `max-w-4xl mx-auto px-4` (16) |
| `/student/lessons` | `main p-4` + `max-w-6xl mx-auto px-4` (16) + card `p-6` (24) |
| `/student/lesson-history` | `main p-4` + `max-w-4xl mx-auto px-4` + card `p-5` (20) |
| `/student/lessons/:id`, `/student/lesson-attempts/:id` | `main p-4` + `max-w-4xl mx-auto px-4` + card `p-4` |
| `/student/progress` | `main p-4` + `max-w-6xl mx-auto px-4` |
| `/student/grammar`, `/student/grammar/:code`, `/student/grammar/my-exercises`, `/student/grammar/attempts/:id` | `main p-4` + wrapper `p-4 sm:p-6 lg:p-8 max-w-5xl mx-auto` (16→24→32), ở một số nhánh là `p-6` |
| `/student/tutor` | `main p-4` + card + header `px-5` (20) |
| `/student/vocabulary` | `main p-4` + card `p-4 sm:p-6` |

Lỗi gốc: shell `main` (`StudentLayout`) đã có `p-4 sm:p-6`, nhưng nhiều trang **lại thêm padding ngang ở wrapper gốc**. Thêm vào đó, card dùng `p-6`/`p-8` ngay cả trên mobile.

## Quy ước padding (áp dụng cho Student Portal)

| Lớp | Padding ngang |
| --- | --- |
| Shell `main` (`StudentLayout`) | `p-4 sm:p-6`, giữ nguyên. **Duy nhất lớp này** tạo gutter cho trang. |
| Wrapper gốc của trang (`max-w-* mx-auto`) | **Không có `px-*` hoặc `p-*`.** Chỉ giữ `max-w-*`, `mx-auto`, khoảng cách dọc (`space-y-*`, `gap-*`) và nếu cần thì `py-*` nhỏ. Mọi nhánh return (loading, error, empty) phải nhất quán. |
| Card cấp 1 (`bg-white rounded-3xl border`) | `p-4 sm:p-6`. Card hero có thể dùng `p-5 sm:p-8`. Không dùng `p-6`/`p-8` cố định trên mobile. |
| Khối con trong card (panel `bg-slate-50 rounded-2xl` …) | `p-3 sm:p-4` |
| Lớp thứ 3 trở đi | Hạn chế. Nếu có thì `p-2.5 sm:p-3`, hoặc bỏ nền/viền và dùng `divide-y`/khoảng cách thay cho một khung nữa. |

Mục tiêu đo ở 360px: chữ trong card cấp 1 cách mép màn hình **≤ 34px** (16 + 16 + border). Chữ trong khối con **≤ 48px**.

Desktop: được phép thay đổi phần padding thừa này, vì người dùng yêu cầu sửa lồng padding chung. Bố cục, màu và kiểu chữ giữ nguyên. Các trang Grammar ở `lg` hiện có `main 24 + wrapper 32` trước card; sau khi sửa chỉ còn padding của `main`.

## Phạm vi file (Student Portal)

- `features/grammar/pages/`: `GrammarRoadmapPage`, `GrammarStepPage`, `GrammarAttemptPage`, `MyGrammarExercisesPage` (mọi nhánh return).
- `features/lessons/pages/`: `StudentLessonsPage`, `StudentLessonHistoryPage`, `StudentLessonDetailPage`, `StudentLessonPlayerPage`.
- `features/vocabulary-games/pages/`: `FallingWordsSetupPage`, `FallingWordsHistoryPage`, `FallingWordsPlayPage`.
- `features/progress/pages/ProgressPage` (+ `components`).
- `features/vocabulary/` (`DailyVocabularyPage`, `VocabularyFlashcard`, `PronunciationStudio`) và `features/tutor/` (`LiveTutorPage`, `TutorSetupSidebar`, `LiveMessengerStream`, `GrammarCorrectionPanel`): chỉ sửa card/panel theo quy ước.
- Card và panel con dùng chung trong `src/shared/components/ui` **chỉ sửa nếu chỉ Student dùng**. Nếu Admin cũng dùng thì không sửa (Admin ngoài phạm vi).

## Ngoài phạm vi

Admin Portal, dialog (có quy ước riêng), Login/Register.

## Kiểm chứng

- `npm run build`, `npx vitest run --maxWorkers=2`.
- Chạy lại audit ở 360px và 1440px, so phần `nesting` trước và sau, xem ảnh chụp các trang trong bảng trên.
- code-reviewer.
