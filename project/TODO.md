# Dreamlands TODO

Three buckets toward playable alpha.

--

## 1. Playable Alpha — Code & Mechanics

Features, fixes, and balancing needed for a complete gameplay loop.

### Frontend

- [x] Redesign severe condition end-of-day screen
- [x] UX design: Town — Guild Bank
- [ ] Fix mobile layout — desktop-first design breaks on phones and tablets:
      fixed-width panels (420px inventory mechanics column), 3-column layouts with
      no stacking breakpoint, 20px base font too large for phones, InstrumentCluster
      overlay not adapted for small screens, no touch-friendly target sizing

### Map Generation

- [ ] Bisect oversized T1 regions — same technique as T3 bisect (TierAssigner splits by
      distance from city), but inner nodes stay T1 and outer nodes become T2. Prevents a
      mega-T1 region from making an entire biome feel safe.
- [ ] Auto-named regions (`MapGenerator.cs` TODO: generated region names for game UI)

### Road Encounter System (Tableau)

- [ ] Design aspect taxonomy — brainstorm 15-25 aspects starting from biome vocabularies
      (affordance: flammable, heavy, loose, elevated, etc. + capability: fire, reach, sharp, etc.).
      Factor from concrete "clever moment" sentences, not top-down. Spec in
      `project/design/encounter-redesign/aspect-based.md`.
- [ ] Design verbs as aspect-threshold slot machines — build on aspect list, define slot
      requirements and output rules
- [ ] Design biome card pools — curated aspect bundles per biome, weighted toward
      characteristic vocabularies

### Rules & Balancing

- [ ] Playtest medicine vs. health drain in T3 areas — currently medicine reduces condition
      stacks but you still lose 1 HP if any severe condition remains. Could be too punishing
      in endgame where multi-stack injuries are common and one bandage per night isn't enough.
- [ ] Haul direction balancing — tune destination selection to favor two sweet spots:
      (1) 1-2 steps deeper on the trade graph, pushing exploration forward, and
      (2) way back toward root (Aldgate), rewarding long return trips and encouraging
      players to try new branches and pick up fresh storylets.

### Quality of Life

- [ ] Hide locked choice requirements — currently we show the `requires` condition text to the
      player as a UI hint. This breaks with arc encounters that offer the same choice multiple
      times gated by different mutually-exclusive conditions (e.g. faction standing). Remove or
      rethink the locked-choice display before launch.
- [ ] Server "quick start" flag — start the player with basic gear and extra gold to skip the early trade loop
- [ ] Biome intro encounters — one-time scripted encounter per biome/tier that fires on
      first entry. `_intro.enc` convention, `SeenBiomeTiers` on PlayerState, `TryPickIntro`
      in selection logic. Design in `project/design/biome_intro_encounters.md`.

### Flavor Text

`lib/Flavor/FlavorText.cs` has real content for region/settlement names and descriptions.
Everything else is a one-liner placeholder.

- [ ] Improve settlement and region names — current names are mechanical (biome root + size
      suffix). Rework after lore bibles are finalized so names can draw on established lore.

### Nice to Have

- [ ] Investigate granting advantage on skill checks when spirits are at 20 (max) —
      mirror of disheartened's disadvantage. Would reward keeping spirits high.

## 2. Deployment, Testing & Hardening

Ship-readiness: hosting, testing, polish, cleanup.

### Deployment & Hosting

- [ ] Set Cosmos DB TTL on `games` container — 30 days (2592000s) to auto-expire idle saves.
      `az cosmosdb sql container update -a <account> -g <rg> -d dreamlands -n games --ttl 2592000`
- [ ] Cloudflare R2 asset CDN — create bucket, attach custom domain, wire up push.sh,
      update web client to use CDN base URL in production. Existing push.sh skeleton works.
      Plan in `project/architecture/cdn_deployment.md`.
- [ ] React app hosting — Cloudflare Pages or Azure Static Web Apps, git-connected deploys.
- [ ] GameServer hosting — Azure Functions, App Service, Fly.io, or similar.
      Needs Cosmos DB or equivalent for player state persistence.
- [ ] World build + deploy pipeline — build.sh + push.sh + app deploy as one scripted flow.
      Version-prefix assets for cache busting.
- [ ] Google OAuth login + session reconnect (very late — production only).
      Analysis in `project/architecture/google_oauth.md`.

### Testing & Regression

- [ ] CLI integration tests — exercise core loops via CLI against GameServer: encounters,
      movement, inventory, market buy/sell. Plan in `project/integration-test-plan.md`.
- [ ] POI position mismatch — observed a case where the server's in-memory map had a
      settlement at (16,5) but map.json on disk had it at (16,7). Player could enter a
      "ghost" settlement that didn't exist in the data. Server restart fixed it. Root cause
      unclear — map was NOT regenerated between server start and the bug appearing. Need a
      regression test that verifies every node's POI in the server's loaded map matches the
      source map.json.

