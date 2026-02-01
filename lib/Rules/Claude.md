# Claude Context - Dreamlands Rules Engine

## Project Overview
This library implements the core game mechanics for Dreamlands, a text-based exploration and trading game. This project serves dual purpose as both design documentation and implementation code - the design philosophy and mechanics are documented alongside their C# implementations.

## Architecture

### Project Structure
- `/lib/Rules/` - This project (game mechanics engine)
- `/lib/Map/` - Map parsing and data structures
- `/lib/Encounter/` - Encounter parsing and data structures
- `/text/encounters/` - Encounter content (adventure fragments)
- `/assets/` - Graphical assets (untracked)
- `/notes/` - Design documentation
  - `seed.md` - Core mechanics specification and design philosophy
  - `pointers.md` - Supporting libraries and content locations

### Design Philosophy

**Type 2 Fun**: Mechanical failures create interesting narrative moments. Poor planning leads to strategic retreats and tough decisions, not frustration.

**Preparation Over Convenience**: Meaningful player choice and resource management over automated systems. Players engage with travel logistics, inventory planning, and risk assessment.

**Discrete Over Continuous**: Use discrete categorization (e.g., 4 danger tiers) instead of continuous scaling to reduce complexity while maintaining strategic depth.

## Core Systems

### 1. Distance & Danger Tiers
Four discrete tiers instead of distance-based continuous scaling:
- **Safe Zone**: Near major settlements, well-patrolled
- **Frontier**: Settled areas, some danger
- **Wilderness**: Remote regions, significant danger
- **Deep Wild**: Unexplored, extreme danger

Used for: encounter generation, travel speed, resource consumption, services/infrastructure, trade availability.

### 2. Trade Goods System
Two-tier architecture separating strategic clarity from narrative richness:

**Economic Layer** (~10-15 categories):
- Drives pricing and arbitrage mechanics
- Clear price differentials between settlement types
- Stable and predictable for strategic planning
- Examples: Textiles, Metals, Foodstuffs, Luxuries

**Flavor Layer** (hundreds of items):
- Narrative richness and world-building
- Maps to economic categories for pricing
- Regional identity and story hooks
- Examples: "Silk from the Eastern Provinces", "Dwarven Steel Ingots"

### 3. Travel & Resource Management
Inspired by real hiking preparation:
- Pack supplies, plan routes, assess risks
- Resource types: Food/rations, water, medical supplies, camping gear, travel papers, animal feed
- Running out forces strategic decisions
- Consumption affected by terrain, weather, emergency rationing
- Consequences: speed reduction, health impacts, forced retreat

### 4. Inventory & Storage
**Local Storage**: Each settlement has separate chest/warehouse storage (no magical shared inventory)
- Creates spatial reasoning challenges
- Makes settlement choice meaningful
- Forces planning around operational bases
- Builds settlement identity

### 5. Encounter System
- **Distance-based tiers**: By danger tier, not continuous distance
- **Template approach**: Universal templates with flavor variations
- **Structure**: Scene-setting → dilemma → multiple resolution paths → mechanical outcomes
- **Plot Keys**: 5-8 evocative, open-ended plot keys per locale for narrative vocabulary

### 6. Session Persistence
**Resume Code System**: 5-letter alphabetic codes for cross-device gameplay
- Single-player security model (collision probabilities acceptable)
- State serialization/restoration
- Version compatibility handling

## Implementation Guidelines

### Code Organization
- **Data-driven**: Use JSON configs where possible
- **Separation of concerns**: Mechanics (this library) vs presentation (UI)
- **Testability**: Mechanics verifiable independent of UI
- **Event/message system**: For UI to respond to mechanical outcomes
- **Version-aware saves**: Future-proof save game format

### Development Priorities
1. Finalize resource consumption mechanics
2. Complete trade goods taxonomy
3. Build encounter template library
4. Develop plot key system
5. Implement storage UI
6. Create travel simulation core loop

## Open Questions & TODOs

### Trade Goods
- Exact number of economic categories
- Handling goods that shift categories by region
- Weight/volume system for inventory management

### Travel & Resources
- Exact consumption rates and balancing
- Granularity of simulation
- Camp/rest mechanics
- Emergency survival options

### Encounters
- Number of templates needed
- Multi-stage encounter handling
- Consequence persistence
- Balance between random and scripted

### World Structure
- Biome transition rules
- Settlement size tiers and characteristics
- Road network generation
- Water navigation mechanics

### Magic System
- How magic affects travel and trade
- Player magic usage
- Economic impact of magical services
- Integration with encounter system

## Design Patterns

### Discrete Categorization
Prefer discrete tiers over continuous values to reduce content complexity while maintaining strategic depth.

### Template + Variation
Universal templates with specific flavor variations provide efficiency over unique content for every combination.

### Spatial Constraints
Systems that create spatial reasoning challenges (like local storage) make player decisions more meaningful.

### Narrative From Mechanics
Mechanical outcomes feed into narrative generation - failures and successes both create interesting stories.

## Context for AI Assistance

When working on this codebase:
- Respect the dual documentation/implementation nature
- Design decisions in markdown, implementation details in code
- Keep type 2 fun principle in mind - failures should be interesting, not frustrating
- Favor discrete systems over continuous complexity
- Maintain separation between mechanics and presentation
- Consider testability and data-driven configuration
- Documentation drift is a risk - keep docs and code in sync
