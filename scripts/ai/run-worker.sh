#!/usr/bin/env bash
# Chạy Codex hoặc AGY (Antigravity CLI) như worker cho một task trong docs/tasks/<task-id>/.
#
#   bash scripts/ai/run-worker.sh <codex|agy> <task-id> <implement|review> [options]
#
# Options:
#   --scope be|fe|root   Thư mục làm việc của worker (mặc định: root)
#   --in-place           Chạy trên working tree hiện tại thay vì worktree riêng
#   --model <name>       Model cho worker (mặc định: cấu hình của CLI)
#   --allow-shell        AGY: tự duyệt mọi quyền tool, kể cả shell (--dangerously-skip-permissions); review vẫn ở plan mode
#   --timeout <dur>      AGY: giới hạn thời gian print mode, ví dụ 30m (mặc định: không giới hạn)
#   --prompt <file>      Prompt khác mặc định
#   --worktree <name>    Tên worktree/branch dùng chung (mặc định: <task-id>-<worker>), để đổi worker giữa chừng
#   --resume <id|last>   Codex: tiếp tục session cũ (giữ context + prompt cache). 'last' = session ghi trong runs/codex-session.txt
#   --compact-limit <n>  Codex + resume: đặt model_auto_compact_token_limit=n (tự bật 200000 khi context lượt trước > 250k)
#
# Sau mỗi lần chạy Codex: session id ghi vào runs/codex-session.txt, kích thước context lượt cuối vào runs/codex-tokens.txt.
#
# Prompt mặc định: docs/tasks/<task-id>/prompt-<worker>.md (implement)
#                  docs/tasks/<task-id>/review-prompt-<worker>.md (review)
# Log: docs/tasks/<task-id>/runs/ (bị .gitignore).
# Worktree: ../EnglishPartner.worktrees/<task-id>-<worker>, branch ai/<task-id>-<worker>.
# Script không commit, push hay merge.
set -euo pipefail

usage() { sed -n '2,23p' "$0" | sed 's/^# \{0,1\}//'; exit 1; }
[[ $# -lt 3 ]] && usage

WORKER=$1 TASK_ID=$2 MODE=$3; shift 3
SCOPE=root IN_PLACE=0 MODEL="" ALLOW_SHELL=0 PROMPT="" TIMEOUT="" WT_NAME="" RESUME="" COMPACT_LIMIT=""
while [[ $# -gt 0 ]]; do
  case $1 in
    --scope) SCOPE=$2; shift 2 ;;
    --in-place) IN_PLACE=1; shift ;;
    --model) MODEL=$2; shift 2 ;;
    --allow-shell) ALLOW_SHELL=1; shift ;;
    --prompt) PROMPT=$2; shift 2 ;;
    --timeout) TIMEOUT=$2; shift 2 ;;
    --worktree) WT_NAME=$2; shift 2 ;;
    --resume) RESUME=$2; shift 2 ;;
    --compact-limit) COMPACT_LIMIT=$2; shift 2 ;;
    *) echo "Option không hợp lệ: $1"; usage ;;
  esac
done

