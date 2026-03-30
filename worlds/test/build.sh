#!/usr/bin/env bash
set -euo pipefail

WORLD_DIR="$(cd "$(dirname "$0")" && pwd)"
WORLD_NAME="$(basename "$WORLD_DIR")"
REPO_ROOT="$(cd "$WORLD_DIR/../.." && pwd)"

SKIP_MAP=false
SKIP_ASSETS=false

for arg in "$@"; do
    case "$arg" in
        --skip-map)    SKIP_MAP=true ;;
        --skip-assets) SKIP_ASSETS=true ;;
        *)             echo "Usage: $0 [--skip-map] [--skip-assets]"; exit 1 ;;
    esac
done

# --- Map generation ---

if [[ "$SKIP_MAP" == false ]]; then
    echo "==> Generating map for '$WORLD_NAME'..."
    dotnet run --project "$REPO_ROOT/mapgen" -- generate "$WORLD_NAME"
else
    echo "==> Skipping map generation (--skip-map)"
fi

# --- Asset copy ---

if [[ "$SKIP_ASSETS" == false ]]; then
    echo "==> Copying assets..."

    for dir in icons portraits equipment vignettes UI; do
        src="$REPO_ROOT/assets/$dir"
        # UI -> ui (lowercase in output)
        dest_name=$(echo "$dir" | tr '[:upper:]' '[:lower:]')
        dest="$WORLD_DIR/assets/$dest_name"

        if [[ -d "$src" ]]; then
            rm -rf "$dest"
            mkdir -p "$dest"
            if compgen -G "$src/*" >/dev/null 2>&1; then
                if [[ "$dir" == "vignettes" ]]; then
                    # Convert vignettes from PNG to WebP for smaller file sizes
                    find "$src" -type d | while read -r subdir; do
                        mkdir -p "$dest/${subdir#$src/}"
                    done
                    find "$src" -type f -name "*.png" | while read -r f; do
                        rel="${f#$src/}"
                        convert "$f" -quality 85 "$dest/${rel%.png}.webp"
                    done
                    echo "    $dest_name/ converted to webp"
                else
                    cp -r "$src"/. "$dest"/
                    echo "    $dest_name/ copied"
                fi
            fi
        else
            echo "    $dest_name/ not found in assets/, skipping"
        fi
    done
else
    echo "==> Skipping asset copy (--skip-assets)"
fi

# --- Encounters ---

echo "==> Bundling encounters..."
dotnet run --project "$REPO_ROOT/text/encounter-tool/EncounterCli" -- bundle "$REPO_ROOT/text/encounters" --out "$WORLD_DIR"

echo "==> Bundling tactical encounters..."
dotnet run --project "$REPO_ROOT/text/encounter-tool/EncounterCli" -- bundle-tactical "$REPO_ROOT/text/encounters" --out "$WORLD_DIR"

# --- Summary ---

echo ""
echo "World '$WORLD_NAME' assembled at $WORLD_DIR/"
echo "Contents:"
ls -1 "$WORLD_DIR/" | sed 's/^/  /'
