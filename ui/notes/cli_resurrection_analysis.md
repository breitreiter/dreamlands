# CLI UI Resurrection: Gap Analysis & Architecture Review

## What Exists Today

| Layer | Location | What's There |
|-------|----------|-------------|
| **Map types** | `lib/Map/` | Node, Region, Poi, Terrain, Direction, MapSerializer — mature, zero deps |
| **Encounter parser** | `lib/Encounter/` | Parses .enc into Encounter/Choice/ConditionalOutcome model — stateless, zero deps |
| **Rules vocabulary** | `lib/Rules/` | Skill, Difficulty, Magnitude, TimePeriod enums + ActionVocabulary with full validation |
| **Balance data** | `lib/Rules/balance/` | 9 YAML files covering character stats, items, food, conditions, combat, trade, settlements, encounters, resources |
| **Old CLI (STALE)** | `ui/Cli/*.cs` | GameState (water/gold/waterskin only), GameRenderer, GameRunner, ExitScenter — namespace `MapGen.Game`. **Predates the balance YAML, screen designs, Rules library, and encounter format evolution. Treat as historical reference only — if it contradicts anything else, it's wrong.** |
| **Screen designs** | `ui/screens/` | encounter, explore, inventory, trade, settlement (detailed); splash (minimal); trip_planner (empty) |
| **Encounter content** | `text/encounters/` | 21 .enc files across 5 biomes + intro + 1 dungeon (brides_cave). Directory structure for all 21 dungeons, 5 biomes × 3 tiers. Generation oracle fragments + prompts in `generation/` |

---

## Gaps That Make Development Risky

### 1. No Player State Model — HIGH RISK

The screen designs assume health, spirits, 7 skills, inventory (pack + scrip), equipped gear, conditions, tags, and dungeon completion tracking. The existing `GameState` only tracks water, gold, waterskin level, visited nodes, and time-of-day. **The balance YAML defines all the numbers** (`character.yaml` has starting stats, skill ranges, spirits penalties; `conditions.yaml` has every condition) **but there are no C# types to hold this state at runtime.**

Missing C# types:
- Player record (health, spirits, skill values, gold)
- Inventory containers (pack with size + items, scrip with size + items)
- Equipment slots (weapon, armor, boots)
- Active conditions set
- World tags set
- Dungeon completion set

### 2. No Item System — MEDIUM RISK (design exists, code doesn't)

The balance YAML files contain a detailed item catalog:
- `equipment.yaml` — tools (camping gear, boots, canteen, etc.), consumables (bandages, tonics), weapons (crude/good/fine with biome-flavored names), armor (light/medium/heavy with biome flavors). Each has price, slots, effects, and availability.
- `food.yaml` — 3 food categories (protein/carbs/sweets) with biome-specific vendor and foraged flavors, stacking, meal bonuses, foraging rules, special prophylactic foods.
- `trade.yaml` — trade good categories (textiles, metals, wood, etc.) with regional price modifiers.

**What's missing is the C# side**: no Item type, no catalog loader, no pack/scrip container logic, no equip/unequip mechanics. But the data design is thorough — this is a coding task, not a design task.

### 3. No Encounter Executor — HIGH RISK

The parser produces a read-only model (`Encounter` → `Choice` → `OutcomePart` with `Mechanics` as raw strings). But nothing:
- Evaluates `@if` conditions against player state (skill checks with dice, `has` item checks, `tag` checks)
- Applies mechanic strings to player state (`damage_health small` → subtract N from health)
- Loads encounter bundles and navigates between encounters via `open`

The encounter screen design is the most detailed doc, but the runtime that drives it doesn't exist. This is the highest-risk gap because it ties together player state, items, conditions, and the encounter model.

### 4. No Balance Loader — MEDIUM RISK

The magnitude → number mapping exists in `character.yaml` (`damage: { trivial: 1, small: 2, medium: 3, large: 4, huge: 5 }` and `bump_skill` with the same scale). Condition drains reference magnitude names. But **no C# code loads these YAML files**. The balance directory's own README describes pseudocode for a `Balance.*` accessor pattern, but it hasn't been built.

This is a prerequisite for the encounter executor — you can't apply `damage_health small` without knowing that "small" means 2.

### 5. Screen Design Coverage — LOW RISK

