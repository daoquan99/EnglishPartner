---
name: delegate-to-worker
description: Giao một phần việc của task EnglishPartner cho Codex CLI hoặc AGY (Antigravity CLI) làm worker — chọn worker, viết prompt theo mẫu, chạy scripts/ai/run-worker.sh trên worktree riêng, kiểm chứng kết quả và xử lý khi worker làm sai. Dùng cho orchestrator (Claude Code) và các agent backend-dev, frontend-dev, test-engineer.
---

# Giao việc cho worker

Đọc [AGENTS.md gốc](../../../AGENTS.md) (Vai trò, Quy trình) và [docs/tasks/README.md](../../../docs/tasks/README.md). Task phải có `docs/tasks/<mã-task>/plan.md` đã được duyệt nếu thuộc diện cần duyệt.

## Chọn worker

| Việc | Mặc định | Ghi chú |
| --- | --- | --- |
| BE (.NET, EF, migration, hub) | Codex | `--scope be` |
| FE (React, UI, hook, test Vitest) | AGY | `--scope fe` |
| Review chéo task rủi ro cao | Worker còn lại | mode `review`, chỉ đọc |
| Việc cần chạy lệnh nhiều (test, build lặp) | Codex | AGY mặc định chỉ được sửa file; `--allow-shell` khi thật cần |

Đổi phân công phải ghi lý do trong `plan.md`.

## Viết prompt

Copy `docs/tasks/_template/prompt-worker.md` thành `prompt-<worker>.md`. Prompt phải tự đủ — worker không thấy hội thoại của orchestrator:

- Mục tiêu một câu và mục review/ADR liên quan.
- **Phạm vi:** danh sách file/thư mục được sửa; file **không được** sửa (migration ngoài plan, contract, file của worker khác).
- Skill cần đọc (đường dẫn đầy đủ từ gốc repo).
- Tiêu chí hoàn thành kiểm tra được và lệnh kiểm chứng.
- Ràng buộc: không commit/push, không thêm package nếu plan không cho, không in secret.
- Mẫu "Worker report" ở cuối.

Một prompt = một phần việc nhỏ, rõ ràng. Không giao "sửa hết review".

## Chạy

```bash
bash scripts/ai/run-worker.sh codex  <mã-task> implement --scope be
bash scripts/ai/run-worker.sh agy    <mã-task> implement --scope fe
bash scripts/ai/run-worker.sh agy    <mã-task> review    --scope be   # review chéo, chỉ đọc
```

Script tạo worktree `../EnglishPartner.worktrees/<mã-task>-<worker>` (branch `ai/<mã-task>-<worker>`), ghi log vào `docs/tasks/<mã-task>/runs/`, in `git diff --stat`. Chạy lại cùng lệnh sẽ dùng lại worktree đó để sửa tiếp. Worktree tạo từ HEAD: thay đổi chưa commit ở working tree chính không có trong worktree — nếu task cần chúng, báo người dùng trước.

Chạy nền khi worker lâu (Bash `run_in_background`); không poll log liên tục.

## Kiểm chứng (bắt buộc, không tin report)

1. `git -C <worktree> status --short` và `git -C <worktree> diff` — chỉ file trong phạm vi bị đổi.
2. Tự chạy lệnh kiểm chứng trong plan tại worktree (`dotnet build/test`, `npm run build`, `npm run test -- <path>`).
3. So tiêu chí hoàn thành với diff thực tế.
4. Chuyển diff cho reviewer theo luồng trong `CLAUDE.md`.

## Khi worker sai

- Sửa ngoài phạm vi: revert phần đó trong worktree (`git -C <worktree> checkout -- <file>`), nhắc lại phạm vi trong prompt sửa lỗi.
- Build/test fail hoặc review có lỗi: viết `prompt-<worker>-fix-<n>.md` chỉ chứa lỗi cụ thể (file, dòng, output lỗi), chạy với `--prompt`.
- Thất bại 2 lần cho cùng một lỗi, worker treo, hết quota hoặc CLI chưa đăng nhập: dừng, báo người dùng, đề xuất đổi worker hoặc orchestrator tự sửa.

## Kết thúc

Ghi `report.md`: worker nào làm phần nào, lệnh đã tự chạy và kết quả, phần chưa kiểm chứng. Worktree và branch giữ nguyên để người dùng review/merge; chỉ xóa (`git worktree remove`) khi người dùng yêu cầu.
