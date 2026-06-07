# Skills

Four skills: Combat, Negotiation, Bushcraft, Cunning. Each has three tiers: Untrained, Trained, Expert.

## Check Mechanic (all skills)

Every skill check offers three approaches. Tier determines outcomes:

- **Untrained**: One or more approaches auto-fail regardless of choice
- **Trained**: The best of the three approaches always succeeds
- **Expert**: Two of the three approaches always succeed; only the worst fails

## Skill Tiers

Tiers are additive — higher tiers include all lower-tier benefits.

### Combat

| Tier | Gear unlocked |
|------|--------------|
| Untrained | Daggers, light armor |
| Trained | + axes, medium armor |
| Expert | + swords, heavy armor |

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
not resist rolls — see `plans/travel_travails.md`.

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
- **Expert**: +40% contract payout. Two of three approaches succeed.

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

Shown on the tableau when the player picks an arc reward. Describes the delta — what this specific upgrade adds.

| Option | Picker label | What it grants |
|--------|-------------|----------------|
| Combat → Trained | Combat | Unlocks axes and medium armor |
| Combat → Expert | Combat | Unlocks swords and heavy armor |
| Negotiation → Trained | Negotiation | +20% contract payout |
| Negotiation → Expert | Negotiation | +40% contract payout |
| Bushcraft → Trained | Bushcraft | Halves travel hazard costs; eat every other night |
| Bushcraft → Expert | Bushcraft | Quarters travel hazard costs |
| Cunning → Trained | Cunning | 40% chance to resist serious conditions (Injured, Poisoned, etc) |
| Cunning → Expert | Cunning | 80% chance to resist serious conditions (Injured, Poisoned, etc) |
| Max Health | Max Health | +1 maximum health |
| Inventory | Inventory | +1 inventory slot |

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
