# Report: Refactor backend sang Modular Monolith (ADR-0006)

- Mã task: `20261101-modular-monolith` · Plan: [plan.md](plan.md) (+ [plan-p2](plan-p2.md), [plan-p3](plan-p3.md), [plan-p4](plan-p4.md), [plan-p5](plan-p5.md)) · Review từng gói: [review.md](review.md)
- Worker: Codex `deepseek-flash` (P1–P5b, một session/phase, resume + compact khi context > 250k); AGY (P5c–P6, một session/module, chạy song song trên worktree riêng). Orchestrator: thiết kế, prompt, kiểm chứng mọi gói, review (code/security/performance reviewer), gộp merge, tự sửa các lỗi chỉ lộ trên SQL Server thật.
- Nhánh: `development` (người dùng yêu cầu merge và làm trực tiếp). Commit chính: `0d2d72c` (P1–P5b + Shared), `eee2264` (Identity), merge Speech/Vocabulary/Lessons/Grammar/LiveTutor, `9d9daf3` (P5i), `4a1cec1` (P6a), `7004232` (P6b).

## Kết quả

| Phase | Nội dung |
| --- | --- |
| P1 | Architecture test quét source (`ModuleMap`, DAG, Contracts sạch, cấm `IAppDbContext` trong feature, allowlist chỉ giảm); 7 `I<X>DbContext`; ~60 handler chuyển sang interface module |
| P2 | Gỡ phụ thuộc chéo bằng Contracts/port (12 quyết định D1–D12, gồm đảo phụ thuộc Speech ↔ Vocabulary); allowlist 142 → **0** |
| P3 | 7 module thành project, namespace giữ nguyên |
| P4 | 7 `<X>DbContext`, schema theo module (`identity`, `catalog`, `speech`, `lessons`, `grammar`, `vocabulary`, `livetutor`), `SqlConnection` scoped dùng chung, `UnitOfWork` commit nhiều context trong một transaction; 7 migration `MoveToModuleSchema_<X>` (chỉ `ALTER SCHEMA … TRANSFER`, 48 bảng) — **viết sẵn, chưa apply** |
| P5 | Mỗi module 5 project `Modules.<X>.{Domain,Application,Infrastructure,Presentation,Contracts}`; Shared 4 project `src/Shared/EnglishLiveTutor.Shared.{Domain,Application,Infrastructure,Presentation}`; `Infrastructure`/`Api` chỉ còn composition/host |
| P6 | Test theo module + snapshot contract API (xem mục Kiểm chứng) |

Route, field JSON, hub method/event, tên bảng/cột, ID migration cũ **không đổi**. FE không phải sửa.

## Kiểm chứng (orchestrator tự chạy)

- `dotnet build EnglishLiveTutor.sln`: 0 lỗi (8 warning nullable có sẵn từ baseline).
- `dotnet test EnglishLiveTutor.sln`: baseline 535 pass / 6 fail → sau P5 **659 pass / 0 fail** (6 test cache audio sửa nguyên nhân: `EF.Functions.DataLength` → `AudioData.Length`, SQL Server vẫn sinh `DATALENGTH`). P6: **665 pass / 0 fail** trong 9 project test (ArchitectureTests 87, Api.IntegrationTests 96 + 6 snapshot, Catalog 9, Identity 45, Speech 30, Vocabulary 128, Lessons 89, Grammar 139, LiveTutor 36).
- Contract API: endpoint inventory 174 dòng (method, route, auth, rate limit, permission; gồm 3 hub SignalR) và shape JSON 5 response **giống hệt** khi sinh trên code trước refactor (`8884da1`).
- Sau P7 (2026-11-01): **681/681 pass**; 7 context has-pending = không đổi; solution chuyển sang `EnglishLiveTutor.slnx`.
- `dotnet ef migrations has-pending-model-changes`: 7 context = không đổi; `migrations list`: 7 ID giữ nguyên.
- Review: P1 ĐẠT; P2 security ĐẠT, code ĐẠT CÓ ĐIỀU KIỆN → đã sửa; P3 ĐẠT; P4 ĐẠT CÓ ĐIỀU KIỆN → đã sửa (migrate lỗi dừng app, logging context, model-vs-snapshot test).

## Đã kiểm trên DB thật (2026-11-01)

- Người dùng chạy app → migration tự apply trên `LingoMint`; người dùng xác nhận các luồng chính chạy OK.
- Orchestrator kiểm chỉ đọc (`sqlcmd`): `identity` 7, `catalog` 7, `speech` 7, `lessons` 7, `grammar` 6, `vocabulary` 17, `livetutor` 4 bảng (mỗi schema gồm `__EFMigrationsHistory`), `dbo` 2 (`IdempotencyRecords` + history cũ) — đúng thiết kế.

