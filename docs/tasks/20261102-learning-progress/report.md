# Report: 20261102-learning-progress

Trạng thái: **Xong**, đã commit trên `development`. Migration chỉ thêm index; bạn tự apply khi chạy app.

## Đã làm

| Phần | Nội dung |
| --- | --- |
| Reader (P1) | Thêm `I<X>ProgressReader` vào Contracts của Vocabulary, Lessons, Grammar, Speech và LiveTutor. Mỗi module tự implement trong Infrastructure của mình, dùng `I<X>DbContext` và đăng ký Scoped. Mỗi reader có 2 hàm. `GetDailyActivityAsync` lọc theo nửa khoảng `[fromUtc, toUtc)` trên cột thời gian, rồi `GROUP BY CONVERT(date, DATEADD(minute, offset, ts))`. `GetSummaryAsync` gom bằng một truy vấn aggregate. Kiểu `DailyActivityCount` đặt ở `Shared.Application/Common/Progress`. |
| Index | Mỗi module thêm một migration `AddProgressIndexes_<X>`, gồm 5 index: `(UserId, ReviewedAt)`, `(UserId, CompletedAtUtc)` ×2 cho Lessons và Grammar, `(UserId, CreatedAt)` ×2 cho Speech và LiveTutor. Migration chỉ có `CreateIndex`/`DropIndex`. |
| Module Progress (P2) | Gồm 3 project: Contracts (rỗng), Application và Presentation. Có `GetProgressOverviewQuery` kèm validator (`days` từ 7 đến 90) và endpoint `GET /api/progress/overview` dùng `RequireAuthorization`; `userId` lấy từ `ICurrentUser`. Handler gọi tuần tự 5 reader. Ngày tính theo `TimeZoneId` của user; múi giờ lạ thì rơi về `Asia/Ho_Chi_Minh`. Streak tính trong 365 ngày, gồm cả hôm nay; nếu hôm nay chưa học thì tính tới hôm qua. |
| Kiến trúc | Đã cập nhật ADR-0006 (thêm module và ghi ngoại lệ 3 project), `ModuleMap`, `ProjectReferenceTests` và `.slnx`. Trong `ModulePersistenceGuardTests`, bỏ `Assert.Single` và chọn migration nền theo suffix. Thêm test `LaterModuleMigrations_OnlyAddOrDropIndexes`: mọi migration sau migration nền chỉ được tạo hoặc xóa index. File này không có trong bảng phạm vi của plan, nhưng phải sửa vì guard cũ giả định mỗi module chỉ có 1 migration. |
| Contract | Snapshot thêm 1 route và shape `ProgressOverviewDto`. `/api/learning/progress` giữ nguyên. |
| FE (P3) | Thêm `features/progress/` gồm `progressApi`, query key `['progress','overview',days]` (`staleTime` 5 phút), `ProgressPage`, `StreakCard`, `ActivityChart` và `SummaryCard`. `ActivityChart` là SVG thuần, có bảng `sr-only` và tooltip qua `<title>`. Có bộ chọn 7/30/90 ngày (`aria-pressed`), đủ trạng thái loading/error (kèm retry)/empty. Đã thêm route `/student/progress`, mục menu "Tiến độ", i18n vi/en và type trong `openapi.types.ts`. Không thêm package. |

## Worker và vòng sửa

| Vòng | Ai làm | Kết quả |
| --- | --- | --- |
| P1+P2 | AGY (1 session) | Build và test pass. Orchestrator tự kiểm SQL và review. |
| Tự sửa sau review | Claude | Grammar summary đổi sang projection, bỏ `TheoryJson`, thêm `!IsDeleted`. Summary của Speech và LiveTutor gộp lại còn 1 truy vấn mỗi module. Làm tròn dùng `AwayFromZero`, để 82.65 hiện là 82.7. |
| Test bổ sung | AGY | Thêm test cross-user cho summary của từng reader, test endpoint với 2 user ở 3 nguồn dùng `Assert.Equal`, test soft-delete và test handler truyền đúng `userId`. Worker chỉ sửa file test. |
| P3 | AGY | Build và test pass. |
| Tự sửa sau code review | Claude | (BE) Rút gọn `ResolveTimeZone`, có fallback `UTC+07`. Sửa cửa sổ streak từ 366 về 365 ngày. Cộng dồn khi reader trả về trùng ngày. Thêm 3 test handler: ranh giới ngày lúc 17:30 UTC tức 00:30 ở +7, múi giờ lạ, và trùng ngày. (FE) Empty state không còn dựa vào `currentStepCode`, vì giá trị này luôn có khi đã publish step. Biểu đồ bỏ 90 điểm Tab, vì đã có bảng `sr-only`. Link thẻ Bài học hiện luôn, vì route không có guard. Thêm dạng số ít `_one` cho tiếng Anh. |

