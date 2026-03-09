# Deployment Plan

## Architecture Overview

```
Browser
  │
  ├── Static SPA ─────────→  Cloudflare Pages  (free tier)
  │                            ui/web dist bundle
  │
  ├── /api/* ─────────────→  Azure Functions  (Consumption plan, .NET 8 isolated)
  │                            GameServer logic
  │                            State in Azure Table Storage
  │
  └── /world/tiles/...  ──→  Cloudflare R2 + CDN  (free tier)
      /world/assets/...        tiles, icons, portraits, equipment, vignettes
```

Three services, three providers, all on free/near-free tiers.

## 1. Cloudflare R2 — Static Game Assets

Already planned in `cdn_deployment.md`. Summary:

- **Bucket**: `game-assets`, public access enabled
- **Custom domain**: `assets.dreamlands.example` (or whatever domain you pick)
- **Upload**: `push.sh` using `aws s3 sync` with R2 endpoint
- **Cache**: `Cache-Control: public, max-age=31536000, immutable`
- **Content**: tiles/, assets/ (~500 MB production). NOT map.json or encounters.bundle.json
- **Cost**: Free. 10 GB storage, 10M reads/month, $0 egress

No new work here — the existing `push.sh` and `cdn_deployment.md` cover this.

## 2. Cloudflare Pages — React SPA

Host the Vite build output as a static site.

### Why Pages over Azure Static Web Apps

- Same Cloudflare account as R2 — one dashboard
- Git-connected deploys (push to branch → auto-deploy)
- Preview deployments per PR for free
- No cold start — it's just static files on the edge
- Free tier: unlimited sites, unlimited bandwidth

### Setup

1. Connect the GitHub repo to Cloudflare Pages
2. Build configuration:
   - **Build command**: `cd ui/web && npm ci && npm run build`
   - **Output directory**: `ui/web/dist`
   - **Environment variable**: `VITE_ASSET_BASE=https://assets.dreamlands.example/worlds/production`
   - **Environment variable**: `VITE_API_BASE=https://api.dreamlands.example` (or leave empty if using Pages Functions proxy — see below)
3. Custom domain: `dreamlands.example`

### API Proxy Option

Cloudflare Pages supports "Functions" — lightweight Workers that run at the edge. You can
use a catch-all `functions/api/[[path]].ts` to proxy `/api/*` requests to the Azure Functions
backend. This keeps the SPA and API on the same origin, avoiding CORS entirely:

```typescript
// functions/api/[[path]].ts
export const onRequest: PagesFunction = async (context) => {
  const url = new URL(context.request.url);
  url.hostname = "api.dreamlands.example";  // your Azure Functions URL
  return fetch(new Request(url.toString(), context.request));
};
```

Pros: no CORS, cleaner URLs, trivial to add later.
Cons: adds ~10ms latency per API call (edge → Azure), minor debugging indirection.

**Recommendation**: Use the proxy. It's 10 lines and eliminates an entire class of problems.

### Asset URL Wiring

The React app currently uses relative `/world/...` URLs served via symlink. For production:

```typescript
// src/config.ts
export const ASSET_BASE = import.meta.env.VITE_ASSET_BASE ?? "/world";
```

All tile/asset URLs become `${ASSET_BASE}/tiles/{z}/{x}/{y}.png`, etc. Leaflet's `tileLayer`
URL template accepts this directly. Dev keeps working via the symlink (VITE_ASSET_BASE unset).

## 3. Azure Functions — Game Backend

Port the current ASP.NET Minimal API to Azure Functions (isolated worker, .NET 8).

### Why Azure Functions

- Pay-per-execution: $0 until meaningful traffic (1M free executions/month)
- No server to manage, no idle costs
- .NET 8 isolated worker model — standard ASP.NET code, not the old in-process model
- Consumption plan: auto-scales to zero

### What Changes from the Current GameServer

The game logic doesn't change. The porting work is:

