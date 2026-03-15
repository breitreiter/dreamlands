# Deployment & Operations Guide

> Placeholders like `<app-name>` and `<resource-group>` refer to your actual Azure/Cloudflare resource names. Check your Azure Portal or `az resource list` if you've forgotten them.

## Architecture Overview

```
game.dreamlands.org (CNAME)
        │
   Cloudflare Pages ──── static frontend + tiles + assets
        │
        ▼ /api/*
   Azure Functions (Consumption) ── GameServer (.NET 8, isolated worker)
        │
   Cosmos DB (Serverless) ── game state persistence
```

- **Frontend**: Cloudflare Pages, serves Vite build + world tiles/assets
- **API**: Azure Functions (Consumption plan), app name `<app-name>`, resource group `<resource-group>`
- **Database**: Cosmos DB (Serverless), database `<cosmos-db>`, container `<cosmos-container>`, partition key `/id`
- **Storage account**: `<storage-account>` — used by the Functions runtime internally, not game data
- **Domain**: `game.dreamlands.org` → CNAME to `<pages-subdomain>.pages.dev`
- **Redirect**: `dreamlands.org/game` → `game.dreamlands.org` (via GitHub Pages redirect)

## How to Deploy

All deployments go through `deploy.sh` at the repo root.

### Update the game server (C# changes, balance changes, map changes)

```bash
AZURE_FUNCTIONAPP_NAME=<app-name> ./deploy.sh api
```

This does: dotnet publish → copies map.json + encounters.bundle.json + api-version from `worlds/production/` → zips → uploads via `az functionapp deployment source config-zip`.

Game data (map, bundle) is baked into the deploy package under `data/`. The server reads paths from env vars `DREAMLANDS_MAP` and `DREAMLANDS_BUNDLE` (set to `data/map.json` and `data/encounters.bundle.json` on the function app).

### Update encounters only (no server restart)

```bash
dotnet run --project text/encounter-tool/EncounterCli -- push text/encounters
```

This does: check syntax → bundle → upload bundle via `az functionapp deploy --type static` → POST `/api/ops/reload-bundle` to hot-reload. Takes ~10s vs ~2min for a full API deploy.

Requires env vars:
- `DREAMLANDS_FUNCTION_APP` — the app name (e.g., `<app-name>`)
- `DREAMLANDS_FUNCTION_KEY` — the function-level key (get it from `az functionapp keys list -n <app-name> -g <resource-group>`)

### Update the frontend (UI changes, world rebuild)

```bash
VITE_API_BASE=https://<app-name>.azurewebsites.net ./deploy.sh web
```

This does: `VITE_API_BASE=... npm run build` → copies `dist/` → strips map.png and encounters dir → deploys to Cloudflare Pages via wrangler.

### Update everything

```bash
AZURE_FUNCTIONAPP_NAME=<app-name> \
VITE_API_BASE=https://<app-name>.azurewebsites.net \
./deploy.sh all
```

### Rebuild the production world (rare)

```bash
worlds/production/build.sh
```

Produces map.json, map.png, tiles/, assets/, encounters.bundle.json. Then deploy API and/or web as needed. This takes ~15 minutes and maxes CPU — don't do it unless the map actually changed.

## What Lives Where

| Data | Where it's stored | How it gets there |
|------|-------------------|-------------------|
| map.json, encounters.bundle.json | Baked into API deploy package (`data/`) | `deploy.sh api` copies from `worlds/production/` |
| Encounter bundle (hot path) | Overwritten on Azure file share | `encounter push` uploads directly |
| Frontend + tiles + assets | Cloudflare Pages | `deploy.sh web` |
| Game saves | Cosmos DB (`dreamlands/games`) | GameServer writes at runtime |
| map.json, tiles, assets (client) | Served from `ui/web/public/world` symlink → `worlds/production/` | World build script |

## Environment Variables

### Azure Function App (set via `az functionapp config appsettings set`)

| Variable | Value | Purpose |
|----------|-------|---------|
| `DREAMLANDS_MAP` | `data/map.json` | Path to map (relative to assembly dir when deployed) |
| `DREAMLANDS_BUNDLE` | `data/encounters.bundle.json` | Path to encounter bundle |
| `DREAMLANDS_API_VERSION` | `1` | API version (also read from `data/api-version` file) |
| `DREAMLANDS_COSMOS` | Connection string | Cosmos DB connection. Falls back to local file store when unset |

### Local dev (optional)

| Variable | Purpose |
|----------|---------|
| `DREAMLANDS_NO_ENCOUNTERS` | Set to `1` to skip encounter triggering |
| `DREAMLANDS_NO_CAMP` | Set to `1` to skip end-of-day camp |
| `DREAMLANDS_SAVES` | Override local save directory (default: `{repoRoot}/saves`) |

