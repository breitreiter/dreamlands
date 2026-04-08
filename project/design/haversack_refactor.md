# Haversack Refactor

> Status: planned, not started.
> Supersedes `free_food.md` (the free-food mechanic is folded in here).
> The existing `haversack.md` and `conditions.md` describe the pre-refactor state and
> should be treated as historical reference until this lands.

## Goal

A more legible inventory and condition system. Loadout becomes a real choice.
Food becomes logistics, not economy. Conditions become binary, scannable, and
predictable. The mental model collapses from "many small consumables of many
types" to "10 slots, every item is one decision."

## One-paragraph summary

Halve the haversack to 10 slots. Collapse food to 1 slot per day, free at any
settlement, biome-flavored as meal kits. Conditions become binary (no stacks).
Minor conditions cure environmentally (rest, settlement, leaving biome) and
drain spirits while active. Serious conditions cure with one dose of specific
medicine and tick HP −1/day while active. HP regenerates +1/day when no
serious conditions are active. Foraging is a once-per-day d20 check at the end
of the day; success skips food consumption but doesn't bank. Markets stop
selling food.

## Locked decisions

### Haversack

- Capacity: **10 slots** (was 20)
- Undivided — no separate sections for food, medicine, trinkets
- Pack capacity unchanged (3 slots, hauls and equipment-class items)
- Hauls live in pack, not haversack (unchanged)

### Food

- Single item def: `food_ration` (replaces `food_protein`, `food_grain`,
  `food_sweets`)
- 1 slot per day of food
- Biome-flavored display name via `FlavorText.RationName(biome)` — each biome
  returns a meal kit description like *"dried goat, dates, and flatbread"* or
  *"smoked eel, river rice, swamp berries"*
- No food types, no balanced-meal mechanic, no combo bonus
- Free refill at every settlement (see Rations Refill below)
- Removed from market catalogs entirely. Foraged food is identical to
  guildhouse food (single ration item).
- End-of-day: consume 1 ration. If none in haversack, lose 1 spirit. No
  condition, no roll, no state tracking.

### Rations refill (settlement service)

- Triggered inside `SettlementRunner.EnsureSettlement` on every call
  (idempotent)
