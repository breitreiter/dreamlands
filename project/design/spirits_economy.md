## Spirits Economy Rework

> Status: planned, not started.
> Sibling to `haversack_refactor.md` — coupled at the seams (both touch
> `EndOfDay` and condition shape) but mostly orthogonal in code.
> Implement after or alongside the haversack refactor.

## Goal

Spirits become a one-way budget on the road. You leave the inn full, you spend
spirits while traveling, you go back to an inn to refill. The road is a slow
grind whose ceiling is "lose nothing today." Encounters are the only positive
swing in the wilderness.

## One-paragraph summary

Delete the daily passive +1 spirits regen entirely. Drain comes only from
active minor conditions (1/day each, stacking) and bad encounter outcomes.
Exhaustion gets a scaling DC — harder to resist each consecutive wilderness
night — and clears on any settlement entry, which makes trip length the
natural pacing knob. Inns sell three tiers of overnight service (bed / bed +
bath / bed + bath + drinks) at 5g / 12g / 25g, restoring 5 / 10 / full
spirits respectively. All tiers clear minor conditions. HP regen remains
passive per the haversack refactor.

## Locked decisions

### No daily passive regen on the road

- Delete `BaseRestSpirits` from `CharacterBalance` (currently 1)
- `BalancedMealSpiritsBonus` already going away with the haversack refactor
- `EndOfDay.ResolveRest` no longer adds spirits — the function either goes
  away or only fires the `RestRecovery` event with `spiritsGain = 0` for UI
  continuity
- A clean travel day costs **0 spirits**. That is the ceiling on road
  performance.

### Drain magnitude

- Active minor conditions: **1 spirit/day each**, stacks across multiple
  conditions
- Today's `SpiritsDrain` values (3/night for freezing, thirsty, exhausted)
  collapse to 1
- Stacking is mostly theoretical — typical case is one minor condition at a
  time. The desert combo (exhausted + thirsty) is the realistic worst case
  and stacks to 2/day, which is correct: the desert *should* hurt more.

### Exhaustion: scaling DC, fire-and-stick

- Today exhaustion is a flat resist roll each wilderness night, sticks once
  failed, drains for the rest of the trip
- Replace with a DC that **scales with consecutive wilderness nights**:
  base DC + N where N = nights since last settlement
- Suggested starting curve: base DC 12, +1 per consecutive wilderness night
  (so first night DC 13, second night DC 14, etc.)
- Once acquired, exhaustion sticks until cleared at a settlement (no daily
  re-resist)
- Effect: variance shrinks. Early-trip failure is unlikely; late-trip
  failure is nearly certain. The mechanic *pushes* the player toward an
  inn instead of randomly punishing day-1 unluckiness.

### Exhaustion clears on settlement

- `Exhausted` gets `ClearedOnSettlement = true` (today it has
  `SpecialCure = "Rest in an inn."` only)
- Same path freezing and thirsty already use
- The "warm bed, hot bath" inn vibe lives in the inn screen flavor text,
  not in being the unique cure for exhaustion
- Counter for the scaling DC resets when the player enters any settlement

### Inn tiered service

`Inn.GetQuote()` is replaced with three concrete service options the player
picks at the inn screen:

| Service | Restores | Cost |
|---|---|---|
| A bed for the night | +5 spirits | 5 gold |
| Bed + hot bath | +10 spirits | 12 gold |
| Bed, bath, evening drinks | full | 25 gold |

- All tiers clear minor conditions (you're inside, you're safe)
- All tiers consume one night (time advance)
- Premium tiers are not more gold-efficient per spirit — they are *more*.
  Budget tier exists so a broke traveler always has a way home.
- HP regen remains passive overnight per the haversack refactor (no
  separate inn HP charge)
- Pricing rationale: starter gold 50g → 2× bed-only or 1× bath; haul
  payouts ~30g/day → premium night is ~0.8 days of work, daunting but
  reachable
- Numbers are placeholders pending haul-payout calibration but anchored
  at 25g cap as the upper bound

