# End-of-Day Maintenance

End-of-day is the core survival loop. It runs every time the day counter advances.

## Terminology

This is "end-of-day", not "resting". Some days end without rest — fleeing a monster all
night, trapped in a collapsing ruin. End-of-day always happens; rest is a component that
can be skipped.

## When It Triggers

End-of-day fires whenever `Day` increments. This includes:
- Normal time advancement (time wraps past Night → Morning)
- `skip_time` advancing past midnight

**No free lunches.** If a modal encounter or `skip_time` jumps you forward multiple days,
you true up on end-of-day for each skipped day. A 3-day time skip means 3 end-of-day
cycles, resolved in order.

### skip_time Flags

The `skip_time` verb accepts flags that modify end-of-day when the day boundary is crossed:
- `no_sleep` — skip rest recovery (base + meal bonus). You were fleeing, not sleeping.
- `no_meal` — skip meal consumption. You didn't have time to eat.
- `no_biome` — skip ambient condition checks. You were indoors/underground/etc.

These flags exist for fiction reasons. If you're running from something all night, it's
absurd to suggest you stopped to cook. The flags let encounters tell end-of-day what
actually happened.

## The Two-Phase Model

End-of-day is presented as a camp screen with two phases.

### Phase 1: Warning (START of camp screen)

1. **Roll ambient condition checks** based on camping tile biome (and tier)
2. **Display ominous warnings** if conditions would be applied:
   - "The cold wind cuts through your camp..." (Freezing imminent)
   - "You feel feverish and your joints ache..." (Swamp fever imminent)
3. **Show existing active conditions**
4. **Player makes decisions**: select meal, use medicines, spend supplies

The normative case is that most warnings are unavoidable — you don't have warm gear, so
you're going to freeze. The warning creates tension and teaches consequences. The edge case
is consumable preventatives: "You feel feverish" → eat jorgo root → condition prevented.

### Phase 2: Resolution (END of camp screen)

Executed in this order:

1. **Consume food** — remove selected food items from haversack
2. **Ambient condition resist checks** — for each biome/tier condition that applies:
   - Roll resist check: d20 + skill + gear resist bonus vs DC (see `dice_mechanics.md`)
   - Gear resist bonuses computed from `ResistModifiers` via `ResistBonusMagnitudes` table
   - If failed and condition not already active: apply condition (Succumb text)
   - If failed and condition already active: no stack reset, show HealFailure text
   - If passed: show Resist text (unless auto-pass from gear)
3. **Apply medicines** — for each medicine the player chose to use:
   - If the player failed the resist check for this same condition (step 2): cure negated,
     show HealFailure text, medicine still consumed
   - Otherwise: deterministic — consume the item, reduce condition stacks by cure magnitude
   - If stacks reach 0: condition removed (HealComplete text)
4. **Condition drain** — for each active condition:
   - Apply HealthDrain (flat, not per-stack)
   - Apply SpiritsDrain (flat, not per-stack)
   - Apply SpecialEffect if any (e.g. `lost` erases map routes)
5. **Rest recovery** (skipped if `no_sleep` flag):
   - Base: +1 Health, +1 Spirits (from `CharacterBalance`)
   - Balanced meal bonus: +1 Health, +1 Spirits (if triad met)
   - Capped at MaxHealth / MaxSpirits
6. **Death check** — if Health <= 0, game over (show condition Death text if applicable)

### Ordering Rationale

Drain before recovery means a bad night can still kill you even if you ate well. Recovery
after drain means food and rest are damage mitigation, not damage prevention. Medicine
before drain means treating a condition in time reduces tonight's suffering — but you still
take the hit from whatever conditions remain.

