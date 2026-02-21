# Dungeon Plan

## What a dungeon IS

A dungeon is a **unique, singular place** in the world. "Bandit Hideout" is *the* Bandit Hideout — one encounter chain, one map decal, one placement. It shows up in a different location every time the map is generated, but there is only ever one of it.

A dungeon is a **narrative adventure**, not a navigable space. The player enters, plays through a chain of encounters linked by `[branch]`, and the adventure ends when they defeat (or flee from) the final encounter. We scene-cut back to the world map outside the dungeon.

Dungeons are **one and done**. Once cleared, the dungeon is marked on the map as completed. The player cannot re-enter. Cleared dungeons need a distinct map marker so players can track what they've finished.

## Placement philosophy

Dungeons should feel like a **reward for exploring**. The maps today have many dead ends — nodes with only one connection leading in/out. These are natural dungeon sites: a player who pushes down a dead-end path should find something worth the trip.

Every dungeon in the roster gets placed on every generated map. The placement algorithm finds a suitable home for each one based on:

1. **Dead-end nodes** (1 connection) — strongest candidates
2. **Low-connectivity nodes** (2 connections) at the edge of explored terrain
3. **Biome fit** — each dungeon has specific terrain(s) where it belongs
4. **Distance tier** — each dungeon has a difficulty that must match its distance from Aldgate
5. **Separation** — dungeons shouldn't cluster; minimum distance between dungeons TBD

Dungeons should never appear on high-connectivity nodes (3-4 connections) since those feel like crossroads, not hidden places.

## The content problem

There is no single source of truth spanning mapgen, text/encounters, and map assets. `dungeons.txt` was created early and is out of sync with the encounter system. The plan below establishes that source of truth.

### Step 1: Figure out the dungeon roster

How many dungeons should exist? Every dungeon we create appears on every map, so the roster size directly determines dungeon density.

**Reasoning from map scale:**
- A 100x100 production map has maybe 3,000-5,000 traversable nodes
- Dead ends might be 10-20% of those — 300-1,000 nodes
- Settlements number 40-80; dungeons should be significantly rarer
- Too few (<8) and the world feels empty of adventure; too many (>25) and they lose specialness

**Working target: 12-18 dungeons in the roster.** Every one appears on every map. This can be tuned — if maps feel sparse, add more dungeons; if they feel cluttered, cut the weakest ones.

### Step 2: Distribution across tiers

Dungeons get harder (and more rewarding) further from Aldgate. The player needs to find dungeons early to learn the system.

| Tier | Distance | Roster slots | Difficulty | Tone |
|------|----------|-------------|------------|------|
| 1 — Safe | 0-15 | 3-4 | Easy. 1-2 encounters. | Clearing a nuisance, small reward. |
| 2 — Frontier | 16-25 | 4-5 | Moderate. 2-3 encounters. | Real danger, meaningful loot. |
| 3 — Wildlands | 26-40 | 3-5 | Hard. 3-4 encounters. | Ancient/forgotten places, serious stakes. |
| 4 — Dark Places | 40+ | 2-4 | Deadly. 3-5 encounters, boss fight. | Eldritch, legendary, campaign-defining. |

Tier 1 dungeons should be short and teach the player what dungeons are. Tier 4 dungeons should be events.

### Step 3: The roster

**See `dungeon_roster.md`** — the canonical source of truth for all 20 dungeons, their biomes, tiers, concepts, decals, and authoring status.

The roster file also contains the biome distribution guide, tier distribution targets, a biome coverage cross-check, and a decisions log.

### Step 4: Build one dungeon end-to-end

Before committing to the full roster, **build one complete dungeon** to validate the authoring experience:

**Candidate: Bandit Hideout (Tier 1, Forest/Hills)**

Why this one:
- Simplest possible dungeon (1-2 encounters)
- Familiar fantasy trope, easy to write
- Tests the full pipeline without high complexity
- Tier 1 means the player hits it early — good for first impressions

**What "building one" means:**
1. Write the `.enc` file(s) for the encounter chain
2. Test with the encounter parser (`encounter-tool check`)
3. Wire it into the game loop (even minimally) to feel the experience
4. Evaluate: Is authoring painful? Is the encounter format expressive enough? Does chaining via `[branch]` feel natural or clunky?

**Directory structure:**

```
text/encounters/dungeon/
  bandit_hideout/
    bandit_hideout_01.enc     # entrance/approach
    bandit_hideout_02.enc     # interior (optional for Tier 1)
    bandit_hideout_03.enc     # confrontation/resolution
```

### Step 5: Remaining authoring

After the prototype validates the approach, author encounters for the rest of the roster. Rough `.enc` file counts:

| Tier | Encounters per dungeon | Dungeons | Total .enc files |
|------|----------------------|----------|------------------|
| 1 | 1-2 | 3 | 3-6 |
| 2 | 2-3 | 3 | 6-9 |
| 3 | 3-4 | 3 | 9-12 |
| 4 | 3-5 | 3 | 9-15 |
| **Total** | | **12** | **~27-42** |

This is a lot of writing. The `encounter-tool fixme` LLM pipeline will help, but each dungeon still needs human design for the choice/consequence structure.

---

## Technical implementation

### Placement algorithm (mapgen)

Add `PlaceDungeons` to `ContentPopulator.Populate()`. Unlike settlements (which fill coverage gaps), dungeon placement assigns a specific home to each dungeon in the roster:

