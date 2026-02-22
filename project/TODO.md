# Dreamlands TODO

Just a list of things that need doing, roughly grouped.

--

## MVP Blockers

[x] Write out final conditions list → update ConditionDef.All
      /home/joseph/repos/dreamlands/project/design/conditions_list.md
[x] Finalize consumables/medicines → update ItemDef.All consumables
      /home/joseph/repos/dreamlands/project/design/haversack.md
[x] Define trade goods with flavor → flesh out TradeBalance
      /home/joseph/repos/dreamlands/project/design/trade_goods.md
[x] Reconcile settlement screen vs balance data mismatches

---

## Map Visual

- [ ] Replace lake sprite
- [ ] Fix mountain coloring
- [ ] Finish mountain POI sprites
- [ ] Improve settlement sprite variability (don't place two identical settlements next to each other)
- [ ] Fix dungeon sprite scaling (PoiPass shares a scale factor derived from settlement decals;
      MountainPass uses a hardcoded `PoiScale = 0.32f` — both may be wrong for dungeon art)
- [ ] Auto-named regions (`MapGenerator.cs` TODO: generated region names for game UI)
- [ ] DungeonRoster refactor (`DungeonRoster.cs` TODO: per-dungeon `descriptor.yaml` files)

## Game UI Assets

- [ ] Icons for all condition types
- [ ] Icons for all core inventory types
- [ ] Vignettes for all encounter types
- [ ] Vignettes for key encounters
- [ ] Game-ready armor
- [ ] Game-ready weapons
- [ ] Game-ready portraits

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
- [ ] Explore/Map/Status
- [ ] Inventory
- [ ] Encounter
- [ ] Daily rest
- [ ] Town — Home
- [ ] Town — Shop
- [ ] Town — Temple
- [ ] Town — Market
- [ ] Town — Guild
- [ ] Town — Healer
- [ ] shadcn/tailwind based design system

## Frontend Implementation

React + Vite scaffold in `ui/web/`. Screen shells exist for all major views.
GameServer running with save persistence.

- [x] Scaffold React app with Vite, API client, game context
- [x] Screen shells — Splash, Explore, Encounter, Inventory, Market, Settlement, GameOver
- [ ] Landing — connect to server, new game / load game
- [ ] Explore/Map/Status — Leaflet integration, movement
- [ ] Inventory — equipment, consumables
- [ ] Encounter — choice rendering, conditional branches
- [ ] Daily rest
- [ ] Town — Home
- [ ] Town — Temple
- [ ] Town — Market
- [ ] Town — Guild storage
- [ ] Town — Healer
- [ ] Town — Inn
- [ ] shadcn/tailwind based design system

## Rules / Game Mechanics

### Settlement Mechanics
`SettlementRunner` in Orchestration handles entering settlements. `AtSettlement` session
mode added. Balance data and Flavor stubs exist but game logic for individual services
is not yet built.

- [ ] Town — Shop
- [ ] Town — Temple
- [ ] Town — Market
- [ ] Town — Guild storage
- [ ] Town — Healer
- [ ] Town — Inn
- [x] Gathering action
      /home/joseph/repos/dreamlands/project/design/foraging.md

### Resource Systems
- [x] Finalize food and medicine
- [ ] Finalize end-of-day (design exists in `project/design/end_of_day_maintenance.md`)
- [ ] Finalize equipment

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
- [ ] Implement foraging action — design complete in `project/design/foraging.md`, no code yet

### Gear Gaps
Analysis in `project/design/gear_gap_analysis.md`. Not blocking gameplay loop but needed for
balanced endgame progression.

- [ ] **Condition resist bonus path** — `ResistModifiers` are `Magnitude` enums (Small/Medium/Large),
      not integers. No code converts them into numeric check bonuses. Either add Magnitude→int
      conversion or give items `SkillModifiers` for resist checks. Affects all 6 condition resist types.
- [ ] **+5 weapon** — best weapons are +4 (bardiche, scimitar, arming_sword). Need one +5 tier 3 / dungeon reward.
- [ ] **Cunning armor** — no armor has a positive Cunning modifier. Need armor progression toward +5.
- [ ] **Negotiation tool** — need a +2 tool to pair with peoples_borderlands (+3) for +5 total.
- [ ] **Bushcraft tool** — need a +3 tool to pair with yoriks_guide (+2) for +5 total.
- [ ] **Mercantile tool** — need a +3 tool to pair with writing_kit (+2) for +5 total.

### Data Authoring
These are design/content tasks that block codegen — the code scaffolding exists but the
data is placeholder or missing.

- [x] Write out final conditions list and effects — reconciled with `conditions_list.md`,
      stacks system, dual-drain model, flavor text in `ConditionFlavor.cs`
- [x] Finalize consumables/medicines — reconciled with `haversack.md`,
      magnitude-based cure/resist, 11 new medicines
- [x] Define trade goods — 62 concrete trade goods as ItemDefs with biome, tier, cost,
      and flavor descriptions. Old TradeCategory system removed.
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
- [x] Encounter frequency — baked into mapgen as placeholders. First visit pulls from bespoke .enc
      pool. Subsequent visits have ~10% chance to trigger a recurring encounter.

## Flavor Text

`lib/Flavor/FlavorText.cs` has real content for region/settlement names and descriptions.
Everything else is a one-liner placeholder. Generation logic needs to be built.

- [ ] Guild office descriptions
- [ ] Market descriptions
- [ ] Guild office rumors
- [ ] Temple descriptions
- [ ] Inn descriptions
- [ ] Healer descriptions
- [ ] Time of day descriptions
- [ ] Weather descriptions
- [ ] Condition warnings

## Infrastructure

- [x] Test projects — xUnit tests for Rules (31), Game (41), Encounter (18) in `tests/`.
- [x] GameServer — ASP.NET Minimal API with file-based save store in `server/GameServer/`.
- [x] Web UI scaffold — React + Vite in `ui/web/`, screen shells for all major views.
- [ ] Web UI — flesh out screens, connect to GameServer, real game loop.

## Cleanup

- [x] Remove `CombatBalance` — combat is just a skill check, no separate system needed.
- [x] Remove `UsesPerLevel` from `CharacterBalance` — no use-based skill advancement, gear only.
- [ ] Remove `SkillUses` dict from `PlayerState` — no use-based skill advancement, gear only.
- [ ] Stale reference docs — `project/reference/mapgen_design.md` references ocean/coast
      terrain that no longer exists. Several reference docs may have similar staleness.
- [ ] `project/design/gaps.md` checkbox audit — many checked items may not reflect current code.
- [x] Remove dead `FlavorNames` class and YAML files — deleted `FlavorNames.cs`,
      `food_names.yaml`, `trade_names.yaml`, `equipment_names.yaml`. Removed YamlDotNet
      from Flavor csproj. Food names now served by static `FoodNames.cs`.
