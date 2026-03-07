# Dreamlands TODO

Just a list of things that need doing, roughly grouped.

--

## MVP Blockers

[x] Write out final conditions list → update ConditionDef.All
      /home/joseph/repos/dreamlands/project/design/conditions_list.md
[x] Finalize consumables/medicines → update ItemDef.All consumables
      /home/joseph/repos/dreamlands/project/design/haversack.md
[x] Define trade goods with flavor → flesh out TradeBalance (removed — replaced by haul system)
      /home/joseph/repos/dreamlands/project/design/trade_goods.md
[x] Reconcile settlement screen vs balance data mismatches

---

## Map Visual

- [ ] Try different map seeds for production — current seed was the first one generated;
      explore a few alternatives and pick the most interesting layout
- [x] Add a safe border to the map so the edge looks intentional against the background
- [ ] Replace lake sprite
- [ ] Hand-drawn river decals — 800x800 tiles chained lake-to-edge.
      Design in `project/design/river_decals.md`.
- [x] Fix mountain coloring
- [x] Finish mountain POI sprites
- [ ] Improve settlement sprite variability — avoid placing identical decals near each other;
      may need more decals (recolored variants)
- [ ] Fix dungeon sprite scaling (PoiPass shares a scale factor derived from settlement decals;
      MountainPass uses a hardcoded `PoiScale = 0.32f` — both may be wrong for dungeon art)
- [ ] More aggressive mountain settlement placement — aim for at least one per mountain biome
- [ ] Investigate tier 1 swamp placement — currently lands far from start. May need tier
      assignment tweaks or biome-aware nudging so early-tier regions stay near the cradle.
- [ ] Auto-named regions (`MapGenerator.cs` TODO: generated region names for game UI)
- [ ] DungeonRoster refactor (`DungeonRoster.cs` TODO: per-dungeon `descriptor.yaml` files)

## Game UI Components

- [x] Extract the parchment gold bar (Market top strip) into a reusable component —
      normalize the pattern for use in other screens (Bank, Settlement, etc.)

## Game UI — Map

- [ ] POI name markers on map — show name plaques for visited settlements/dungeons on the
      Leaflet map. Needed for trade trip planning. Server sends `visitedPois` in GameResponse,
      client renders as Leaflet markers. Plan in `project/architecture/poi_markers.md`.

## Game UI Assets

- [x] Icons for all condition types
- [x] Icons for all core inventory types
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
- [x] Dungeon — Bride's Cave
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
- [x] Explore/Map/Status
- [x] Inventory
- [x] Encounter
- [x] Daily rest
- [x] Town — Shop
- [x] Town — Market
- [ ] Town — Guild Bank
- [ ] shadcn/tailwind based design system
- [ ] Clean up rest screen and improve legibility

## Frontend Implementation

React + Vite scaffold in `ui/web/`. Screen shells exist for all major views.
GameServer running with save persistence.

- [x] Scaffold React app with Vite, API client, game context
- [x] Screen shells — Splash, Explore, Encounter, Inventory, Market, Settlement, GameOver
- [ ] Landing — connect to server, new game / load game
- [x] Explore/Map/Status — Leaflet integration, movement
- [x] Inventory — full-screen build with character, mechanics, and inventory panels
- [x] Encounter — immersive two-panel layout, choice rendering, conditional branches
- [x] Dungeon continuous scroll — scene transitions clear and redraw instead of accumulating.
      Server sends combined outcome+encounter for NavigatedTo but the client resets segments
      on each new encounter title. Need to track dungeon context so continuation detection works
      across scene boundaries (Encounter.tsx).
- [ ] Daily rest
- [x] Town — Home
- [x] Town — Temple
- [x] Town — Market (rewrite UI for haul claiming + buy-only shop)
- [x] Town — Guild storage
- [x] Town — Inn / Chapterhouse
- [x] Review UX for exiting inventory and market screens — normalized exit UX across all exitable screens
- [ ] shadcn/tailwind based design system

