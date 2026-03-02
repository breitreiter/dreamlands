# Foraging — Automatic End-of-Day

Foraging is an automatic step in end-of-day resolution. Every night spent in the wilderness, the player scrounges for food. Bushcraft skill and gear determine the yield.

## Mechanics

- **Skill check**: d20 + bushcraft skill + equipment bonus (same as encounter bushcraft checks)
- **Disheartened**: imposes disadvantage on the roll
- **No luck reroll**: foraging is a background activity, not a dramatic moment
- **No nat 1/20 rules**: this is a multi-tier harvest, not pass/fail

## Yield Thresholds

| Total Roll | Yield |
|------------|-------|
| < 16       | 0 food |
| 16+        | 1 food |
| 18+        | 2 food |
| 20+        | 3 food |

## Modifier Range

- **+0**: no bushcraft skill, no gear
- **+10**: bushcraft 4 + kopis (+5 foraging bonus) + relevant token (+1)

## Food Items

- Food only — no medicines (avoids haversack bloat)
- Items cycle through protein → grain → sweets for variety
- Foraged food gets biome-appropriate flavor names via `FoodNames.Pick(..., foraged: true)`
- Items are real `ItemInstance`s added to haversack before food consumption
- `ResolveFood` picks the best meal from all available food (purchased + foraged)

## Sequence in EndOfDay.Resolve

Foraging runs after resist checks and before food consumption:

1. Read/clear flags
2. Snapshot pre-existing conditions
3. RollResists
4. **ResolveForaging** (adds food to haversack)
5. ResolveFood (consumes food, including what was just foraged)
6. ResolveMedicines
7. ApplyNewConditions
8. ConditionDrain
9. Rest
10. Disheartened
11. Death check

## Skipped At Settlements

When `noBiome` is true (inn stays, settlement rest), foraging is skipped — the player is indoors.

## Expected Averages

- **+10 bushcrafter**: ~1.95 food/day
- **+0 character**: ~0.45 food/day
