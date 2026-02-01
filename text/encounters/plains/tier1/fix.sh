#!/usr/bin/env bash
set -euo pipefail

# ── Resolve paths relative to this script's location ──
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEXT_DIR="$(cd "$SCRIPT_DIR/../../.." && pwd)"
PROJECT="$TEXT_DIR/encounter-tool/EncounterCli"

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

# ── Parse arguments ──
EXTRA_ARGS=()
PROMPTS_ONLY=false
TARGET_FILE=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --prompts-only) PROMPTS_ONLY=true; EXTRA_ARGS+=(--prompts-only) ;;
        --config)       CONFIG_ARG=(--config "$2"); shift ;;
        *)              
            if [[ -z "$TARGET_FILE" ]]; then
                TARGET_FILE="$1"
            else
                EXTRA_ARGS+=("$1")
            fi
            ;;
    esac
    shift
done

# ── Validate target file ──
if [[ -z "$TARGET_FILE" ]]; then
    echo "Usage: $0 <file.enc> [--config <path>] [--prompts-only]" >&2
    echo "" >&2
    echo "Process FIXME: lines in an encounter file using LLM." >&2
    echo "Replaces FIXME: lines with REVIEW: expanded prose." >&2
    echo "" >&2
    echo "Options:" >&2
    echo "  --config <path>     Path to appsettings.json (API config)" >&2
    echo "  --prompts-only      Show prompts without calling LLM" >&2
    exit 1
fi

# Allow relative paths from script directory
if [[ ! -f "$TARGET_FILE" ]]; then
    if [[ -f "$SCRIPT_DIR/$TARGET_FILE" ]]; then
        TARGET_FILE="$SCRIPT_DIR/$TARGET_FILE"
    else
        echo "File not found: $TARGET_FILE" >&2
        exit 1
    fi
fi

TARGET_FILE="$(realpath "$TARGET_FILE")"

echo "=== Encounter Fixme ==="
echo "File:     $TARGET_FILE"
if [[ ${#CONFIG_ARG[@]} -gt 0 ]]; then
    echo "Config:   ${CONFIG_ARG[1]}"
fi
if [[ "$PROMPTS_ONLY" == "true" ]]; then
    echo "Mode:     prompts only (no LLM calls)"
fi
echo ""

dotnet run --project "$PROJECT" -- fixme \
    "$TARGET_FILE" \
    "${CONFIG_ARG[@]}" \
    "${EXTRA_ARGS[@]}"

if [[ "$PROMPTS_ONLY" == "false" ]]; then
    echo ""
    echo "=== Review changes in $TARGET_FILE ==="
fi
