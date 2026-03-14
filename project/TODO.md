# Dreamlands TODO

Just a list of things that need doing, roughly grouped.

--

## Map Visual

- [ ] Try different map seeds for production — current seed was the first one generated;
      explore a few alternatives and pick the most interesting layout
- [ ] Replace lake sprite
- [ ] Hand-drawn river decals — 800x800 tiles chained lake-to-edge.
      Design in `project/design/river_decals.md`.
- [ ] Improve settlement sprite variability — avoid placing identical decals near each other;
      may need more decals (recolored variants)
- [ ] Fix dungeon sprite scaling (PoiPass shares a scale factor derived from settlement decals;
      MountainPass uses a hardcoded `PoiScale = 0.32f` — both may be wrong for dungeon art)
- [ ] More aggressive mountain settlement placement — aim for at least one per mountain biome
- [ ] Investigate tier 1 swamp placement — currently lands far from start. May need tier
      assignment tweaks or biome-aware nudging so early-tier regions stay near the cradle.
- [ ] Bisect oversized T1 regions — same technique as T3 bisect (TierAssigner splits by
      distance from city), but inner nodes stay T1 and outer nodes become T2. Prevents a
      mega-T1 region from making an entire biome feel safe.
- [ ] Auto-named regions (`MapGenerator.cs` TODO: generated region names for game UI)
- [ ] DungeonRoster refactor (`DungeonRoster.cs` TODO: per-dungeon `descriptor.yaml` files)
- [ ] Remove old decal directories — the tier-aware passes now load from the new
      `assets/map/decals/{plains,swamp,forest}/` structure instead of the old flat dirs.
      After visually confirming the new paths work (`mapgen generate test`, inspect map.png
      — T1 areas should look identical to before), delete these originals:
        - `assets/map/decals/grass_tufts/` → split into `plains/t1/grass/` and `swamp/t1/grass/`
        - `assets/map/decals/farm_stuff/` → moved to `plains/t1/farms/`
        - `assets/map/decals/bogs/` → moved to `swamp/t1/bogs/`
        - `assets/map/decals/trees/` → moved to `forest/t1t2/` (palm + beech excluded, same as before)
      Verification: grep mapgen/Rendering/ for old directory names (`grass_tufts`, `farm_stuff`,
      `bogs`, `"trees"`) — should return zero hits. If any other code references the old paths
      (build scripts, asset pipeline), update those too before deleting.

## Game UI — Map

- [ ] POI name markers on map — show name plaques for visited settlements/dungeons on the
      Leaflet map. Needed for trade trip planning. Server sends `visitedPois` in GameResponse,
      client renders as Leaflet markers. Plan in `project/architecture/poi_markers.md`.

## Game UI Assets

- [ ] Vignettes for all encounter types
- [ ] Vignettes for key encounters

## World Building

- [ ] Finish mountain bible

## Writing

Each tier is ~5 encounters (~30 min each). Each dungeon is ~1 hour.

### Plains
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Dungeon — Sodality of the Furrow

### Swamp
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Dungeon — Fort Contrition
- [ ] Dungeon — The Bile Vaults
- [ ] Dungeon — Redoubt of the Mire Baron
- [ ] Dungeon — The Fever Palace
- [ ] Dungeon — Submersion of Saint Evarre

### Mountains
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Dungeon — Vulture's Pulpit
- [ ] Dungeon — The Tabernacle of Honest Men
- [ ] Dungeon — The Frostwright's Needle
- [ ] Dungeon — Greyspire Hermitage
- [ ] Dungeon — Marrowpeak Redoubt

### Scrub
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Dungeon — Azharan, the Unruled
- [ ] Dungeon — The Caliph's Dreaming
- [ ] Dungeon — Sepulcher of the Ninety-Nine

### Forest
- [ ] Tier 1
- [ ] Tier 2
- [ ] Tier 3
- [ ] Repeating
- [ ] Dungeon — Blackivy Manse
- [ ] Dungeon — The Charnel Garden
- [ ] Dungeon — The Librarians' Mound
- [ ] Dungeon — The Fane of Whispering Antlers
- [ ] Dungeon — The Pallid Court
- [ ] Dungeon — Lammasgate

### Encounter Pipeline
- [ ] Locale guides for remaining biome/tier combos
- [ ] Batch-generate skeletons via `generate` command

## UX Design

Screen designs live in `project/screens/`. Some are well-specified, some are empty.

- [ ] Landing
- [ ] Town — Guild Bank
- [ ] shadcn/tailwind based design system
- [ ] Clean up rest screen and improve legibility

## Frontend Implementation

React + Vite scaffold in `ui/web/`. Screen shells exist for all major views.
GameServer running with save persistence.

- [ ] Landing — connect to server, new game / load game
- [ ] Daily rest
- [ ] Camp screen death — make the camp screen more dramatic when the player dies
      (condition drain kills you overnight). Currently shows the same layout as a normal rest.