### Map Polish

- [ ] Try different map seeds for production — current seed was the first one generated;
      explore a few alternatives and pick the most interesting layout
- [ ] Replace lake sprite
- [ ] Hand-drawn river decals — 800x800 tiles chained lake-to-edge.
      Design in `project/design/river_decals.md`.
- [ ] Improve settlement sprite variability — avoid placing identical decals near each other;
      may need more decals (recolored variants)
- [ ] Remove old decal directories — the tier-aware passes now load from the new
      `assets/map/decals/{plains,swamp,forest}/` structure instead of the old flat dirs.
      After visually confirming the new paths work (`mapgen generate test`, inspect map.png
      — T1 areas should look identical to before), delete these originals:
        - `assets/map/decals/grass_tufts/` → split into `plains/t1/grass/` and `swamp/t1/grass/`
        - `assets/map/decals/farm_stuff/` → moved to `plains/t1/farms/`
        - `assets/map/decals/bogs/` → moved to `swamp/t1/bogs/`
        - `assets/map/decals/trees/` → moved to `forest/t1t2/` (palm + beech excluded, same as before)
      Verification: grep mapgen/Rendering/ for old directory names (`grass_tufts`, `farm_stuff`,
      `bogs`, `"trees"`) — should return zero hits.

### Cleanup

- [ ] DungeonRoster refactor (`DungeonRoster.cs` TODO: per-dungeon `descriptor.yaml` files)
- [ ] Stale reference docs — `project/reference/mapgen_design.md` references ocean/coast
      terrain that no longer exists. Several reference docs may have similar staleness.
- [ ] `project/design/gaps.md` checkbox audit — many checked items may not reflect current code.

### Unused Code Audit (2026-03-07)

Results from `jb inspectcode`. Most of these are awaiting final system design, not dead.
None are obviously aged-out — keep for now, revisit when their parent systems are built.

**Stubs awaiting systems:**
- `FlavorText.cs` — 9 placeholder methods (temple, inn, market, weather, etc.)
- `ImperialCalendar.cs` — full calendar type, not yet wired into UI
- `ItemDef.Slots`, `ItemDef.CapacityBonus` — inventory system not finalized
- `SettlementBalance.Services` — settlement services config not yet used
- `SettlementGraph.GetParent/GetChildren/GetSettlementsInBiome` — query methods for future use

**Utility methods with no callers yet:**
- `Direction.Opposite()`
- `ActionVocabulary.IsValidName()`, `.Validate()`
- `Difficulty.GetInfo()`, `Magnitude.GetInfo()/.ScriptName()`, `TimePeriod.GetInfo()/.ScriptName()`
- `ItemDef.IsValidId()`

**Mapgen:**
- `Noise.Octaves()`, `TerrainPass.Draw()`, `SettlementPlacer.GetTraversableNeighbor()`
- `MapGenerator.FindRegions()`, `PoiPass.BiomeToDungeonFolder`

**Server DTOs:**
- `ActionRequest.Quantity`, `ActionRequest.OfferIndex` — request fields not yet read

**Enum member:**
- `EncounterResult.Completed` — never matched on

## 3. Writing

Content authoring — encounters, lore, art. Can proceed incrementally with minimal code changes.

Each tier is ~5 encounters (~30 min each). Each arc is ~1 hour.

### World Building

- [ ] Finish mountain bible

### Encounter Pipeline

- [ ] Locale guides for remaining biome/tier combos
- [ ] Batch-generate skeletons via `generate` command
- [ ] Arc reachability lint — `check` command should walk the encounter graph from Start,
      enumerate all paths (branching on choices × check pass/fail), and warn on: unreachable
      encounters, paths that never reach `+finish_dungeon`/`+flee_dungeon`, and cycles with
      no exit. Treats arcs as directed graphs, not individual files.

### Plains
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Arc — Sodality of the Furrow

### Swamp
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Arc — Fort Contrition
- [ ] Arc — The Bile Vaults
- [ ] Arc — Redoubt of the Mire Baron
- [ ] Arc — The Fever Palace
- [ ] Arc — Submersion of Saint Evarre

### Mountains
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Arc — Vulture's Pulpit
- [ ] Arc — The Tabernacle of Honest Men
- [ ] Arc — The Frostwright's Needle
- [ ] Arc — Greyspire Hermitage
- [ ] Arc — Marrowpeak Redoubt

### Scrub
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Arc — Azharan, the Unruled
- [ ] Arc — The Caliph's Dreaming
- [ ] Arc — Sepulcher of the Ninety-Nine

### Forest
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Arc — Blackivy Manse
- [ ] Arc — The Charnel Garden
- [ ] Arc — The Librarians' Mound
- [ ] Arc — The Fane of Whispering Antlers
- [ ] Arc — The Pallid Court
- [ ] Arc — Lammasgate

### Art Assets

- [ ] Vignettes for all encounter types
- [ ] Vignettes for key encounters