### Build-time

| Variable | Used by | Purpose |
|----------|---------|---------|
| `VITE_API_BASE` | Vite build | API base URL baked into frontend |
| `AZURE_FUNCTIONAPP_NAME` | deploy.sh | Target function app |
| `AZURE_RESOURCE_GROUP` | deploy.sh | Resource group (default: `<resource-group>`) |
| `DREAMLANDS_FUNCTION_APP` | encounter push | App name for bundle upload |
| `DREAMLANDS_FUNCTION_KEY` | encounter push | Function key for admin endpoint |

## Local Dev Setup

Run the game server locally — no Azure needed:

```bash
dotnet run --project server/GameServer
```

Without `DREAMLANDS_COSMOS`, it uses `LocalFileStore` writing to `{repoRoot}/saves/`. Without `DREAMLANDS_MAP`/`DREAMLANDS_BUNDLE`, it walks up from the assembly to find `Dreamlands.sln` and reads from `worlds/production/`.

Frontend dev server with API proxy:

```bash
cd ui/web && npm run dev    # port 3000, proxies /api to localhost:7071
```

## Troubleshooting

### API returns 500 or won't start

- **Check Application Insights** in Azure Portal for exception details
- **Cold start**: Consumption plan functions can take 10-30s after being idle. The frontend sends a fire-and-forget `/api/health` ping on the splash screen to warm it up
- **Missing data files**: If map.json or encounters.bundle.json aren't found, GameData constructor crashes. Check that `deploy.sh api` ran cleanly and the env vars point to the right paths

### API version mismatch error in browser

The frontend bakes `__API_VERSION__` from `api-version` at the repo root. The server reads the same file (or the env var). If you deployed the API but not the frontend (or vice versa) and the version changed, you'll get a mismatch banner.

Fix: deploy both, or make sure the `api-version` file matches what's deployed.

### CORS errors in browser console

GameServer is configured to accept cross-origin requests. If you see CORS errors:
- Confirm the frontend is hitting the right API URL (check `VITE_API_BASE` used at build time)
- Confirm the function app is actually running (hit `/api/health` directly)

### Encounter push fails

- **"Set DREAMLANDS_FUNCTION_APP and DREAMLANDS_FUNCTION_KEY"** — set these env vars. Get the function key with:
  ```bash
  az functionapp keys list -n <app-name> -g <resource-group>
  ```
- **Azure upload fails** — make sure `az login` is current. Sessions expire.
- **Reload returns 401/403** — function key is wrong or expired. Re-fetch it.

### Tiles not loading in browser

- Check that `ui/web/public/world` symlink points to the right world directory
- Check that the world has been built (`worlds/production/build.sh`)
- Tiles are at `public/world/tiles/{z}/{x}/{y}.png` — the symlink must resolve

### Cosmos DB issues

- **Locally**: just unset `DREAMLANDS_COSMOS` to fall back to file-based saves
- **Production**: connection string is set on the function app. Refresh it if you rotated keys:
  ```bash
  COSMOS_CONN=$(az cosmosdb keys list -n <cosmos-account> -g <resource-group> \
      --type connection-strings --query "connectionStrings[0].connectionString" -o tsv)
  az functionapp config appsettings set -n <app-name> -g <resource-group> \
      --settings "DREAMLANDS_COSMOS=$COSMOS_CONN"
  ```

### Azure login / CLI issues

```bash
az login           # re-authenticate (opens browser)
wrangler login     # re-authenticate Cloudflare
az account show    # verify correct subscription
```

## Cost Controls

| Service | Free Tier | Expected Cost |
|---------|-----------|---------------|
| Cloudflare Pages | Unlimited bandwidth, 500 deploys/mo | $0 |
| Azure Functions (Consumption) | 1M executions/mo, 400K GB-s | $0 |
| Cosmos DB (Serverless) | Per-request pricing | ~$0.10-0.50/mo |
| Azure Storage (Functions bookkeeping) | — | ~$0.01/mo |
| App Insights (capped at 100 MB/day) | 5 GB/mo | $0 |
| **Total** | | **< $1/mo** |

Budget alert is set at $10/mo on the resource group (50%/100%/150% thresholds). Application Insights daily cap is 0.1 GB — uncapped App Insights is the #1 surprise Azure bill.

## CLI Tools Required

- `az` — Azure CLI (`curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash`)
- `func` — Azure Functions Core Tools v4 (`npm install -g azure-functions-core-tools@4 --unsafe-perm true`). The apt repo doesn't support Ubuntu 25.10+.
- `wrangler` — Cloudflare CLI (`npm install -g wrangler`)
