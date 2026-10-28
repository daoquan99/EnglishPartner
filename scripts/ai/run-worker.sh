#!/usr/bin/env bash
# Chạy Codex hoặc AGY (Antigravity CLI) như worker cho một task trong docs/tasks/<task-id>/.
#
#   bash scripts/ai/run-worker.sh <codex|agy> <task-id> <implement|review> [options]
#
# Options:
#   --scope be|fe|root   Thư mục làm việc của worker (mặc định: root)
#   --in-place           Chạy trên working tree hiện tại thay vì worktree riêng
#   --model <name>       Model cho worker (mặc định: cấu hình của CLI)
#   --allow-shell        AGY implement: tự duyệt mọi quyền tool, kể cả shell (--dangerously-skip-permissions)
#   --timeout <dur>      AGY: giới hạn thời gian print mode, ví dụ 30m (mặc định: không giới hạn)
#   --prompt <file>      Prompt khác mặc định
#
# Prompt mặc định: docs/tasks/<task-id>/prompt-<worker>.md (implement)
#                  docs/tasks/<task-id>/review-prompt-<worker>.md (review)
# Log: docs/tasks/<task-id>/runs/ (bị .gitignore).
# Worktree: ../EnglishPartner.worktrees/<task-id>-<worker>, branch ai/<task-id>-<worker>.
# Script không commit, push hay merge.
set -euo pipefail

usage() { sed -n '2,18p' "$0" | sed 's/^# \{0,1\}//'; exit 1; }
[[ $# -lt 3 ]] && usage

WORKER=$1 TASK_ID=$2 MODE=$3; shift 3
SCOPE=root IN_PLACE=0 MODEL="" ALLOW_SHELL=0 PROMPT="" TIMEOUT=""
while [[ $# -gt 0 ]]; do
  case $1 in
    --scope) SCOPE=$2; shift 2 ;;
    --in-place) IN_PLACE=1; shift ;;
    --model) MODEL=$2; shift 2 ;;
    --allow-shell) ALLOW_SHELL=1; shift ;;
    --prompt) PROMPT=$2; shift 2 ;;
    --timeout) TIMEOUT=$2; shift 2 ;;
    *) echo "Option không hợp lệ: $1"; usage ;;
  esac
done

[[ $WORKER =~ ^(codex|agy)$ ]] || { echo "Worker phải là codex hoặc agy"; exit 1; }
[[ $MODE =~ ^(implement|review)$ ]] || { echo "Mode phải là implement hoặc review"; exit 1; }
[[ $TASK_ID =~ ^[A-Za-z0-9._-]+$ ]] || { echo "task-id chỉ gồm chữ, số, . _ -"; exit 1; }
command -v "$WORKER" >/dev/null || { echo "Không tìm thấy CLI '$WORKER' trong PATH"; exit 1; }

REPO=$(git rev-parse --show-toplevel)
TASK_DIR="$REPO/docs/tasks/$TASK_ID"
[[ -d $TASK_DIR ]] || { echo "Chưa có $TASK_DIR — tạo plan trước"; exit 1; }
if [[ -z $PROMPT ]]; then
  [[ $MODE == implement ]] && PROMPT="$TASK_DIR/prompt-$WORKER.md" || PROMPT="$TASK_DIR/review-prompt-$WORKER.md"
fi
[[ -f $PROMPT ]] || { echo "Không thấy prompt: $PROMPT"; exit 1; }
# Đọc nội dung ngay tại đây: các lệnh phía dưới đổi thư mục làm việc.
PROMPT_BODY=$(cat "$PROMPT") || { echo "Không đọc được prompt: $PROMPT"; exit 1; }
[[ -n ${PROMPT_BODY//[[:space:]]/} ]] || { echo "Prompt rỗng: $PROMPT"; exit 1; }
PROMPT_NAME=$(basename "$PROMPT")

case $SCOPE in
  be) SUBDIR=EnglishPartner.BE ;;
  fe) SUBDIR=EnglishPartner.FE ;;
  root) SUBDIR=. ;;
  *) echo "Scope phải là be, fe hoặc root"; exit 1 ;;
