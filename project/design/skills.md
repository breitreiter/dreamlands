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
| Trained | 30% chance to resist travel conditions |
| Expert | 60% chance to resist travel conditions + rations every other day |

### Cunning

Resists **severe conditions**: Injured, Poisoned, Irradiated, Lattice Sickness.

| Tier | Resistance chance |
|------|-------------------|
| Untrained | None |
| Trained | 30% |
| Expert | 60% |

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
- **Trained**: 30% chance to resist travel conditions (Freezing, Exhausted, etc). Encounter checks are fair.
- **Expert**: 60% chance to resist travel conditions (Freezing, Exhausted, etc). Encounter checks are generous.

### Cunning

- **Untrained**: No passive benefit. Encounter checks are punishing.
- **Trained**: 30% chance to resist serious conditions (Injured, Poisoned, etc). Encounter checks are fair.
- **Expert**: 60% chance to resist serious conditions (Injured, Poisoned, etc). Encounter checks are generous.

---

## Level-up Picker Text

Shown on the tableau when the player picks an arc reward. Describes the delta — what this specific upgrade adds.

| Option | Picker label | What it grants |
|--------|-------------|----------------|
| Combat → Trained | Combat | Unlocks axes and medium armor |
| Combat → Expert | Combat | Unlocks swords and heavy armor |
| Negotiation → Trained | Negotiation | +20% contract payout |
| Negotiation → Expert | Negotiation | +40% contract payout |
| Bushcraft → Trained | Bushcraft | 30% chance to resist travel conditions (Freezing, Exhausted, etc) |
| Bushcraft → Expert | Bushcraft | 60% chance to resist travel conditions (Freezing, Exhausted, etc) |
| Cunning → Trained | Cunning | 30% chance to resist serious conditions (Injured, Poisoned, etc) |
| Cunning → Expert | Cunning | 60% chance to resist serious conditions (Injured, Poisoned, etc) |
| Max Health | Max Health | +1 maximum health |
| Inventory | Inventory | +1 inventory slot |

---

## Addendum: Condition System Harmonization

### Travel Conditions

Travel conditions share a uniform structure: different triggers and cures, but identical
mechanical effect — a fixed spirits drain each day until cured.

| Condition | Trigger | Cure |
|-----------|---------|------|
| Freezing | Camping in cold biomes without protection | Warm shelter or leave cold biome |
| Thirsty | Traveling in scrub without water | Drink water |
| Exhausted | Applied every day at end-of-day | Scarecrow's Boots (immune); otherwise Bushcraft resist |
| Hungry | Resting without food | Eat |
| Lost | TBD trigger | Enter a settlement |
| Disheartened | Low spirits threshold | Spirits restored above threshold |

**Hungry** is refactored back into the travel condition family. It is auto-applied at the
end of a rest when the player has no food. Same spirits drain as the other travel conditions.
Previously modeled as a separate starvation mechanic; unified here for consistency.

Bushcraft's passive resistance (30%/60%) applies to all travel conditions, including Hungry.

**Balance note**: Resist chances are speculative. Playtest at each Bushcraft tier to check
whether daily condition accumulation feels punishing, manageable, or trivial. If Untrained
is too miserable, add a base resist (e.g. 20%) before skill investment. If Trained doesn't
feel like a meaningful upgrade, raise the tier thresholds. Negotiation is confirmed solid
(both contract bonus and check behavior tested). Combat and Cunning are untested.

### Serious Conditions

Serious conditions (Injured, Poisoned, Irradiated, Lattice Sickness) should already follow
the same harmonized structure: fixed daily health drain, different triggers and cures.
Verify that no serious condition has a unique mechanical shape before shipping.
