#!/usr/bin/env bash
set -e

WORKDIR="${1:-.}"

echo "Normalizing line endings to UNIX (LF) in: $WORKDIR"

# Resolve to absolute path (optional but safer)
WORKDIR="$(cd "$WORKDIR" && pwd)"

cd "$WORKDIR"

find . -type f \( \
    -name "*.cs" \
 -o -name "*.prefab" \
 -o -name "*.asset" \
 -o -name "*.meta" \
\) \
! -path "./.git/*" \
! -path "./Library/*" \
! -path "./Temp/*" \
! -path "./obj/*" \
-print0 |
while IFS= read -r -d '' file; do
  # Only touch text files
  if file "$file" | grep -q text; then
    echo "fixing $file"
    sed -i.bak 's/\r$//' "$file"
    rm -f "$file.bak"
  fi
done

echo "Line ending normalization complete."