- **settlement.md** — detailed. Defines a town panel with 5 building types: temple (blessings, purification), inn (relax, lodging, buy food), healer (mending, cures), guild outpost (storage, upgrades, gossip), market (straight to trade). Each has its own interaction flow.
- **splash.md** — minimal: restore code input or new game. No character creation flow. `character.yaml` says 10 skill points across 7 skills (0-3 each) — point-buy screen needed?
- **trip_planner.md** — still empty. `resources.yaml` defines auto-travel requirements. Can be deferred.

Note: settlement.md introduces **buildings not in `settlements.yaml`** — the inn doesn't appear in the balance data, which has healer, temple, storage, entertainment. The guild outpost replaces the generic "storage" service with a more narrative framing. These need reconciliation.

### 6. Encounter Content & Trigger System — MEDIUM RISK

21 .enc files exist across 5 biomes (plains/swamp/mountains tier 1, swamp tier 2) plus an intro sequence and one dungeon (brides_cave with 3 rooms). Directory structure is set up for all 21 dungeons and 5 biomes × 3 tiers, but most are empty — 21 of a target 200 encounters are written.

`encounters.yaml` defines base trigger chance (30%/day, decreasing as pool depletes), distribution by biome and tier, and structure (overworld = atomic, dungeons = multi-stage). Oracle generation pipeline exists (`generation/` has Situation/Forcing/Twist fragments + prompt templates) for producing more content. But no C# logic to select and trigger encounters at runtime.

### 7. No Encounter Bundle Loader — LOW RISK

`BundleCommand` writes `encounters.bundle.json` but nothing reads it back. The CLI UI needs to deserialize the bundle, look up encounters by id and category, and feed them to the executor.

---

## Contradictions & Confusion

### TimeOfDay vs TimePeriod

`GameState` defines its own `TimeOfDay` enum (Morning/Afternoon/Evening/Night). `Dreamlands.Rules` has `TimePeriod` with the same four values. These need to be unified — the Rules version should win.

### Water Economy: Old GameState vs Balance Data

The old `GameState` was built entirely around water/waterskin as the core survival pressure. The balance YAML takes a different approach: `resources.yaml` treats water as a daily consumable (1/day, +1 in mountains/swamps) alongside food, with shortage causing foraging checks and damage. The screen designs don't mention water in the status bar at all. **Decision: the old water-centric GameState is obsolete.** Water is now just a resource like food, managed through the food/provisions system. The waterskin upgrade loop is gone.

### Food System Not Reflected in Screen Designs

`food.yaml` describes an elaborate meal system (3 units/day, 3 categories, balanced meal bonuses, foraging with bushcraft checks, special prophylactic foods). None of this appears in the screen designs. The inventory screen mentions "scrip" for consumables but doesn't describe food categories, meal composition, or foraging UI. This is a significant design gap — the food system is one of the primary resource pressures but has no UI spec.

### Condition System Scope

`conditions.yaml` defines 11 conditions with biome affinities, overnight contraction chances, resistance items, and cures. The explore screen mentions "terse summary of conditions" but doesn't specify how condition contraction (overnight rolls), resistance checks, or cure application are presented to the player. The encounter screen handles `add_condition` as a mechanic, but the rest/overnight resolution flow — where most conditions are actually contracted — has no screen design.

### Verb Name Mismatch

encounter.md references `add_random_item` (singular) but `ActionVocabulary` defines `add_random_items` (plural). Minor, but the screen doc should match the canonical code.

### GameState Namespace

`GameState` lives in `namespace MapGen.Game` and depends on `Map`, `Node`, `MapRenderer`, `SettlementPlacer` — all mapgen internals. Can't be reused without moving or rewriting.

### Settlement Screen vs Balance Data

`settlement.md` defines 5 building types: temple, inn, healer, guild outpost, market. `settlements.yaml` defines services: market, water source, storage, healer, temple, entertainment. Mismatches:
- **Inn** is in the screen design but not the balance data. It provides relax (spirits), lodging (skip to morning), and food purchasing.
- **Entertainment** is in the balance data but not the screen design. The inn's "relax" might be this?
- **Guild outpost** replaces the balance data's generic "storage" with a narrative wrapper (factor NPC, gossip mechanic). Good evolution, but the balance YAML needs updating.
- **Water source** is in balance data as a universal service but doesn't appear as a building in the screen design — presumably implicit.

### Dreamlands.Rules Not in Main Solution

`Dreamlands.Rules` is in `Encounter.sln` but not in `Dreamlands.sln`. Any new lib/Game project would need Rules, so the main solution needs updating.

---

## Library Boundaries

### Rules vs Game

