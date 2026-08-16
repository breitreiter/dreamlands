# Skills

Four skills: Combat, Negotiation, Bushcraft, Cunning. Each has three tiers: Untrained, Trained, Expert.

## Check Mechanic (all skills)

Every skill check offers three approaches — one correct, one wrong, one neutral (the
approach the encounter names as neither). Tier determines outcomes:

| Tier | Correct | Neutral | Wrong |
|------|---------|---------|-------|
| Untrained | 50% coinflip | Fail | Fail |
| Trained | Succeed | Fail | Fail |
| Expert | Succeed | Succeed | Fail |

The wrong approach always fails, at every tier. See `SkillResolution.ResolvePicker`.
Each non-direct cell carries a `ConnectorKind` so the engine can narrate the gap
between the pick and the outcome.

Approach rosters (`ApproachRoster.cs`) are fixed per skill; which one is correct is
authored per encounter:

| Skill | Approaches |
|-------|-----------|
| Combat | Rush · Strategize · Outlast |
| Negotiation | Charm · Reason · Threaten |
| Bushcraft | Push · Plan · Reroute |
| Cunning | Hide · Bluff · Scheme |

## Skill Tiers

Tiers are additive — higher tiers include all lower-tier benefits.

### Combat

| Tier | Gear unlocked | `ItemDef.RequiredCombat` |
|------|--------------|--------------------------|
| Untrained | Daggers, light armor | `SkillTier.Untrained` |
| Trained | + axes, medium armor | `SkillTier.Trained` |
| Expert | + swords, heavy armor | `SkillTier.Expert` |

Combat has no passive effect outside gear access — it does not change damage, HP, or
the move pool directly. What better gear buys is *named moves* (`ItemDef.RpsMoves`):
the weapon supplies every Attack in the pool, the armor every Defend.

### Enforcement

`Mechanics.MeetsCombatRequirement(state, def)` is the single gate. Two callers:

- `Mechanics.ApplyEquip` — refuses the equip verb (returns null, which the server
  turns into a `combat_tier_too_low` rejection naming the tier).
- `Market.Buy` — above-tier gear can still be **bought**, it just doesn't auto-equip.
  Note the knock-on: auto-equip is what lets a purchase bypass pack capacity, so
  buying gear you can't wear needs a free pack slot where buying gear you can wear
  would not.

The gate only has to hold at equip time — tiers never fall, so an equipped item never
needs re-validating. Nothing unequips retroactively.

Arc-reward gear is gated like anything else, deliberately: an early arc grants medium
armor the player cannot wear yet, which is the nudge to spend a tableau pick on Combat.

### Negotiation

| Tier | Contract payout bonus |
|------|-----------------------|
| Untrained | None |
| Trained | +20% |
| Expert | +40% (total, not additive with Trained) |

### Bushcraft

| Tier | Passive benefit |
|------|----------------|
| Untrained | None |
| Trained | Halves all travel hazard costs; rations every other day |
| Expert | Quarters all travel hazard costs; rations every other day |

Travel hazards (thirst/cold/fatigue) are deterministic per-step spirit costs,
not resist rolls — see `plans/travel_travails.md`. The tier indexes
`HazardDef.UnitsPerSpirit` directly:

| Hazard | Untrained | Trained | Expert |
|--------|-----------|---------|--------|
| Thirst (scrub steps) | 1 spirit / 2 steps | / 4 | / 8 |
| Cold (mountain steps) | 1 spirit / 2 steps | / 4 | / 8 |
| Fatigue (nights camped) | 1 spirit / night | / 2 nights | / 4 |

Food cadence is a straight Untrained-vs-trained split (`EndOfDay.ShouldEatTonight`):
Trained and Expert both eat on odd days only.

### Cunning

Resists **severe conditions**: Injured, Poisoned, Irradiated, Lattice Sickness.

| Tier | Resistance chance |
|------|-------------------|
| Untrained | None |
| Trained | 40% |
| Expert | 80% |

---

