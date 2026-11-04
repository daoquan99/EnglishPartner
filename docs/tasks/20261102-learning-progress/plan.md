# Plan: Trang "Tiến độ học" (module Progress)

- Mã task: `20261102-learning-progress`
- Ngày: 2026-11-02
- Liên quan: luật 1, 2, 6, 7, 9, 10, 13; ADR-0006 (thêm module, thêm cạnh DAG)
- **Cần người dùng duyệt:** Có. Lý do: feature mới, đổi contract (route và DTO mới), thêm module vào ADR-0006, có thể có migration thêm index.
- Trạng thái: Đã duyệt 2026-11-02 ("ok bạn làm đi") — gồm các điểm cần chốt theo đề xuất

## Mục tiêu

Học viên có trang `/student/progress` gồm:
- chuỗi ngày học (streak) hiện tại và dài nhất;
- biểu đồ hoạt động trong 30 ngày;
- thẻ tóm tắt cho từng mảng: Từ vựng, Bài học, Ngữ pháp, Phát âm, Live Tutor.

Dữ liệu lấy từ 5 module. Mỗi module đọc dữ liệu của chính mình qua reader trong `Contracts`. Module mới `Progress` ghép kết quả lại và không join bảng của module khác.

## Tiêu chí hoàn thành

- [x] `GET /api/progress/overview?days=30` (`days` trong khoảng 7–90, mặc định 30, ngoài khoảng thì 400) trả `ProgressOverviewDto` của **chính user lấy từ claims**. Chưa đăng nhập thì 401.
- [x] Test cross-user: dữ liệu của user B không ảnh hưởng tới kết quả của user A (với mỗi reader và với endpoint).
- [x] Ngày được tính theo `TimeZoneId` của user (`Identity.Contracts`). Có test với múi giờ +7: hoạt động lúc 23:30 và 00:30 giờ địa phương rơi vào hai ngày khác nhau.
- [x] Streak: số ngày liên tiếp có ít nhất một hoạt động, tính tới hôm nay. Nếu hôm nay chưa học thì tính tới hôm qua. Streak dài nhất xét trong 365 ngày gần nhất. Có test cho các trường hợp biên.
- [x] Mỗi reader chạy tối đa 2 truy vấn tổng hợp (`GROUP BY` ngày, `COUNT`/`AVG`). Không tải entity về rồi mới đếm. Có index phù hợp (xem phần Dữ liệu). *Ngoại lệ, ghi nhận 2026-11-02:* Grammar summary dùng 2 truy vấn: danh sách step đã publish (không phụ thuộc user, chỉ projection, bỏ `TheoryJson`) và progress của user. Lý do là để tái dùng `GrammarRoadmapCalculator`, nên Grammar có tổng cộng 3 truy vấn.
- [x] Module `Progress` chỉ tham chiếu `Contracts` của Identity, Vocabulary, Lessons, Grammar, Speech, LiveTutor. Cập nhật ADR-0006 (bảng module và DAG), `ModuleMap`, `ProjectReferenceTests`. `ModuleBoundaryScanner` không có vi phạm.
- [x] FE: route `/student/progress`, mục menu "Tiến độ", adapter, query key, đồng bộ `openapi.types.ts`, i18n vi/en, có đủ trạng thái loading/empty/error. Biểu đồ vẽ bằng SVG/CSS, **không thêm package**. Hiển thị ổn ở 360px *(chưa kiểm trên trình duyệt, xem report)*.
- [x] Build và test BE pass; FE `npm run build` và vitest pass. Thêm 1 dòng vào snapshot contract (inventory 175 endpoint) và thêm shape cho DTO mới.

## Hiện trạng đã xác minh

