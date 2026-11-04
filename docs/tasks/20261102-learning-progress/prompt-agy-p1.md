# Việc giao cho AGY: P1 + P2 — reader tiến độ ở 5 module, module Progress và endpoint

## Bối cảnh (session mới, đọc kỹ)

- Thiết kế và tiêu chí: `docs/tasks/20261102-learning-progress/plan.md`. **Đọc hết trước khi làm.** Người dùng đã duyệt, kể cả ngoại lệ module Progress chỉ có 3 project và migration chỉ thêm index.
- Luật: `AGENTS.md` (luật 1, 2, 6, 7, 9, 10, 13), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`.
- Skill cần đọc:
  - `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/feature-cqrs/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/ef-performance/SKILL.md`
  - `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
  - `.agents/skills/api-contract-sync/SKILL.md`
- Solution `EnglishPartner.BE/EnglishLiveTutor.slnx`. Làm trên `development`, **không commit**. Không chạy `database update`, không kết nối SQL. Không có mạng: chạy `dotnet restore --ignore-failed-sources`, sau đó dùng `--no-restore`.
- Một worker khác có thể đang tạo file trong `docs/tasks/20261102-grammar-a2-content/`. Không đụng vào thư mục đó.
- Phần FE làm ở bước sau, **không** sửa `EnglishPartner.FE`.

## Yêu cầu

### 1. Kiểu dùng chung

Thêm `public sealed record DailyActivityCount(DateOnly Date, int Count);` vào `src/Shared/EnglishLiveTutor.Shared.Application/Common/Progress/`.

### 2. Reader ở 5 module

Mỗi module thêm `I<X>ProgressReader` vào `Modules.<X>.Contracts`, đặt implementation trong `Modules.<X>.Infrastructure` dùng context scoped `I<X>DbContext`, và đăng ký DI scoped trong `Add<X>Infrastructure`.

Mọi interface có:

```csharp
Task<IReadOnlyList<DailyActivityCount>> GetDailyActivityAsync(string userId, DateOnly fromLocal, DateOnly toLocal, int utcOffsetMinutes, CancellationToken ct);
```

Quy tắc chung cho mọi reader:

- Lọc theo `userId`. Tính khoảng thời gian UTC: `fromUtc = fromLocal 00:00 − offset` và `toUtc = (toLocal + 1 ngày) 00:00 − offset`, nửa mở `[fromUtc, toUtc)`.
- Nhóm theo ngày địa phương **trong SQL**, tức là `GroupBy` trên biểu thức thời gian cộng offset rồi lấy ngày, sau đó `Select` ra `Count`. Không tải entity về rồi mới đếm.
- Mỗi method chạy đúng 1 truy vấn. `GetSummaryAsync` chạy tối đa 2 truy vấn tổng hợp.
- Dùng `AsNoTracking`. Tôn trọng soft delete, tức là giữ query filter mặc định.

| Module | Hoạt động (`GetDailyActivityAsync`) | Summary (`GetSummaryAsync(userId, fromUtc, ct)`) | Index cần có |
| --- | --- | --- | --- |
| Vocabulary | `VocabularyReviewAttempts.ReviewedAt` | `VocabularyProgressSummary(Studied, Mastered, DueForReviewNow, AverageMastery)`, tính giống `GetLearningProgressQuery` hiện có (đọc handler này và dùng lại logic, **không** sửa handler/endpoint cũ) | `(UserId, ReviewedAt)` |
| Lessons | `LessonAttempts` đã hoàn thành, theo `CompletedAtUtc` | `LessonProgressSummary(CompletedAttempts, AverageScore100?)`, lấy field điểm thật của attempt | `(UserId, CompletedAtUtc)` |
| Grammar | `GrammarAttempts` có `CompletedAtUtc` | `GrammarProgressSummary(IReadOnlyList<GrammarLevelProgress(Level, PassedSteps, TotalSteps)>, CurrentStepCode?)`. `TotalSteps` đếm các step đã publish. `CurrentStepCode` là step đầu tiên theo (level, order) chưa Passed nhưng đã mở khóa | `(UserId, CompletedAtUtc)` |
| Speech | `PronunciationAssessments.CreatedAtUtc` | `SpeechProgressSummary(Assessments, AverageScore?, AverageScoreSinceFrom?)` | `(UserId, CreatedAtUtc)` |
| LiveTutor | `Conversations` theo thời điểm tạo (cột audit tạo) | `LiveTutorProgressSummary(Sessions, SessionsSinceFrom)` | `(UserId, <cột tạo>)` |

