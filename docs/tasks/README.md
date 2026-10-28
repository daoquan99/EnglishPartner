# Hồ sơ task

Mỗi task có một thư mục `docs/tasks/<mã-task>/`, mã dạng `YYYYMMDD-<slug>` (ví dụ `20261028-s1-jwt-secret`). Quy trình đầy đủ ở [AGENTS.md gốc](../../AGENTS.md#quy-trình-một-task) và [CLAUDE.md](../../CLAUDE.md).

| File | Ai viết | Nội dung |
| --- | --- | --- |
| `plan.md` | planner (Claude) | Mục tiêu, phạm vi, contract, rủi ro, phân công, kiểm chứng |
| `prompt-<worker>.md` | backend-dev / frontend-dev / test-engineer | Việc giao cho Codex hoặc AGY |
| `prompt-<worker>-fix-<n>.md` | dispatcher | Lỗi cụ thể cần worker sửa |
| `review.md` | orchestrator | Kết luận của các reviewer |
| `report.md` | orchestrator | Kết quả cuối, lệnh đã chạy, phần chưa kiểm chứng |
| `runs/` | `scripts/ai/run-worker.sh` | Log worker — không commit (`.gitignore`) |

Mẫu ở [`_template/`](_template/).

## Lệnh

```bash
bash scripts/ai/run-worker.sh codex  <mã-task> implement --scope be
bash scripts/ai/run-worker.sh agy    <mã-task> implement --scope fe
bash scripts/ai/run-worker.sh codex  <mã-task> review    --scope fe   # review chéo, chỉ đọc
```

Worker chạy trong `../EnglishPartner.worktrees/<mã-task>-<worker>` (branch `ai/<mã-task>-<worker>`). Người dùng review và merge; script không commit.