**Rules** = vocabulary and constants. Declarative. "What exists, what's valid, what does this term mean."
- Enums and lookup tables (Skill, Difficulty, Magnitude, TimePeriod)
- ActionVocabulary — verb definitions and string validation ("is `damage_health small` legal?")
- Balance data — YAML loader and typed records (item catalog, condition definitions, magnitude tables, food categories, settlement services, combat stats). These are definitions, not runtime state.

**Game** = runtime state and mutation. "What happens when you play."
- Player state (health, spirits, skills, inventory, conditions, tags)
- Encounter executor — evaluate conditions against player state, apply mechanics, roll dice
- Inventory logic — add/remove/equip items, container capacity
- Rest/day resolution — food consumption, overnight condition rolls, drain application
- Encounter selection — pick encounters by biome/tier, track which have fired

**Rubric**: if it answers "what are the rules?" it goes in Rules. If it answers "what happened?" it goes in Game. Rules is the dictionary; Game uses the dictionary to play. Game depends on Rules, never the reverse.

**Balance YAML** lives in Rules because it's still declarative data — "a small magnitude equals 2," "bandages cure injured," "swamp fever has a 40% overnight chance." Game reads these values and acts on them.

## What Belongs in /lib vs What's Truly UI

### Belongs in /lib (game engine, UI-agnostic)

Pure logic, no rendering or input handling:

- **Balance loader** — parse the 9 YAML files into typed C# objects. Prerequisite for everything else.
- **Player state model** — health, spirits, skills, gold, inventory, conditions, tags, time, position
- **Item model + catalog** — Item type, equipment slots, loaded from balance YAML
- **Inventory logic** — pack/scrip containers, equip/unequip, overflow rules, size tracking
- **Food/rest resolution** — daily consumption, meal composition, overnight condition rolls, foraging
- **Encounter executor** — evaluate conditions, roll skill checks, apply mechanics to player state
- **Encounter bundle loader** — deserialize bundle JSON, lookup by id/category
- **Skill check resolver** — roll dice against DC (with spirits penalty, equipment bonus), return pass/fail
- **Random encounter selector** — given biome/tier, pick from available encounters using `encounters.yaml` distribution

Natural home: `lib/Game/` (namespace `Dreamlands.Game`), referencing Dreamlands.Map, Dreamlands.Encounter, and Dreamlands.Rules. Note: this would add a YamlDotNet dependency for the balance loader (MapGen already uses it).

### Truly UI (stays in ui/Cli or equivalent)

- Screen rendering (ANSI formatting, layout, colors)
- Input handling (key reads, menus, prompts)
- Screen transitions and flow control
- Map visualization
- ExitScenter-style hint text (could migrate to lib if multiple UIs want it)

### Shared CLI/UI Library: Not Useful

A shared library between CLI and a hypothetical future GUI wouldn't add value. The rendering and input models are fundamentally different (Console.ReadKey vs event loop, ANSI escape codes vs widget tree). What they share is the game engine — and that lives in /lib. Keep UI projects independent, both referencing the same /lib libraries.

---

## Suggested Build Order

Before touching UI code, build the game engine in `lib/Game/`:

1. **Balance loader** — parse YAML files into typed C# records (magnitudes, items, conditions, food, settlements, etc.)
2. **Player state** — full character sheet model, initialized from balance data
3. **Item model + inventory** — Item type from balance catalog, pack/scrip containers, equip/unequip
4. **Rest/day resolution** — food consumption, meal bonuses, overnight condition rolls, condition drain
5. **Encounter executor** — evaluate conditions, apply mechanics, skill checks (using balance-defined DCs and spirits penalties)
6. **Bundle loader** — deserialize what BundleCommand produces, encounter selection by biome/tier

Then the CLI UI becomes a thin shell: render state, collect input, call engine, render results.

## Open Design Questions

These are questions the balance data doesn't fully answer:

- **Settlement screen flow**: Is trade a sub-screen of settlement? Can you access healer/temple/storage from the same screen? What does "visit settlement" look like vs just being on the tile?
- **Rest screen**: Where does overnight resolution happen? The player needs to see condition rolls, food consumption, and drain effects. No screen design exists for this.
- **Character creation**: `character.yaml` says 10 skill points distributed across 7 skills (0-3 each). Is there a point-buy screen, or random, or preset archetypes?
- **Death/game over**: `encounters.yaml` describes two death modes (injury+recovery in civilization, permadeath in deep wild). No screen design for either.
- **Skill improvement**: `character.yaml` says 5 uses per level, max 10. How is skill use tracked? Does the player see progress toward the next level?
