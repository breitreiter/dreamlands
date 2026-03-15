#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORLD="${DREAMLANDS_WORLD:-production}"
WORLD_DIR="$SCRIPT_DIR/worlds/$WORLD"

usage() {
  echo "Usage: deploy.sh <api|web|all>"
  echo ""
  echo "  api  — publish GameServer and deploy to Azure Functions"
  echo "  web  — build frontend, assemble with world assets, deploy to Cloudflare Pages"
  echo "  all  — both"
  echo ""
  echo "Environment:"
  echo "  DREAMLANDS_WORLD          World to deploy (default: production)"
  echo "  VITE_API_BASE             API base URL for frontend (required for web)"
  echo "  AZURE_FUNCTIONAPP_NAME    Azure Function App name (required for api)"
  echo "  AZURE_RESOURCE_GROUP      Azure resource group (default: dreamlands-rg)"
  exit 1
}

deploy_api() {
  local app_name="${AZURE_FUNCTIONAPP_NAME:?Set AZURE_FUNCTIONAPP_NAME}"

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

  echo "==> Deploying to Azure Function App: $app_name..."
  local zip_path="$SCRIPT_DIR/.deploy/api.zip"
  (cd "$SCRIPT_DIR/.deploy/api" && zip -qr "$zip_path" .)
  az functionapp deployment source config-zip \
    -n "$app_name" -g "${AZURE_RESOURCE_GROUP:-dreamlands-rg}" \
    --src "$zip_path"

  echo "==> API deployed."
}

deploy_web() {
  local api_base="${VITE_API_BASE:?Set VITE_API_BASE to the Azure Function App URL}"

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

  echo "==> Deploying to Cloudflare Pages..."
  npx wrangler pages deploy "$SCRIPT_DIR/.deploy/web" --project-name=dreamlands

  echo "==> Web deployed."
}

case "${1:-}" in
  api) deploy_api ;;
  web) deploy_web ;;
  all) deploy_api; deploy_web ;;
  *)   usage ;;
esac
