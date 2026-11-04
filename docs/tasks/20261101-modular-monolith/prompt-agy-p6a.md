# Việc giao cho AGY: P6a — tách test theo module (chỉ di chuyển)

## Bối cảnh (session mới)

- Luật: `AGENTS.md` (Definition of Done: test cross-user bắt buộc cho thay đổi bảo mật), `EnglishPartner.BE/AGENTS.md`, `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` (Project layout), skill `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`.
- Người dùng đã chốt (plan-p5.md mục "Bổ sung", P6): test theo module, **giữ toàn bộ test hành vi hiện có** (chỉ di chuyển).
- Hiện có 1 project `tests/EnglishLiveTutor.Domain.Tests` (56 file test + `Architecture/`, `Infrastructure/ApiTestFactory.cs`, `TestDoubles/`, `LessonTestDoubles.cs`). `dotnet test` = N test pass (ghi N trước khi sửa). Làm trên `development`, không commit. Không mạng: `dotnet restore --ignore-failed-sources`, `--no-restore`.

## Cấu trúc đích

```text
tests/
  EnglishLiveTutor.Tests.Common/                    class library: Infrastructure/ApiTestFactory.cs, TestDoubles/**, LessonTestDoubles.cs, helper dùng chung (không có [Fact])
  EnglishLiveTutor.ArchitectureTests/               Architecture/** (ModuleMap, scanner, allowlist, ProjectReference, persistence/migration guard, ApplicationAssemblies)
  EnglishLiveTutor.Api.IntegrationTests/            test chạy qua WebApplicationFactory/HTTP (endpoint, ProblemDetails, auth/JWT qua HTTP, rate limit, ownership qua endpoint), CqrsDependencyInjectionTests, TypedHttpClientConstructorTests
  Modules/<X>/EnglishLiveTutor.Modules.<X>.Tests/   domain + handler + infrastructure + cross-user security của module X
```

## Quy tắc

1. Mỗi test project: net10.0, cùng package test như project hiện tại (xunit, runner, Microsoft.NET.Test.Sdk, coverlet, EF InMemory, Mvc.Testing khi cần) **cùng version**, không thêm package mới. Reference: `Tests.Common` + project src cần thiết (module test chỉ reference project của module đó + composition nếu test dựng DI toàn app). Thêm vào `EnglishLiveTutor.sln` (nếu `dotnet sln add` lỗi solution folder, sửa `.sln` thủ công; `dotnet sln list` kiểm).
2. Phân file vào module theo **type nghiệp vụ chính** mà test kiểm (handler/entity/service của module nào). File kiểm nhiều module (ví dụ DailyVocabulary + Speech score) → module chủ của use case. File test qua HTTP (`ApiTestFactory`, `HttpClient`) → `Api.IntegrationTests`. Không tách một file test thành nhiều file trừ khi bắt buộc để build; nếu tách thì ghi report.
3. **Chỉ di chuyển**: không đổi tên test, assertion, dữ liệu seed, logic. Namespace có thể giữ `EnglishLiveTutor.Domain.Tests…` (đơn giản nhất) hoặc đổi theo project — **chọn giữ namespace** để diff nhỏ.
4. `InternalsVisibleTo`: hiện `src/EnglishLiveTutor.Infrastructure/EnglishLiveTutor.Infrastructure.csproj` và `src/Modules/LiveTutor/EnglishLiveTutor.Modules.LiveTutor.Infrastructure/*.csproj` (và chỗ khác nếu có) cho phép `EnglishLiveTutor.Domain.Tests` → cập nhật đúng tên assembly test mới đang dùng type internal đó.
5. `ModuleBoundaryScanner.FindSolutionRoot()` và đường dẫn allowlist trong Architecture test phải trỏ đúng sau khi chuyển (allowlist ở `tests/EnglishLiveTutor.ArchitectureTests/Architecture/module-boundary-allowlist.txt`).
6. Xóa project `tests/EnglishLiveTutor.Domain.Tests` sau khi chuyển hết (khỏi `.sln` và thư mục).
7. Cập nhật `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md` mô tả cấu trúc test mới nếu sandbox cho phép (nếu không, ghi report).

## Tiêu chí

- Build 0 lỗi; `dotnet test EnglishLiveTutor.sln` tổng số test = N (không mất test), 0 fail. Ghi số test từng project.
- Mỗi `Modules/<X>/…Tests` chỉ chứa test của module X (liệt kê file → project trong report).

## Worker report

```
### Worker report
- Số test trước: N; sau: tổng + theo project
- File → project: <bảng>
- File phải tách/sửa ngoài di chuyển: <danh sách + lý do> hoặc "không"
- InternalsVisibleTo đã sửa: …
- Lệnh đã chạy và kết quả: …
- Chưa làm / cần quyết định: …
```