- `GET /api/learning/progress` (Vocabulary, `DailyLearningEndpoints.cs:141`) → `LearningProgressDto`, gồm: tổng sense, đã học, mastered, review, learning, đến hạn ôn, mastery trung bình, số lần chấm phát âm, điểm phát âm trung bình. Endpoint này **giữ nguyên**.
- Nguồn có mốc thời gian:

  | Module | Nguồn | Mốc thời gian |
  | --- | --- | --- |
  | Vocabulary | `VocabularyReviewAttempts` | `ReviewedAt` |
  | Vocabulary | `UserVocabularyProgresses` | |
  | Lessons | `LessonAttempts` | `CompletedAtUtc`, điểm |
  | Grammar | `GrammarAttempts` | `StartedAtUtc`/`CompletedAtUtc`, `Score100` |
  | Grammar | `UserGrammarStepProgress` | `PassedAtUtc` |
  | Speech | `PronunciationAssessments` | `CreatedAtUtc`, điểm |
  | LiveTutor | `Conversations` | thời điểm tạo, có `UserId` |

- Index hiện có:
  - Grammar: `(UserId, StartedAtUtc)`
  - Speech: `(UserId, VocabularyId, CreatedAtUtc)`
  - Lessons: `(UserId, LessonId)`
  - LiveTutor: index theo `UserId`
  - Vocabulary review attempts: **chưa xác minh**
- FE chưa có thư viện biểu đồ. Layout student nằm ở `app/router.tsx:129`.
- `Identity.Contracts` có `IUserDirectory` và `UserLearningProfile` (cần xác minh có `TimeZoneId`; nếu chưa có thì thêm vào contract).

## Thiết kế

### Contracts (mỗi module nguồn)

Mỗi module thêm `I<X>ProgressReader` vào `Modules.<X>.Contracts`, implement trong Infrastructure của module đó và dùng context scoped của module:

```csharp
Task<<X>ProgressSummary> GetSummaryAsync(string userId, CancellationToken ct);
Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct);
```

`DailyActivityCount(DateOnly Date, int Count)` nằm ở `Shared.Application`. Ngày được nhóm trong SQL theo `DATEADD(minute, offset, ts)` rồi đổi sang date. Offset là offset hiện tại của múi giờ user; sai lệch khi có DST được chấp nhận và ghi vào DTO doc. Việt Nam không có DST.

### Module `Progress`

Xin bạn duyệt: module này **chỉ có 3 project**: `Contracts` (rỗng, giữ cho đồng dạng), `Application`, `Presentation`. Không có `Domain`/`Infrastructure` vì module không có bảng nào. Đây là ngoại lệ so với quy ước 5 project; nếu duyệt, sẽ ghi ngoại lệ vào ADR-0006.

- `GetProgressOverviewQuery(userId, days)`:
  - Gọi lần lượt từng reader. Không chạy song song, vì các context dùng chung một `SqlConnection` scoped.
  - Gộp hoạt động theo ngày, tính streak trên 365 ngày (chỉ gọi `GetDailyActivityAsync` cho đủ khoảng; dữ liệu trả về tối đa 365 dòng mỗi module).
- Endpoint mỏng `GET /api/progress/overview`, `RequireAuthorization()`. Không cần permission mới, vì chỉ đọc dữ liệu của chính mình.
- `ProgressOverviewDto`:

  ```text
  { timeZoneId, days, currentStreakDays, longestStreakDays, activeDaysInRange,
    activity: [{ date, vocabularyReviews, lessonAttempts, grammarAttempts, pronunciationAssessments, tutorSessions }],
    vocabulary: { studied, mastered, dueForReviewNow, averageMastery },
    lessons: { completedAttempts, averageScore100 },
    grammar: { levels: [{ level, passedSteps, totalSteps }], currentStepCode },
    speech: { assessments, averageScore, averageScoreInRange },
    liveTutor: { sessions, sessionsInRange } }
  ```

### FE

- `features/progress/`: gồm `api/progressApi.ts`, `queries.ts` (key `['progress','overview',days]`) và `pages/ProgressPage.tsx`.
- Trang dùng các thành phần sau:
  - `StreakCard`
  - `ActivityChart` (cột xếp chồng theo mảng, SVG, có `aria-label` và bảng ẩn cho trình đọc màn hình)
  - 5 `SummaryCard` có link sang trang tương ứng
  - bộ chọn 7/30/90 ngày
- Thêm vào menu student và i18n vi/en. Theo skill `student-portal-design`.

## Phạm vi

