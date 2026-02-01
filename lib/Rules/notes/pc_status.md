# PC Status System

*Core mechanics for player character wellbeing and condition tracking*

## Ablative Stats

PCs have two ablative stats representing different aspects of survivability:

### Health
- **Concept**: Abstract representation of physical wellbeing and non-deadness
- **Type**: Integer pool (traditional HP)
- **Reduced by**: Combat damage, status condition effects, starvation
- **Restored by**: Medical supplies, town healer (infinite medicine), time

### Spirits
- **Concept**: Abstract representation of mental wellbeing, fortitude, and morale ("in good spirits")
- **Type**: Integer pool (parallel to health)
- **Mechanical impact**: As spirits decrease, increasing penalties to skill check rolls
- **Reduced by**: 
  - Status condition effects (daily drain)
  - Scripted encounter events:
    - Moral failures (ignoring someone in need)
    - Combat defeats
    - Witnessing cosmic horrors beyond human comprehension
- **Restored by**:
  - Settlement services: Entertainment or relaxation activities
  - Scripted encounter events:
    - Helping someone in need
    - Clever solutions to tricky situations
    - Moral victories

**Design Note**: Most events don't mutate these stats directly. Instead, events apply **conditions**, which then cause stat changes over time.

### Spirits Roll Penalty Mechanic

**Key difference from Health**: While health is purely ablative (tracks survivability), spirits has an active mechanical impact:

- **As spirits decrease → skill check rolls receive increasing penalties**
- This creates a death spiral mechanic: low spirits → worse rolls → more failures → more spirits loss
- Adds psychological/morale dimension to gameplay
- Thematically ties to cosmic horror: witnessing the incomprehensible degrades your effectiveness

**Open questions**:
- Penalty formula: Linear (e.g., -1 per 10 spirits lost)? Stepped thresholds (full bonus until 50%, then -2, then -4)?
- Does this apply to all rolls or only certain types (e.g., exclude physical combat)?

---

## Condition System

Conditions are the primary mechanism for ongoing stat changes. Rather than directly damaging health/spirits, most events apply a condition that has lasting effects.

### Condition Properties

Each condition has the following properties:

1. **Name**: The condition identifier (e.g., "Injured", "Hungry", "Sick")

2. **Trigger**: The event or circumstance that applies the condition
   - *Implementation note*: May be stored in the trigger source (encounter text, tile type, etc.) rather than in the condition definition itself
   - **Two trigger types**:
     - **Encounter-based**: Applied during encounters (fall in bog, injured in combat)
     - **Ambient/camping-based**: Checked during rest phase based on camping tile only (not tiles traveled through)

3. **Preventative**: What prevents the condition from being applied
   - *Implementation note*: May be stored in gear/item definitions (e.g., camping gear prevents "Freezing") rather than in the condition itself
   - **Two types of preventatives**:
     - **Gear-based**: Must have equipment before camping (warm clothes, camping gear, mosquito netting)
     - **Consumable-based**: Can be applied during rest screen (jorgo root, special foods)
   - See `food_drink.md` for two-phase warning/resolution system

4. **Cure**: What removes the condition
   - *Implementation note*: May be stored in item/service definitions (e.g., medical supplies cure "Sick") rather than in the condition itself

5. **Effect**: What the condition does mechanically
   - **Typical effect**: Daily loss of health, spirits, or both
   - **Other effects**: Could potentially include speed penalties, skill check penalties, etc. (TBD)

### Known Conditions

Initial list of conditions (not exhaustive):

- **Injured**: Physical trauma from combat or accidents
- **Hungry**: Prolonged lack of food (from failed foraging or resource exhaustion)
- **Sick**: Disease or infection
- **Freezing**: Cold exposure without proper camping gear
- **Exhausted**: Fatigue from overexertion or lack of rest

---

## Open Questions

### Spirits Stat Details
- [x] **Restoration sources defined**: Settlement entertainment/relaxation, scripted event rewards
- [ ] What is the starting value for spirits?
- [ ] Roll penalty formula: How much penalty at what spirits levels? (Linear? Stepped thresholds?)
- [ ] What happens at 0 spirits? (Different from 0 health? Can you continue with 0 spirits?)
- [ ] Settlement entertainment/relaxation: Cost? Daily limit? Instant or over time?
- [ ] Can spirits exceed starting value (temporary morale boost)?

### Condition Effect Mechanics
- [x] **Effects are deterministic**: Each condition causes a fixed daily loss of health/spirits (e.g., -2 health/day)
- [x] **No severity levels**: Conditions don't escalate through stages. Severity is tracked indirectly via the ablative stats getting lower.
  - Example: You don't progress from "Injured" → "Badly Injured" → "Critical". You stay "Injured" but your health drops each day until you die or get healed.
- [ ] Can conditions stack? (Injured + Sick = both effects apply?)
- [ ] Do conditions interact? (Sick + Exhausted = extra penalty?)

### Condition Lifecycle
- [ ] Do conditions have durations/timers, or do they persist until cured?
- [ ] Can conditions spontaneously improve without treatment?
- [ ] Does cure fully remove condition, or does it take time after treatment?

### Implementation Architecture
- [ ] Where are triggers actually defined? (Encounter files? Tile definitions?)
- [ ] Where are preventatives defined? (Gear items? Character state?)
- [ ] Where are cures defined? (Item properties? Service definitions?)
- [ ] Is there a central condition registry, or are they defined by their sources?

---

## Thematic Integration

### Cosmic Horror Theme
The spirits mechanic directly reinforces the cosmic horror theme:
- **Witnessing incomprehensible horrors** drains spirits
- Low spirits creates mechanical penalties (worse rolls)
- Creates a **sanity/morale** system without explicitly calling it "sanity"
- Aligns with magic theme: encountering dark magic damages your psyche

### Moral Dimension
Spirits creates mechanical weight for moral choices:
- **Helping others** restores spirits (and improves rolls)
- **Ignoring people in need** drains spirits (and worsens performance)
- Makes "being a good person" mechanically advantageous, not just flavor
- Adds psychological consequence to selfish play

---

## Relationship to Other Systems

### Camping Gear (from decisions.md)
- **Current decision**: Camping gear prevents status conditions when sleeping
  - Disease (infected from unsanitary rest)
  - Freezing (exposure in cold biomes)
- **Alignment**: Matches preventative model above

### Medical Supplies (from decisions.md)
- **Current decision**: Consumed to treat disease status conditions
- **Alignment**: Matches cure model above

### Status Condition Timers (from decisions.md)
- **Current decision**: Deterministic daily stat loss
- **Effect model**: Conditions cause fixed daily loss of health/spirits until cured
- **Severity tracking**: No condition levels - severity measured by how low your stats get, not condition stage