- Về index: chỉ thêm khi chưa có index nào **bắt đầu bằng** `UserId` kèm đúng cột thời gian. Thêm bằng `HasIndex` trong configuration. Mỗi module bị đổi thêm một migration bằng `dotnet ef migrations add AddProgressIndexes_<X> --context <X>DbContext --project src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure --startup-project src/EnglishLiveTutor.Api --output-dir <thư mục Migrations hiện có của context>`. Migration chỉ được gồm `CreateIndex`/`DropIndex`.
- `tests/EnglishLiveTutor.ArchitectureTests/Architecture/ModulePersistenceGuardTests.cs`:
  - `GetModuleMigrations()` hiện dùng `Assert.Single(migrationsAssembly.Migrations)`. Đổi thành chọn migration nền theo ID (ID kết thúc bằng `_MoveToModuleSchema_<X>`; với Shared thì là `_InitialCreate_Shared`). Giữ nguyên mọi assert đang áp dụng lên migration nền.
  - Thêm test `LaterModuleMigrations_OnlyAddOrDropIndexes`: mọi migration sau migration nền chỉ được chứa `CreateIndexOperation` hoặc `DropIndexOperation`. Ghi rõ trong comment: lần đầu cần migration khác thì nới test này có chủ đích.
  - Sửa các chỗ khác trong file đang giả định mỗi context chỉ có một migration (nếu có).

### 3. Module Progress (3 project)

- Tạo `src/Modules/Progress/EnglishLiveTutor.Modules.Progress.{Contracts,Application,Presentation}`, theo mẫu `.csproj` của một module hiện có (cùng TargetFramework, Nullable, ImplicitUsings, kiểu package).
  - `Contracts`: rỗng, chỉ có `AssemblyInfo` nếu mẫu có.
  - `Application`: tham chiếu `Shared.Application`, `Shared.Domain`, cùng `Contracts` của Identity, Vocabulary, Lessons, Grammar, Speech và LiveTutor.
  - `Presentation`: tham chiếu `Progress.Application` và `Shared.Presentation` (theo mẫu Presentation hiện có).
- Trong `Application/Features/Progress/Queries/GetProgressOverview/`:
  - `GetProgressOverviewQuery(string UserId, int Days) : IQuery<ProgressOverviewDto>` và validator FluentValidation `Days` trong khoảng 7–90.
  - Handler:
    1. Lấy `TimeZoneId` qua `IUserDirectory.GetLearningProfileAsync`. Nếu null hoặc không hợp lệ thì dùng `Asia/Ho_Chi_Minh`. Thử `TimeZoneInfo.FindSystemTimeZoneById` với cả ID IANA và Windows.
    2. Tính `todayLocal` theo `TimeProvider` (inject để test được), offset hiện tại, cửa sổ biểu đồ `[today − Days + 1, today]` và cửa sổ streak `[today − 365, today]`.
    3. Gọi **tuần tự** 5 reader cho cửa sổ streak. Lấy phần cửa sổ biểu đồ từ cùng dữ liệu đó, không truy vấn lần hai. Sau đó gọi 5 `GetSummaryAsync`.
    4. Streak:
       - `current`: số ngày liên tiếp có hoạt động (tổng các mảng ≥ 1), tính lùi từ hôm nay. Nếu hôm nay = 0 thì tính lùi từ hôm qua.
       - `longest`: dãy dài nhất trong cửa sổ 366 ngày.
       - `activeDaysInRange`: số ngày có hoạt động trong cửa sổ biểu đồ.
    5. `activity` có đủ `Days` phần tử, kể cả ngày không có hoạt động, sắp tăng dần theo ngày.
  - `ProgressOverviewDto` đúng các field ghi trong plan (camelCase khi ra JSON). `date` serialize dạng `yyyy-MM-dd`.
