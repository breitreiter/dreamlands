---
kind: plan
title: Arc-completion leveling system
state: exploring
created: 2026-05-16
updated: 2026-05-16 (Mercantile folded into Negotiation; boots arc added)
touches:
  files: []
  features: [arcs, leveling, skills, progression]
---

# Arc Leveling

## Design Intent

Completing an arc is the primary progression event. Each arc awards one upgrade chosen
from a tableau. The tableau is symmetric: every reward slot caps at 2, so the whole
system is explained in one sentence — "pick one upgrade per arc; nothing stacks past 2."

Players cannot max everything. The arc budget is slightly larger than the skill budget,
so build choices matter and replay paths diverge naturally.

## Arc Budget

| Pool | Count |
|------|-------|
| Total arcs | 20 |
| Legendary weapon arcs (capstone, endgame) | 3 |
| Legendary armor arcs | 3 |
| Legendary boots arc | 1 |
| Fixed carry-capacity arcs | 2 |
| **Tableau arcs** | **11** |

Equipment arcs grant their item directly — no tableau pick.
The two carry-capacity arcs are fixed rewards, not tableau choices.

## Tableau Rewards

Each tableau arc offers a pick from:

| Reward | Cap |
|--------|-----|
| Combat skill | 2 |
| Negotiation skill | 2 |
| Cunning skill | 2 |
| Bushcraft skill | 2 |
| Max health | 2 |
| Inventory slots | 2 |

Skills: 4 × 2 = **8 points** to full max. With 11 tableau arcs, 3 arcs go to health/inventory.
Health and inventory each cap at 2 but there are only 3 picks between them — players can
max one or split, but cannot max both. That's a genuine build choice.

No orphan arcs. Budget is exact.

## Why the Cap Matters

Without a cap, a player could stack health or inventory and skip all skill investment.
They'd haul freely and survive longer but fail every encounter check and get crushed in
combat (can't wear armor they can't use). The cap at 2 makes health and inventory
meaningful build choices rather than a trap.

## Open Questions

- Do capstone arcs offer a tableau pick in addition to their equipment? Could be a reward
  for completing the hardest content.
- Does the tableau show all options always, or does each arc surface a curated subset
  (e.g., only skills relevant to that arc's biome)?
