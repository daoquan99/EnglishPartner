# EnglishPartner — luật chung cho mọi AI agent

File này là **nguồn luật duy nhất** cho Claude Code, Codex và AGY (Antigravity CLI). Claude đọc qua `CLAUDE.md` (import file này); Codex và AGY đọc `AGENTS.md` trực tiếp. Luật riêng từng phần nằm ở `EnglishPartner.BE/AGENTS.md` và `EnglishPartner.FE/AGENTS.md`; khi mâu thuẫn, file gần thư mục đang sửa hơn được ưu tiên, trừ **Luật bất biến** dưới đây luôn áp dụng.

## Monorepo

| Thư mục | Nội dung | Build / test |
| --- | --- | --- |
| `EnglishPartner.BE` | .NET 10, solution `EnglishLiveTutor.slnx` (Modular Monolith: `src/Modules/<X>` × 5 project, `src/Shared`, host Api; ADR-0006), SQL Server, SignalR, Gemini | `dotnet build EnglishPartner.BE/EnglishLiveTutor.slnx` · `dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx` |
| `EnglishPartner.FE` | React 19, Vite, TanStack Query, Zustand, Tailwind 4 | `npm run build` · `npm run test -- <đường-dẫn-test>` (chạy trong `EnglishPartner.FE`) |
| `docs/reviews` | Báo cáo review có checklist | |
| `docs/adr` | Quyết định kiến trúc đã chốt | |
| `docs/tasks` | Hồ sơ từng task: plan, prompt giao việc, report, review | |

## Luật bất biến

**Bảo mật**
1. Danh tính người dùng chỉ lấy từ claims (`ICurrentUser`). Không nhận `userId` từ query/body/hub payload cho dữ liệu cá nhân; không thêm fallback `default-learner`.
2. Mọi query, command và hub method đụng dữ liệu cá nhân phải kiểm tra owner. Không tìm thấy hoặc không phải chủ → 404.
3. Không ghi secret, mật khẩu hay fallback secret vào code, `appsettings.json` hoặc `.env` được commit. Thiếu cấu hình bắt buộc → fail-fast lúc khởi động.
4. API key BYO của học viên chỉ dùng trong phạm vi request/phiên: không log, không lưu DB, dọn khi phiên kết thúc (ADR-0003).
5. Endpoint gọi AI và endpoint auth phải có rate limit và timeout.

**Performance**
6. Endpoint danh sách luôn phân trang với page size mặc định và giới hạn tối đa; không trả cả bảng.
7. Không `Include` collection tăng vô hạn khi chỉ cần vài bản ghi; danh sách project sang DTO.
8. Không gọi provider AI bên trong SQL transaction; không dùng lock toàn cục cho dữ liệu nhiều người dùng.

**Kiến trúc**
9. Code mới theo pattern mới: MediatR (ADR-0001), `I<Module>DbContext` của module + UnitOfWork, owner check (ADR-0006). Không sao chép pattern cũ (repository Singleton, `IDbContextFactory` trong từng method, handler gọi handler khác).
10. Đổi contract (route, DTO, field JSON, header phân trang, hub method/event) phải sửa đồng bộ BE DTO, `EnglishPartner.FE/src/shared/api/openapi.types.ts`, API adapter và query keys FE trong cùng task.
11. Endpoint mỏng: bind → `ISender.Send` → map kết quả. Lỗi đi qua global exception handler/ProblemDetails (ADR-0005).
12. Migration xóa hoặc biến đổi dữ liệu thật phải là migration riêng, có ghi chú backup và chỉ chạy trên DB thật khi người dùng xác nhận.

**Modular Monolith** (ADR-0006, áp dụng cho BE)
13. Module chỉ dùng phần `Contracts` của module khác. Không dùng entity, repository, handler, service nội bộ hay `DbSet` của module khác; không join bảng của module khác. Phụ thuộc mới giữa module phải có trong bảng module của ADR-0006 và không tạo vòng.
14. Không transaction xuyên module. Hiệu ứng phụ sang module khác đi qua outbox event với handler idempotent; đọc đồng bộ qua interface trong `Contracts`.
15. Refactor cấu trúc là **chỉ di chuyển**: giữ nguyên route, field JSON, hub method/event, tên bảng/cột, ID và lịch sử migration; không trộn sửa hành vi vào cùng diff. Build và toàn bộ test phải giữ nguyên kết quả so với baseline.

