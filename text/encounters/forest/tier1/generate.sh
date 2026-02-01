#!/usr/bin/env bash
set -euo pipefail

# ── Resolve paths relative to this script's location ──
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEXT_DIR="$(cd "$SCRIPT_DIR/../../.." && pwd)"
LOCALE="$SCRIPT_DIR/locale_guide.txt"
ORACLES="$TEXT_DIR/encounters/generation"
PROJECT="$TEXT_DIR/encounter-tool/EncounterCli"

# ── Validate required files ──
for f in "$LOCALE" "$ORACLES/Situation.txt" "$ORACLES/Forcing.txt" "$ORACLES/Twist.txt"; do
    if [[ ! -f "$f" ]]; then
        echo "Missing required file: $f" >&2
        exit 1
    fi
done

# ── Determine output path ──
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
BIOME="$(basename "$(dirname "$SCRIPT_DIR")")"
TIER="$(basename "$SCRIPT_DIR")"
OUT="$SCRIPT_DIR/generated_${TIMESTAMP}.enc"

# ── Find config (appsettings.json) ──
# Search order: script dir, encounter-tool dir, text dir
CONFIG=""
for candidate in "$SCRIPT_DIR/appsettings.json" "$TEXT_DIR/encounter-tool/appsettings.json" "$TEXT_DIR/appsettings.json"; do
    if [[ -f "$candidate" ]]; then
        CONFIG="$candidate"
        break
    fi
done

CONFIG_ARG=()
if [[ -n "$CONFIG" ]]; then
    CONFIG_ARG=(--config "$CONFIG")
fi

# ── Pass-through flags ──
EXTRA_ARGS=()
PROMPTS_ONLY=false
while [[ $# -gt 0 ]]; do
    case "$1" in
        --prompts-only) PROMPTS_ONLY=true; EXTRA_ARGS+=(--prompts-only) ;;
        --config)       CONFIG_ARG=(--config "$2"); shift ;;
        --out)          OUT="$2"; shift ;;
        *)              EXTRA_ARGS+=("$1") ;;
    esac
    shift
done

echo "=== Encounter Generation ==="
echo "Biome:    $BIOME/$TIER"
echo "Locale:   $LOCALE"
echo "Oracles:  $ORACLES"
if [[ "$PROMPTS_ONLY" == "false" ]]; then
    echo "Output:   $OUT"
fi
if [[ ${#CONFIG_ARG[@]} -gt 0 ]]; then
    echo "Config:   ${CONFIG_ARG[1]}"
fi
echo ""

dotnet run --project "$PROJECT" -- generate \
    --out "$OUT" \
    "${CONFIG_ARG[@]}" \
    "${EXTRA_ARGS[@]}"

if [[ "$PROMPTS_ONLY" == "false" && -f "$OUT" ]]; then
    echo ""
    echo "=== Written to $OUT ==="
fi
