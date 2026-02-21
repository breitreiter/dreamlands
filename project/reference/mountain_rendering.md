# Sprite-Based Mountain Rendering for Fantasy Maps

Research notes for MountainPass implementation. Covers techniques used by Nortantis, Azgaar, Tolkien-style cartography guides, and procedural generation literature.

## The Core Problem

Treating all mountain tiles identically — same density, same sprite mix — produces random-looking scatter instead of cohesive ranges. The fix is computing a **height field** within the mountain region so sprites graduate from foothills at the edges to tall peaks along the interior ridgeline.

## 1. Interior Distance Field (BFS)

The standard approach across multiple tools and papers. ~20 lines of code.

1. Identify all mountain tiles with at least one non-mountain 4-neighbor. These are boundary tiles (distance = 0).
2. BFS inward. Each ring increments distance by 1.
3. Maximum-distance tiles approximate the **medial axis / ridgeline** of the region.

```
Queue frontier
int[,] dist  // -1 = unvisited

// Seed boundary tiles
for each mountain tile (x,y):
    if any 4-neighbor is not Mountain:
        dist[x,y] = 0; frontier.Enqueue((x,y))

// BFS inward
while frontier not empty:
    (x,y) = frontier.Dequeue()
    for each 4-neighbor (nx,ny):
        if Mountain && dist == -1:
            dist[nx,ny] = dist[x,y] + 1
            frontier.Enqueue((nx,ny))
```

### Breaking up concentricity with noise

Pure BFS creates regular concentric rings. Perturb with low-frequency noise:

```
effectiveHeight = bfsDistance + noise(x * 0.15, y * 0.15) * 1.5
normalizedHeight = clamp(effectiveHeight / maxDist, 0, 1)
```

Brash and Plucky's variant: randomize distance increments during BFS itself using `delta = spacing * (1 + jaggedness * (rand() - rand()))`. The `rand() - rand()` produces a triangular distribution centered on zero, giving organic non-concentric ridgelines.

Another option: Fisher-Yates shuffle the neighbor visitation order during BFS so ridges branch out organically in all directions instead of showing directional bias from fixed iteration order.

## 2. Height-to-Sprite Mapping

Instead of mixing all sprites everywhere, height selects the sprite pool:

| Normalized Height | Sprites | Role |
|---|---|---|
| 0.0–0.2 | Small hills | Edge foothills |
| 0.2–0.4 | Big hills | Inner foothills |
| 0.4–0.6 | Small mountains | Mid-range |
| 0.6–0.8 | Big mountains | Interior |
| 0.8–1.0 | Pinnacles + Big mountains | Ridgeline peaks |

Scale should also increase toward center — bigger sprites at the spine, smaller at edges. Nortantis scales mountain icons based on polygon width relative to average width between neighbors.

## 3. Density Modulation

Edge tiles should be sparser, interior tiles denser:

```
skipChance = baseSkip + (1 - normalizedHeight) * 0.3
```

This creates natural thinning at the edges of the range while keeping the interior packed.

## 4. Unified Y-Sort and Bottom-Anchoring

All sources agree: collect ALL placements (foothills, mountains, pinnacles, POI) into one list, sort by Y, draw back-to-front. This is the painter's algorithm.

All sprites should be **bottom-anchored** (grid point = base of sprite, sprite extends upward). This creates the layered depth effect where mountains in front occlude the bases of mountains behind them. Center-anchored sprites fight this illusion.

Nortantis's anchor formula: base Y-offset from polygon center bottom, `Point(c.loc.x, bottom.loc.y - (scaledHeight / 2) - offset)`.

## 5. Overlap Strategy

For the dense Tolkien look, mountains should overlap 30–50% vertically. Cell size relative to sprite height controls this — smaller cells = more overlap.

Key principles from Map Effects (Tolkien-style tutorials):
- **Tallest peaks along the ridgeline center**, gradually smaller moving outward
- **Narrow highlights, wide shadows** — light from one side (typically right/northwest)
- **Leave a halo around peaks** where they overlap shadowed mountains behind — prevents visual muddiness
- **Three-size hierarchy minimum** — large iconic peaks, mid-size secondary mountains, small foothills

## 6. Our Asset Inventory (49 mountain decals)

| Category | Count | Directory |
|---|---|---|
| Big individual hills | 10 | `mountains/Big individual hills/` |
| Small individual hills | 10 | `mountains/Small individual hills/` |
| Big individual mountains | 12 | `mountains/Big individual mountains/` |
| Small individual mountains | 10 | `mountains/Small individual mountains/` |
| Individual pinnacles | 7 | `mountains/Individual pinnacles/` |

Plus 5 dungeon POI decals in `poi/dungeons/mountains/` (~250-470px, colored ruins/fortresses).

## 7. Implementation Plan for MountainPass v2

1. **`ComputeInteriorDistance(map)`** — BFS over mountain tiles, returns `int[,]` distance field
2. **Noise perturbation** — seeded noise to break up concentric rings, normalize to [0,1]
3. **Single unified scatter loop** — one grid pass that picks sprite category, scale, and skip chance based on the tile's normalized height
4. **All placements collected into one list** (foothills + mountains + pinnacles + dungeons), Y-sorted, bottom-anchored, drawn back-to-front
5. **Dungeons** — same placement list, very sparse, merged into Y-sort

## Sources

- [Red Blob Games: Elevation Control via Distance Fields](https://www.redblobgames.com/x/1728-elevation-control/) — BFS distance fields, harmonic mean formula for combining distance constraints
- [Red Blob Games: Polygonal Map Generation](http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/) — distance-from-coast elevation
- [Brash and Plucky: Procedural Island Generation Part III](https://brashandplucky.com/2025/09/17/procedural-island-generation-iii.html) — BFS with randomized distance increments, Fisher-Yates neighbor shuffling
- [Map Effects: Tolkien-Style Mountains](https://www.mapeffects.co/tutorials/tolkien-mountains) — halo technique, ridgeline-centered tallest peaks, three-size hierarchy
- [Map Effects: Varying Mountain Height](https://www.mapeffects.co/tutorials/varying-mountain-height) — tallest along ridgeline center, gradual tapering outward
- [Nortantis IconDrawer.java](https://github.com/jeheydorn/nortantis/blob/master/src/nortantis/IconDrawer.java) — sprite-based mountain rendering with elevation thresholds, Y-sort, bottom-anchor, range clustering
- [Azgaar's Fantasy Map Generator](https://github.com/Azgaar/Fantasy-Map-Generator) — blob-based heightmap templates, relief icon density system
- [Sigil of Kings: Procedural Painterly Mountains](https://byte-arcane.github.io/sigil-of-kings-website/2020/06/11/procedural-generation-of-painterly-mountains/) — distance-field mountain shading
- [ESRI: Fantasy Maps Pt 1 Mountainification](https://www.esri.com/arcgis-blog/products/analytics/analytics/fantasy-maps-pt-1-mountainification/) — multi-layer elevation-ranged symbols
- [NULLPOINTER: Generating a Fantasy Map](https://www.nullpointer.co.uk/generating-a-fantasy-map.html) — diffusion-based altitude spreading, 5-sprite mountain selection by height
