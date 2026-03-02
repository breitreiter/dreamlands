# End-of-Day Maintenance

End-of-day is the core survival loop. It runs every time the day counter advances.

## When It Triggers

End-of-day fires whenever `Day` increments. This includes:
- Normal time advancement (time wraps past Night to Morning)
- `skip_time` advancing past midnight

### skip_time Flags

The `skip_time` verb accepts flags that modify end-of-day when the day boundary is crossed:
- `no_sleep` — skip rest recovery. You were fleeing, not sleeping.
- `no_meal` — skip food consumption. You didn't have time to eat.
- `no_biome` — skip ambient condition checks AND foraging. You were indoors.

These flags are stored as pending booleans on PlayerState and consumed at the start of
resolution. Inn stays set `no_biome` (sheltered from threats, no wilderness to forage in).

## Resolution Sequence

Everything is automatic — no player choices. Food and medicine are auto-selected from the
haversack. The sequence runs in `EndOfDay.Resolve()`:

### 1. Read and Clear Flags

Consume `PendingEndOfDay`, `PendingNoSleep`, `PendingNoMeal`, `PendingNoBiome`.

### 2. Snapshot Pre-existing Conditions

Record which conditions the player already has. This matters for medicine: cures only
target pre-existing conditions (not ones acquired tonight).

### 3. Roll Resist Checks

For each ambient threat based on camping biome and tier:
- Roll: `d20 + skill + gear resist bonus` vs condition DC
- Disheartened imposes disadvantage
- Luck reroll on failure (same as encounter checks)
- Natural 1 always fails, natural 20 always passes
- Results recorded but NOT applied yet — new conditions are deferred to step 7

**Events emitted**: `ResistPassed` or `ResistFailed` for each threat.

Skipped entirely when `no_biome` is true.

### 4. Forage for Food

Automatic bushcraft check against three DC thresholds:
- Roll: `d20 + bushcraft skill + equipment bonus` (same bonus sources as encounter checks)
- Disheartened imposes disadvantage
- No luck reroll (background activity, not dramatic)
- No nat 1/20 rules (multi-tier harvest, not pass/fail)

| Total Roll | Yield |
|------------|-------|
| < 16       | 0 food |
| 16+        | 1 food |
| 18+        | 2 food |
| 20+        | 3 food |

Foraged items cycle through protein, grain, sweets for variety. They are real ItemInstances
added to the haversack with biome-appropriate flavor names (via `FoodNames.Pick` with
`foraged: true`). They sit alongside any purchased food already in the haversack.

**Event emitted**: `Foraged(rolled, modifier, itemsFound)` — always emitted, even if 0 items.

Skipped when `no_biome` is true (settlements, inns).

### 5. Consume Food

Auto-select up to 3 food items from haversack. Strategy:
1. **Try balanced meal first**: find one protein + one grain + one sweets
2. **If balanced impossible**: grab up to 3 food items in haversack order

This includes any food just added by foraging. Items are removed from haversack.

**Balanced meal** = all three food types present. Grants bonus rest recovery in step 9.

**Hungry stacks**:
- Full meal (3 food): cure 1 existing hungry stack
- Shortage (ate < 3): if `(3 - eaten) > current stacks`, set stacks to shortage
- Shortage never reduces existing stacks (missing 1 meal when already at 3 stacks = no change)
- Hungry max stacks = 3 (from condition definition)

**Events emitted**: `FoodConsumed(eaten, balanced)` or `Starving` (if 0 food).
Plus `HungerChanged(newStacks)` or `HungerCured` as applicable.

### 6. Consume Medicine

Auto-consume medicine from haversack for pre-existing active conditions only. One medicine
per condition per night.

- If the player **failed tonight's resist** for the same condition AND already had it:
  medicine consumed but cure negated (`CureNegated` event). The biome overwhelmed it.
- Otherwise: deterministic cure. Reduce stacks by 1. If stacks reach 0, condition removed.

Medicine never targets conditions acquired tonight (step 7 hasn't run yet, and only
pre-existing conditions are eligible).

**Events emitted**: `CureApplied` or `CureNegated` per medicine, plus `ConditionCured`
when stacks hit 0.

### 7. Apply New Conditions

For each failed resist from step 3: if the player doesn't already have that condition,
apply it now at full stacks (from condition definition).

This ordering means medicine only treats what you came in with. New conditions from
tonight's failed resists won't be cured until tomorrow night.

