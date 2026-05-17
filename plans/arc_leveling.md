---
kind: plan
title: Arc-completion leveling system
state: shipped
created: 2026-05-16
updated: 2026-05-17
touches:
  files:
    - lib/Game/PlayerState.cs
    - lib/Game/Mechanics.cs
    - lib/Game/MechanicResult.cs
    - lib/Rules/ActionVocabulary.cs
    - lib/Rules/ArcRewards.cs
    - lib/Orchestration/EncounterRunner.cs
    - server/GameServer/GameResponse.cs
    - server/GameServer/GameFunctions.cs
    - ui/web/src/api/types.ts
    - ui/web/src/api/client.ts
    - ui/web/src/GameContext.tsx
    - ui/web/src/App.tsx
    - ui/web/src/screens/Tableau.tsx
    - tests/Dreamlands.Orchestration.Tests/TableauTests.cs
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
| Total arcs | 19 |
| Legendary weapon arcs (capstone, endgame) | 3 |
| Legendary armor arcs | 3 |
| Legendary boots arc (Scarecrow Boots immunity) | 1 |
| Fixed carry-capacity arcs | 2 |
| **Tableau arcs** | **10** |

Equipment arcs grant their item directly — no tableau pick.
The two carry-capacity arcs are fixed rewards, not tableau choices.
Total: 3 + 3 + 1 + 2 = 9 equipment arcs; 19 − 9 = 10 tableau arcs.

## Tableau Rewards

For what each skill tier unlocks (gear, passives, encounter check behavior), see
[`project/design/skills.md`](../project/design/skills.md).

Each tableau arc offers a pick from:

| Reward | Cap | Pick effect |
|--------|-----|-------------|
| Combat skill | 2 | +1 tier |
| Negotiation skill | 2 | +1 tier |
| Cunning skill | 2 | +1 tier |
| Bushcraft skill | 2 | +1 tier |
| Max health | 2 | +5 max health |
| Inventory slots | 2 | +1 pack slot |

Skills: 4 × 2 = **8 points** to full max. With 10 tableau arcs, 2 arcs go to health/inventory.
Health and inventory each cap at 2 but there are only 2 picks between them — players can
max one or split, but cannot max both. That's a genuine build choice.

No orphan arcs. Budget is exact.

## Implementation

Arc completion is signaled by `+finish_dungeon` or `+flee_dungeon` (existing verbs).
When an arc should also grant a tableau pick, authors emit `+add_level` in the same
mechanic block. Equipment arcs use `+item` instead — no `+add_level`.

When `+add_level` fires, the runner suspends on `EncounterStep.AwaitTableauPick` before
returning to explore. The player picks a slot via `pick_reward` action; the runner applies
the effect and decrements `PendingLevels`. If multiple picks are pending (e.g. two `+add_level`
in one encounter) the tableau stays open until all picks are consumed.

Closed-tab resilience: `PendingLevels` and `ArcRewardsTaken` persist on `PlayerState`. If
the player closes the tab while the tableau is open, `GetGame` re-emits `AwaitTableauPick`.

## Why the Cap Matters

Without a cap, a player could stack health or inventory and skip all skill investment.
They'd haul freely and survive longer but fail every encounter check and get crushed in
combat (can't wear armor they can't use). The cap at 2 makes health and inventory
meaningful build choices rather than a trap.

## Open Questions

- Do capstone arcs offer a tableau pick in addition to their equipment? Could be a reward
  for completing the hardest content.
- Curated subset per arc (e.g., only skills relevant to that arc's biome) deferred — currently
  all available (uncapped) slots are shown.
