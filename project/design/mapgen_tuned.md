## Settlement Spacing Rubric

**Start: ~(40,1). Distances are Manhattan distance from start.**

| Distance from Start | Spacing | Zone | Design Intent |
|---|---|---|---|
| 0–15 | 7–8 | Cradle | Learn food/travel. Mistakes are cheap. |
| 25–50 | 15–18 | Midlands | Real packing decisions. Forage is useful but not required. |
| 50–75 | 25–30 | Wilds | Committed expeditions. Loadout tradeoffs matter. |
| 75+ | 35 | Frontier | Death marches. Arrival bruised is expected. |

## Placement Heuristics

**Don't place on a grid.** The bands give you distance constraints; scatter settlements organically within them. A midlands settlement 17 tiles from one neighbor and 14 from another feels natural. Exactly 16 and 16 feels game-y.

**Lateral connections matter.** Two settlements both 50 tiles from start but 30 tiles from each other create a nice "do I go through A or B" decision. The bands are radial but your network shouldn't be.

**Rough settlement count estimate:** For a full 100x100 map you're probably looking at 40–55 settlements total, heavily weighted toward the cradle/midlands quadrant near start, thinning dramatically toward the far edges.

