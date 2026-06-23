---
kind: plan
title: "Debug Tooling Lockdown"
state: ready
created: 2026-06-05
updated: 2026-06-05
status: current
touches:
  features: [debug-tools, server, web-ui, encounters]
provenance:
  author: claude
---
# Debug Tooling Lockdown

Replace the Shady Trader debug encounter with a proper dev-only loadout picker,
and gate all debug API endpoints behind a localhost source check. Once this
lands, the "debug affordance sweep" launch blocker in `TODO.md` evaporates:
nothing left to remember on launch day — buttons are DEV-gated, endpoints are
localhost-gated, the trader is gone.

Churn assessment (verified against code 2026-06-05): low. Everything here is
additive or delete-only; no live play path is modified.

## Background — verified call graphs

- **The Shady Trader** (`text/encounters/plains/tier1/The Shady Trader.enc`)
  claims "does not appear in production builds" but nothing enforces that; it is
  present in `worlds/production/encounters.bundle.json` today. No code references
  it. Its mechanic verbs (`add_level`, `give_gold`, `add_item`) are real verbs
  used by arc encounters (Bride's Cave, signal_array, relay_post) and stay.
- **`combat/list` + `combat/begin`** (`GameFunctions.cs` ~2035, ~2060) are called
  ONLY from `CombatPicker.tsx` via `GameContext.doCombatBegin` (single call
  site). Real fights begin server-side during the overworld roll — players never
  hit these routes.
- **`debug/add-condition`** (`GameFunctions.cs` ~1419) is called only by the CLI
  test client (`ui/cli/GameClient.cs:45`), which already runs against localhost.
- The "Pick a fight" button (`Explore.tsx:861`) is already hidden in prod builds
  via `import.meta.env.DEV`. The new loadout button uses the same gate.

## Step 1 — Loadout picker (do FIRST, it replaces the trader's job)

**Server**: new endpoint cloning `DebugAddCondition`'s shape (~35 lines):

- `POST game/{id}/debug/loadout` with body `{ "name": "..." }`
- Loadouts **hardcoded server-side** — client picks a name, never sends
  arbitrary mechanics. Port the trader's options:
  - `skill-up` — `add_level` (the "I know Kung-fu" choice; +repool not needed
    since this isn't an encounter)
  - `outdoorsman` — 500g, hunting_knife, lamellar, sleeping_kit, waterskin
  - `combat` — 500g, scimitar, brigandine, sleeping_kit, waterskin
  - `top-tier` — port the third trader loadout (top-tier gear; read it from the
    .enc before deleting)
- Apply via the same `Mechanics.Apply` verbs the trader used (already tested),
  then `store.Save(player)`. Return the standard game response (or a simple ok +
  let the UI refetch status) so the UI refreshes.

**UI** (all DEV-gated, additive):

- Clone `CombatPicker.tsx` → `LoadoutPicker.tsx` (~70 lines, mostly copy).
  Static list, no fetch needed — names + one-line descriptions.
- Second button next to "Pick a fight" in `Explore.tsx` inside the existing
  `import.meta.env.DEV` block (~8 lines). Label suggestion: "Gear up".
- One `debugLoadout()` function in `api/client.ts`.

## Step 2 — Nuke the Shady Trader

- Delete `text/encounters/plains/tier1/The Shady Trader.enc` (after porting the
  top-tier loadout in Step 1).
- Rebuild bundles for both worlds (`worlds/test/build.sh` /
  `worlds/production/update-encounters.sh` or build.sh `--skip-map --skip-assets`).
- Grep the rebuilt `encounters.bundle.json` for "Shady" to confirm it's gone.
- Check off / update the TODO.md entries that reference it (Launch Blockers
  sweep item; the `[x]` "quick start flag" QoL item points at the sweep).

## Step 3 — Localhost gate on debug endpoints

- One helper in `GameFunctions.cs`:
  `static bool IsLocalRequest(HttpRequest req)` →
  `IPAddress.IsLoopback(req.HttpContext.Connection.RemoteIpAddress ?? <deny>)`.
- Guard at the top of 4 endpoints, return 404 (NotFound, not 403 — don't
  advertise the route exists): `DebugAddCondition`, `CombatList`, `CombatBegin`,
  and the new `DebugLoadout`.
- **Fails closed in Azure**: behind the Functions front-end the remote IP is
  never loopback, so a surprise means "debug tools blocked," never "exposed."

## Smoke test (local)

1. `dotnet run --project server/GameServer` + web dev server.
2. "Gear up" button: apply each loadout, confirm gold/items/level land and the
   status panel refreshes.
3. "Pick a fight" still works (gate didn't break the picker locally).
4. CLI `debug/add-condition` still works against localhost.
5. `npm run build` the web UI and confirm neither dev button renders in the
   production build (`vite preview`).
6. `dotnet test Dreamlands.sln` — nothing here should touch tests, confirm.

## Post-deploy verification (whenever the next `./deploy.sh api` happens)

- `curl -X POST https://dreamlands-api.azurewebsites.net/api/game/<any-id>/debug/loadout -d '{"name":"combat"}'`
  → expect 404.
- Same for `combat/list` (GET) → 404.
- Then collapse the "debug affordance sweep" launch blocker in TODO.md.

## Order

Step 1 → Step 2 → Step 3, one branch. Step 1 must land before Step 2 so the
fast gear-up testing route never disappears.