- `Presentation`: endpoint mỏng `GET /api/progress/overview?days=` dùng `RequireAuthorization()`, có `MapProgressEndpoints()` trong `ProgressEndpointRouteBuilderExtensions`. `userId` chỉ lấy từ `ICurrentUser`; nếu không có thì `ApiProblem.Unauthorized()`, theo mẫu `DailyLearningEndpoints`. Thiếu `days` thì dùng 30.
- Composition:
  - `Api.csproj`: thêm tham chiếu 3 project.
  - `Composition/Application`: thêm `AddProgressModule`; thêm assembly vào `ApplicationAssemblies` (MediatR và validator).
  - `Program.cs`: thêm `app.MapProgressEndpoints();` sau `MapGrammarEndpoints()`.
  - `.slnx`: thêm folder `/src/Modules/Progress/`.
- Kiến trúc:
  - `ModuleMap`: thêm Progress.
  - `ProjectReferenceTests`: thêm test cho 3 project, chốt đúng danh sách tham chiếu ở trên.
  - `ModuleBoundaryScanner`: không có vi phạm.
  - Sửa mọi test kiến trúc đang giả định 7 module hoặc 5 project mỗi module, để Progress được tính là ngoại lệ 3 project (ghi lý do trong comment).
  - `docs/adr/0006-modular-monolith.md`: thêm dòng `Progress` vào bảng module (phụ thuộc Contracts của Identity, Vocabulary, Lessons, Grammar, Speech, LiveTutor; chỉ đọc; 3 project), và thêm một gạch đầu dòng trong "Cập nhật khi triển khai".
- Snapshot contract: chạy lại cơ chế sinh snapshot của `ApiContractSnapshotTests` theo đúng cách test đó hướng dẫn. Inventory phải tăng **đúng 1 dòng** (`GET /api/progress/overview`). Thêm shape cho `ProgressOverviewDto` nếu test có danh sách shape. Các dòng khác không được đổi.

### 4. Test

- Reader (mỗi module, EF InMemory):
  - đếm đúng theo ngày với offset +420 (bản ghi lúc 16:30Z và 17:30Z cùng ngày UTC rơi vào hai ngày địa phương khác nhau);
  - dữ liệu của user khác không được tính;
  - bản ghi ngoài khoảng ngày không được tính;
  - bản ghi đã soft delete không được tính;
  - summary trả đúng giá trị.
- Handler, dùng reader giả và `FakeTimeProvider` hoặc `TimeProvider` tự viết:
  - streak khi hôm nay có hoạt động;
  - streak khi hôm nay chưa có hoạt động nhưng hôm qua có;
  - streak = 0;
  - `longest` lớn hơn `current`;
  - `activity` có đủ `Days` phần tử, kể cả ngày trống;
  - múi giờ null hoặc không hợp lệ thì dùng múi giờ mặc định.
- Endpoint (`Api.IntegrationTests`):
  - chưa đăng nhập thì 401;
  - `days=5` và `days=91` thì 400;
  - dữ liệu user A và user B tách riêng: A chỉ thấy số liệu của A.

## Kiểm chứng (chạy trong `EnglishPartner.BE`)

```bash
dotnet restore EnglishLiveTutor.slnx --ignore-failed-sources
dotnet build EnglishLiveTutor.slnx --no-restore
dotnet test EnglishLiveTutor.slnx --no-build
# với mỗi context có migration mới:
dotnet ef migrations has-pending-model-changes --context <X>DbContext --project src/Modules/<X>/EnglishLiveTutor.Modules.<X>.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
```

Kết quả bắt buộc:
- build 0 lỗi;
- toàn bộ test pass;
- has-pending báo "No changes" với mọi context;
- snapshot inventory chỉ thêm 1 dòng.

## Không được

- Sửa endpoint hoặc DTO hiện có.
- Join bảng của module khác.
- Cho module nguồn tham chiếu Progress.
- Sửa migration cũ.
- Tạo migration có thao tác khác ngoài index.
- Thêm package.
- Sửa FE.
- Commit.

## Worker report

Nộp report gồm:
- danh sách file tạo/sửa, nhóm theo module;
- bảng reader → truy vấn LINQ (trích code) → index dùng;
- các migration đã tạo;
- các chỗ sửa trong test kiến trúc kèm lý do;
- diff snapshot (dòng thêm);
- lệnh đã chạy và kết quả;
- những gì chưa làm.