### Spirits floor and 0-spirits behavior

- Floor at 0 (no negative spirits)
- At 0 spirits, tactical encounters auto-fail per existing `.tac` scripting
  convention (this is the implicit "broken" state)
- No additional skill check penalty needs to be added — the tactical
  failure cliff is enough teeth
- Note: today `SkillChecks` may apply a spirits penalty to skill checks.
  Verify during implementation; if so, decide whether it stays or whether
  the tactical-fail cliff is sufficient.

### Sources of road regen

- Encounter outcomes that explicitly grant `+spirits N` (the only positive
  swing in the wilderness)
- Nothing else. No food bonus. No rest bonus. No biome bonus.

## Trip-cost math

Assumes haversack 10, 5-ration refill cap, foraging is roughly a coin flip,
drain = 1/day per active minor condition.

| Trip | Days | Clean run | Bad run (exhausted early, ~2 missed meals) |
|---|---|---|---|
| Cradle hop | 1.5 | 0 | ~1 |
| Midlands hop | 3–4 | 0 | ~5 |
| Wilds round-trip | 6 | 0 | ~7 |
| Frontier round-trip | 10 | 0 | ~13 |
| Deep one-way push | 9 | 0 | ~12 |

A bad frontier trip leaves the player at ~7/20 spirits — battered, definitely
inn-bound, not dead. A clean frontier trip costs nothing, which is the design
ceiling.

## Code changes

### `lib/Rules/CharacterBalance.cs`

- Remove `BaseRestSpirits` (or set to 0 and flag for deletion)
- Remove `BalancedMealSpiritsBonus` (already going with haversack refactor)
- Add `ExhaustionBaseDC = 12` and `ExhaustionDCPerNight = 1`
- Add inn pricing constants:
  ```csharp
  public int InnBedCost { get; init; } = 5;
  public int InnBedSpirits { get; init; } = 5;
  public int InnBathCost { get; init; } = 12;
  public int InnBathSpirits { get; init; } = 10;
  public int InnFullCost { get; init; } = 25;
  ```
- Remove `InnNightlyCost` (the old flat 9g)

### `lib/Rules/ConditionDef.cs`

- All minor conditions: `SpiritsDrain` 3 → 1
- `Exhausted`: add `ClearedOnSettlement = true`
- Verify freezing/thirsty already have `ClearedOnSettlement = true` (they do)

### `lib/Game/PlayerState.cs`

- Add `ConsecutiveWildernessNights` (int, default 0)
- Reset to 0 on any `SettlementRunner.EnsureSettlement` call
- Increment in `EndOfDay.Resolve` after a wilderness rest

### `lib/Game/EndOfDay.cs`

- Delete `ResolveRest` (or keep it as an empty event-fire stub)
- In `RollResists`, when rolling exhaustion specifically, use
  `balance.Character.ExhaustionBaseDC + state.ConsecutiveWildernessNights`
  instead of the static `ResistDifficulty`. Other ambient conditions keep
  their flat DC.
- Increment `ConsecutiveWildernessNights` at end of resolution if not at a
  settlement
- Remove the `ate` precondition on rest recovery (it no longer matters —
  there is no rest recovery)

### `lib/Game/Inn.cs`

- Replace `GetQuote()` with three discrete service options
- New API: `GetServiceOptions(PlayerState, BalanceData)` returning a list of
  `InnService` records (id, name, cost, spiritsRestored)
- New API: `BookService(PlayerState, BalanceData, string serviceId, Random)`
  that:
  - Validates the player can afford it
  - Deducts gold
  - Adds spirits (capped at MaxSpirits for the bath tier; sets to MaxSpirits
    for the full tier)
  - Clears all minor conditions
  - Advances time by one night
  - Returns a result describing what happened (for UI)
- The old `StayAtInn` / "stay one night" CLI command maps to the bed tier
  for backwards compatibility, or gets replaced by an explicit
  `inn-book <bed|bath|full>` command

