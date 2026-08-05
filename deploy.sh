#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORLD="${DREAMLANDS_WORLD:-production}"
WORLD_DIR="$SCRIPT_DIR/worlds/$WORLD"

AZURE_FUNCTIONAPP_NAME="${AZURE_FUNCTIONAPP_NAME:-dreamlands-api}"
AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-dreamlands-rg}"
VITE_API_BASE="${VITE_API_BASE:-https://dreamlands-api.azurewebsites.net}"

usage() {
  echo "Usage: deploy.sh <api|web|all>"
  echo ""
  echo "  api  — publish GameServer and deploy to Azure Functions"
  echo "  web  — build frontend, assemble with world assets, deploy to Cloudflare Pages"
  echo "  all  — both"
  exit 1
}

deploy_api() {
  local app_name="$AZURE_FUNCTIONAPP_NAME"

  echo "==> Publishing GameServer..."
  dotnet publish "$SCRIPT_DIR/server/GameServer/GameServer.csproj" \
    -c Release -o "$SCRIPT_DIR/.deploy/api"

  # host.json is required by func CLI but not included by dotnet publish
  cp "$SCRIPT_DIR/server/GameServer/host.json" "$SCRIPT_DIR/.deploy/api/"

  # Copy game data into publish output
  echo "==> Bundling game data from $WORLD_DIR..."
  mkdir -p "$SCRIPT_DIR/.deploy/api/data"
  cp "$WORLD_DIR/map.json" "$SCRIPT_DIR/.deploy/api/data/"
  cp "$WORLD_DIR/encounters.bundle.json" "$SCRIPT_DIR/.deploy/api/data/"
  cp "$SCRIPT_DIR/api-version" "$SCRIPT_DIR/.deploy/api/data/"

  # .fight files. GameData looks for a "combat" dir beside the bundle
  # (DREAMLANDS_COMBAT_DIR overrides). Without this the API loads with no combat
  # bundle at all and every fight 400s "Combat bundle not loaded" — silently, since
  # nothing else depends on it. Missed until 2026-08-04.
  rm -rf "$SCRIPT_DIR/.deploy/api/data/combat"
  cp -r "$WORLD_DIR/combat" "$SCRIPT_DIR/.deploy/api/data/combat"
  echo "    $(find "$SCRIPT_DIR/.deploy/api/data/combat" -name '*.fight' | wc -l) fight file(s)"

  echo "==> Deploying to Azure Function App: $app_name..."
  local zip_path="$SCRIPT_DIR/.deploy/api.zip"
  rm -f "$zip_path"
  (cd "$SCRIPT_DIR/.deploy/api" && zip -qr "$zip_path" .)
  az functionapp deployment source config-zip \
    -n "$app_name" -g "$AZURE_RESOURCE_GROUP" \
    --src "$zip_path"

  echo "==> API deployed."
}

deploy_web() {
  local api_base="$VITE_API_BASE"

  echo "==> Building frontend (API base: $api_base)..."
  cd "$SCRIPT_DIR/ui/web"
  VITE_API_BASE="$api_base" npm run build
  cd "$SCRIPT_DIR"

  echo "==> Assembling deploy directory..."
  rm -rf "$SCRIPT_DIR/.deploy/web"
  cp -r "$SCRIPT_DIR/ui/web/dist" "$SCRIPT_DIR/.deploy/web"

  # Remove large files that aren't needed at runtime (map.png, encounters, etc.)
  rm -f "$SCRIPT_DIR/.deploy/web/world/map.png"
  rm -rf "$SCRIPT_DIR/.deploy/web/world/encounters"

  local file_count
  file_count=$(find "$SCRIPT_DIR/.deploy/web" -type f | wc -l)
  echo "    $file_count files to deploy"

  # Pages decides Production vs Preview from the branch name, and without an
  # explicit --branch wrangler infers it from the current git branch. Deploying
  # from any branch but main therefore lands as a *preview* and leaves
  # merchant.dreamlands.org untouched — silently, with a success message.
  # Pin it. Set CF_PAGES_BRANCH to something else to publish a preview on purpose.
  local pages_branch="${CF_PAGES_BRANCH:-main}"

  echo "==> Deploying to Cloudflare Pages (branch: $pages_branch)..."
  npx wrangler pages deploy "$SCRIPT_DIR/.deploy/web" \
    --project-name=dreamlands --branch "$pages_branch" --commit-dirty=true

  echo "==> Web deployed. Live at https://merchant.dreamlands.org"
}

case "${1:-}" in
  api) deploy_api ;;
  web) deploy_web ;;
  all) deploy_api; deploy_web ;;
  *)   usage ;;
esac