## Vai trò trong quy trình

| Vai trò | Ai | Được làm | Không được làm |
| --- | --- | --- | --- |
| Orchestrator, planner, reviewer, verifier | Claude Code | Lập plan, viết prompt giao việc, chạy build/test, review diff, cập nhật checklist | Đánh dấu Done khi chưa tự chạy kiểm chứng |
| Worker | Codex (`codex exec`), AGY (`agy --print`) | Sửa đúng file trong phạm vi prompt, chạy kiểm tra được phép, viết report | Tự mở rộng phạm vi, sửa migration/contract ngoài prompt, commit/push/merge |
| Người quyết định | Người dùng | Duyệt plan, chốt ADR, xác nhận thao tác phá hủy dữ liệu, merge | |

Phân công mặc định (từ 2026-11-01): **Codex** với model `deepseek-flash` (mặc định trong cấu hình Codex, không cần `--model`) làm cả BE và FE; review chéo do các reviewer của Claude đảm nhận sau mỗi lần worker chạy. AGY là worker dự phòng. Orchestrator có thể đổi phân công trong `plan.md`.

## Quy trình một task

1. **Plan** — tạo `docs/tasks/<mã-task>/plan.md` từ `docs/tasks/_template/`. Nêu mục tiêu, mục review/ADR liên quan, file bị ảnh hưởng, contract thay đổi, rủi ro dữ liệu, phân công worker và lệnh kiểm chứng.
2. **Duyệt** — task có migration, đổi contract hoặc đụng dữ liệu thật phải được người dùng duyệt plan trước khi code.
3. **Giao việc** — viết `prompt-<worker>.md`, chạy worker trên git worktree riêng bằng `scripts/ai/run-worker.sh`.
4. **Kiểm chứng** — orchestrator tự chạy build/test trong worktree; không tin báo cáo "đã xong" của worker.
5. **Review** — security/performance/code review đọc diff độc lập; lỗi quay lại bước 3.
6. **Hoàn tất** — viết `report.md` (đã làm, đã kiểm chứng gì, còn thiếu gì), đánh dấu `[x]` trong file review liên quan. Người dùng quyết định merge.

Bug nhỏ 1–2 file, không đổi contract: được làm trực tiếp, vẫn phải kiểm chứng và review.

## Skills

Skill nằm trong `.agents/skills/` (Codex và AGY tự tìm theo thư mục làm việc; Claude dùng bản tham chiếu sinh bởi `scripts/ai/sync-claude-skills.sh`). Prompt giao việc phải ghi rõ đường dẫn skill cần đọc.

| Phạm vi | Skill |
| --- | --- |
| Chung | [security-audit](.agents/skills/security-audit/SKILL.md), [api-contract-sync](.agents/skills/api-contract-sync/SKILL.md), [realtime-session](.agents/skills/realtime-session/SKILL.md), [delegate-to-worker](.agents/skills/delegate-to-worker/SKILL.md) |
| Backend | xem bảng skill trong [EnglishPartner.BE/AGENTS.md](EnglishPartner.BE/AGENTS.md) |
| Frontend | xem bảng skill trong [EnglishPartner.FE/AGENTS.md](EnglishPartner.FE/AGENTS.md) |

## Definition of Done

- Build pass cho phần bị sửa; test liên quan pass; với thay đổi bảo mật có test chứng minh user A không truy cập được dữ liệu user B.
- Không vi phạm Luật bất biến; contract BE/FE đồng bộ.
- `report.md` ghi rõ lệnh đã chạy và kết quả, phần chưa kiểm chứng (SQL Server thật, SignalR, microphone, provider AI).
- Không commit, push hay merge khi người dùng chưa yêu cầu.

## Giao tiếp

Báo cáo, plan, review bằng tiếng Việt; giữ nguyên tên file, symbol, route và contract. Không in giá trị secret, token, API key hay connection string.
