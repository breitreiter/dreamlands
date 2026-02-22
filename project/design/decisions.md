# Design Decisions
*Concrete specifications locked down from questionnaire sessions*

## Resource Management

### Consumption Granularity
**Decision**: Per-day tracking (not per-hex, not per-journey)
- Resources consumed once per day of travel
- Simple enough for planning, granular enough for strategy

### Food System
**Decision**: Abstract rations (1 ration = 1 day's food)
- **Base consumption**: 1 ration per day
- **Terrain penalties**: Flat +1 in difficult terrain (mountains, swamps)
- **Measurement**: Integer count, no fractions

### Water System
**Decision**: Separate resource tracked independently
- **Base consumption**: 1 water per day (same rate as rations)
- **Terrain penalties**: Flat +1 in difficult terrain (consistent with food)
- **Not bundled** - water scarcity is a distinct challenge from food scarcity

### Shortage Consequences
**Decision**: Forced foraging when resources run out
- **Mechanism**: Skill check (Foraging skill)
- **Success**: Find enough to continue (avoid starvation)
- **Failure**: Health damage, speed penalties, or forced retreat

### Medical Supplies
**Decision**: Tracked resource for disease prevention/treatment
- **Not for healing injuries** - injuries heal via town healer or time
- **Disease treatment**: Consume medical supplies to treat disease status conditions
- **Disease prevention**: Certain biomes/situations may require medical supplies to avoid infection

### Camping Gear
**Decision**: Tool/equipment (not consumable)
- Binary have/don't-have
- **Risk without it**: Status conditions when sleeping
  - Disease (infected from unsanitary rest)
  - Freezing (exposure in cold biomes)
- **Status conditions are on timers** - progressive worsening

## Inventory & Trade

### Trade Good Categories
**Decision**: 9 economic categories with flavor variations
- **Economic categories**: Textiles, Metals, Wood, Stone, Gems, Spices, Tools, Weapons, Books
- **Flavor varieties**: Decorative names for color (silk scarves vs wool blankets = both "Textiles")
- **Exception for Tools/Gear**: Specific varieties have mechanical differences (camping gear vs pickaxe)
- **Exception for Foods**: Some few varieties have medicinal properties (expensive, collectible, may have lore value)
- **Food explicitly excluded**: Not a trade good (see Auto-Travel system)

### Regional Pricing
**Decision**: Biome-appropriate goods have locally depressed prices
- Mountains want food (food expensive in mountains, cheap in farmlands)
- Not extreme arbitrage, but enough to match intuitive expectations
- Creates natural trade routes

### Inventory Capacity
**Decision**: Abstract slot system
- **No weight in pounds** - too much math
- **No mounts/vehicles** - don't want to track where you parked your llama
- **Gear can increase capacity** - backpacks, specialized containers
- Simple integer slots

### Local Storage
**Decision**: Paid tiered capacity system
- **Access**: Instant and free when in settlement (no time cost, no per-use fees)
- **Capacity**: Starts limited, pay to expand capacity
- **Spatial constraint**: Storage is per-settlement, forces planning about where to base operations

## Settlement & World Structure

### Settlement Services
**Decision**: Universal + specialized services
- **Universal (all settlements)**: Market, free water
- **Common (many settlements)**: Storage chests
- **Rare (some settlements)**: Healer (infinite medicine application to PC)
- **TBD availability**: Entertainment/relaxation (restores spirits - prevalence unclear)

### Danger Tiers
**Decision**: Auto-assigned by graph distance from starting city
- **Safe Zone**: Near starting city
- **Frontier**: Medium distance
- **Wilderness**: Far distance
- **Deep Wild**: Maximum distance
- Note: If not currently implemented in mapgen, this is a bug to fix

### Travel Speed
**Decision**: Fixed hexes/nodes per day
- **Not variable by terrain** - terrain affects encounters/resources, not speed
- Simplifies planning and mental math
- Speed only affected by status conditions (injury, exhaustion, etc.)

### Auto-Travel System
**Decision**: Fast travel along discovered routes (inspired by Trade Wars sector navigation)
- **Route Discovery**: Once you manually travel a route, it becomes available for auto-travel
- **Settlement-to-Settlement**: Can auto-travel between any two settlements you've visited
- **Auto-Purchase Supplies**: System automatically purchases rations + water at waypoint settlements
  - Deducts cost from player gold
  - Uses fair/standard pricing (no regional variance during auto-travel)
- **Failure Handling**: If insufficient gold for supplies, stops at last affordable settlement with clear message
- **Guaranteed Food/Water**: All settlements always have food and water available for purchase
  - Prevents auto-travel from breaking due to supply shortages
  - Keeps resource management meaningful (costs gold) without micromanagement
- **No Encounters**: Auto-travel skips encounter rolls (abstracted as "established safe route")
- **Time Passage**: Days pass normally, status condition timers tick
- **Cannot Auto-Travel To**: Dungeons, unexplored wilderness POIs, or unmapped areas
  - First visit to dungeon always requires manual travel
  - Keeps exploration challenging and resource management relevant

**Design Rationale**: Removes tedium from established trade routes while preserving challenge of exploration and resource management for new areas. Food removed from trade goods to prevent exploitation of guaranteed availability.

## Encounter System

### Encounter Pool
**Decision**: 200 bespoke hand-crafted encounters
- **One-and-done**: Each encounter can only trigger once
- **Throttling**: As biome encounters exhaust, they become rarer
- **Design intent**: Late-game established routes have fewer interruptions (encounters become chore)

### Encounter Frequency
**Decision**: Random chance during travel
- Not fixed per-day or per-hex
- Frequency decreases as encounter pool depletes
- Eventually quite rare for frequently traveled routes

### Plot Keys
**Decision**: Generate quest hooks and multi-encounter arcs
- 5-8 plot keys per locale
- Used to chain related encounters
- Create narrative continuity without pre-scripted storylines

### Multi-Stage Encounters
**Decision**: Context-dependent
- **Overworld**: Never multi-stage (atomic encounters only)
- **Dungeons**: Always multi-stage (room-to-room progression)

### Encounter Persistence
**Decision**: One-shot, never repeats
- Once resolved, removed from encounter pool
- Creates evolving world (routes become "tamed")
- Total pool of 200 ensures variety before exhaustion

## Character System

### Skill System
**Decision**: 8-12 specific skills
- **Confirmed skills**:
  1. Foraging (finding food in wild)
  2. Medicine (treating conditions/healing)
  3. Combat (fighting encounters)
  4. Negotiation (social encounters/trading)
  5. Survival (general wilderness competence)
  6. Tracking (following trails, navigation)
  7. Cunning (trickery, awareness, staying one step ahead)
  9. Lore (identifying things, knowledge checks)
  10. Trading (better prices at markets)

**Excluded**: Animal Handling (no mounts), Lockpicking (dungeon-specific, can fold into other skills)

### Skill Checks
**Decision**: Dice roll vs difficulty (d20 style)
- Roll + skill modifier vs target number
- Degrees of success possible (not just pass/fail)

### Skill Advancement
**Decision**: Use-based improvement
- Use foraging → foraging skill improves
- Organic progression through gameplay
- No explicit leveling or training payments

### Initial Skills
**Decision**: Character creation encounter (zeroth encounter)
- Player describes their character narratively
- System assigns starting skill bonuses based on description
- Not rigid class selection, but guided point distribution

### Ablative Stats
**Decision**: Two stats for physical and mental wellbeing with different roles
- **Health**: Integer pool (traditional HP)
  - Abstract representation of physical wellbeing and non-deadness
  - Purely ablative (tracks survivability)
  - Reduced by combat, status conditions, starvation
  - **Healing**: Medical supplies required (or town healer with infinite medicine)
- **Spirits**: Integer pool with active mechanical impact
  - Abstract representation of mental wellbeing, fortitude, and morale
  - **Mechanical impact**: As spirits decrease, increasing penalties to skill check rolls
  - Reduced by: Status conditions, moral failures, combat defeats, witnessing cosmic horrors
  - **Restoration**: Settlement entertainment/relaxation, helping others, clever solutions, moral victories
  - Creates death spiral: low spirits → worse rolls → more failures → lower spirits

### Status Conditions
**Decision**: Condition-based system with structured properties
- **Primary mechanism**: Events apply conditions, which cause ongoing stat changes
- **Condition properties**: Name, trigger, preventative, cure, effect
- **Effect model**: Deterministic daily loss of health, spirits, or both (e.g., -2 health/day)
- **No severity levels**: Conditions don't escalate. Severity tracked indirectly via stat depletion.
- **Known conditions**: Injured, Hungry, Sick, Freezing, Exhausted (+ more)
- **Treatment**: Requires medical supplies or town healer
- **Stacking rules**: Multiple different conditions stack freely (Injured + Sick = both drain health)
- **Lifecycle**: Conditions persist until cured (no natural expiration)
- **Open questions**: See `conditions.md` for complete condition inventory and spirits restoration details

## Save System

### Save Format & Storage
**Decision**: JSON in Azure Table Storage or Cosmos DB
- **Why**: Fast key-value lookup (resume code = partition key)
- **Structure**: Single JSON blob per resume code
- **Performance**: Sub-10ms reads (Table Storage) or <5ms (Cosmos DB)
- **Cost**: Pennies per month for hundreds of saves

### Save Semantics
**Decision**: One save per resume code
- **Resume codes**: 5-letter alphabetic codes
- **Cross-device**: Server-stored for device portability
- **Unlimited codes**: Players can generate as many as they want (true single-player)

### Autosave Timing
**Decision**: Right before closing HTTP socket
- After each player action/turn that modifies state
- Ensures state persists without player thinking about it
- Azure Function friendly (save before response)

### Version Compatibility
**Decision**: Best effort loading
- Try to load save from any version
- Fail gracefully if incompatible (version too old)
- No automatic migration system
- Version field in JSON for compatibility checks

## Magic System

### Player Access
**Decision**: No player magic
- NPCs/enemies may use magic (as antagonists)
- Player never learns or casts spells
- No magical items for player use

### Magic Theme
**Decision**: Cosmic horror - magic is dangerous and corrupting
- Magic is a cautionary tale
- Magic users pay terrible price
- Encounters with magic are warnings, not opportunities
- Think medieval European witch fears: everyone worried, few actually see it

### Magic Prevalence
**Decision**: Uncommon (like medieval witch fears)
- Most people never directly encounter magic
- Rumors and paranoia common
- When magic appears, it's significant and dangerous

### Magic Impact
**Decision**: Minimal mechanical impact
- **Travel**: No effect (no teleportation, no speed buffs)
- **Economy**: No effect (no magical trade goods or services in markets)
- **Healing**: Town healers are non-magical (functionally infinite mundane medicine)

## Dungeon System

### Dungeon Generation
**Decision**: Fully hand-crafted
- All 21 dungeons designed by hand
- Not procedural, not templates
- Quality over quantity

### Dungeon Count
**Decision**: Exactly 21 dungeons
- Fixed number, hand-placed during world generation
- Consistent across all playthroughs (same seed = same dungeons)

### Dungeon Placement
**Decision**: POI scatter algorithm
- Distribute evenly across map
- Avoid clustering
- Respect biome/terrain constraints
- Distance-appropriate for danger tiers

### Dungeon Rewards
**Decision**: All three reward types
- **Loot**: Gold, trade goods, equipment
- **Lore**: World-building, cosmic horror reveals
- **Plot**: Quest progression, narrative advancement

### Dungeon Permanence
**Decision**: One-time clear
- Once cleared, marked complete
- Never respawns
- Contributes to win condition (clear all 21)

## Game Structure

### Win Condition
**Decision**: Clear all 21 dungeons
- Explicit goal, measurable progress
- No sandbox/emergent end state
- Completion-focused design

### Game Length Target
**Decision**: 5-10 hours for completion
- Roughly 30 minutes per dungeon including prep/travel
- Short, focused campaign
- Designed for single playthrough

### Difficulty Settings
**Decision**: None - fixed difficulty
- Single balanced experience
- No easy/normal/hard modes
- Consistent design intent for all players

### Death Consequences
**Decision**: Hybrid injury/permadeath system
- **In civilization or close to it**: Injury, recovery required, expedition cut short (sad but not fatal)
- **Overextended (Deep Wild, low supplies)**: Permadeath, full restart
- Creates tension gradient (risk increases with distance from safety)

### Replayability
**Decision**: No new game plus
- Designed for one playthrough
- Different world seeds for variety (new maps)
- No bonuses, unlocks, or increased difficulty modes

## Implementation Priorities

### Phase 1: Core Loop
1. Resource consumption (rations, water) - per day tracking
2. Travel mechanics - fixed speed, danger tiers
3. Basic inventory - slot system
4. Settlement services - market, storage, healer

### Phase 2: Character System
1. 10 core skills - implementation
2. Skill checks - d20 resolution
3. HP tracking - damage and healing
4. Status conditions - Disease, Freezing, etc.
5. Use-based advancement

### Phase 3: Encounters
1. Encounter pool - 200 bespoke encounters
2. One-shot system - removal after resolution
3. Plot keys - quest hook generation
4. Combat resolution - abstract/narrative

### Phase 4: Dungeons
1. Hand-craft 21 dungeons
2. Multi-stage encounter system
3. Loot/lore/plot rewards
4. Dungeon completion tracking

### Phase 5: Persistence
1. JSON serialization
2. Azure Table Storage integration
3. Resume code generation/validation
4. Autosave on response

### Phase 6: Trade System
1. 8-10 economic categories
2. Regional pricing modifiers
3. Market UI
4. Trade goods flavor text

## Open Questions (Still TBD)

### Resource Details
- Exact terrain penalty values (+1 confirmed, but which terrains?)
- Foraging success rates by biome and skill level
- Medical supply consumption rate (1 per treatment? More granular?)

### Status Conditions
- Complete list of all status conditions (partial list established: Injured, Hungry, Sick, Freezing, Exhausted)
  - Note: See `conditions.md` for detailed condition inventory including Tier 2/3 variants

### Spirits Mechanics
- Starting spirits value and expected range
- Roll penalty formula (linear vs stepped thresholds? which rolls affected?)
- Zero spirits behavior: Functions like zero health (reaching zero = death/defeat), but creates death spiral through roll penalties before reaching it
- Settlement entertainment: cost, daily limit, restoration rate 
- Can spirits exceed starting value (temporary morale boost)?

### Skill Details
- Starting skill point distribution from character creation
- Improvement rate (uses per skill increase)
- Exact target numbers for skill checks

### Trade Economics
- Regional price multipliers (how much cheaper is biome-appropriate goods?)
- Starting gold and typical transaction values

### Combat Balance
- Equipment bonuses to combat skill checks
- HP values (starting, enemy levels)
- Injury severity from combat failure

