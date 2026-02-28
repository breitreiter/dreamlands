# River Decal System

Procedural rivers looked bad — loopy pathing, per-tile rendering artifacts. Replaced with
hand-drawn decal tiles that get chained together along computed paths.

## Tile Design

Each tile is **829x829px** (covers ~6 map tiles at 128px). Transparent PNG with a river
segment drawn through it.

829 is prime and 829 mod 128 = 61, so decal edges always land ~48% through a map tile
and the offset pattern never repeats. This prevents visible grid alignment.

Every tile has two connection points:
- **Entry** (top edge): high or low
- **Exit** (bottom edge): high or low

This gives **4 tile types**:

| Type | Entry | Exit  | Shape           |
|------|-------|-------|-----------------|
| HH   | High  | High  | Straight, left  |
| HL   | High  | Low   | Diagonal sweep  |
| LH   | Low   | High  | Diagonal sweep  |
| LL   | Low   | Low   | Straight, right |

Chaining rule: **exit of tile N must match entry of tile N+1** (high→high, low→low).

## Variants

Draw 2-3 art variants per type to avoid visible repetition on long rivers. That's
8-12 total assets for visually unique rivers across the whole map.

## Rendering

Rivers flow from lakes toward the nearest map edge. The path is a sequence of tiles
oriented along the flow direction.

1. Compute a coarse path from lake to edge (straight-line with slight wander)
2. Pick an initial entry point (random high/low)
3. For each segment, pick a tile whose entry matches the previous exit
4. Composite onto the canvas as a rendering pass (between terrain and trees)

Tiles are axis-aligned to the flow direction — vertical rivers use tiles as-is,
horizontal rivers rotate 90 degrees.

## Connection Points

"High" and "low" are relative to the tile's local coordinate system:
- For vertical flow: high = left side, low = right side
- Entry/exit points should land at a consistent Y offset from the tile edge (~50px in)
  so joints overlap slightly and hide seams

## Asset Location

River tile PNGs go in `assets/map/rivers/`. Naming convention:

```
river_hh_1.png    # high entry, high exit, variant 1
river_hl_1.png    # high entry, low exit, variant 1
river_lh_2.png    # low entry, high exit, variant 2
river_ll_1.png    # low entry, low exit, variant 1
```

## Future

- Lake-to-river transitions (source tiles with no entry point, just an exit)
- River mouth tiles for map-edge termination
- Width variation (narrow upstream, wider downstream) via separate tile sets
- Confluence tiles where two rivers meet
