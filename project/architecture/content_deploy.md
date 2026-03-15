# Content Deploy Pipeline

**Goal:** Push encounter file changes to production in seconds, without redeploying the Azure Function App.

## Problem

The bundle (`encounters.bundle.json`) is baked into the API deploy package. `GameData` loads it once at startup as a singleton. Any .enc file change requires a full `deploy.sh api` cycle: dotnet publish, package upload, function app restart, cold start. That's ~2 minutes per iteration — tolerable for code changes, miserable for a content authoring loop.

## Current Flow

```
edit .enc files
  → update-encounters.sh (check + bundle)        ~5s
  → deploy.sh api (publish + upload + restart)    ~2 min
  → cold start                                    ~15s
```

## Proposed Flow

```
edit .enc files
  → encounter-cli push                            ~10s total
    1. check (syntax validation)
    2. bundle (produce encounters.bundle.json)
    3. upload bundle to Azure file share
    4. hit /api/admin/reload-bundle endpoint
```

## Changes

### 1. GameData: hot-reload support (server change)

`GameData.Bundle` becomes reloadable. The map stays singleton — it never changes between deploys.

```csharp
// GameData.cs — new members
private readonly string _bundlePath;
private EncounterBundle _bundle;
private readonly object _bundleLock = new();

public EncounterBundle Bundle { get { lock (_bundleLock) return _bundle; } }

public void ReloadBundle()
{
    var fresh = EncounterBundle.Load(_bundlePath);
    lock (_bundleLock) _bundle = fresh;
}
```

No per-request file I/O. No file watcher complexity. Explicit reload on demand.

### 2. Admin reload endpoint (server change)

A single new endpoint in `GameFunctions.cs`:

```csharp
[Function("ReloadBundle")]
public IActionResult ReloadBundle(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/reload-bundle")] HttpRequest req)
{
    _data.ReloadBundle();
    return new OkObjectResult(new { status = "reloaded" });
}
```

`AuthorizationLevel.Function` means the caller needs the function key — not public, but no extra auth infra. The function key is already available from `az functionapp keys list`.

### 3. `push` command in encounter-cli

New command: `encounter push [<path>] [--world <name>]`

Steps:
1. Run `check` on the encounter directory (fail-fast on syntax errors)
2. Run `bundle` to produce `encounters.bundle.json` in the world directory
3. Upload the bundle to the Azure Function App's file share via Azure CLI
4. POST to `/api/admin/reload-bundle` with the function key to trigger reload

This belongs in encounter-cli, not a standalone script, because:
- It reuses the existing check + bundle logic directly (same process, no shelling out)
- It's part of the encounter authoring workflow alongside check/bundle/fixme/generate
- Authors already have encounter-cli in their muscle memory
- The Azure upload + reload call is ~15 lines of process spawning, not worth a separate tool

Configuration (Azure Function App name, function key) reads from environment variables:
- `DREAMLANDS_FUNCTION_APP` — the app name (e.g., `dreamlands-api`)
- `DREAMLANDS_FUNCTION_KEY` — the function-level key for the admin endpoint

These can live in a `.env` file or shell profile. No config files to manage.

### 4. Upload mechanism

Azure Functions on a Consumption plan run from a file share mounted at `/home/`. The deploy script already puts the bundle at `/data/encounters.bundle.json` relative to the app root. We can overwrite that file directly:

```bash
az functionapp deploy --name dreamlands-api --resource-group dreamlands-rg \
  --src-path encounters.bundle.json \
  --target-path data/encounters.bundle.json \
  --type static
```

This updates the single file without restarting the app. Then the reload endpoint tells GameData to re-read it.

Fallback if `az functionapp deploy --type static` doesn't cooperate with the consumption plan: use the Kudu VFS API directly (`/api/vfs/data/encounters.bundle.json`), which is a simple PUT with the same function app credentials.

## What This Doesn't Cover

- **Frontend deploy**: encounter changes are API-only. No Cloudflare Pages deploy needed.
- **Map changes**: still require full `deploy.sh api` (rare, expensive mapgen anyway).
- **CI/CD**: no GitHub Actions. This is a solo/small-team project — a CLI command you run locally is simpler and more transparent than a pipeline you have to debug remotely. If that changes later, the `push` command's logic is easy to wrap in a workflow.

## Migration

1. Add reload support to GameData + the admin endpoint → deploy once with `deploy.sh api`
2. Add `push` command to encounter-cli
3. From then on, encounter iterations are just `encounter push text/encounters`

The old `update-encounters.sh` and `deploy.sh api` still work unchanged. `push` is additive.

## Effort

- GameData reload: ~15 lines of C#
- Admin endpoint: ~10 lines of C#
- encounter-cli push command: ~60 lines of C# (check + bundle + spawn az + spawn curl)
- One full API deploy to get the reload endpoint live
