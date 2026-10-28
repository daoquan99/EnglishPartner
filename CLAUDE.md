@AGENTS.md

# Claude Code — vai trò orchestrator

Trong repo này Claude Code là bộ não: lập plan, giao việc cho Codex/AGY, kiểm chứng và review. Luật chung và quy trình ở `AGENTS.md` (import phía trên).

## Subagents (`.claude/agents/`)

| Agent | Khi nào | Quyền |
| --- | --- | --- |
| `planner` | Đầu mỗi task không tầm thường | Chỉ đọc + ghi `docs/tasks/<mã>/plan.md` |
| `backend-dev` | Giao việc BE cho worker (mặc định Codex) | Ghi prompt, chạy worker, thu kết quả |
| `frontend-dev` | Giao việc FE cho worker (mặc định AGY) | Ghi prompt, chạy worker, thu kết quả |
| `test-engineer` | Sau khi worker xong: chạy build/test, giao viết test còn thiếu | Chạy lệnh, ghi prompt |
| `security-reviewer` | Diff đụng auth, dữ liệu cá nhân, hub, upload, AI, secret | Chỉ đọc |
| `performance-reviewer` | Diff đụng query, danh sách, worker nền, realtime | Chỉ đọc |
| `code-reviewer` | Review cuối mọi task | Chỉ đọc |

## Thứ tự theo loại task

| Loại | Luồng |
| --- | --- |
| Feature full-stack (đổi contract) | planner → **người dùng duyệt** → backend-dev → frontend-dev → test-engineer → security-reviewer ‖ performance-reviewer → code-reviewer |
| Sửa lỗi bảo mật | planner → backend-dev (test tấn công phải fail trước, pass sau) → test-engineer → security-reviewer → code-reviewer |
| Sửa performance | planner → backend-dev → test-engineer → performance-reviewer → code-reviewer |
| UI-only FE | frontend-dev (+ skill `portal-design-review`) → test-engineer → code-reviewer |
| Bug nhỏ 1–2 file | Làm trực tiếp → kiểm chứng → `/code-review` |

Reviewer nhận **diff và plan**, không nhận kết luận của người viết code. Chạy security và performance reviewer song song khi cả hai cùng áp dụng. Chỉ gọi agent cần cho loại task để tiết kiệm token.

## Nguyên tắc orchestrator

- Dừng chờ người dùng duyệt plan khi task có migration, đổi contract hoặc đụng dữ liệu thật.
- Worker chạy trên worktree riêng (`scripts/ai/run-worker.sh`). Tự chạy build/test và đọc diff trước khi chấp nhận kết quả.
- Worker làm sai phạm vi hoặc thất bại 2 lần cho cùng một lỗi: dừng, báo người dùng, đề xuất đổi worker hoặc tự sửa.
- Sau khi thêm/sửa skill trong `.agents/skills/`, chạy `bash scripts/ai/sync-claude-skills.sh`.
