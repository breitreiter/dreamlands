#!/usr/bin/env bash
set -euo pipefail

WORLD_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$WORLD_DIR/../.." && pwd)"

echo "==> Checking encounter syntax..."
dotnet run --project "$REPO_ROOT/text/encounter-tool/EncounterCli" -- check "$REPO_ROOT/text/encounters"

echo "==> Bundling encounters..."
dotnet run --project "$REPO_ROOT/text/encounter-tool/EncounterCli" -- bundle "$REPO_ROOT/text/encounters" --out "$WORLD_DIR"

echo "==> Checking tactical encounter syntax..."
dotnet run --project "$REPO_ROOT/text/encounter-tool/EncounterCli" -- check-tactical "$REPO_ROOT/text/encounters"

echo "==> Bundling tactical encounters..."
dotnet run --project "$REPO_ROOT/text/encounter-tool/EncounterCli" -- bundle-tactical "$REPO_ROOT/text/encounters" --out "$WORLD_DIR"

echo "==> Converting encounter vignettes to webp..."
src="$REPO_ROOT/assets/vignettes"
dest="$WORLD_DIR/assets/vignettes"
if [[ -d "$src" ]]; then
    converted=0
    find "$src" -type f -name "*.png" | while read -r f; do
        rel="${f#$src/}"
        out="$dest/${rel%.png}.webp"
        mkdir -p "$(dirname "$out")"
        if [[ ! -f "$out" || "$f" -nt "$out" ]]; then
            convert "$f" -quality 85 "$out"
            ((converted++)) || true
        fi
    done
    echo "    vignettes/ up to date"
else
    echo "    vignettes/ not found in assets/, skipping"
fi

echo "==> Reloading GameServer bundle..."
curl -sf -X POST http://localhost:7071/api/ops/reload-bundle && echo "    reloaded" || echo "    GameServer not running, skipping"

echo "==> Done."
