# Storylet System — Research & Migration Assessment

## What Are Storylets?

A storylet is a self-contained narrative unit with **prerequisites** (conditions that make it available) and **effects** (state changes after it plays). Unlike branching trees where nodes link explicitly to each other, storylets float in a pool — the engine surfaces whichever ones the player currently qualifies for. Plot emerges from accumulated state changes rather than authored sequences.

The canonical example is Fallen London, but Hades and Wildermyth use the same pattern. Emily Short's term is "quality-based narrative" (QBN) — all progress is tracked through shared numerical qualities/tags, and storylets gate on ranges of those qualities.

### Key Components

- **Qualities / Tags**: Integer or boolean state variables that govern everything. A storylet's prerequisites are expressions over these: "Dangerous >= 40 and Wounds < 3." They serve double duty as narrative flags and resource currency.
- **Prerequisites**: The gate on a storylet. Boolean combinations of quality ranges, location checks, prior-storylet flags, time-of-day, inventory.
- **Priority / Urgency**: Not all qualifying storylets are equal. High-priority (story-advancing) always surface first; low-priority (flavor) fill gaps.
- **Decks vs Pools**: Pools show all qualifying content simultaneously (player picks). Decks draw in sequence with shuffle logic to control pacing and prevent repetition.

### Pros vs Fixed Encounter Trees

- **Linear content cost.** Adding one storylet doesn't require re-authoring downstream branches.
- **Emergent combinations.** Players assemble their own narrative paths from the available pool.
- **Graceful sparsity.** Missing content for a rare state falls back to lower-priority content rather than dead-ending.
- **Multi-author friendly.** Contributors write independent storylets against a shared quality schema.
- **DLC / live-service friendly.** New storylets slot in without touching existing ones.

### Cons

- **Weak pacing control.** Can't guarantee the order players experience beats. Dramatic arcs require extra design effort.
- **Bookkeeping overhead.** Every aspect of narrative progress must be explicitly tracked as a quality.
- **Content sparsity at launch.** The system feels empty until the database is large enough to feel reactive.
- **Discoverability problem.** Players may never raise a quality needed to unlock certain content.

### Notable Games

- **Fallen London / StoryNexus** (Failbetter, 2009): Canonical QBN. Enormous storylet database, all gated by integer qualities.
- **Hades** (Supergiant, 2020): 22,000+ voiced lines. Dialogue pool filtered by gameplay conditions, sorted by priority tier. Roguelite loop clears high-priority content first.
- **Wildermyth** (Worldwalker, 2021): Event pool gated by personality stats, relationships, party composition, campaign chapter. Recent-use weighting de-dupes across campaigns.

### References

