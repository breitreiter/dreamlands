#!/usr/bin/env bash
set -euo pipefail
WORLD_DIR="$(cd "$(dirname "$0")" && pwd)"
WORLD_NAME="$(basename "$WORLD_DIR")"
REPO_ROOT="$(cd "$WORLD_DIR/../.." && pwd)"
SYMLINK="$REPO_ROOT/ui/web/public/world"
ln -sfn "../../../worlds/$WORLD_NAME" "$SYMLINK"
echo "Dev UI now using world '$WORLD_NAME'"
