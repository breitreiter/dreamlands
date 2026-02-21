# Game Mechanics Specification
*Living document tracking core systems and design decisions*

## Core Design Philosophy

### Type 2 Fun
The game embraces "type 2 fun" where mechanical failures create interesting narrative moments rather than pure punishment. Poor planning should force strategic retreats and tough decisions, not just frustration. Success comes from smart preparation, not grinding or luck.

### Preparation Over Convenience
Systems favor meaningful player choice and resource management over automated convenience. Players should engage with travel logistics, inventory planning, and risk assessment rather than having these abstracted away.

### Discrete Over Continuous
Where possible, use discrete categorization systems instead of continuous scaling. This reduces complexity while maintaining strategic depth (e.g., 4 danger tiers instead of distance-based continuous scaling).

## Distance & Danger Tiers

### Four-Tier System
- **Safe Zone**: Near major settlements, well-patrolled
- **Frontier**: Settled areas, some danger
- **Wilderness**: Remote regions, significant danger
- **Deep Wild**: Unexplored, extreme danger

### Design Rationale
Discrete tiers reduce content complexity from hundreds of combinations to manageable sets while providing clear strategic implications for travel planning.

### Implementation Notes
- Used for encounter generation
- Affects travel speed, resource consumption
- Determines available services/infrastructure
- Influences trade good availability and pricing

## Trade Goods System

### Two-Tier Architecture

#### Economic Layer (Broad Taxonomy)
Purpose: Drive pricing and arbitrage mechanics
- ~10-15 broad categories
- Clear price differentials between settlement types
- Stable, predictable for strategic planning

Examples: Textiles, Metals, Foodstuffs, Luxuries, etc.

#### Flavor Layer (Specific Items)
Purpose: Narrative richness and world-building
- Hundreds of specific items
- Map to economic categories for pricing
- Provide regional identity and story hooks

Examples: "Silk from the Eastern Provinces", "Dwarven Steel Ingots", "Salted Fish from Northreach"

### Design Rationale
Separates strategic clarity (economic) from narrative richness (flavor), solving the "too generic vs too complex" problem in traditional trading games.

### Open Questions
- Exact number of economic categories
- How to handle goods that shift categories by region (luxury in one place, common elsewhere)
- Weight/volume system for inventory management

## Travel & Resource Management

### Core Concept
Travel preparation inspired by real hiking - players pack supplies, plan routes, assess risks. Running out of food/water forces strategic decisions.

### Resource Types (TBD)
- Food/rations
- Water
- Medical supplies
- Camping gear
- Travel papers/documents?
- Animal feed (if mounts implemented)

### Consumption Mechanics (TBD)
- Base rates vs terrain modifiers
- Weather effects
- Emergency rationing options
- Consequences of shortages (speed reduction, health impacts, forced retreat)

### Open Questions
- Exact resource consumption rates
- How granular to make the simulation
- Balance between realism and gameplay
- Camp/rest mechanics
- Emergency survival options

## Inventory & Storage

### Local Storage System
Each settlement has a local chest/warehouse for player storage. No magical shared inventory across the world.

### Design Rationale
- Creates spatial reasoning challenges
- Makes settlement choice meaningful
- Forces planning around where to base operations
- Builds settlement identity (your stuff is "in Northreach")

### Implementation Needs
- Storage capacity limits per settlement
- UI for managing transfers between inventory and storage
- Clear indication of what's stored where
- Possibly paid storage for larger quantities

## Encounter System

### Distance-Based Tiers
Encounters categorized by danger tier (not continuous distance), with each tier having appropriate challenge levels and reward structures.

### Template Approach
Universal encounter templates with flavor variations provide efficiency over unique content for every biome-distance combination.

### Structure Requirements
Each encounter should include:
- Scene-setting (where, what you see/hear)
- A dilemma or choice point
- Multiple resolution paths with different costs/benefits
- Mechanical outcomes feeding into narrative generation

### Plot Keys
5-8 evocative, open-ended plot keys per locale that provide narrative vocabulary without prescribing specific outcomes.

### Open Questions
- Exact number of encounter templates needed
- How to handle multi-stage encounters
- Consequence persistence across encounters
- Balance between random and scripted encounters

## Dungeon/POI Placement

### Procedural Generation
Seed-and-expand algorithms for natural placement of features like dungeons, ruins, and points of interest.

### Constraints
- Appropriate for biome
- Distance from settlements
- Not clustered too densely
- Accessible (not in impassable terrain)

### Open Questions
- Exact placement algorithms
- Density parameters
- How to make dungeons feel hand-crafted despite generation
- Interior generation vs hand-crafted interiors

## Map & World Structure

### Biome System
Organic, natural-looking biomes using seed-and-expand rather than rigid grids.

### Settlement Spacing
Based on realistic hiking ranges - settlements at distances that make sense for travelers on foot or with pack animals.

### Open Questions
- Exact biome transition rules
- Settlement size tiers and their characteristics
- Road network generation
- Water navigation mechanics

## Magic & World-Building

### Approach
Hand-crafted foundational elements (factions, magic systems, history) with AI-generated volume content for encounters and descriptions.

### Open Questions
- How magic affects travel and trade
- Whether players can use magic
- Economic impact of magical services
- Integration with encounter system

## Session Persistence

### Resume Code System
5-letter alphabetic codes for cross-device gameplay (security analysis completed - collision probabilities acceptable for single-player game).

### Implementation Needs
- Save state serialization
- Code generation and validation
- State restoration
- Handling version compatibility

## Next Steps & Priorities

1. **Finalize resource consumption mechanics** - Exact rates, consequences, emergency options
2. **Complete trade goods taxonomy** - Lock down economic categories and initial flavor items
3. **Encounter template library** - Build out the core templates
4. **Plot key generation** - Develop the plot key system for locales
5. **Storage UI implementation** - Management screens for local chests
6. **Travel simulation** - Core loop for movement, consumption, encounters

## Notes for Implementation

- Systems should be data-driven where possible (JSON configs, etc.)
- Clear separation between mechanics (this library) and presentation (UI)
- Consider testability - mechanics should be verifiable independent of UI
- Event/message system for UI to respond to mechanical outcomes
- Save game format should be version-aware for future updates