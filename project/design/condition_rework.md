# Condition Rework — Two-Tier Severity

Supersedes drain values and condition list in `conditions.md`, `conditions_list.md`.
Updates end-of-day flow in `end_of_day_maintenance.md`.

## Problem

The nightly maintenance screen is a UX burden. Most nights it's just an annoying
interruption with nothing meaningful to report. But we can't remove it, because
conditions drain health and we need to show the player why their stats are changing.

## Solution

Split conditions into two severity tiers with different UX treatments:

- **Minor conditions** drain spirits only. Reported via toast. No interruption.
- **Severe conditions** drain health. Trigger a full crisis screen. Rare, genuinely lethal.

If the player has no severe conditions, end-of-day collapses to a toast ("Another
restless night. Spirits -2."). If they do have a severe condition, they see the full
accounting — because they are fighting for their life and need to understand the math.

## Minor Conditions (Spirit Drain, Toast)

All minor conditions drain **2 spirits/night** (current Small magnitude).

| Condition | Biome | Trigger | Special |
|-----------|-------|---------|---------|
| Exhausted | universal | ambient resist | Cured by inn rest |
| Freezing | mountains | ambient resist | Clears on settlement/leaving biome |
| Thirsty | scrub | ambient resist | Clears on settlement |
| Disheartened | universal | spirits < 10 | Disadvantage on all rolls. No drain. |
| Lost | universal | ambient resist | Erases discovered map routes. No drain. |

### Spirit Economy

Recovery per night:
- Balanced meal (protein + grain + sweets): **+2 spirits**
- 3 unbalanced food: **+1 spirits**
- Less than 3 food: **+0 spirits**

A well-fed traveler with one minor condition breaks even (+2 from food, -2 from
condition). Two minor conditions or missed meals and spirits start dropping, which
leads to failed checks (via spirits as a tactical currency), which leads to severe
conditions. The death spiral is there — it just runs through spirits and
decision-making, not direct health damage.

### Changes from Current

- Freezing: **drop health drain** (was Trivial). Spirit drain only.
- Thirsty: **drop health drain** (was Small). Spirit drain only.
- Exhausted: no change (already spirit-only).
- Lost: no change (already no drain).
- Disheartened: no change.

## Severe Conditions (Health Drain, Crisis Screen)

All severe conditions drain **5 hp/night** (Large magnitude). All have **3 stacks**
requiring 3 medicines to fully cure. All are encounter-only — never acquired from
ambient resist checks.

| Condition | Biome Threat | Medicine | Notes |
|-----------|-------------|----------|-------|
| Injured | universal | Bandages | Any tier. Failed combat checks, misfortune. |
| Poisoned | forest T3 | Antidote | Venomous creatures, toxic plants. |
| Irradiated | plains/mesa T3 | Specialist | The Glowing Curse. Lattice radiation. |
| Lattice Sickness | swamp T3 | Specialist | New condition. Replaces swamp fever's role. |

### Lethality Math

Max health: 20. Recovery with good food: +1 hp/night. Net drain: **-4 hp/night**.

**Dead in 5 nights** with good food. 4 without.

| From | Nearest Settlement | Days Walking (5 tiles/day) | Survive? |
|------|-------------------|---------------------------|----------|
| T1 anywhere | ~6 tiles | ~1.5 days | Yes, easily |
| T2 worst case | ~17 tiles | ~3.5 days | Yes, if heading straight there |
| T3 near Lorath | ~13 tiles | ~2.5 days | Yes, barely |
| T3 deep (The City) | ~36 tiles | ~7 days | No |

The design intent: getting injured without bandages is survivable if you're near
civilization. In deep T3, it's a death sentence. The decision point was packing
bandages before you left, not the moment you got hurt.

### Haversack Pressure

22 haversack slots. 3 food/day. Each medicine = 1 slot.

| Loadout | Medicine Slots | Food Slots | Days of Food |
|---------|---------------|------------|-------------|
| No medicine | 0 | 22 | ~7 days |
| Bandages only | 3 | 19 | ~6 days |
| Bandages + 1 specialist | 6 | 16 | ~5 days |
| Bandages + 2 specialists | 9 | 13 | ~4 days |
| Full coverage (all 4) | 12 | 10 | ~3 days |

You cannot pack for everything. Deep T3 expeditions require choosing which threats
to prepare for and which to gamble on. This is the core loadout decision.

### T3 Biome Threats

Each T3 biome has at most one specialist condition. Mountains (the Courthouse) has
none — it's a giant building full of clerks, not wilderness. You don't die of
exposure in a courthouse.

| Biome | Severe Condition | Medicine |
|-------|-----------------|----------|
| Forest | Poisoned | Antidote |
| Plains/Mesa | Irradiated | Anti-radiation meds |
| Swamp | Lattice Sickness | Lattice medicine |
| Mountains | — | — |

### Tier Progression

- **T1**: Injuries heal fast because the chapterhouse is 1-2 days away. Free healing.
  Minor conditions are the only real threat, and they're manageable with decent food.
- **T2**: Far enough from the chapterhouse that bandages matter. Getting injured without
  them means a tense walk home. This is where players learn to pack medicine. All
  settlements stock bandages.
- **T3**: Specialist medicines required. Each biome has its own lethal condition. You
  need bandages AND the right specialist medicine for wherever you're going. Haversack
  space becomes brutally tight. No-prep = no-return = no-apologies.

## Removed Conditions

### Hungry

The meal system already handles food scarcity. Missing meals = no spirit recovery =
spirits spiral = failed checks. Adding a hungry condition on top double-dips and makes the math confusing. Recovery tiers (balanced meal +2, unbalanced
+1, nothing +0) are sufficient punishment.

### Gut Worms

Cut. Forest's T3 threat is poisoned. Gut worms was a mild T2 nuisance that doesn't
fit the two-tier model — too annoying to be minor, not scary enough to be severe.

### Swamp Fever (as currently defined)

Replaced by Lattice Sickness. Swamp fever was a mild ambient condition at all tiers.
The new model needs swamp's severe threat to be T3-only, thematically tied to the
lattice/color, and genuinely lethal. Swamp at T1-T2 has no unique condition — just
the universal minor conditions (exhausted, lost).

## End-of-Day Changes

### Toast Path (no severe conditions)

If the player has no active severe conditions after resolution:
- Auto-consume food, auto-apply medicine (same as now)
- Collapse all results into a single toast notification
- Examples: "A restless night. Spirits -2." / "The cold bites but you endure."
- No full-screen interruption

### Crisis Path (severe condition active)

If the player has any active severe condition:
- Show full nightly accounting screen (same as current)
- This screen is now rare and meaningful — it means you're in real trouble
- Shows health drain, medicine consumption, days-to-death math
- The screen is a crisis dashboard, not a maintenance chore

## Settlement & Market TODO

- All outposts must stock bandages
- All settlements in T3 biomes must stock the relevant specialist medicine
- Future: inn upgrade system (convert frontier outpost to chapterhouse for
  exorbitant cost, as endgame prep for final T3 push)

## Future Considerations

- Severe conditions may gain stacking severity (multiple stacks of injured = worse
  drain?) if the single drain rate proves too binary
- Inn upgrade system for converting outposts to chapterhouses
- Tuning how often encounters inflict severe conditions (if injured feels too
  punishing, reduce acquisition rate rather than drain rate)
