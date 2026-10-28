#!/usr/bin/env bash
# Sinh .claude/skills/<name>/SKILL.md trỏ về skill nguồn trong các thư mục .agents/skills/
# (root, EnglishPartner.BE, EnglishPartner.FE) để Claude Code tìm thấy cùng bộ skill
# mà Codex và AGY dùng. Nguồn duy nhất là .agents/skills — không sửa bản sinh ra.
#
#   bash scripts/ai/sync-claude-skills.sh
set -euo pipefail

REPO=$(git rev-parse --show-toplevel)
OUT="$REPO/.claude/skills"
MARKER=".generated-from-agents"
mkdir -p "$OUT"

# Xóa các bản đã sinh trước đó (chỉ những thư mục có marker).
for d in "$OUT"/*/; do
  [[ -f "$d$MARKER" ]] && rm -rf "$d"
done

count=0
for src in "$REPO"/.agents/skills/*/SKILL.md \
           "$REPO"/EnglishPartner.BE/.agents/skills/*/SKILL.md \
           "$REPO"/EnglishPartner.FE/.agents/skills/*/SKILL.md; do
  [[ -f $src ]] || continue
  name=$(sed -n 's/^name:[[:space:]]*//p' "$src" | head -1)
  desc=$(sed -n 's/^description:[[:space:]]*//p' "$src" | head -1)
  [[ -n $name && -n $desc ]] || { echo "Bỏ qua (thiếu frontmatter): $src"; continue; }
  if [[ -d "$OUT/$name" ]]; then
    echo "Trùng tên skill '$name' — bỏ qua $src"; continue
  fi
  rel=${src#"$REPO"/}
  mkdir -p "$OUT/$name"
  touch "$OUT/$name/$MARKER"
  cat > "$OUT/$name/SKILL.md" <<EOF
---
name: $name
description: $desc
---

<!-- Sinh tự động bởi scripts/ai/sync-claude-skills.sh. Sửa skill nguồn, không sửa file này. -->

Skill nguồn: \`$rel\`. Đọc toàn bộ file đó (đường dẫn tính từ gốc repo) và làm theo; các link tương đối trong file nguồn tính từ thư mục của nó.
EOF
  count=$((count + 1))
done
echo "Đã sinh $count skill trong .claude/skills"
