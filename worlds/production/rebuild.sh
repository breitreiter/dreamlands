#!/usr/bin/env bash
set -euo pipefail
WORLD_DIR="$(cd "$(dirname "$0")" && pwd)"
WORLD_NAME="$(basename "$WORLD_DIR")"
REPO_ROOT="$(cd "$WORLD_DIR/../.." && pwd)"
echo "Rebuilding world '$WORLD_NAME'..."
dotnet run --project "$REPO_ROOT/mapgen" -- generate "$WORLD_NAME"