- [ ] shadcn/tailwind based design system

## Rules / Game Mechanics

### Settlement Mechanics
- [ ] Haul direction balancing — tune destination selection to favor two sweet spots:
      (1) 1-2 steps deeper on the trade graph, pushing exploration forward, and
      (2) way back toward root (Aldgate), rewarding long return trips and encouraging
      players to try new branches and pick up fresh storylets.

### Non-Blocking
- [ ] Investigate granting advantage on skill checks when spirits are at 20 (max) —
      mirror of disheartened's disadvantage. Would reward keeping spirits high.
- [ ] Foraging rework — foraged food should always be unbalanced (single category per night).
      Bushcraft keeps you alive (+1 recovery) but never yields a balanced meal (+2 recovery).
      Only purchased food can be balanced. Preserves the SERE fantasy while keeping market
      food valuable. See `project/design/condition_rework.md` for context.
- [ ] Treated severe conditions skip penalty — if a severe condition (injured, poisoned,
      irradiated, lattice sickness) is successfully treated that night, it should not
      impose its 5 hp drain. Currently treatment cures stacks but the drain still fires
      if the condition was present at the start of the night.
- [ ] Market stocking — all outposts must stock bandages. All T3 biome settlements must
      stock the relevant specialist medicine for their biome's severe condition.
- [ ] Protective gear in markets — pack-carried protective items (heavy_furs, canteen,
      lattice_ward, etc.) are not stocked anywhere. Players can only get them from
      encounters. Need market availability so players can prepare for biome hazards.
- [ ] Inn upgrade system — convert frontier outpost to chapterhouse for exorbitant cost.
      Endgame prep for final T3 push. Design TBD.

### Gear Gaps
Analysis in `project/design/gear_gap_analysis.md`. Not blocking gameplay loop but needed for
balanced endgame progression.

- [ ] **Condition resist bonus path** — `ResistModifiers` are `Magnitude` enums (Small/Medium/Large),
      not integers. No code converts them into numeric check bonuses. Either add Magnitude→int
      conversion or give items `SkillModifiers` for resist checks. Affects all 6 condition resist types.

## Flavor Text

`lib/Flavor/FlavorText.cs` has real content for region/settlement names and descriptions.
Everything else is a one-liner placeholder. Generation logic needs to be built.

- [ ] Improve settlement and region names — current names are mechanical (biome root + size
      suffix). Rework after swamp lore bible is finalized so names can draw on established lore.
- [ ] Guild office descriptions
- [ ] Market descriptions
- [ ] Guild office rumors
- [ ] Temple descriptions
- [ ] Inn descriptions
- [ ] Time of day descriptions
- [ ] Weather descriptions
- [ ] Condition warnings

## Infrastructure

- [ ] Web UI — flesh out screens, connect to GameServer, real game loop.
- [ ] Google OAuth login + session reconnect (very late — production only).
      Analysis in `project/architecture/google_oauth.md`.

## Testing / Regression

- [ ] CLI integration tests — exercise core loops via CLI against GameServer: encounters,
      movement, inventory, market buy/sell. Plan in `project/integration-test-plan.md`.
- [ ] POI position mismatch — observed a case where the server's in-memory map had a
      settlement at (16,5) but map.json on disk had it at (16,7). Player could enter a
      "ghost" settlement that didn't exist in the data. Server restart fixed it. Root cause
      unclear — map was NOT regenerated between server start and the bug appearing. Need a
      regression test that verifies every node's POI in the server's loaded map matches the
      source map.json.

## Quality of Life

- [ ] Server "quick start" flag — start the player with basic gear and extra gold to skip the early trade loop
- [ ] Biome intro encounters — one-time scripted encounter per biome/tier that fires on
      first entry. `_intro.enc` convention, `SeenBiomeTiers` on PlayerState, `TryPickIntro`
      in selection logic. Design in `project/design/biome_intro_encounters.md`.

## Cleanup

- [ ] Stale reference docs — `project/reference/mapgen_design.md` references ocean/coast
      terrain that no longer exists. Several reference docs may have similar staleness.
- [ ] `project/design/gaps.md` checkbox audit — many checked items may not reflect current code.

## Deployment & Hosting

- [ ] Cloudflare R2 asset CDN — create bucket, attach custom domain, wire up push.sh,
      update web client to use CDN base URL in production. Existing push.sh skeleton works.
      Plan in `project/architecture/cdn_deployment.md`.
- [ ] React app hosting — Cloudflare Pages or Azure Static Web Apps, git-connected deploys.
- [ ] GameServer hosting — Azure Functions, App Service, Fly.io, or similar.
      Needs Cosmos DB or equivalent for player state persistence.
- [ ] World build + deploy pipeline — build.sh + push.sh + app deploy as one scripted flow.
      Version-prefix assets for cache busting.

## Unused Code Audit (2026-03-07)

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
