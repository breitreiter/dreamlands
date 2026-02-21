# MapGen Design Document

## Overview

MapGen generates world maps for an adventure game. Maps are navigable graphs representing a coherent world surface where exploration of the topology is part of the gameplay.

## World Size

Map dimensions are configurable per-world:

- **Development target**: 60×20 (fits in terminal for ANSI visualization)
- **Production target**: ~100×100 (or larger)

The underlying grid provides spatial coordinates for nodes. The graph is derived from this grid but is sparser than a full lattice.

## Edge Directions (Prototype)

For the initial prototype, edges are restricted to **four cardinal directions**: North, South, East, West.

This enables ASCII visualization where each cell is rendered as:
- **Background color**: Terrain type
- **Box-drawing character**: Graph connectivity

### Connection → Character Mapping

Each node has 0-4 connections. The 16 states map to box-drawing:

```
Connections   Char    Connections   Char
──────────────────    ──────────────────
(none)         ·      N+S+E          ├
N              ╵      N+S+W          ┤
S              ╷      N+E+W          ┴
E              ╶      S+E+W          ┬
W              ╴      N+S+E+W        ┼
N+S            │
E+W            ─
N+E            └
N+W            ┘
S+E            ┌
S+W            ┐
```

### Example Output

```
┌─┬─·
│ │
├─┼─┐
    │
·───┘
```

(With ANSI background colors for terrain—green for forest, blue for water, etc.)

## Core Concepts

### Map as Graph

The world is modeled as a **graph**, not a grid. Nodes represent discrete locations; edges represent traversable connections between them.

- **Nodes**: Each node is a ~4x4 mile region with a terrain type and properties
- **Edges**: Connections between adjacent nodes (not all adjacent nodes are necessarily connected)
- **Loose connectivity**: The graph is intentionally sparse—not every possible connection exists

### Spatial Coherence

Although the map is a graph, it must represent a believable world surface:

- **No teleportation**: Following edges should feel like traversing physical space. An edge from the northern mountains shouldn't lead directly to the southern coast.
- **Geographic clustering**: Similar terrain types should cluster together naturally
- **Transition zones**: Terrain should transition sensibly (forest → hills → mountains, not desert → tundra)

### Terrain Abstraction

At 4x4 miles per node, terrain is abstract rather than detailed:

- A "village" node means there's a village somewhere in that region, not a street-level map
- A "riverland" node means the area is defined by rivers/wetlands, not the exact river path
- A "mountain" node is mountainous terrain, not a specific peak

## Terrain Types (Initial Set)

| Terrain | Description | Connectivity | Notes |
|---------|-------------|--------------|-------|
| Ocean | Open water | Low | Impassable without a boat |
| Coast | Beaches, cliffs, harbors | Medium | Always adjacent to ocean |
| Plains | Open grassland, farmland | High | Easy travel, many connections |
| Forest | Woodland, light woods | Medium | Moderate travel difficulty |
| Hills | Arid rolling terrain, desert scrubland | Medium | Sparse palms, arabesque settlements |
| Mountains | High elevation, rugged | Low | Few passages, natural barriers |
| Swamp | Wetlands, marshes | Low | Difficult to traverse |
| Riverland | River valleys, deltas, floodplains | Medium | Wide rivers/wetlands (distinct from river-as-barrier) |

## Terrain Adjacency Rules

Valid transitions (terrain A can be adjacent to terrain B):

```
Ocean ↔ Coast
Coast ↔ Plains, Forest, Hills, Swamp
Plains ↔ Forest, Hills, Riverland
Forest ↔ Hills, Mountains, Swamp, Riverland
Hills ↔ Mountains, Plains, Forest
Mountains ↔ Hills, Forest (high altitude only)
Swamp ↔ Coast, Forest, Riverland
Riverland ↔ Plains, Forest, Swamp, Hills
```