Review:
- security-reviewer: ĐẠT CÓ ĐIỀU KIỆN, điều kiện là bổ sung test cross-user. Đã làm.
- performance-reviewer: ĐẠT CÓ ĐIỀU KIỆN, điều kiện là gộp truy vấn. Đã gộp; Grammar còn 3 truy vấn và đã ghi ngoại lệ trong plan.
- code-reviewer: ĐẠT CÓ ĐIỀU KIỆN. Đã xử lý các mục 2–8, 12 và một phần 13.

## Kiểm chứng (orchestrator tự chạy)

| Lệnh / bước | Kết quả |
| --- | --- |
| `dotnet build EnglishLiveTutor.slnx` | 0 warning, 0 error |
| `dotnet test EnglishLiveTutor.slnx` | **777/777 pass** (baseline 749) |
| `npm run build` (FE) | pass, chỉ còn cảnh báo chunk lớn có từ trước |
| `npm run test` (FE) | **368/368 pass**, trong đó `src/features/progress` có 23 test |
| DB tạm `LingoMint_ProgCheck`, restore từ bản sao COPY_ONLY của `LingoMint`, đã xóa cùng file `.bak`. **Không đụng `LingoMint`.** | Cả 8 context migrate được. 5 index được tạo đúng schema module. |
| API chạy trên DB tạm, gọi bằng user có dữ liệu thật. Mật khẩu chỉ được đổi trong DB tạm. | Không token: 401. `days` = 5, 91 hoặc `abc`: 400. Owner: 200, 85 ms ở lần gọi thứ hai (lần đầu 1.2–1.3 s do khởi động lạnh). Không có log `fail`/`crit`. |
| So số đếm theo ngày giữa API (`days=90`) và SQL viết tay `CONVERT(date, DATEADD(minute, 420, ts))` | Khớp ở cả 5 mảng. Lệch duy nhất là 1 conversation đã xóa mềm, bị loại đúng theo query filter. Streak dài nhất 5 ngày (15–19/10), có 8 ngày hoạt động: đúng. |
| Hai truy vấn đã gộp (`AVG(CASE …)`, `COUNT` có điều kiện) trên DB tạm | Dịch được sang SQL. Giá trị trung bình khớp SQL thủ công (82.65). |

## Ghi chú hành vi

- Phiên Live Tutor và các bản ghi khác đã xóa mềm không được tính là hoạt động, kể cả trong streak.
- Offset múi giờ lấy theo thời điểm hiện tại. Với múi giờ có DST, các ngày ở phía bên kia mốc chuyển giờ có thể lệch 1 giờ. Việt Nam không có DST.
- Cách tính ngày giả định cột `datetimeoffset` lưu ở `+00:00`, đúng với cách app đang ghi. Bản ghi có offset khác sẽ rơi vào sai ngày.
- `GetSummaryAsync(fromUtc)`: chỉ Speech và LiveTutor dùng `fromUtc` (cho số liệu trong khoảng). Ba module còn lại bỏ qua tham số này.

## Nợ còn lại

- `UserDirectory.GetLearningProfileAsync` (code cũ) so sánh `u.Id.ToString() == userId || u.Username == userId`, nên không seek được theo PK. Nên mở task riêng.
- Index không có `INCLUDE (IsDeleted, …)`. Khi một user có hàng chục nghìn dòng thì cần đo `STATISTICS IO` và cân nhắc thêm.
- Mỗi request quét 365 ngày ở 5 bảng. Nếu dữ liệu lớn thì cân nhắc bảng tổng hợp theo ngày.
- Tạo index sẽ khóa ghi trong thời gian ngắn. Với dữ liệu hiện tại thì không đáng kể; nếu bảng lên tới hàng triệu dòng thì nên apply ngoài giờ cao điểm.
- FE: còn vài key i18n thừa (`daysCount`, `noPermissionTooltip`, …) và `error as any` ở `ProgressPage.tsx`. Icon menu dùng tạm `nav-dashboard.svg`.

## Chưa kiểm chứng được

- Giao diện trên trình duyệt thật: layout 360px, độ dài text vi/en, rê chuột vào tooltip.
- Execution plan và `STATISTICS IO` trên dữ liệu lớn. Dữ liệu dev chỉ có 1 user với khoảng 100 bản ghi.
