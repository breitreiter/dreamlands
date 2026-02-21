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
[ ] Reconcile settlement screen vs balance data mismatches  

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

Blocked on UX design + GameServer. Each screen is a separate task.

- [ ] Landing
- [ ] Explore/Map/Status
- [ ] Inventory
- [ ] Encounter
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
No game logic exists for settlement interaction. `SettlementBalance` data and Flavor
stubs exist but nothing in `lib/Game/` processes them. Screen design has mismatches
with balance data (inn vs entertainment, guild outpost vs storage) — reconcile first.

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

### Data Authoring
These are design/content tasks that block codegen — the code scaffolding exists but the
data is placeholder or missing.

- [x] Write out final conditions list and effects — reconciled with `conditions_list.md`,
      stacks system, dual-drain model, flavor text in `ConditionFlavor.cs`
- [x] Finalize consumables/medicines — reconciled with `haversack.md`,
      magnitude-based cure/resist, 11 new medicines
- [x] Define trade goods — 62 concrete trade goods as ItemDefs with biome, tier, cost,
      and flavor descriptions. Old TradeCategory system removed.
- [ ] Per-food flavor descriptions — `FlavorText.FoodName()` returns `""` for description,
      hardcoded generic names ignore the biome-specific YAML data in `FlavorNames`

### Other Mechanics
- [ ] Save/load — PlayerState is JSON-serializable but nothing calls it.

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

- [ ] Test projects — zero coverage. Scaffold xUnit for Rules, Game, Encounter at minimum.
- [ ] GameServer — Azure Function tier from `project/architecture/web_plan.md`. Not urgent
      until game loop stabilizes.
- [ ] Web UI — just a Leaflet map viewer. No game screens. Blocked on GameServer + Figma.

## Cleanup

- [ ] Remove `CombatBalance` (`lib/Rules/CombatBalance.cs`) and its reference in `Balance.cs` —
      combat is just a skill check now, no separate combat system needed.
- [ ] Remove `SkillUses` dict from `PlayerState` — no use-based skill advancement, gear only.
- [ ] Stale reference docs — `project/reference/mapgen_design.md` references ocean/coast
      terrain that no longer exists. Several reference docs may have similar staleness.
- [ ] `project/design/gaps.md` checkbox audit — many checked items may not reflect current code.