1. **New project**: `server/GameFunctions/` — Azure Functions isolated worker project
2. **Endpoint mapping**: Each `app.Map*()` in Program.cs becomes a `[Function("name")]` method
3. **Startup**: Load map.json + encounters.bundle.json from Azure Blob Storage (or embed in
   deployment package if they're small enough — 2 MB + 220 KB is fine to embed)
4. **State store**: Replace `LocalFileStore` with Azure Table Storage (see below)
5. **CORS**: Configure in Azure portal or `host.json` — lock to your Pages domain only

### State Storage: Azure Table Storage

Azure Table Storage is the simplest, cheapest option for key-value game state:

- **Table**: `GameSaves`
- **PartitionKey**: `"game"` (single partition is fine at hobby scale)
- **RowKey**: `gameId`
- **Properties**: `StateJson` (compressed PlayerState), `Version` (int), `UpdatedAt`
- **Concurrency**: ETag-based optimistic concurrency (built into Table Storage)
- **Cost**: Effectively free — $0.045/GB/month storage, $0.00036 per 10K transactions

Why not Cosmos DB: Table Storage does everything we need at 1/100th the cost. Cosmos adds
RU billing complexity and a $25/month minimum on provisioned mode. Serverless Cosmos is
viable but still more expensive than Tables for pure key-value access.

Migration path to Cosmos if needed later: Table Storage and Cosmos Table API are wire-compatible.
You can literally point the SDK at a Cosmos endpoint later without changing code.

### Embedding Map + Bundle vs. Blob Storage

The map.json (2 MB) and encounters.bundle.json (220 KB) are read-only and loaded once at startup.
Two options:

**Option A — Embed in deployment package** (simpler):
- Include both files in the Functions project as content files
- They deploy with the code, no Blob Storage dependency
- To update: redeploy the function
- Good enough until you need to update encounters without redeploying code

**Option B — Azure Blob Storage** (more flexible):
- Upload to a private blob container during world push
- Function reads at startup and caches in static memory
- Can update encounters by uploading a new blob + restarting the function
- Adds one more Azure service but is more operationally flexible

**Recommendation**: Start with Option A. The encounters bundle changes rarely, and redeploying
a Function takes <30 seconds. Move to Blob when/if it becomes a pain point.

## Initial Setup Checklist

### Accounts

- [ ] **Cloudflare account** (free) — for R2 + Pages
- [ ] **Azure account** (free tier) — for Functions + Table Storage
- [ ] **Domain** — register something, add to Cloudflare DNS
- [ ] **GitHub repo** — already exists, connect to CF Pages

### Cloudflare Setup

1. Add domain to Cloudflare, update nameservers at registrar
2. Create R2 bucket `game-assets`, enable public access
3. Attach custom subdomain `assets.dreamlands.example` to the bucket
4. Generate R2 API token (read/write on `game-assets`)
5. Connect repo to Pages, configure build settings (see Section 2)
6. Attach custom subdomain `dreamlands.example` to the Pages project
7. (Optional) Add the API proxy function

### Azure Setup

1. Create resource group `dreamlands-prod`
2. Create Storage Account `dreamlandsprod` (or similar)
   - Create Table `GameSaves`
   - (If using Blob for map/bundle) Create container `world-data`, upload files
3. Create Function App `dreamlands-api`
   - Runtime: .NET 8 isolated
   - Plan: Consumption (serverless)
   - Region: pick one close to you (e.g., East US, West Europe)
4. Configure Function App settings:
   - `AzureWebJobsStorage` → connection string (auto-configured)
   - `GameTableConnection` → Storage Account connection string
   - (If using Blob) `WorldBlobConnection` → same or different Storage Account
5. (Optional) Attach custom domain `api.dreamlands.example` via Azure + CF DNS

### Local Key Storage

Keep all secrets out of the repo. Use a `.env.local` file (gitignored) or your shell profile:

```bash
# Cloudflare R2 (for push.sh)
export CF_ACCOUNT_ID="..."
export R2_ACCESS_KEY_ID="..."
export R2_SECRET_ACCESS_KEY="..."

# Azure (for deployment tooling — az CLI handles auth separately)
# Use `az login` for interactive dev, managed identity in production
```

For Azure Functions deployment, use `az functionapp publish` or GitHub Actions with
`AZURE_FUNCTIONAPP_PUBLISH_PROFILE` secret. Never store Azure connection strings locally
unless testing — use `az login` + DefaultAzureCredential.

### URLs (production)

| Service | URL |
|---------|-----|
| Game (SPA) | `https://dreamlands.example` |
| API | `https://dreamlands.example/api/*` (proxied to Azure) or `https://api.dreamlands.example` |
| Assets | `https://assets.dreamlands.example/worlds/production/tiles/...` |

## Security Hardening

You said you're not especially worried about game state but very worried about API keys and
runaway cloud bills. Here's the plan in that priority order.

### Prevent Bill Shock (highest priority)

1. **Azure budget alert**: Set a Cost Management budget on the resource group at $5, $15, $50
   thresholds with email alerts. Do this before deploying anything.

2. **Azure spending cap**: The free Azure account has a spending cap. If you're on Pay-As-You-Go,
   consider setting a spending limit in the subscription settings.

3. **Function App throttling**:
   - Set `functionAppScaleLimit` in host.json to cap concurrent instances (e.g., 3)
   - Set `maxConcurrentRequests` in host.json (e.g., 100)
   - These prevent a DDoS or bot from spinning up dozens of instances

4. **R2 has no egress fees**, so a traffic spike to assets costs nothing. This is the main
   reason R2 was chosen.

5. **Pages has no bandwidth fees** on the free tier. Another non-risk.

6. **Table Storage is inherently cheap** — even a million requests costs ~$0.04.

7. **Application Insights** (if enabled): Set a daily data cap (e.g., 100 MB/day) immediately.
   Uncapped App Insights is the #1 surprise Azure bill for hobby projects.

### Protect API Keys and Secrets

1. **Never commit secrets**. The `.env.local` pattern above keeps them out of git.

2. **Azure Key Vault** (optional): For production, store the Table Storage connection string
   in Key Vault and reference it from Function App settings. This is best practice but overkill
   for a hobby project — Function App settings are encrypted at rest and not exposed via API.

3. **R2 API token**: Scope it to read/write on the single `game-assets` bucket only. If leaked,
   the worst case is someone overwrites your tiles (annoying, not expensive).

4. **GitHub Actions secrets**: If you set up CI/CD, use GitHub's encrypted secrets for the
   Azure publish profile. Never echo secrets in workflow logs.

5. **CORS lock-down**: In the Function App, set allowed origins to `https://dreamlands.example`
   only. No `*`. This prevents other sites from calling your API.

### Prevent Abuse

1. **Rate limiting**: Azure Functions doesn't have built-in rate limiting. Options:
   - **Cloudflare proxy** (recommended): If you proxy API calls through Pages Functions or
     put the Azure Functions domain behind Cloudflare, you get Cloudflare's free DDoS protection
     and can add rate limiting rules (free tier: 1 rule, enough for `/api/*`)
   - **In-code rate limiting**: Middleware that tracks requests per IP using Table Storage or
     in-memory cache. Simple but adds code.
   - **Azure API Management** (APIM): Full-featured but expensive and overkill

   **Recommendation**: Route the API through Cloudflare (either via Pages proxy or by pointing
   `api.dreamlands.example` through CF's orange-cloud proxy to Azure). This gives you free
   DDoS protection and basic rate limiting with zero code.

2. **Input validation**: The game engine already validates actions, but add request-size limits
   in the Function App (default is 100 MB — lower it to 1 MB in host.json).

3. **Game creation throttle**: Rate-limit `POST /api/game/new` more aggressively than other
   endpoints (e.g., 5/minute per IP). Each new game creates a Table Storage row, and while
   storage is cheap, unbounded creation by a bot is wasteful.

4. **No admin endpoints**: The Function App should have no management, debug, or diagnostic
   endpoints exposed. Azure provides these via the portal.

### Security Testing Before Launch

- [ ] Verify CORS rejects requests from other origins
- [ ] Verify no secrets in the deployed bundle (search dist/ and function package for API keys)
- [ ] Verify rate limiting works (hit `/api/game/new` 20 times rapidly, confirm throttling)
- [ ] Verify Function App is not accessible on any port other than 443
- [ ] Verify R2 bucket allows reads but not writes from public URLs
- [ ] Verify Table Storage is not publicly accessible (it shouldn't be by default)
- [ ] Run `az security assessment` on the resource group (Azure Defender free tier)
- [ ] Check Azure Advisor recommendations for the resource group

## Test/Staging Environment

**Recommendation: You don't need a separate cloud staging environment yet.**

Here's why:

- The game engine has 295 deterministic tests that cover mechanics, encounters, and orchestration
- The backend is stateless — if it works locally with the production map + bundle, it'll work
  in Azure
- Cloudflare Pages gives you **preview deployments per PR for free** — every push to a non-main
  branch gets a unique URL. This covers frontend staging
- The map/encounters rarely change, and when they do, you can verify locally

**What to do instead:**

1. **Local integration testing**: Run GameServer locally against the production map.json and
   encounters.bundle.json. This catches 99% of issues.
2. **Pages preview deploys**: Push a branch, get a preview URL, click around. Free and automatic.
3. **Azure Functions staging slots**: If you eventually want a staging API, Azure Functions
   supports deployment slots (requires Standard plan, not free). Cross that bridge later.

**When to add a cloud staging environment:**

- When you have users and breaking production is costly
- When the deploy pipeline is complex enough that "works locally" isn't sufficient
- When you add external dependencies (Cosmos DB, external APIs) that behave differently
  from local mocks

## Deployment Workflow

### First Deploy (one-time)

```
1. Set up Cloudflare: domain, R2 bucket, Pages project
2. Set up Azure: resource group, storage account, function app
3. Write the Azure Functions project (port from GameServer)
4. Write the VITE_ASSET_BASE config + Pages proxy function
5. Push assets to R2: worlds/production/push.sh
6. Deploy functions: az functionapp publish or GitHub Actions
7. Push to main → Pages auto-deploys
8. Verify end-to-end
```

### Ongoing Deploys

**Code changes** (game logic, UI):
```
git push → Pages auto-deploys frontend
az functionapp publish → deploys backend (or set up GitHub Actions for this too)
```

**World regeneration** (rare — map or encounter changes):
```
worlds/production/build.sh          # rebuild map + tiles + encounters
worlds/production/push.sh           # sync tiles + assets to R2
az functionapp publish               # if map.json/bundle.json are embedded in the function
```

**Encounter-only update**:
```
worlds/production/update-encounters.sh   # rebundle .enc files
az functionapp publish                    # redeploy with new bundle (if embedded)
```

### GitHub Actions (optional but recommended)

A simple workflow that deploys on push to main:

```yaml
# .github/workflows/deploy.yml
name: Deploy
on:
  push:
    branches: [main]

jobs:
  deploy-functions:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0' }
      - run: dotnet publish server/GameFunctions -c Release -o ./publish
      - uses: Azure/functions-action@v1
        with:
          app-name: dreamlands-api
          package: ./publish
          publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
  # Frontend deploys automatically via Cloudflare Pages git integration
```

## Code to Write

These are the concrete pieces of code needed for deployment:

### 1. Azure Functions Project (`server/GameFunctions/`)

New project — a thin adapter over the existing Orchestration library:

- `Program.cs` — Worker host setup, load map + bundle
- `GameFunctions.cs` — HTTP trigger functions mirroring the current endpoints
- `TableGameStore.cs` — `IGameStore` implementation using Azure Table Storage
- `host.json` — function app configuration (rate limits, request size limits)

The game logic stays in `lib/Orchestration/` and `lib/Game/` — unchanged.

### 2. Asset Base Configuration (`ui/web/src/config.ts`)

~5 lines. Read `VITE_ASSET_BASE` and export it for use in tile/asset URLs.

### 3. Pages Proxy Function (`ui/web/functions/api/[[path]].ts`)

~10 lines. Proxies `/api/*` to Azure Functions.

### 4. Deploy Scripts

- Update `push.sh` with final R2 bucket/domain details
- (Optional) GitHub Actions workflow

### 5. IGameStore Interface

Extract an interface from the current `LocalFileStore` so both local dev and Azure Table
implementations share the same contract. This is the one interface that's justified — it's
a real environment boundary.

## Other Practical Considerations

### Domain and DNS

Pick a domain early. Everything chains off it — Cloudflare DNS, Pages custom domain, R2
custom domain, Azure Functions custom domain, CORS config, Google OAuth redirect URIs
(from `google_oauth.md`). Changing it later means updating all of these.

### Cold Starts

Azure Functions Consumption plan has cold starts (~2-5 seconds for .NET 8 isolated). The
first request after idle takes noticeably longer. Options:

- **Timer trigger warmup**: A function that runs every 5 minutes to keep the instance warm.
  Costs a few cents/month in extra executions. Simple and effective.
- **Azure Functions Premium plan**: Always-warm instances, $0 idle but minimum ~$15/month.
  Overkill for now.
- **Client-side handling**: Show a loading indicator on the first API call. The splash screen
  already has a natural loading state — use it.

**Recommendation**: Accept cold starts for now. Add the timer warmup if it bothers you.
The splash screen → new game flow naturally absorbs the first cold start.

### Monitoring (Minimal)

- **Azure portal metrics**: Function execution count, duration, errors — built in, free
- **Application Insights**: Enable with a daily data cap (100 MB). Gives you request logs,
  failure analysis, and basic dashboards. Free up to 5 GB/month.
- **Cloudflare analytics**: Pages and R2 both have built-in analytics. Free.
- **Alerting**: Azure budget alerts (see Security section) + App Insights failure alerts

Don't build dashboards or set up Grafana until you have traffic to look at.

### Backup and Recovery

- **Game state**: Table Storage has no built-in point-in-time restore on the free tier.
  If you care about save recovery, add a daily export to Blob Storage (a timer-triggered
  function that dumps the table). But honestly — at hobby scale, saves are disposable.
- **World data**: map.json and encounters.bundle.json are derived from source (mapgen + .enc
  files). If R2 data is lost, just re-run `build.sh` + `push.sh`.
- **Code**: it's in git. Nothing to do.

### When Google OAuth Gets Added

The `google_oauth.md` plan is compatible with this deployment:

- Google OAuth client ID goes in Function App settings (not in code)
- Token verification happens in the Functions backend
- User store goes in the same Table Storage account (new table `Users`)
- CORS is already locked down to the Pages domain
- OAuth redirect URI will be `https://dreamlands.example` (the Pages domain)

### Cost Summary (Projected)

| Service | Free Tier | Projected Monthly Cost |
|---------|-----------|----------------------|
| Cloudflare R2 | 10 GB storage, 10M reads | $0 |
| Cloudflare Pages | Unlimited sites/bandwidth | $0 |
| Azure Functions (Consumption) | 1M executions, 400K GB-s | $0 |
| Azure Table Storage | N/A but very cheap | <$0.01 |
| Azure App Insights (capped) | 5 GB/month | $0 |
| Domain registration | — | ~$10/year |
| **Total** | | **~$1/month or less** |

This stays $0 until you have real traffic. Even at 1000 daily active players, you'd stay
well under $5/month.

## Time Estimate (Human Effort)

Total: **6–9 hours** spread across two sessions (one for setup, one for integration testing).
Most of the code will be written by Claude; this is the time *you* spend clicking, pasting,
reviewing, and verifying.

### Session 1: Accounts + Infrastructure (~2–3 hours)

| Task | Your Time | Notes |
|------|-----------|-------|
| Register domain | 15 min | Pick a name, pay, done |
| Cloudflare account + add domain | 15 min | Create account, update nameservers at registrar |
| Create R2 bucket + public access + custom domain | 15 min | Dashboard clicks, wait for DNS propagation |
| Generate R2 API token, save locally | 10 min | Copy 3 values to `.env.local` |
| Azure account (if new) | 20 min | Sign up, verify, set up billing. Skip if you have one |
| Azure resource group + storage account + table | 15 min | Portal or `az` CLI — straightforward |
| Azure Function App creation | 15 min | Portal wizard or `az functionapp create` |
| Azure budget alerts ($5/$15/$50) | 10 min | Cost Management → Budgets. Do this before anything else |
| App Insights daily cap | 5 min | If enabled during Function App creation |
| Copy connection strings to Function App settings | 10 min | 2-3 settings in the portal |
| DNS propagation wait | 0 min active | Happens in the background, but might delay testing by 10-30 min |

**Subtotal: ~2–2.5 hours of active work**, plus up to 30 min of DNS propagation downtime
where you can do something else.

### Session 2: Code + Deploy + Verify (~4–6 hours)

| Task | Your Time | Notes |
|------|-----------|-------|
| Review Azure Functions project (Claude writes it) | 30 min | Read through GameFunctions.cs, TableGameStore.cs, host.json |
| Review IGameStore interface + wiring | 10 min | Small change, just verify the contract |
| Review asset base config + Pages proxy | 10 min | ~15 lines total, quick check |
| First R2 push (`push.sh`) | 15 min | ~500 MB upload, depends on your connection. Watch for errors |
| First Function App deploy | 10 min | `dotnet publish` + `az functionapp publish` |
| First Pages deploy | 10 min | Push to main, watch Cloudflare build log |
| End-to-end testing: splash → new game → play | 30 min | Click through the full flow, check tile loading, API calls |
| Debug first-deploy issues | 30–90 min | CORS misconfiguration, wrong env var name, asset URL typo, etc. There's always something |
| Security spot-check | 20 min | Run through the security checklist in the plan |
| CORS verification | 10 min | `curl` from wrong origin, confirm rejection |
| Set up GitHub Actions (optional) | 20 min | Copy workflow YAML, add publish profile as repo secret, push, watch it run |

**Subtotal: ~3–5 hours**, depending on how many first-deploy surprises you hit.

### Ongoing (per deploy)

| Task | Your Time |
|------|-----------|
| Code change deploy (push to main) | 0 min (auto) |
| World regeneration + asset push | 20 min (mostly waiting for mapgen + upload) |
| Encounter-only update | 5 min |
| Check Azure bill / alerts | 2 min/month |

### Risk Buffer

The "debug first-deploy issues" line is the wild card. Common time sinks:
- DNS propagation not complete when you try to test (fix: wait, or test with the `.pages.dev` URL first)
- CORS or proxy misconfiguration (fix: usually one setting, but finding it takes 20 min)
- Azure Functions cold start feels broken the first time (fix: just wait 5 seconds)
- Asset URLs wrong in production but fine in dev (fix: check VITE_ASSET_BASE value)

None of these are hard problems, but they all require round-trips of "change setting → redeploy → test → nope → try again." Budget an hour for this and be pleasantly surprised if you don't need it.
