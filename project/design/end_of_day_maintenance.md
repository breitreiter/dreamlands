foods cluster into three groups
- protein
- carbs
- sweets

at the end of every day, we force you into a camp screen.

in that screen, you can select meal ingredients. we'll suggest a balance. in the game fiction, you're selecting dinner and breakfast.

if you had a balanced meal, you get a resist to some conditions. if you ate certain foods, you may get a per-food resist to a condition.

## Condition Check Timing - Two-Phase System

**Decision**: Use a warning-then-resolution model to telegraph consequences while allowing limited player response.

### Phase 1: START of Rest Screen (Warning Phase)
- **Roll** for conditions from today's travel/camping
- **Display ominous hints** if conditions would be applied:
  - "The cold wind cuts through your camp..." (Freezing imminent)
  - "You feel feverish and your joints ache..." (Swamp fever imminent)
  - "Your muscles scream with exhaustion..." (Exhausted imminent)
- **Show** existing conditions you're bringing into camp
- **Player makes decisions**: Select meals, use items, spend medical supplies

### Phase 2: END of Rest Screen (Resolution Phase)
- **Check preventatives**: Did player address the warnings?
  - Jorgo root in meal → prevents swamp fever
  - Balanced meal → resists some conditions
  - Medical supplies used → treats existing conditions
- **Apply conditions**: If warnings weren't addressed (or couldn't be)
  - No warm gear → Freezing condition applied
  - No jorgo root → Sick condition applied
- **Tick existing conditions**: Daily health/spirits drain

### Design Notes

**Normative case** (most conditions):
- Warning given, but player has no immediate fix
- "It's freezing cold, you don't have warm gear" → **unavoidable**
- Creates tension and teaches consequences for poor preparation
- Examples: Freezing (need gear from town), Exhausted (need rest days)

**Edge case** (consumable preventatives):
- Warning given, player can respond with food/item
- "You feel feverish" → eat jorgo root → **condition prevented**
- Requires having the right supplies AND recognizing the need
- Examples: Swamp fever (jorgo root), possibly others TBD

### Ambient Condition Source
**Decision**: Ambient conditions (Freezing, Swamp Fever, etc.) are checked based on **camping tile only**, not tiles traveled through.

**Example**: 
- Travel through 3 swamp hexes, camp in plains → **no swamp fever risk**
- Travel through 1 swamp hex, camp in swamp → **swamp fever risk**

**Design benefits**:
- Simple: one check per rest, not per-tile-crossed
- Tactical: push through dangerous terrain to reach safe camping
- Narrative: you get sick from sleeping in hazardous conditions, not passing through them

**Implication**: Swamp fever, freezing, etc. are **camping hazards**, not travel hazards. Duration of exposure doesn't matter, only where you sleep.

**The Triad:** Players consume up to 3 food units per day. Category is the mechanic — protein, carbs, sweets — and names are regional color, exactly like weapon types. Eating one from each category is a "balanced meal" and grants +1 Health restore, +1 Spirits restore overnight. This is a mild but reliable reward for planning ahead. You *can* eat three proteins and survive, but you miss the bonus.

| Meal Quality | Requirement | Bonus |
|-------------|-------------|-------|
| Balanced | 1 protein + 1 carb + 1 sweet | +1 Health, +1 Spirits |
| Partial / Monotone | 3 units, incomplete triad | No bonus, no penalty |
| Skimped | 1-2 units | No bonus, no penalty, but risky |
| Starving | 0 units | Hungry condition applied |