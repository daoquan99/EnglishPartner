# P5j — đổi tên phần dùng chung thành 4 project Shared (làm trước P5d)

Theo `plan-p5.md` mục "Bổ sung". Chỉ đổi tên project/thư mục — **không đổi namespace** (`EnglishLiveTutor.Domain.Common…`, `EnglishLiveTutor.Application.Common…`, `EnglishLiveTutor.Application.Modules` giữ nguyên).

1. `src/EnglishLiveTutor.Domain/` → `src/EnglishLiveTutor.Shared.Domain/`, csproj `EnglishLiveTutor.Shared.Domain.csproj`. Nếu csproj không khai báo `RootNamespace`, thêm `<RootNamespace>EnglishLiveTutor.Domain</RootNamespace>` để file mới tạo sau này giữ namespace cũ.
2. `src/EnglishLiveTutor.SharedKernel/` → `src/EnglishLiveTutor.Shared.Application/`, csproj `EnglishLiveTutor.Shared.Application.csproj` (`RootNamespace` `EnglishLiveTutor.Application` nếu cần tương tự).
3. `EnglishLiveTutor.Shared.Infrastructure`, `EnglishLiveTutor.Shared.Presentation` giữ tên.
4. Cập nhật mọi `ProjectReference` (src + tests), `EnglishLiveTutor.sln` (xóa entry cũ, thêm entry mới; `dotnet sln list` kiểm), `ApplicationAssemblies.All` (nếu tham chiếu assembly theo tên/marker), `InternalsVisibleTo`, `ModuleMap.cs` (path `EnglishLiveTutor.Shared.Domain/` và `EnglishLiveTutor.Shared.Application/` → Shared; bỏ path cũ), `ProjectReferenceTests`/`ApplicationAssembliesTests` (rule theo tên mới: `.Domain` của module chỉ được reference `EnglishLiveTutor.Shared.Domain`; Contracts/Application/Infrastructure/Presentation reference `EnglishLiveTutor.Shared.Application` thay `SharedKernel`; Shared.* không reference project module).
5. Tài liệu trong `EnglishPartner.BE/` (AGENTS.md, `.agents/**`) có nhắc tên project cũ → ghi danh sách file + dòng trong report (orchestrator tự sửa; không sửa nếu sandbox chặn).
6. Kiểm chứng: build `--no-restore` 0 lỗi; toàn bộ test pass; `has-pending-model-changes` cho `AppDbContext`… không cần — chỉ chạy cho `CatalogDbContext` và `IdentityDbContext` (= không đổi); allowlist 0; `git diff -M --stat` cho thấy rename.

Không đổi logic, namespace, route, migration; không thêm package; không commit. Worker report: rename, reference đã sửa (số csproj), test sửa, tài liệu cần sửa, lệnh + số test.