**Event emitted**: `ConditionAcquired(conditionId, stacks)` for each new condition.

### 8. Condition Drain

For each active condition (pre-existing and newly acquired):
- Apply `HealthDrain` (flat per condition, not per-stack)
- Apply `SpiritsDrain` (flat per condition, not per-stack)
- Apply `SpecialEffect` if defined (e.g. `lost` erases explored tiles)

**Events emitted**: `ConditionDrain(conditionId, healthLost, spiritsLost)` and/or
`SpecialEffect(conditionId, effect)`.

### 9. Rest Recovery

Skipped if `no_sleep` flag was set.

- Base: +1 Health, +1 Spirits (from `CharacterBalance`)
- Balanced meal bonus: +1 Health, +1 Spirits (if step 5 achieved a balanced meal)
- Capped at MaxHealth / MaxSpirits

**Event emitted**: `RestRecovery(healthGained, spiritsGained)`.

### 10. Evaluate Disheartened

- If Spirits < threshold (10) and not disheartened: gain disheartened
- If Spirits >= threshold and disheartened: clear disheartened

Disheartened imposes disadvantage on all skill checks (encounter, resist, and foraging).

**Event emitted**: `DisheartendGained` or `DisheartendCleared`.

### 11. Death Check

If Health <= 0, the player is dead.

**Event emitted**: `PlayerDied(conditionId)` — names the condition that dealt the killing
drain, if applicable.

## Ordering Rationale

**Foraging before food consumption** means foraged items are immediately available for
tonight's meal. A skilled bushcrafter can partially self-sustain.

**Drain before recovery** means a bad night can kill you even if you ate well. Recovery
after drain means food and rest are damage mitigation, not damage prevention.

**Medicine before new conditions** means cures only help what you already had. You can't
pre-emptively cure tonight's new affliction. But if you passed the resist (or the condition
came from an encounter), the cure works automatically.

**Resist negates cure** means: if you're camping in the mountains with freezing and you
fail the resist, your medicine is wasted. You can't outrun the biome with medicine alone.
Leave or find shelter.

## Ambient Conditions

Checked based on **camping tile only**, not tiles traveled through.

### Biome Conditions

| Condition | Biome | Tier |
|---|---|---|
| Freezing | Mountains | Any |
| Thirsty | Scrub | Any |
| Swamp Fever | Swamp | Any |
| Irradiated | Plains | Tier 3 |
| Gut Worms | Forest | Tier 2 |

### Universal Conditions (every wilderness rest)

| Condition | Notes |
|---|---|
| Exhausted | Resist check every night |
| Lost | Resist check every night |

### Encounter-Only Conditions

Never acquired from ambient checks:
- **Poisoned** — from encounters
- **Injured** — from encounters
- **Disheartened** — from low spirits (evaluated in step 10, not a resist check)

## Food & Meals

### The Triad

Up to 3 food items consumed per night. Each has a type: Protein, Grain, or Sweets.

| Meal Quality | Requirement | Rest Bonus |
|---|---|---|
| Balanced | 1 protein + 1 grain + 1 sweet | +1 Health, +1 Spirits |
| Partial | 1-3 items, incomplete triad | None |
| Starving | 0 items | Hungry condition |

### Food Sources

- **Market**: all settlements stock all 3 food types
- **Foraging**: automatic nightly bushcraft check yields 0-3 items

### Hungry

- Stacking condition (max 3 stacks)
- Gained when food shortage exceeds current stacks
- Cured 1 stack per full meal (3 food items eaten)
- Has health and spirits drain like other conditions

## Condition Recovery

- **No automatic decay.** Conditions never improve on their own.
- **Deterministic cures.** Consume the right medicine, reduce stacks by 1. No roll.
- **Exception: resist negation.** Failed same-night resist for the same condition wastes
  the medicine.
- **ClearedOnSettlement conditions** (exhausted, lost, thirsty) clear automatically
  during full inn recovery.
- **Drain is flat per condition**, not per-stack.

## Inn Stays

Inn calls `EndOfDay.Resolve` with `no_biome = true`:
- No ambient resist checks
- No foraging
- Normal food consumption, medicine, drain, rest recovery
- Inn additionally clears exhausted after resolution
- Full recovery stay (`StayFullRecovery`) bypasses the normal sequence entirely:
  restores to max, consumes all needed medicine, clears ClearedOnSettlement conditions