[[ $WORKER =~ ^(codex|agy)$ ]] || { echo "Worker phải là codex hoặc agy"; exit 1; }
[[ $MODE =~ ^(implement|review)$ ]] || { echo "Mode phải là implement hoặc review"; exit 1; }
[[ $TASK_ID =~ ^[A-Za-z0-9._-]+$ ]] || { echo "task-id chỉ gồm chữ, số, . _ -"; exit 1; }
[[ -z $WT_NAME ]] && WT_NAME="$TASK_ID-$WORKER"
[[ $WT_NAME =~ ^[A-Za-z0-9._-]+$ ]] || { echo "--worktree chỉ gồm chữ, số, . _ -"; exit 1; }
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
  BASE="$(dirname "$REPO")/EnglishPartner.worktrees/$WT_NAME"
  BRANCH="ai/$WT_NAME"
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
    powershell.exe -NoProfile -Command "New-Item -ItemType Junction -Path '$(cygpath -w "$BASE/EnglishPartner.FE/node_modules")' -Target '$(cygpath -w "$REPO/EnglishPartner.FE/node_modules")' | Out-Null"
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
  if [[ -n $RESUME ]]; then
    [[ $RESUME == last ]] && RESUME=$(cat "$RUNS/codex-session.txt" 2>/dev/null || true)
    [[ $RESUME =~ ^[0-9a-fA-F-]{36}$ ]] || { echo "Không có session id hợp lệ để resume"; exit 1; }
    # resume không nhận -C/-s/--add-dir: chạy trong WORKDIR và truyền sandbox qua -c.
    TASK_DIR_TOML=$(cygpath -m "$TASK_DIR" 2>/dev/null || echo "$TASK_DIR")
    ARGS=(exec resume -c "sandbox_mode=\"$SANDBOX\"" -c "sandbox_workspace_write.writable_roots=[\"$TASK_DIR_TOML\"]" -o "$LAST")
    # Tự nén khi context lượt trước > 250k (trừ khi đã chỉ định --compact-limit).
    PREV_CTX=$(cat "$RUNS/codex-tokens.txt" 2>/dev/null || echo 0)
    [[ -z $COMPACT_LIMIT && ${PREV_CTX:-0} -gt 250000 ]] && COMPACT_LIMIT=200000 && echo "== Context $PREV_CTX > 250k: bật compact"
    [[ -n $COMPACT_LIMIT ]] && ARGS+=(-c "model_auto_compact_token_limit=$COMPACT_LIMIT")
    [[ -n $MODEL ]] && ARGS+=(-m "$MODEL")
    ARGS+=("$RESUME" -)
    HEADER="Tiếp tục cùng session, task $TASK_ID, chế độ $MODE, thư mục làm việc $WORKDIR.
Việc mới là nội dung của $PROMPT_NAME bên dưới. Phạm vi của việc trước đã kết thúc; chỉ làm đúng việc mới này.
Tuân thủ AGENTS.md. Không commit, push, merge. Kết thúc bằng phần 'Worker report'.
---
"
    ( cd "$WORKDIR" && printf '%s%s' "$HEADER" "$PROMPT_BODY" | codex "${ARGS[@]}" ) 2>&1 | tee "$LOG"
    STATUS=${PIPESTATUS[0]}
  else
    ARGS=(exec -C "$WORKDIR" -s "$SANDBOX" --add-dir "$TASK_DIR" -o "$LAST")
    [[ -n $MODEL ]] && ARGS+=(-m "$MODEL")
    printf '%s%s' "$HEADER" "$PROMPT_BODY" | codex "${ARGS[@]}" - 2>&1 | tee "$LOG"
    STATUS=${PIPESTATUS[1]}
  fi
  SID=$(grep -m1 -oE 'session id: [0-9a-fA-F-]{36}' "$LOG" | awk '{print $3}' || true)
  [[ -n $SID ]] && echo "$SID" > "$RUNS/codex-session.txt"
  # Kích thước context thật = last_token_usage.input_tokens của lượt cuối (không phải "tokens used" tích lũy).
  CTX=""
  if [[ -n $SID ]]; then
    SFILE=$(grep -rl "$SID" "$HOME/.codex/sessions" 2>/dev/null | head -1 || true)
    [[ -n $SFILE ]] && CTX=$(grep -oE '"last_token_usage":\{"input_tokens":[0-9]+' "$SFILE" | tail -1 | grep -oE '[0-9]+$' || true)
  fi
  [[ -n $CTX ]] && echo "$CTX" > "$RUNS/codex-tokens.txt" && echo "== Session $SID, context tokens: $CTX"
else
  AGY_MODE=accept-edits
  [[ $MODE == review ]] && AGY_MODE=plan
  ARGS=(--mode "$AGY_MODE" --add-dir "$TASK_DIR" --output-format text)
  # review (plan mode) cũng cần chạy lệnh đọc (grep, git, dotnet list); plan mode vẫn chặn sửa file.
  [[ $ALLOW_SHELL == 1 ]] && ARGS+=(--dangerously-skip-permissions)
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