## Rules / Game Mechanics

### Settlement Mechanics
`SettlementRunner` in Orchestration handles entering settlements. Settlement presence is
derived from player position (no separate mode). Inn/Chapterhouse logic and tests are done.
Market buy/sell with auto-equip works. Remaining services need game logic.

- [x] Town — Shop
- [x] Town — Market (haul system: generation, claiming, auto-delivery, market buy-only)
- [x] Town — Guild storage
- [x] Town — Inn / Chapterhouse (game logic)
- [x] Stock medicine in biome-appropriate markets
- [x] Rework haul destination hints — replaced 3x3 sector grid with relative offset:
      "A plains settlement 2 days east of Aldgate". Manhattan distance ÷ 5 tiles/day,
      8-way cardinal/intercardinal direction via atan2.
- [ ] Haul respawn pacing — currently hauls re-populate immediately when re-entering the market,
      giving settlements effectively infinite hauls. Need a respawn rubric tied to node
      connectivity: hub nodes (high total child count) should replenish quickly since players
      pass through often, while leaf/dead-end nodes should replenish slowly or not at all.
      Figure out the right cooldown curve and whether it's time-based (game days) or
      visit-based.
- [ ] Dynamic haul generation — when a player exhausts all bespoke hauls for a given route,
      generate vague/generic hauls on the fly (e.g. "sealed guild casket", "unmarked parcel",
      "bonded cargo") so trade never dead-ends. Keep flavor minimal and mysterious to avoid
      clashing with hand-written hauls.
- [x] Gathering action
      /home/joseph/repos/dreamlands/project/design/foraging.md

### Resource Systems
- [x] Finalize food and medicine
- [x] Finalize end-of-day — auto-consume food/medicine, ambient threats, condition drain,
      rest recovery, disheartened threshold, death check. Full `EndOfDay.cs` implementation.
- [x] Finalize equipment — full roster overhaul: 17 weapons (+1 to +5), 16 armor pieces
      (light/medium/heavy), 5-tier boot ladder, 12 single-purpose tools.

### End-of-Day Blockers
These must be resolved before end-of-day can be implemented.

- [x] **Food item definitions** — 3 food ItemDefs (food_protein, food_grain, food_sweets),
      trivial cost, FoodType on ItemDef, biome-aware flavor names via FlavorText.FoodName().
- [x] **Food in marketplace** — all settlements stock all 3 food types, always at max stock,
      food restocks alongside trade goods. Buy creates ItemInstance with FoodType set;
      callers can pass createFood delegate for flavor-named instances.
- [x] **Condition acquisition & recovery mechanics** — resist is a standard skill check with
      gear bonuses from `ResistModifiers` → `ResistBonusMagnitudes`. Cure is deterministic
      (consume item → heal stacks), negated only by same-night failed resist.
- [x] **Skill check formalization** — unified formula in `SkillChecks.cs`: encounter checks
      via `GetItemBonus()`, resist checks via `GetResistBonus()`. Both use the same
      `Roll()` with nat 1/20, advantage, luck rerolls.

### Non-Blocking
- [x] Implement foraging action — `ResolveForaging` in `EndOfDay.cs`, design in `project/design/foraging.md`

### Gear Gaps
Analysis in `project/design/gear_gap_analysis.md`. Not blocking gameplay loop but needed for
balanced endgame progression.

- [ ] **Condition resist bonus path** — `ResistModifiers` are `Magnitude` enums (Small/Medium/Large),
      not integers. No code converts them into numeric check bonuses. Either add Magnitude→int
      conversion or give items `SkillModifiers` for resist checks. Affects all 6 condition resist types.