### `lib/Orchestration/SettlementRunner.cs`

- In `EnsureSettlement`, after rations refill, reset
  `ConsecutiveWildernessNights = 0`
- Apply `ClearedOnSettlement` condition cures (this path may already exist —
  verify)

### `lib/Game/SkillChecks.cs`

- Audit for any spirits-based skill check penalty
- Decide whether to keep it (extra teeth at low spirits) or remove it (the
  tactical fail-at-0 cliff is enough)
- Default recommendation: **remove** — fewer hidden modifiers, the cliff
  is the rule

### `ui/cli`

- Replace `inn` / `rest` / `inn-recover` commands with `inn` (lists
  services) and `inn-book <id>` (books one)
- Or keep `rest` as an alias for `inn-book bed`

## Edge cases

| Case | Handling |
|---|---|
| Player at 0 spirits books bed (5sp) | Spirits → 5, normal flow |
| Player at 18/20 books full (25g) | Restores to 20, still costs 25g — premium is a vibe purchase, not a transaction |
| Player can't afford any tier | Inn screen shows all three with affordability flags; `BookService` returns failure result |
| Exhausted player enters settlement, leaves same day | Exhaustion cleared on entry, counter reset, fresh start |
| Player overnight in settlement without booking inn | Counter still resets (settlement entry resets it, not inn booking) — this is intentional, settlements are safe regardless of whether you spend gold |
| Encounter grants `+spirits 5` while at max | No-op (capped) |
| Tactical encounter starts at 0 spirits | Auto-fails per existing `.tac` convention |
| Player has both exhausted and thirsty | Drain stacks: 2/day total |

## Tests

New / updated tests:

`tests/Game.Tests/EndOfDayTests.cs`:
- No spirits gained from a clean rest day (regen deleted)
- 1 spirit lost per active minor condition
- Multiple minors stack (exhausted + thirsty = -2)
- Exhaustion DC scales with `ConsecutiveWildernessNights`
- Counter increments on wilderness rest, not on settlement rest

`tests/Game.Tests/InnTests.cs`:
- Each tier deducts correct gold
- Each tier restores correct spirits
- All tiers clear minor conditions
- Insufficient gold returns failure
- Full tier sets spirits to max regardless of starting value
- Exhaustion cleared by inn (via settlement entry path, not inn-specific)

`tests/Orchestration.Tests/SettlementRunnerTests.cs`:
- `EnsureSettlement` resets `ConsecutiveWildernessNights`
- `EnsureSettlement` clears all `ClearedOnSettlement` conditions including
  exhaustion

## Open decisions

1. **Exhaustion DC curve.** Base 12 / +1 per night is the starting value. Tune
   in playtest. At base 12 +1/night, with average resist modifier ~+3, players
   coin-flip around night 6 and near-certain fail by night 9 — reasonable for
   a 10-day frontier trip.
2. ~~**`SkillChecks` low-spirits penalty.**~~ **Resolved**: the comment in
   `SkillChecks.cs:22` was aspirational — no implementation existed. Comment
   removed; no penalty added. Teeth at zero come from `TacticalRunner.cs:214-221`
   (auto-fail on Spirits ≤ 0), which is enough.
3. **Inn pricing precision.** 5/12/25 are placeholders anchored to "≤25 for
   full." Final tuning waits for haul-payout calibration.
4. ~~**`Disheartened` condition.**~~ **Resolved**: never existed in code (only
   in old design docs). No def to delete.

## Tabled / out of scope

- **Haul payout calibration.** Inn pricing assumes ~30g/day haul income.
  If that number drifts, inn prices should retune in proportion.
- **Encounter `+spirits` audit.** This refactor doesn't touch existing
  encounters that grant or drain spirits. A pass to make sure the values
  feel right under the new economy is a separate ticket.
- **Spirits as a tactical resource.** `.tac` files spend spirits as a
  currency. That system stays as-is; this refactor only changes how the
  spirits pool is filled and drained on the overworld.