| Được sửa | Không được sửa |
| --- | --- |
| `src/Modules/Progress/**` (mới) | Endpoint và DTO hiện có (gồm `/api/learning/progress`) |
| `Modules.<X>.Contracts` (+ reader), `Modules.<X>.Infrastructure` (implement reader + đăng ký DI) của 5 module nguồn | Entity và bảng của module nguồn (chỉ được thêm index) |
| `Api.csproj`, `Composition/*` (đăng ký module), `Program.cs` (`MapProgressEndpoints`) | |
| `.slnx`, `ModuleMap`, `ProjectReferenceTests`, `ApplicationAssemblies`, snapshot contract (thêm dòng) | |
| `docs/adr/0006-modular-monolith.md` (bảng module) | |
| FE: `src/features/progress/**`, router, menu, i18n, `shared/api/openapi.types.ts` | Feature FE khác |

## Contract thay đổi

| Loại | Trước | Sau | Tương thích ngược? |
| --- | --- | --- | --- |
| route | — | `GET /api/progress/overview?days=` | Có (route mới) |
| DTO | — | `ProgressOverviewDto` | Có |

## Dữ liệu và migration

- Migration **chỉ thêm index**, không đụng dữ liệu. Index chỉ thêm khi truy vấn của reader thiếu index dẫn đầu bằng `UserId` kèm cột thời gian. Dự kiến:
  - Lessons: `(UserId, CompletedAtUtc)`
  - Speech: `(UserId, CreatedAtUtc)`
  - Vocabulary review attempts: `(UserId, ReviewedAt)` nếu chưa có
  - LiveTutor: `(UserId, CreatedAt)` nếu chỉ có index `UserId`
- Mỗi module một migration `AddProgressIndexes_<X>`.
- Bạn tự apply khi chạy app, như các lần trước. Không cần backup riêng vì chỉ tạo index. Trên bảng lớn, việc tạo index sẽ khóa bảng trong thời gian ngắn.
- Guard test của P8 (quy định migration module chỉ gồm `EnsureSchema` + `Sql`) có thể chỉ áp dụng cho migration `MoveToModuleSchema_*`. Worker phải kiểm tra lại. Nếu test áp dụng cho mọi migration thì viết index bằng `Sql` có `IF NOT EXISTS`.

## Phân công

| Phần việc | Worker | Scope | Skill cần đọc |
| --- | --- | --- | --- |
| P1. 5 reader trong Contracts và Infrastructure, index/migration, test reader (kèm cross-user và múi giờ) | AGY, mỗi module một session, chạy song song | be | `modular-monolith`, `feature-cqrs`, `ef-performance`, `backend-testing` |
| P2. Module Progress (query, endpoint, đăng ký), ADR, architecture test, snapshot | AGY | be | `modular-monolith`, `feature-cqrs`, `api-contract-sync` |
| P3. FE trang Progress | AGY | fe | `api-contract-sync`, `student-portal-design`, `frontend-testing` |

## Kiểm chứng

```bash
# trong EnglishPartner.BE
dotnet build EnglishLiveTutor.slnx && dotnet test EnglishLiveTutor.slnx
dotnet ef migrations has-pending-model-changes   # cho từng context bị đổi
dotnet ef migrations script --idempotent          # đọc SQL index
# trong EnglishPartner.FE
npm run build && npm run test -- src/features/progress
```

Orchestrator kiểm SQL của reader trên DB tạm, khôi phục từ bản sao `LingoMint` giống cách kiểm P8: chạy `EXPLAIN`/xem plan, đo thời gian.

Không kiểm chứng được tĩnh: giao diện trên trình duyệt thật. Phần này kiểm bằng Chrome nếu bạn cho phép.

## Review cần chạy

- [x] security-reviewer (owner và claims)
- [x] performance-reviewer (query, index, số truy vấn)
- [x] code-reviewer

## Rủi ro và câu hỏi mở

- Định nghĩa "hoạt động" cho từng mảng:
  - Từ vựng: mỗi lần ôn.
  - Bài học: attempt hoàn thành.
  - Ngữ pháp: attempt hoàn thành.
  - Phát âm: mỗi lần chấm.
  - Live Tutor: mỗi hội thoại được tạo.

  Bạn có thể đổi.
- Ngoại lệ 3 project cho module `Progress` (xem phần Thiết kế).