## Lưu ý / chưa kiểm chứng (từ trước khi apply)

- **Chạy migration trên `LingoMint`**: chạy project với `Database:MigrateOnStartup=true`. Trước đó chạy [preflight-check-lingomint.sql](preflight-check-lingomint.sql) (chỉ đọc): 49 bảng ở `dbo`, không có view/proc/synonym ngoài EF trỏ `dbo.X` (view `WITH SCHEMABINDING` sẽ làm TRANSFER fail), tài khoản có `db_owner`/`db_ddladmin`. Backup đã có ở `C:\Backups`. Migration lỗi → app dừng với log `Critical` (không chạy với schema nửa vời); chạy lại sẽ tiếp từ module dở.
- Sau khi apply, mọi nhánh chưa có refactor (`master`, các worktree cũ) chạy trên cùng DB sẽ lỗi vì bảng không còn ở `dbo`. Quay lại: `dotnet ef database update 0 --context <X>DbContext` cho 7 context (chuyển bảng về `dbo`).
- Transaction nhiều context trên SQL Server thật, SignalR/realtime, provider Gemini/AGY: test chạy EF InMemory, không chứng minh được.
- SQL viết tay phải dùng `[identity].[Users]` (`identity` là từ khóa T-SQL).
- (Đã xử lý ở P7b) Shape snapshot phủ cả 7 module: 12 response (gồm 2 ProblemDetails 404) giống hệt khi sinh trên code trước refactor `8884da1`.

## Nợ còn lại / quyết định hoãn

- Outbox: hoãn (ADR-0006) — observer Speech → Vocabulary chạy đồng bộ trong cùng unit of work.
- Theo rà soát Catalog, giữ nguyên có chủ đích (luật 15): query dùng `IModuleDbContextFactory` thay vì context scoped; vài query trả entity ra JSON; `IPromptCatalogReader` singleton.
- Type trong project module vẫn `public` (chưa chuyển `internal`).
- (Đã xử lý ở P7a) Helper cấu hình DbContext gom thành `AddModuleDbContext<>()` trong Shared.Infrastructure, `UnitOfWork` chuyển sang Shared.Infrastructure (participant pattern), xóa factory cũ, bỏ `InternalsVisibleTo` thừa. Còn lại (chấp nhận): 7 hàm `Map<X>Endpoints()` có sẵn nhưng `Program.cs` giữ lời gọi rời để nguyên thứ tự route.
- Worktree cũ đã merge (`../EnglishPartner.worktrees/20261101-modular-monolith`, `mm-p5{d,e,f,g,h}-*`) và stash `backup: docs copies…` — giữ lại, xóa khi người dùng yêu cầu.

## P8 — bỏ `AppDbContext` + 10 migration cũ (2026-11-01)

- `src` không còn `AppDbContext`/`IAppDbContext`/thư mục `Infrastructure/Migrations`. Mỗi DB được dựng chỉ từ migration của `SharedDbContext` (schema `shared`) + 7 module.
- 7 migration `MoveToModuleSchema_<X>` giữ nguyên ID (DB đã apply không chạy lại) nhưng tự chứa: chặn DB cũ chưa đủ 10 migration cũ → chuyển bảng từ `dbo` nếu có → tạo nếu thiếu.
- Đã kiểm trên SQL Server (DB tạm, đã xóa): DB trống, DB khôi phục từ backup trước refactor, bản sao `LingoMint` → cùng cấu trúc, số dòng không đổi; DB cũ dở dang bị chặn bằng lỗi rõ ràng.
- **Lần chạy app tới trên `LingoMint`:** chỉ có migration `InitialCreate_Shared` chạy — chuyển `dbo.IdempotencyRecords` sang `shared.IdempotencyRecords` (metadata, giữ dữ liệu; đã thử trên bản sao). 7 module không làm gì.
- Khác biệt chấp nhận: DB tạo mới không có 6 `DEFAULT` constraint lịch sử (`livetutor.Conversations.UserId`, `speech.ScoringRubricCriteria.DescriptorLevel0..4`) mà DB cũ có — model EF không khai báo, EF luôn ghi giá trị tường minh.
- DB của môi trường khác: nếu còn ở bản trước refactor, phải có đủ 10 migration cũ (tới `20261029040750_AddGrammarRoadmap`) — nếu không, chạy bản `2bc98e4` trước rồi mới lên bản P8.