- [x] **+5 weapon** — zweihander (Combat +5).
- [x] **Cunning armor** — light armor ladder: silks (+1) → nightveil (+5).
- [x] **Negotiation tool** — letters_of_introduction (+2) pairs with peoples_borderlands (+3).
- [x] **Bushcraft tool** — no Bushcraft-boosting tools exist yet. Need +2 and +3 items for +5 total.
- [x] **Mercantile tool** — assayers_kit (+3) pairs with traders_ledger (+2).

### Data Authoring
These are design/content tasks that block codegen — the code scaffolding exists but the
data is placeholder or missing.

- [x] Write out final conditions list and effects — reconciled with `conditions_list.md`,
      stacks system, dual-drain model, flavor text in `ConditionFlavor.cs`
- [x] Finalize consumables/medicines — reconciled with `haversack.md`,
      magnitude-based cure/resist, 11 new medicines
- [x] Define trade goods — removed (replaced by 152-entry haul catalog in `HaulDef.*` files).
      Old 71 TradeGood ItemDefs deleted from `ItemDef.BuildAll()`.
- [x] Per-food flavor descriptions — `FoodNames.cs` has ~90 biome×category×source entries
      with evocative per-name descriptions. `FlavorText.FoodName()` picks from static data.
      Market purchases get flavor names + descriptions via `createFood` callback.

### Condition Runtime
All condition resolution (acquisition, cures, drain, stack decay, special effects) happens
during end-of-day. See "Finalize end-of-day" above — condition runtime is part of that work.

### Other Mechanics
- [x] Save/load — GameServer with file-based GameStore, PlayerState persisted as JSON

## Design Decisions Needed

See `project/design/gaps.md` for the full list. Resolved items noted inline.

- [x] Combat model — single skill check, same as any other check
- [x] Skill advancement — no use-based advancement, gear only
- [x] Character creation — flat start, initial backgrounder encounter sets skills based on character background
- [ ] Wire up player creation encounter chain — replace random skill spread in NewGame() with actual intro encounter. Should advise player to buy food before setting out.
- [x] Encounter frequency — baked into mapgen as placeholders. First visit pulls from bespoke .enc
      pool. Subsequent visits have ~10% chance to trigger a recurring encounter.

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

- [x] Test projects — xUnit tests: Rules (42), Game (158), Encounter (18), Map (17),
      Orchestration (37) in `tests/`. All passing.
- [x] GameServer — ASP.NET Minimal API with file-based save store in `server/GameServer/`.
- [x] Web UI scaffold — React + Vite in `ui/web/`, screen shells for all major views.
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

- [x] Remove `CombatBalance` — combat is just a skill check, no separate system needed.
- [x] Remove `UsesPerLevel` from `CharacterBalance` — no use-based skill advancement, gear only.
- [x] Remove `SkillUses` dict from `PlayerState` — never existed in code, vestigial TODO.
- [ ] Stale reference docs — `project/reference/mapgen_design.md` references ocean/coast
      terrain that no longer exists. Several reference docs may have similar staleness.
- [ ] `project/design/gaps.md` checkbox audit — many checked items may not reflect current code.
- [x] Remove dead `FlavorNames` class and YAML files — deleted `FlavorNames.cs`,
      `food_names.yaml`, `trade_names.yaml`, `equipment_names.yaml`. Removed YamlDotNet
      from Flavor csproj. Food names now served by static `FoodNames.cs`.

## Deployment & Hosting

- [ ] Cloudflare R2 asset CDN — create bucket, attach custom domain, wire up push.sh,
      update web client to use CDN base URL in production. Existing push.sh skeleton works.
      Plan in `project/architecture/cdn_deployment.md`.
- [ ] React app hosting — Cloudflare Pages or Azure Static Web Apps, git-connected deploys.
- [ ] GameServer hosting — Azure Functions, App Service, Fly.io, or similar.
      Needs Cosmos DB or equivalent for player state persistence.
- [ ] World build + deploy pipeline — build.sh + push.sh + app deploy as one scripted flow.
      Version-prefix assets for cache busting.