Invalid transitions (should never be directly adjacent):
- Ocean ↔ Mountains, Forest (need coast buffer)
- Mountains ↔ Ocean, Swamp (elevation mismatch)

## Rivers as Barriers

Rivers are a special case: they're not just terrain, they're **directional barriers** within a node.

### The Problem

A node might contain a river, but we don't want to subdivide nodes to track which bank you're on. Yet rivers should block movement—you can't just walk across.

### Proposed Solution: River Edge Position

A river runs along one edge of a node (N/S/E/W). This determines connectivity:

```
Example: River on WEST edge

    [Node A]
        |
   ~~~~ | ~~~~  ← river runs here
        |
    [Node B]  ← you are here, entered from east

You CAN go:
  - North/South (following the river)
  - East (away from river, back where you came)

You CANNOT go:
  - West (blocked by river)

UNLESS: There's a bridge (boolean flag on the node)
```

### River Properties per Node

```
RiverEdge: None | North | South | East | West
HasBridge: bool (allows crossing to the river side)
```

### Implications

- Rivers create natural chokepoints (must find bridge or go around)
- River paths are easy to follow (high connectivity along the river)
- Fords/bridges become strategic locations
- A river is really a sequence of nodes with compatible RiverEdge values

### Open Questions

- Can a node have river on multiple edges? (confluence, bends)
- How do we represent a river that runs *through* the center vs along an edge?
- Should river direction (upstream/downstream) matter for gameplay?

## Connectivity Rules

How many edges a node typically has, based on terrain:

| Terrain | Typical Degree | Rationale |
|---------|----------------|-----------|
| Plains | 4-6 | Open terrain, easy access |
| Coast | 3-4 | Land on one side, water on other |
| Forest | 3-4 | Paths exist but not everywhere |
| Hills | 2-4 | Terrain limits routes |
| Mountains | 1-3 | Few passes, natural chokepoints |
| Swamp | 1-3 | Difficult to traverse |
| Riverland | 3-4 | Rivers provide/block routes |
| Ocean | 2-4 | Sailing routes (if accessible) |

## Generation Approach (TBD)

Two broad philosophies:

### Top-Down: Heightmap First

Generate elevation via noise (Perlin, Simplex, diamond-square), then derive terrain:
- Low elevation + water threshold → Ocean/Coast
- Low elevation + moisture → Swamp/Riverland
- Medium elevation → Plains/Forest (based on moisture)
- High elevation → Hills → Mountains

Pros: Natural-looking terrain gradients, elevation "comes for free"
Cons: Can feel samey, less control over specific features

### Bottom-Up: Feature Placement

Place features deliberately, then fill in:
- **Random walk** for rivers (carve from mountains to coast)
- **Splats** for terrain blobs (drop a forest, let it spread)
- **Contagion** for organic growth (terrain spreads to neighbors with probability)

Pros: More control, can ensure interesting features exist
Cons: Harder to guarantee coherence, may need constraint repair

### Hybrid?

Given the 4×4 mile tile scale, a hybrid might work:
1. Rough elevation zones (coastal, lowland, highland, mountain)
2. Random-walk rivers from high to low
3. Terrain splats with adjacency constraints
4. Graph connectivity derived from terrain + barriers

The generation must ensure:
- [ ] The graph is connected (can reach any node from any other)
- [ ] Spatial coherence is maintained (nearby nodes are nearby in the graph)
- [ ] Terrain adjacency rules are respected
- [ ] Connectivity feels organic, not uniform

## Open Questions

- Should there be named regions/biomes at a higher level?
- How do we handle water travel? (boats, ships)
- Do we need elevation as a separate property, or derive it from terrain?
- How do we seed points of interest? (towns, dungeons, landmarks)
- River representation: edge-based blocking vs. something richer?
- Which generation approach to try first?

## Out of Scope (For Now)

- Persistence / serialization
- Map editing / revision
- Detailed sub-node features
- Weather / seasons
- Political boundaries
