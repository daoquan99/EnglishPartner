# Prompt cho AGY: P3, trang "Tiến độ học" (FE)

Bạn là worker FE. Trước khi làm, đọc các file sau:
- `AGENTS.md`
- `EnglishPartner.FE/AGENTS.md`
- Plan: `docs/tasks/20261102-learning-progress/plan.md`, mục "FE" và "Tiêu chí hoàn thành".
- Skill:
  - `.agents/skills/api-contract-sync/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/student-portal-design/SKILL.md`
  - `EnglishPartner.FE/.agents/skills/frontend-testing/SKILL.md`

BE đã xong. **Không sửa gì trong `EnglishPartner.BE`.** Không commit.

## Contract BE (đã chốt, không đổi)

`GET /api/progress/overview?days=<7..90>`. Mặc định `days=30`; ngoài khoảng 7–90 trả 400 (ProblemDetails). Chưa đăng nhập trả 401. JSON dùng camelCase:

```ts
interface ProgressOverviewDto {
  timeZoneId: string;
  days: number;
  currentStreakDays: number;
  longestStreakDays: number;          // trong 365 ngày gần nhất
  activeDaysInRange: number;
  activity: ProgressDailyActivityDto[]; // đúng `days` phần tử, ngày tăng dần, ngày cuối là hôm nay (theo múi giờ user)
  vocabulary: { studied: number; mastered: number; dueForReviewNow: number; averageMastery: number };
  lessons: { completedAttempts: number; averageScore100: number | null };
  grammar: { levels: { level: string; passedSteps: number; totalSteps: number }[]; currentStepCode: string | null };
  speech: { assessments: number; averageScore: number | null; averageScoreInRange: number | null };
  liveTutor: { sessions: number; sessionsInRange: number };
}
interface ProgressDailyActivityDto {
  date: string; // "yyyy-MM-dd", ngày địa phương của user
  vocabularyReviews: number; lessonAttempts: number; grammarAttempts: number;
  pronunciationAssessments: number; tutorSessions: number;
}
```

Endpoint chỉ cần đăng nhập, không cần permission riêng.

## Việc cần làm

1. **`src/shared/api/openapi.types.ts`:** thêm các type trên, đặt tên `ProgressOverviewDto`, `ProgressDailyActivityDto`, `ProgressVocabularySummaryDto`, `ProgressLessonSummaryDto`, `ProgressGrammarLevelDto`, `ProgressGrammarSummaryDto`, `ProgressSpeechSummaryDto`, `ProgressLiveTutorSummaryDto`, có comment section giống các phần khác.
2. **Thư mục `src/features/progress/`**, theo cấu trúc của `src/features/grammar/`:
   - `api/progressApi.ts`: hàm `getOverview(days)` dùng `httpClient`.
   - `hooks/useProgressQueries.ts`:
     - `progressKeys = { all: ['progress'], overview: (days) => ['progress','overview',days] }`
     - `useProgressOverviewQuery(days)` với `staleTime: 5 * 60 * 1000`.
   - `pages/ProgressPage.tsx`, xuất trong `index.ts`.
   - `components/`:
     - `StreakCard`: streak hiện tại, streak dài nhất, số ngày hoạt động trong khoảng.
     - `ActivityChart`: cột xếp chồng theo 5 mảng, vẽ bằng **SVG thuần**, **không thêm package nào**. Có `role="img"` và `aria-label` tóm tắt, kèm một `<table>` ẩn bằng class `sr-only` cho trình đọc màn hình. Khi rê chuột hoặc focus vào cột thì hiện ngày và số lượng (dùng `<title>` trong SVG là đủ). Màu mỗi mảng lấy từ token hoặc Tailwind sẵn có, phải có legend. Khi `days=90`, cột hẹp lại nhưng không được tràn ngang ở màn 360px; dùng `viewBox` và `preserveAspectRatio`.
     - `SummaryCard`: dùng chung cho 5 thẻ Từ vựng, Bài học, Ngữ pháp, Phát âm, Live Tutor. Mỗi thẻ có link sang trang tương ứng: `/student/vocabulary`, `/student/lessons`, `/student/grammar`, `/student/vocabulary` (phát âm nằm trong trang từ vựng), `/student/tutor`. Chỉ hiện link khi user có permission của route đó: xem `RequirePermission` trong `src/app/router.tsx` và cách menu `StudentLayout` kiểm tra quyền. Không có quyền thì vẫn hiện thẻ nhưng không có link. Giá trị `null` hiển thị là "—". Grammar hiện tiến độ theo level, dạng thanh `passedSteps/totalSteps`, và step hiện tại.
   - Bộ chọn 7/30/90 ngày, dạng segmented button có `aria-pressed`.
   - Đủ các trạng thái: loading (`LoadingState`), error có nút thử lại (`FeedbackBanner` hoặc theo pattern sẵn có), empty khi mọi số đều 0 (`EmptyState`, gợi ý bắt đầu học, có link).
3. **Router:** thêm route `progress` (lazy) dưới `/student` trong `src/app/router.tsx`, không bọc `RequirePermission`.
4. **Menu:** thêm mục "Tiến độ" vào `src/app/layouts/StudentLayout.tsx`, làm theo đúng markup của các mục hiện có, hỗ trợ cả khi sidebar thu gọn. Dùng một icon sẵn có trong `public/assets/icons/navigation/`; chọn cái gần nghĩa nhất, không tạo icon mới.
5. **i18n:** thêm vào cả `src/shared/i18n/locales/vi.ts` và `en.ts`, gồm `nav.progress`, `nav.progressSub` và nhóm key `progress.*`. Không hardcode chuỗi hiển thị trong component mới.
6. **Test (Vitest)**, đặt trong `src/features/progress/__tests__/`:
   - `progressApi`: gọi đúng URL với `days`.
   - Render `ProgressPage` với dữ liệu giả cho các trạng thái loading, error, empty và có dữ liệu.
   - Đổi 7/30/90 thì đổi query key.
   - `ActivityChart` có đúng số cột, có bảng `sr-only`.
   - Link của thẻ bị ẩn khi user thiếu quyền.
   - Route `/student/progress` render được qua `AppRoutes`. Xem mẫu ở `src/features/grammar/__tests__/studentLayoutGrammarRoute.test.tsx`.
   - Có test i18n key tồn tại ở cả vi và en, theo mẫu `grammarI18nKeys.test.ts`.

## Kiểm chứng

Chạy trong `EnglishPartner.FE`:
```bash
npm run build
npm run test -- src/features/progress
npm run test
```
Tất cả phải pass. Không chạy dev server. Không thêm dependency vào `package.json`.

## Báo cáo cuối

Gồm:
- danh sách file đã tạo hoặc sửa;
- lệnh đã chạy và kết quả, gồm số test pass;
- icon đã chọn;
- quyết định UI đáng chú ý và những gì chưa làm.