- Top up haversack to **5 rations** (a normal trip's worth, half of capacity)
- Never displaces other items. If haversack has fewer than 5 free slots after
  counting existing rations, fill what fits.
- Re-renders of the settlement screen catch up freed slots automatically (the
  storage edge case from `free_food.md` — store a trinket, refill picks up the
  freed slot)
- Returns count added for UI signal (small notice when > 0, suppressed at 0)

### Conditions

Binary. No stacks. Two tiers:

**Minor conditions** — bothersome, not lethal
- Active = drains spirits (exact value pending spirits economy rework)
- Cured *environmentally*: staying at an inn, reaching a settlement, leaving
  the biome that caused it, a successful resist roll on a subsequent day, etc.
- **No medicine.** May have vestigial anti-X items in `ItemDef.cs` that should
  be removed in this refactor.
- Examples: Cold, Tired, Sun-Sick, Lost

**Serious conditions** — death clock
- Active = ticks HP −1 at end of day (see HP below)
- Blocks HP regeneration while active
- Cured by **one dose** of the specific medicine (one bandage clears Injured,
  one anti-rad clears Irradiated, etc.)
- Chapterhouse exists in theory but in practice the death clock runs out
  before you can walk back to the city, so field medicine is genuinely
  essential
- Examples: Injured, Irradiated, Frostbitten, Poisoned

### HP

- HP is the shared serious-condition death clock
- End of day:
  - If **any** serious condition is active → HP −1 (one tick total, not one
    per condition — see open decision below)
  - If **no** serious condition is active → HP +1 (capped at MaxHealth)
- Minor conditions do **not** block HP regen
- HP = 0 → death
- Inn stay still works as a time-skip recovery (full HP/spirits, costs nights
  at `InnNightlyCost`, see Inn below)

### Medicine

- Each medicine cures one specific serious condition with one dose
- Conditions are binary, so "one dose" is unambiguous (no stacks to track)
- Medicines occupy 1 haversack slot each
- Bandages: clear Injured (one dose, one slot)
- Anti-rads, antitoxin, etc.: same pattern

### Foraging

- End of day, automatic
- Roll: `d20 + bushcraft skill + bushcraft gear bonus` vs **DC 20**
- Success: skip the day's ration consumption (no food eaten, no penalty)
- Failure: normal ration consumed (or spirit penalty if none)
- **Cannot bank** — successful forage is a free meal, not a haversack add
- Bushcraft remains the T3 enabler: at +10 total (skill + gear), success rate
  is 55% per day, ~4-5 free meals on an 8-day expedition. At lower totals it's
  a marginal perk.

### Markets

- Food removed from settlement catalogs entirely
- Bandages and medicines remain in catalogs (universal availability for
  bandages, rare/biome-specific for serious-condition medicines)
- No sell-back of food (foraged food no longer exists as a haversack item)
- No other market changes in this refactor

### Inn

- `Inn.GetQuote()` formula unchanged structurally
- `InnNightlyCost = 9` stays for now (the "3× trivial food cost" rationale is
  gone but the felt cost is roughly right; revisit during spirits rework)
- Inn full-recovery still restores HP/spirits to max, clears minor conditions
  via `ClearedOnSettlement`, consumes medicine for serious conditions if
  carried (though serious-condition presence usually means you wouldn't have
  made it this far without already curing them in the field)

## Code changes

### `lib/Rules/CharacterBalance.cs`

- `StartingHaversackSlots`: 20 → 10
- Remove or repurpose `BalancedMealSpiritsBonus`
- `InnNightlyCost`: leave at 9 for now, flag for spirits-rework revisit

### `lib/Rules/ItemDef.cs`

- Replace `food_protein`, `food_grain`, `food_sweets` with single `food_ration`
- Remove `Cost` from food items (or set to 0 — they no longer enter market
  pricing paths)
- Remove `FoodType` field from `ItemDef` and the `FoodType` enum entirely (no
  longer needed)
- Audit medicine items: any "anti-exhaustion" / "anti-cold" / minor-condition
  cures should be removed (minor conditions are environmental-cure only)
- Bandages: confirm they cure `Injured` rather than just adding HP

### `lib/Rules/FoodDef.cs`

- Delete (or strip to a stub if other code references it)

### `lib/Game/Market.cs`

- `InitializeSettlement`: delete the food-stocking block (lines 17-19)
- Remove food-related branches in stock switch (line 87 area)
- Remove food-skip in `Buy` (line 192)
- `createFood` parameter on `Buy` / `ApplyOrder`: simplify to `createRation`
  or remove if rations are constructed inline

### `lib/Game/Rations.cs` (new)

```csharp
public static class Rations
{
    public const int RefillTarget = 5;

    public static int Refill(PlayerState player, BalanceData balance,
        Func<string, Random, ItemInstance>? createRation,
        string biome, Random rng)
    {
        int current = player.Haversack.Count(i => i.DefId == "food_ration");
        int needed = RefillTarget - current;
        int added = 0;

        for (int i = 0; i < needed; i++)
        {
            if (player.Haversack.Count >= player.HaversackCapacity) break;
            var item = createRation?.Invoke(biome, rng)
                ?? new ItemInstance("food_ration", "Rations");
            player.Haversack.Add(item);
            added++;
        }

        return added;
    }
}
```

### `lib/Orchestration/SettlementRunner.cs`

After `Market.Restock(...)` in `EnsureSettlement`:

```csharp
var refilled = Rations.Refill(
    session.Player, session.Balance,
    session.CreateRation, biome, session.Rng);
```

Wire `CreateRation` through `GameSession` (currently `CreateFood` exists for
the granular food types — replace).

### `lib/Game/EndOfDay.cs`

Restructure the per-day flow:

1. **Roll ambient resists** — unchanged structure, but conditions added are
   binary; if a condition is already active, the resist failure is a no-op
   (don't add a stack, don't re-trigger)
2. **Foraging check** — `d20 + bushcraft + bushcraft gear` vs DC 20. Success
   sets a `foragedToday` flag.
3. **Food consumption**:
   - If `foragedToday`, skip
   - Else if a `food_ration` is in the haversack, remove one
   - Else, set `noFoodToday` flag
4. **Spirit updates**:
   - If `noFoodToday`, spirits −1
   - Apply spirits drain from active minor conditions (values TBD pending
     spirits rework)
5. **HP updates**:
   - If any serious condition is active: HP −1
   - Else: HP +1 (capped at MaxHealth)
6. **Death check**: HP ≤ 0 → death event

Remove the entire `ResolveFood` balanced-meal path. Replace with the simpler
ration-or-penalty logic above.

### `lib/Game/Inn.cs`

- `GetQuote` formula unchanged
- `StayAtInn`: `ClearedOnSettlement` minor conditions still get cleared,
  serious conditions still cured by carried medicine if any (rare in
  practice — usually you've already cleared serious in the field or you're
  dead)

### `lib/Game/Conditions.cs` and `lib/Rules/Condition*.cs`

- `ActiveConditions` becomes `HashSet<string>` instead of
  `Dictionary<string, int>` (no stack counts)
- Or keep the dictionary but treat any non-zero value as "active" — less
  invasive but messier
- I'd prefer the HashSet — it forces all the stack-handling code to surface
  during the refactor and get deleted
- Add `Severity` enum with `Minor` / `Serious` (already partially exists as
  `ConditionSeverity`)
- `EndOfDay` reads severity to decide HP vs spirits drain

### `lib/Flavor/`

- New `FlavorText.RationName(biome)` returning meal kit strings per biome.
  Suggested starting palette:
  - Plains: *"smoked sausage, hard cheese, oat cakes"*
  - Mountains: *"dried goat, dates, flatbread"*
  - Forest: *"mushroom jerky, hazelnuts, honey biscuit"*
  - Scrub: *"dried lizard, cactus pulp, pemmican"*
  - Swamp: *"smoked eel, river rice, swamp berries"*
- Remove or stub the old `FoodName` function

### Encounter `.enc` files

- Grep for `food_protein`, `food_grain`, `food_sweets`, `gain food_*` patterns
- Replace with `food_ration` / `gain food_ration`
- Audit `+condition` mechanics for any that assume stacking (they become
  no-ops if the condition is already active — usually fine, sometimes the
  intent was to escalate and needs a different mechanic)

## Edge cases

| Case | Handling |
|---|---|
| Player arrives at settlement with empty haversack | Refill adds 5 rations |
| Player arrives full | Refill adds 0; storing/discarding frees slots, next `EnsureSettlement` call refills |
| Player has 3 rations on arrival | Refill adds 2 (top up to 5) |
| Player skips food while foraging succeeded | Counts as fed, no penalty, no ration consumed |
| Multiple serious conditions active | HP −1 once total, not once per condition (open decision below — could be one-per) |
| Serious condition active but cured mid-day | Tick still applies at end of day if active at any point? Or only if active at end of day? **Decision: only end-of-day state matters.** |
| Bandage cures Injured but HP is at 1 | HP regen kicks in next day (no serious conditions); after 3 days you're at 4 |
| Player tries to overstock food at multiple settlements | Refill caps at 5; they can't stockpile rations to push deeper |
| Encounter that grants a stacking condition (e.g. `+condition cold +condition cold`) | Second add is a no-op; if escalation was intended the encounter needs rewriting |
| Old saves with `food_protein` etc. in haversack | Migration: convert to `food_ration` 1:1 (or just clear and refund). Probably fine to break saves during refactor. |

## Tests

New test file: `tests/Game.Tests/RationsTests.cs`
- Refill empty → 5 added
- Refill partial → tops up to 5
- Refill at target → 0 added
- Refill capped by space
- Refill is idempotent within a single `EnsureSettlement` flow

`tests/Game.Tests/EndOfDayTests.cs` updates:
- Eating consumes 1 ration
- Foraging success skips consumption
- No ration → −1 spirit
- Serious condition active → HP −1
- No serious condition → HP +1
- Mixed: minor condition active doesn't block HP regen
- Multiple serious conditions → HP −1 (not −2) [pending decision below]

`tests/Game.Tests/ConditionsTests.cs` updates:
- Adding an already-active condition is a no-op
- Bandage clears Injured

`tests/Game.Tests/MechanicsTests.cs` updates:
- `+condition X` is binary, idempotent

`tests/Orchestration.Tests/SettlementRunnerTests.cs` updates:
- Rations refill on `EnsureSettlement`
- Existing tests that buy food from market → delete or repurpose

## Open decisions before implementation

1. **HP tick: −1 per serious condition or −1 flat regardless?**
   I lean **flat −1** — simpler, prevents stacking-doom from a single
   unlucky encounter chain, keeps HP arithmetic legible. Per-condition is
   more punishing but harder to reason about.
2. **`ActiveConditions` shape: HashSet vs Dictionary-with-1.**
   I lean **HashSet** — forces all stack-handling code to surface and get
   deleted, can't accidentally leave a `++` somewhere.
3. **Refill target = 5?** Half the haversack, one normal trip. Could be 4
   (more pack pressure) or 6 (more comfortable). 5 is a starting point.
4. **Migration of old saves.** Refactor will break them. OK to break, or
   write a one-time migration?

## Tabled / out of scope

These are real follow-up tickets, not part of this refactor:

- **Spirits economy rework.** Now scoped in `spirits_economy.md` — sibling
  refactor, implement after or alongside this one. Drain values, exhaustion
  scaling, inn tiered pricing, and the deletion of daily passive regen all
  live there.
- **Trinket rebalance.** Trinkets get squeezed by the smaller haversack. The
  refactor accepts this. A separate pass should decide whether trinkets are
  rare/situational (current direction) or whether the system needs rethinking
  to make them feel essential without forcing pack pressure. **Combat
  trinkets explicitly out of scope** — the existing combat itemization is
  too entangled to retune as part of this work.
- **Bushcraft +10 ceiling verification.** The foraging math assumes a max
  bushcraft total of ~+10 (skill 4 + gear ~+6). Bushcraft gear exists in
  `ItemDef.cs` but the actual bonus ceiling should be confirmed before the
  foraging DC is locked in production.
- **Inn pricing recalibration.** `InnNightlyCost = 9` stays for now. Revisit
  alongside spirits rework once the new daily spirits arithmetic is settled.
- **Bank/storage capacity.** No changes — confirmed no coherent impact from
  haversack shrink.

## Implementation order (suggested)

1. ItemDef + FoodDef changes (single ration item, drop FoodType)
2. ActiveConditions → HashSet, condition-add becomes idempotent
3. Conditions tier audit (Minor vs Serious) + cure-removal for minor
4. EndOfDay rewrite (foraging → ration consume → spirit/HP updates)
5. Rations.cs + SettlementRunner refill wiring
6. Market food removal
7. Haversack capacity 20 → 10
8. FlavorText.RationName + biome palette
9. Encounter .enc audit + fixes
10. Test updates and new tests
11. UI signal for refill (polish)