```
For each dungeon in the roster:
  1. Filter nodes to: correct biome, correct distance tier, no existing POI, not water
  2. Score candidates: strongly prefer dead ends (1 connection), then low-connectivity (2)
  3. Among top candidates, pick one that maintains separation from already-placed dungeons
  4. Place the dungeon (it keeps its identity — name, encounter ID, decal)
```

If a dungeon can't find a valid placement (no nodes match its biome+tier), skip it for this map. This is expected and common — small test maps (15x20) will place few or no dungeons, and that's fine. The placement loop must never fail on an empty candidate set.

Replace `dungeons.txt` with a richer format that encodes the full roster (biome affinities, tier, encounter ID, etc.), or define the roster in code.

### Dungeon data model

Extend `Poi` or create a subclass:

```csharp
// On Poi:
public string? EncounterId { get; set; }  // links to .enc bundle ID
public bool Cleared { get; set; }          // one-and-done tracking
```

The `EncounterId` maps to the encounter chain's entry point (e.g., `bandit_hideout_01`). The encounter chain handles everything from there via `[branch]`.

Since each dungeon is unique, the `Poi.Type` field (e.g., "Bandit Hideout") is already sufficient to identify which dungeon this is — no separate "instance ID" needed.

### Map decals (PNG renderer)

Each dungeon gets its own **fixed decal** — a unique visual icon on the rendered map. Since every dungeon is a unique place, its decal is part of its identity. A player should be able to glance at the map and recognize "that's the Bandit Hideout" by its icon alone.

Decals live in `assets/map/decals/poi/dungeons/` (paralleling `assets/map/decals/poi/settlements/`). Each dungeon needs:

- **Uncleared decal**: The primary icon. Should read clearly at map scale and evoke the dungeon's character.
- **Cleared decal**: A muted/faded variant, or the same icon with a completion mark. Must be visually distinct from uncleared so the player can scan what's left.

| Dungeon | Decal idea |
|---------|------------|
| Bandit Hideout | Crossed swords over a cave mouth |
| Collapsed Mine | Pickaxe and timber frame |
| Smuggler's Cave | Anchor or barrel in a cave |
| Forgotten Tomb | Tombstone / sarcophagus |
| Abandoned Fort | Crumbling tower |
| Hidden Cave | Dark cave mouth |
| Ancient Ruins | Broken column / archway |
| Monster Lair | Beast skull |
| Haunted Barrow | Mound with ghostly wisps |
| Abyssal Pit | Spiraling descent |
| Dragon's Den | Dragon skull |
| Eldritch Temple | Warped spire / tentacles |

Rendering integration: `PoiPass` already handles settlement decals. Dungeon decals follow the same pattern — load the decal image, composite it onto the node's map tile. Cleared state determines which variant to draw.

### Cleared state & map markers

- `GameState` tracks `HashSet<Node> ClearedDungeons`
- `PoiPass` (PNG renderer) draws uncleared or cleared decal based on state
- Terminal renderer: `D` for uncleared, `✓` for cleared
- `MapSerializer` needs to persist cleared state per save

### Encounter runtime (minimal)

The encounter parser exists in `EncounterLib`. To play encounters in the game loop:

1. Load the bundled encounter JSON (from `encounter-tool bundle`)
2. Display body text
3. Present choices
4. Resolve mechanics (`[damage]`, `[give_gold]`, `[combat]`, etc.)
5. Follow `[branch]` to next encounter in chain
6. On chain completion → mark dungeon cleared, return to world map

This is the most significant engineering piece. Even a minimal version needs to handle: text display, choice selection, skill checks, combat resolution, branching, and mechanical effects on `GameState`.

**Question for later:** How much of the encounter runtime do we build in the CLI vs defer to the web frontend?

---

## Build order

| Phase | Work | Output |
|-------|------|--------|
| **0** | Write this plan ✓ | `dungeon_plan.md` |
| **1a** | Finalize roster (names, biomes, tiers) | Updated content file or code |
| **1b** | Dungeon placement algorithm | `DungeonPlacer.cs` + integration |
| **2** | Author Bandit Hideout encounters | 2-3 `.enc` files |
| **3** | Minimal encounter runtime in CLI | Playable dungeon in terminal |
| **4a** | Cleared state, map markers, serialization | Full dungeon lifecycle |
| **4b** | Dungeon decals (per dungeon, uncleared + cleared) | `assets/map/decals/poi/dungeons/` |
| **5** | Author remaining Tier 1-2 dungeons | ~6-12 `.enc` files |
| **6** | Author Tier 3-4 dungeons | ~12-18 `.enc` files |

Phases 1a and 1b can happen in parallel. Phase 2 can start once we're confident in the format. Phase 3 is the gating risk — if the encounter runtime is too painful, we need to rethink.

---

## Open questions (to resolve as we go)

- Minimum separation distance between dungeons?
- Should Tier 1 dungeons always be 1 encounter, or can some be 2?
- Dungeon naming: are the roster names final, or do they get proper names during authoring? (e.g., "Bandit Hideout" → "Blackthorn Hollow")
- Do dungeons have a "flavor text" description visible on the world map before entering? (e.g., "A dark cave mouth yawns behind a curtain of vines.")
- Can you flee a dungeon mid-chain? What happens — reset to entrance, or progress saved?
- Combat system doesn't exist yet. Dungeons need combat. How do we handle `[combat X]` and `[defeats X]`? Stub it? Auto-resolve? Dice roll?
- ~~What happens if a dungeon can't be placed on a given map?~~ **Resolved: skip it gracefully.** Small test maps (15x20) will routinely lack the biome/tier combinations needed for most of the roster. Placement must never break on a small map — just place what fits and move on.