## Character Sheet Text

One line per tier shown under each skill. Must communicate the passive benefit and check behavior.

### Combat

- **Untrained**: Daggers and light armor only. Encounter checks are punishing.
- **Trained**: Adds axes and medium armor. Encounter checks are fair.
- **Expert**: Adds swords and heavy armor. Encounter checks are generous.

### Negotiation

- **Untrained**: No contract bonus. Encounter checks are punishing.
- **Trained**: +20% contract payout. Encounter checks are fair.
- **Expert**: +40% contract payout. Encounter checks are generous.

### Bushcraft

- **Untrained**: No passive benefit. Encounter checks are punishing.
- **Trained**: Halves travel hazard costs; eat every other night. Encounter checks are fair.
- **Expert**: Quarters travel hazard costs. Encounter checks are generous.

### Cunning

- **Untrained**: No passive benefit. Encounter checks are punishing.
- **Trained**: 40% chance to resist serious conditions (Injured, Poisoned, etc). Encounter checks are fair.
- **Expert**: 80% chance to resist serious conditions (Injured, Poisoned, etc). Encounter checks are generous.

---

## Level-up Picker Text

Shown on the tableau when the player picks an arc reward. Describes the delta — what this
specific upgrade adds. Source of truth is `ArcRewards.All`; the tableau screen wraps each
label as "Train {label}" / "Master {label}" (✓ Trained / ✓ Mastered once owned).

| Slot id | Label | First pick | Second pick |
|---------|-------|-----------|-------------|
| `combat` | Combat | You can now equip axes and medium armor | You can now equip swords and heavy armor |
| `negotiation` | Negotiation | Contracts pay 20% more on delivery | Contracts pay 40% more on delivery |
| `cunning` | Cunning | 40% chance to resist serious conditions (injured, etc) | 80% chance to resist serious conditions (injured, etc) |
| `bushcraft` | Bushcraft | Halves travel hazard costs; eat every other night | Quarters travel hazard costs |
| `health` | Constitution | Gain +1 max health | Gain +1 max health |
| `inventory` | Packing | Gain +1 pack slot | Gain +1 pack slot |

Every slot caps at 2 picks (`ArcRewardSlot.Cap`). Health and pack each grant 1 per pick
(`ArcRewards.HealthPerPick`, `ArcRewards.InventoryPerPick`), so a maxed track is +2 health
or +2 slots over the starting 4 health / 8 slots.

> `plans/arc_leveling.md` still says "+5 max health" for the health track. The code grants
> +1. The plan is the stale one.

---

## Addendum: Condition System

### Travel hazards (NOT conditions)

> **Superseded 2026-06-07 by the travails system** (`plans/travel_travails.md`).
> The old "travel conditions" — freezing, thirsty, exhausted, as nightly
> spirit-draining, resist-rolled, settlement-cleared status effects — are gone.

The road's wear is now a set of **deterministic hazard channels** (no dice):

| Hazard | Accrues | Spared by | Mitigated by Bushcraft |
|--------|---------|-----------|------------------------|
| Thirst | Per step in scrub | Waterskin | yes (halve/quarter the cost) |
| Cold | Per step in mountains | Wool Bedroll | yes |
| Fatigue | Per night camped on the road | Scarecrow Boots | yes |

Exposure accrues per step/night and charges spirits at threshold crossings;
the toll is summarized at the tail end of each journey. Missing a meal (the old
"Hungry") is still a flat -1 spirit at end-of-day, handled in EndOfDay, not as a
condition. Settlement nights are free.

### Conditions (encounter-applied)

The only real conditions left are **severe** ones — Injured, Poisoned,
Irradiated, Lattice Sickness — each a fixed 1 HP/night drain until treated by
the matching reusable medicine kit. Cunning gives a passive resist (40%/80%)
when an encounter tries to apply one. Plus **Lost** (minor): a navigation
failure that triggers a Lost encounter; Bushcraft-resisted, Cartographer's Kit
prevents it.