- [Storylets: You Want Them — Emily Short](https://emshort.blog/2019/11/29/storylets-you-want-them/)
- [Pacing Storylet Structures — Emily Short](https://emshort.blog/2020/01/21/pacing-storylet-structures/)
- [Beyond Branching: QBN and Salience-Based Narrative — Emily Short](https://emshort.blog/2016/04/12/beyond-branching-quality-based-and-salience-based-narrative-structures/)
- [QBN to Resource Narratives — Alexis Kennedy / Weather Factory](https://weatherfactory.biz/qbn-to-resource-narratives/)
- [GDC 2021: Breathing Life into Greek Myth — Hades dialogue](https://www.gdcvault.com/play/1026975/Breathing-Life-into-Greek-Myth)

---

## Current System vs Storylets

| Dimension | Current System | Storylet System |
|---|---|---|
| **Selection** | Random from `{biome}/tier{n}` pool | Filter all encounters by prerequisite expressions |
| **Gating** | `UsedEncounterIds` (seen/unseen) + `recurring` flag | Rich prerequisites: tag ranges, skill thresholds, items, time, location |
| **State queries** | Only inside choices (`@if`, `[requires]`) | Also on the encounter itself (controls visibility) |
| **Organization** | Directory = category. Fixed biome/tier buckets | Flat pool. Encounters self-declare where/when they're valid |
| **Trigger** | Walk onto POI node, random pick from category | Any trigger point (POI, rest, settlement, camp...), engine filters pool |
| **Pacing** | Implicit (one encounter per POI, exhaustion = silence) | Explicit priority tiers + urgency weights |
| **Extensibility** | Add file to correct directory | Add file anywhere, prerequisites handle the rest |

---

## What We Already Have

The runtime primitives for storylets mostly exist:

- **Tags** (`PlayerState.Tags`, `+add_tag`, `tag <id>` condition) — these are qualities.
- **Conditions** (`ActiveConditions` dictionary with integer counters) — these are qualities too.
- **Item checks** (`has <item_id>`) — inventory prerequisites.
- **Skill checks** (`check <skill> <difficulty>`, `meets <skill> <target>`) — stat prerequisites.
- **Used-encounter tracking** (`UsedEncounterIds`) — replay prevention.
- **Stateless engine** — already `(state, args) → (state, results)`, no refactoring needed.
- **Condition evaluation** — `Conditions.Evaluate()` already dispatches all the above. We'd be extending it, not replacing it.

---

## Migration Plan

### 1. Encounter-Level Prerequisites

The core change. Prerequisites currently exist only on **choices** (`[requires ...]`). Add them to the **encounter itself** as front-matter:

```
[requires tag rescued_maren]
[requires meets bushcraft 8]
The Hermit's Return
You spot a familiar figure...
choices:
```

**Touches:** .enc format spec, `EncounterParser`, `Encounter` model (add `Requires` list), `BundleLoader` (serialize it), `EncounterSelection` (filter by it).

### 2. Richer Selection Logic

Replace `PickOverworld()`'s "random from category" with "filter pool by prerequisites, then pick."

Each encounter declares its **valid contexts** (biome, tier, location type, time-of-day) as prerequisites rather than relying on directory structure. `EncounterSelection` evaluates all prerequisites against `PlayerState` + current node.

Pragmatic hybrid: keep directory-based categories as a first-pass filter for performance (don't evaluate 200 prerequisites when only 30 are plains/tier2), then apply prerequisite filtering within that subset.

### 3. Priority / Urgency

Add a `Priority` field to encounters (`high`, `normal`, `low`). Selection picks randomly from the highest-priority tier that has qualifying encounters. Prevents story-critical beats from being drowned by flavor.

### 4. More Trigger Points

Currently encounters only fire at POI nodes. A storylet system wants more hooks:

- Resting at an inn
- Entering a settlement
- End-of-day camp
- Returning to a previously-visited location
- Reaching a time/day threshold

Each trigger point calls the same selection logic with different context. The engine is already stateless, so this is wiring.

### 5. .enc Format Changes

Add front-matter for encounter-level metadata:

```
[requires tag rescued_maren]
[priority high]
[context biome=swamp tier=2]
[recurring]
```

This replaces current conventions (directory = category, `recur-` prefix = recurring) with explicit declarations. Both conventions could coexist during a transition period.

### 6. Bundle Format Changes

Add `requires`, `priority`, and `context` fields to each encounter entry. `EncounterBundle` gets a new method like `GetQualifying(playerState, nodeContext)` alongside the existing `GetByCategory()`.

---

## What Stays Unchanged

- **Game library** (`Mechanics.Apply`, `SkillChecks`, `Choices`, `Conditions`) — untouched.
- **PlayerState** — already has Tags, Conditions, Skills, Items. Might add tag conventions but the structure is fine.
- **EncounterRunner** — the execution flow (Begin → Choose → End) stays identical. Only selection changes.
- **MapGen** — POI placement stays as-is. The map doesn't need to know about storylet prerequisites.
- **Existing .enc files** — they all still work. Encounters without prerequisites behave like today (always eligible within their category).

---

## Rough Sizing

| Change | Scope | Risk |
|---|---|---|
| Encounter-level `requires` in parser/model/bundle | ~1-2 days | Low — extends existing pattern |
| `priority` + `context` fields | ~half day | Low |
| New selection logic (`GetQualifying`) | ~1 day | Medium — needs thought on performance + fallback |
| Additional trigger points | ~half day each | Low per trigger |
| Migrate existing encounters to new metadata | ~1 day | Low — mostly mechanical |
| .enc format spec update | ~half day | Low |
| **Total** | **~5-7 days** | |

The transition is **incremental**. Step 1 (encounter-level requires) gives 80% of the value. Everything else layers on top. Existing encounters keep working unchanged — they're just storylets with no prerequisites, which is valid.

---

## Open Design Questions

The biggest question isn't structural — it's **authorial**: what qualities/tags should encounters track and gate on? The tag vocabulary becomes the backbone of the system. That's a design exercise more than an engineering one.

Other questions:

- **How aggressively to move away from directory-based categories?** The hybrid approach (directory as first filter, prerequisites as second) is practical but adds conceptual overhead.
- **Should locked storylets be visible?** Showing "you need X to unlock this" is engaging but requires UI work. Hiding them is simpler but risks the discoverability problem.
- **Deck vs pool?** Current system is pool-like (random from qualifying). A deck (draw-and-discard) would prevent repeats more elegantly than `UsedEncounterIds` but adds complexity.
- **What triggers matter?** POI-only is simple. Adding settlement/rest/camp triggers multiplies the content needed to avoid sparsity.