esac

if [[ $IN_PLACE == 1 ]]; then
  BASE=$REPO
else
  BASE="$(dirname "$REPO")/EnglishPartner.worktrees/$TASK_ID-$WORKER"
  BRANCH="ai/$TASK_ID-$WORKER"
  if [[ ! -d $BASE ]]; then
    mkdir -p "$(dirname "$BASE")"
    if git -C "$REPO" show-ref --verify --quiet "refs/heads/$BRANCH"; then
      git -C "$REPO" worktree add "$BASE" "$BRANCH"
    else
      git -C "$REPO" worktree add -b "$BRANCH" "$BASE" HEAD
    fi
    echo "Lưu ý: worktree tạo từ HEAD — thay đổi chưa commit trong working tree chính không có trong worktree."
  fi
  # FE trong worktree dùng chung node_modules qua junction (không cài lại).
  if [[ $SCOPE == fe && ! -e $BASE/EnglishPartner.FE/node_modules && -d $REPO/EnglishPartner.FE/node_modules ]]; then
    cmd //c mklink /J "$(cygpath -w "$BASE/EnglishPartner.FE/node_modules")" "$(cygpath -w "$REPO/EnglishPartner.FE/node_modules")" >/dev/null
  fi
fi
WORKDIR="$BASE/$SUBDIR"

RUNS="$TASK_DIR/runs"; mkdir -p "$RUNS"
STAMP=$(date +%Y%m%d-%H%M%S)
LOG="$RUNS/$STAMP-$WORKER-$MODE.log"
LAST="$RUNS/$STAMP-$WORKER-$MODE-last.md"

HEADER="Bạn là worker ($WORKER) cho task $TASK_ID, chế độ $MODE.
Thư mục làm việc: $WORKDIR
Hồ sơ task (plan.md, skill cần đọc) ở: $TASK_DIR
Việc cần làm là nội dung của $PROMPT_NAME, chép đầy đủ bên dưới. Chỉ làm đúng việc này; không thực hiện các prompt khác trong thư mục task.
Tuân thủ AGENTS.md. Không commit, push, merge; không sửa ngoài phạm vi prompt.
Kết thúc bằng phần 'Worker report' theo mẫu trong prompt.
---
"

echo "== $WORKER $MODE | task $TASK_ID | $WORKDIR"
set +e
if [[ $WORKER == codex ]]; then
  SANDBOX=workspace-write; [[ $MODE == review ]] && SANDBOX=read-only
  ARGS=(exec -C "$WORKDIR" -s "$SANDBOX" --add-dir "$TASK_DIR" -o "$LAST")
  [[ -n $MODEL ]] && ARGS+=(-m "$MODEL")
  printf '%s%s' "$HEADER" "$PROMPT_BODY" | codex "${ARGS[@]}" - 2>&1 | tee "$LOG"
  STATUS=${PIPESTATUS[1]}
else
  AGY_MODE=accept-edits
  [[ $MODE == review ]] && AGY_MODE=plan
  ARGS=(--mode "$AGY_MODE" --add-dir "$TASK_DIR" --output-format text)
  [[ $MODE == implement && $ALLOW_SHELL == 1 ]] && ARGS+=(--dangerously-skip-permissions)
  [[ -n $MODEL ]] && ARGS+=(--model "$MODEL")
  [[ -n $TIMEOUT ]] && ARGS+=(--print-timeout "$TIMEOUT")
  ( cd "$WORKDIR" && agy "${ARGS[@]}" --print "$HEADER$PROMPT_BODY" ) 2>&1 | tee "$LOG"
  STATUS=${PIPESTATUS[0]}
  cp "$LOG" "$LAST"
fi
set -e

echo
echo "== Exit code: $STATUS"
echo "== Log: $LOG"
echo "== Kết quả cuối: $LAST"
if [[ $IN_PLACE == 0 ]]; then
  echo "== Worktree: $BASE (branch $BRANCH)"
  git -C "$BASE" status --short
  git -C "$BASE" diff --stat
fi
exit "$STATUS"