Resist before cure means: if you're camping in the mountains and you fail the freezing resist
check, your medicine for freezing is wasted — the environment overwhelmed it. You can't
outrun the biome with medicine alone; you need to leave or find shelter. But if you passed
the resist (or the condition came from an encounter, not tonight's biome), the cure works
automatically — no roll needed.

## Food & Meals

### The Triad

Players consume up to 3 food units per day. Food has a category — Protein, Grain, Sweets —
and names are regional color (biome-flavored), exactly like weapon types.

| Meal Quality | Requirement | Effect |
|---|---|---|
| Balanced | 1 protein + 1 grain + 1 sweet | +1 Health, +1 Spirits bonus |
| Partial / Monotone | 3 units, incomplete triad | No bonus, no penalty |
| Skimped | 1-2 units | No bonus, no penalty, but risky |
| Starving | 0 units | Hungry condition applied |

### Hungry Condition

Hungry is triggered by eating 0 food. It has 2 stacks with trivial health and spirits drain.

**Hungry is cured by food.** Eating a meal (any food at all) removes a stack. This is
intentionally forgiving — we don't want the player starving while eating as much as the game
allows. But we also don't want alternate-day fasting to be free, so the condition lags
slightly: skip a day, get hungry (2 stacks), eat next day (-1 stack), eat again (-1 stack,
cured). One missed meal costs you two days of mild drain. Harsh enough to notice, forgiving
enough to recover from.

## Ambient Conditions

Checked based on **camping tile only**, not tiles traveled through.

- Travel through 3 swamp hexes, camp in plains → no swamp fever risk
- Travel through 1 swamp hex, camp in swamp → swamp fever risk

This is simple (one check per rest), tactical (push through danger to reach safe ground),
and narratively coherent (you get sick from sleeping in hazards, not passing through them).

### Biome Conditions

| Condition | Biome | Tier | Trigger |
|---|---|---|---|
| Freezing | Mountains | Any | Camping in mountains |
| Thirsty | Scrub | Any | Camping in scrub |
| Swamp Fever | Swamp | Any | Camping in swamp |
| Irradiated | Plains | Tier 3 only | Camping in tier-3 plains |

### Universal Conditions (checked every rest)

| Condition | Trigger |
|---|---|
| Hungry | No food consumed |
| Road Flux | Every rest (resist check) |
| Lost | Every rest (resist check) |

### Encounter-Only Conditions

These are never acquired from ambient checks — only from encounter mechanics:

| Condition | Typical Source |
|---|---|
| Poisoned | Failed encounter combat/trap |
| Injured | Lost medium+ health in one encounter |
| Exhausted | Encounter consequence or travel extremes |

## Condition Recovery Rules

From `conditions_list.md` (authoritative source), updated per `dice_mechanics.md`:

- **Conditions do not improve on their own.** No automatic stack decay.
- **Recovery is deterministic.** Consume the right curative item → reduce stacks by cure
  magnitude. No skill check for the cure itself.
- **Exception: failed resist negates cure.** If you failed tonight's resist check for
  this same condition, the medicine is consumed but does nothing (HealFailure text).
  This only applies to ambient conditions checked during this end-of-day — encounter-sourced
  conditions always respond to medicine.
- **0 stacks = cured.** Condition removed, HealComplete text shown.
- **Drain is flat per condition**, not per-stack. 1 stack of Injured drains the same as
  3 stacks of Injured.
- **SpecialCure** conditions (Freezing, Thirsty, Exhausted) have alternate cure paths
  that bypass the normal medicine system.

## Blockers

All blockers resolved. End-of-day is ready for implementation.

### Resolved

- ~~**Food item definitions**~~ — 3 food ItemDefs (protein/grain/sweets), biome-aware flavor
  names via `FlavorText.FoodName()`.
- ~~**Food in marketplace**~~ — all settlements stock all 3 food types at max stock, food
  restocks alongside trade goods.
- ~~**Condition resist/cure mechanics**~~ — resist is a standard skill check with gear
  bonuses from `ResistModifiers` → `ResistBonusMagnitudes`. Cure is deterministic (consume
  item → heal stacks), negated only by a same-night failed resist. See `dice_mechanics.md`.
- ~~**Skill check formalization**~~ — unified formula in `SkillChecks.cs`: `d20 + skill +
  gear bonus vs DC`, with nat 1/20, advantage/disadvantage, luck rerolls. Resist bonuses
  via `GetResistBonus()`, encounter bonuses via `GetItemBonus()`.
