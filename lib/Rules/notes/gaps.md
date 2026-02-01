# Rules Engine — Specification Gaps

*What needs to be resolved before (or during) encoding rule systems into code.*

## Ready to Implement

These areas have enough structural definition to start coding today. Missing tuning numbers can use placeholder constants.

- [x] Resource model (per-day food/water/medical/camping gear, terrain penalties, foraging fallback)
- [x] Inventory slot system (integer slots, gear bonuses)
- [x] Trade good categories (9 named categories, economic vs flavor layer)
- [x] Danger tiers (4 discrete tiers, auto-assigned by graph distance)
- [x] Auto-travel system (route discovery, auto-purchase, failure handling, time passage, restrictions)
- [x] Game state model (win condition, encounter pool depletion, dungeon permanence, save format)

---

## Blocking Gaps

Structural or definitional gaps — the shape of the system is unclear, not just the numbers.

### Skill Check Resolution

- [ ] How many degrees of success? (pass/fail? pass/partial/fail? sliding scale?)
- [ ] What are the degree thresholds? (beat DC by 5? 10? margin-based?)
- [ ] Do degrees map to outcomes universally or per-encounter?
- [ ] This defines the return type of `ResolveSkillCheck` — the most important function in the library

### Combat Model

- [ ] Is combat a single skill check, multiple rounds, or an opposed roll?
- [ ] Do enemies have mechanical stats (HP, combat rating) or is it purely a DC?
- [ ] How does damage flow — fixed amounts, roll-based, tied to degrees of success?
- [ ] Is there an enemy model at all, or is combat just "a skill check with flavor"?

### Status Condition System

*Structural model defined in `pc_status.md`. Remaining gaps:*

- [x] Effect mechanics — **DECIDED**: deterministic daily loss (e.g., -2 health/day)
- [x] Severity tiers — **DECIDED**: no condition levels, severity tracked via stat depletion
- [x] Spirits restoration — **DECIDED**: settlement entertainment/relaxation, scripted event rewards
- [ ] Complete list of all status conditions (partial list: Injured, Hungry, Sick, Freezing, Exhausted)
- [ ] Condition stacking rules — do multiple conditions apply independently? (Injured + Sick = both drain health?)
- [ ] Condition lifecycle — persist until cured, or can they expire naturally?
- [ ] Spirits roll penalty formula — linear? stepped thresholds? applies to all rolls or subset?
- [ ] Zero spirits consequence — what happens at 0 spirits vs 0 health?
- [ ] Settlement entertainment details — cost? daily limit? instant or gradual restoration?
- [ ] Starting spirits value and expected range

### Death Boundary

- [ ] What mechanically defines "close to civilization" vs "overextended"?
  - Danger tier? Hex distance from settlement? Days of travel to safety?
- [ ] Edge cases — dying in a dungeon that's in the Frontier tier?
- [ ] What does "injury" (non-permadeath) look like mechanically?
  - HP reduction? Forced status conditions? Gold loss? All of the above?

### Skill Advancement Formula

- [ ] How many uses per +1 bonus? Flat count or diminishing returns?
- [ ] Is there a skill cap?
- [ ] Does check difficulty matter? (trivial checks shouldn't grind skills)
- [ ] What's the expected modifier range, start to endgame?

### Encounter Frequency

- [ ] Base encounter probability per travel day or hex
- [ ] Depletion formula — linear? proportional to remaining pool? step function?
- [ ] Does frequency vary by danger tier?

### Mechanic Action Vocabulary

- [ ] Canonical list of supported `[mechanic]` actions (the Rules engine's public API)
- [ ] Parameter shapes for each action (e.g. `skill_check <skill> <dc>`, `damage <amount>`)
- [ ] Are actions extensible or is this a closed set?
- [ ] This bridges the Encounter and Rules libraries — needs definition before either side is complete

### Plot Key Mechanics

- [ ] Are plot keys boolean tags on game state? Or richer (integer counters, enums)?
- [ ] Do they gate encounter availability? (encounter X only fires if key Y is set)
- [ ] Can they be consumed/unset, or only set?
- [ ] Do they affect encounter text, encounter selection, or both?

---

## Tuning Numbers (Not Blocking)

These can use placeholder constants. Explicitly called out as open questions in `decisions.md`.

- [ ] Which terrains count as "difficult" (beyond mountains and swamps)
- [ ] Foraging target numbers by biome and skill level
- [ ] Medical supply consumption rate (1 per treatment? more granular?)
- [ ] Starting HP and damage values
- [ ] Trade good base prices and regional multipliers
- [ ] Starting gold and typical transaction values
- [ ] Inventory slot count (starting and with gear bonuses)
- [ ] Travel speed (hexes per day)
- [ ] Camping gear risk probabilities per biome
- [ ] Equipment bonuses to combat skill checks
- [ ] Settlement service distribution (what determines common vs rare services)
- [ ] Healer scope (cure all conditions? full HP heal? both?)
