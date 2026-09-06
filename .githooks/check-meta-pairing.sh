#!/bin/sh
# Unity .meta pairing guard: every tracked file/dir in scope needs a tracked
# <path>.meta, and no tracked .meta may lack its base. Checks the whole index,
# so pre-commit and CI share one code path. Exempt names (Unity never metas
# them): dotfiles, *~, cvs, *.tmp.

set -u

PATHS=$(git ls-files -- Assets/ Packages/ | grep -v '^Packages/[^/]*$' || true)

EXEMPT='(^|/)(\.[^/]*|[^/]*~|cvs|[^/]*\.tmp)$'
BOUNDARY='^(Assets|Packages/[^/]+)$'

METAS=$(echo "$PATHS" | grep '\.meta$' | sort -u || true)
FILES=$(echo "$PATHS" | grep -v '\.meta$' | grep -vE "$EXEMPT" || true)

# Needed-meta set: files + ancestor dirs strictly deeper than Assets or Packages/<pkg>
DIRS=""
CUR=$(echo "$FILES" | sed -E 's|/[^/]+$||' | grep -vE "$BOUNDARY" | grep -vE "$EXEMPT" | sort -u || true)
while [ -n "$CUR" ]; do
    DIRS=$(printf '%s\n%s\n' "$DIRS" "$CUR")
    CUR=$(echo "$CUR" | sed -E 's|/[^/]+$||' | grep -vE "$BOUNDARY" | grep -vE "$EXEMPT" | sort -u || true)
done
NEEDED=$(printf '%s\n%s\n' "$FILES" "$DIRS" | sed '/^$/d' | sort -u)

HAVE=$(echo "$METAS" | sed 's/\.meta$//' | sort -u)

TMP1=$(mktemp) && TMP2=$(mktemp)
trap 'rm -f "$TMP1" "$TMP2"' EXIT
printf '%s\n' "$NEEDED" > "$TMP1"
printf '%s\n' "$HAVE" > "$TMP2"

MISSING=$(comm -23 "$TMP1" "$TMP2")
ORPHAN=$(comm -13 "$TMP1" "$TMP2")

if [ -z "$MISSING" ] && [ -z "$ORPHAN" ]; then
    exit 0
fi

echo "ERROR: Unity .meta pairing violations detected."
if [ -n "$MISSING" ]; then
    echo ""
    echo "Missing .meta (open Unity to generate, then stage the .meta):"
    echo "$MISSING" | sed 's/^/  /'
fi
if [ -n "$ORPHAN" ]; then
    echo ""
    echo "Orphan .meta (base file gone; git rm the .meta or restore the file):"
    echo "$ORPHAN" | sed 's/^/  /'
fi
exit 1
