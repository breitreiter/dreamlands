# Haul Economy Balancing

## Design Goals

1. Hauls that nudge players further down the trade graph (exploration)
2. Hauls that send players to unexplored settlements
3. Hauls that occasionally send players back to Aldgate (for unlocked storylets)
4. Short back-trips exist for leaf nodes but are clearly the less exciting option via price signal

## Payout Formula

```
payout = base + (manhattan × distRate) + (destDepth × depthRate) + explorationBonus
```

| Component | Value | Rationale |
|---|---|---|
| base | 5 | Floor payment — "thanks for carrying something" |
| distRate | 2 per tile | Scales with travel time, covers food cost (1.8g/tile) with slim margin |
| depthRate | 1 per depth level | Rewards pushing into the frontier; Aldgate (depth 0) gets nothing |
| explorationBonus | 8 | Flat bonus for unvisited destination; decisive but not exploitable |

## Economic Context

- Food cost: 9g/day
- Travel speed: 5 orthogonal moves/day
- Effective travel cost: **1.8g/tile**
- Production map depth range: 0–17
- Previous formula: manhattan × 3 (flat, no depth/exploration signal)

## Example Payouts

| Scenario | Manhattan | Dest Depth | Explored? | Payout | Food Cost | Profit |
|---|---|---|---|---|---|---|
| Leaf back-trip to shallow visited | 5 | 3 | yes | 18 | 9 | 9 |
| Leaf back-trip to mid visited | 5 | 7 | yes | 22 | 9 | 13 |
| Forward push, mid, unvisited | 10 | 8 | no | 41 | 18 | 23 |
| Forward push, mid, visited | 10 | 8 | yes | 33 | 18 | 15 |
| Deep frontier, unvisited | 8 | 14 | no | 43 | 14 | 29 |
| Deep frontier, visited | 8 | 14 | yes | 35 | 14 | 21 |
| Long haul back to Aldgate | 25 | 0 | yes | 55 | 45 | 10 |
| Short hop, shallow, visited | 3 | 2 | yes | 13 | 5 | 8 |

## Price Scale

All equipment uses `Magnitude`-based pricing. Sell price is 50% of buy price.

| Magnitude | Buy | Sell |
|---|---|---|
| Trivial | 3 | 1 |
| Small | 15 | 7 |
| Medium | 40 | 20 |
| Large | 80 | 40 |
| Huge | 200 | 100 |

## Equipment Prices

### Weapons

Weapons occupy one pack slot. Three weapon classes with different stat profiles:
- **Daggers**: Combat +1 (flat), Foraging +1 to +5
- **Axes**: Combat +1 to +4, Foraging +1 to +3
- **Swords**: Combat +2 to +5, Foraging +0 (pure combat)

| Item | Class | Cost | Buy | Combat | Forage | Biome | Tier |
|---|---|---|---|---|---|---|---|
| Bodkin | Dagger | Small | 15 | +1 | +1 | Plains | 1 |
| Skinning Knife | Dagger | Small | 15 | +1 | +2 | Swamp | 1 |
| Jambiya | Dagger | Small | 15 | +1 | +2 | Scrub | 1 |
| Seax | Dagger | Medium | 40 | +1 | +3 | Mountains | 2 |
| Kukri | Dagger | Medium | 40 | +1 | +3 | Scrub | 2 |
| Hunting Knife | Dagger | Large | 80 | +1 | +4 | Mountains | 2 |
| Kopis | Dagger | — | — | +1 | +5 | — | — |
| Hatchet | Axe | Small | 15 | +1 | +1 | Forest | 1 |
| Tomahawk | Axe | Small | 15 | +2 | +1 | Forest | 1 |
| War Axe | Axe | Medium | 40 | +2 | +2 | Forest | 2 |
| Broadaxe | Axe | Large | 80 | +3 | +2 | Mountains | 2 |
| Bardiche | Axe | Large | 80 | +3 | +1 | Mountains | 2 |
| Labrys | Axe | — | — | +4 | +3 | — | — |
| Falchion | Sword | Small | 15 | +2 | — | Plains | 1 |
| Short Sword | Sword | Medium | 40 | +3 | — | Plains | 2 |
| Tulwar | Sword | Medium | 40 | +3 | — | Scrub | 2 |
| Scimitar | Sword | Large | 80 | +4 | — | Scrub | 2 |
| Arming Sword | Sword | Large | 80 | +4 | — | Plains | 2 |
| Zweihänder | Sword | — | — | +5 | — | — | — |

### Armor

Armor occupies one pack slot. Three armor classes with different stat profiles:
- **Light**: Cunning +0 to +5, Freezing resist +0 to +3
- **Medium**: Cunning +1 to +2, Injury resist +1 to +3, Freezing resist +1 to +5
- **Heavy**: Injury resist +1 to +5, Freezing resist +0 to +2

| Item | Class | Cost | Buy | Cunning | Injury R | Freeze R | Biome | Tier |
|---|---|---|---|---|---|---|---|---|
| Tunic | Light | — | — | — | — | — | Plains | 1 |
| Silks | Light | Small | 15 | +1 | — | — | Scrub | 1 |
| Waxed Poncho | Light | Small | 15 | +2 | — | +1 | Swamp | 1 |
| Traveling Cloak | Light | Medium | 40 | +3 | — | +2 | Mountains | 2 |
| Embroidered Kaftan | Light | Large | 80 | +4 | — | +2 | Scrub | 2 |
| Nightveil | Light | — | — | +5 | — | +3 | — | — |
| Leather | Medium | Small | 15 | +1 | +1 | +1 | Forest | 1 |
| Hide Armor | Medium | Small | 15 | +1 | +1 | +2 | Mountains | 1 |
| Buff Coat | Medium | Medium | 40 | +1 | +2 | +3 | Forest | 2 |
| Lamellar | Medium | Large | 80 | +2 | +2 | +3 | Mountains | 2 |
| Frostward Harness | Medium | — | — | +2 | +3 | +5 | — | — |
| Gambeson | Heavy | Small | 15 | — | +1 | +1 | Mountains | 1 |
| Chainmail | Heavy | Small | 15 | — | +2 | — | Plains | 1 |
| Scale Armor | Heavy | Medium | 40 | — | +3 | — | Scrub | 2 |
| Brigandine | Heavy | Large | 80 | — | +4 | +1 | Plains | 2 |
| Warden's Plate | Heavy | — | — | — | +5 | +2 | — | — |

### Boots

| Item | Cost | Buy | Exhaust R | Biome | Tier |
|---|---|---|---|---|---|
| Fine Boots | Small | 15 | +1 | Plains | 1 |
| Heavy Work Boots | Small | 15 | +2 | Mountains | 1 |
| Riding Boots | Medium | 40 | +3 | Scrub | 2 |
| Trail Boots | Large | 80 | +4 | Forest | 2 |
| Windstriders | — | — | +5 | — | — |

### Tools

| Item | Cost | Buy | Effect | Biome | Tier |
|---|---|---|---|---|---|
| Canteen | Small | 15 | Thirst R +2 | — | — |
| Waterskin | Medium | 40 | Thirst R +3 | Scrub | 2 |
| Letters of Introduction | Medium | 40 | Negotiation +2 | Scrub | 1 |
| Peoples of the Borderlands | Large | 80 | Negotiation +3 | Mountains | 2 |
| Trader's Ledger | Medium | 40 | Mercantile +2 | Plains | 1 |
| Assayer's Kit | Large | 80 | Mercantile +3 | Mountains | 2 |
| Cartographer's Kit | Large | 80 | Lost R +5 | Plains | 1 |
| Sleeping Kit | Large | 80 | Exhaust R +4 | Forest | 2 |

### Consumables

| Item | Cost | Buy | Effect | Biome | Tier |
|---|---|---|---|---|---|
| Food (any) | Trivial | 3 | 1 meal | — | — |
| Bandages | Trivial | 3 | Cures injured | — | — |
| Pale Knot Berry | Small | 15 | Cures exhausted | Plains | 2 |
| Mudcap Fungus | Small | 15 | Cures poisoned | Swamp | 2 |
| Siphon Glass | Medium | 40 | Cures lattice sickness | Swamp | 3 |
| Shustov Tonic | Medium | 40 | Cures irradiated | Plains | 3 |

### Services

| Service | Cost | Notes |
|---|---|---|
| Inn (per night) | 9 | 3× food cost; rest + meal recovery |
| Storage (basic) | 10 | 10 slots |
| Storage (expanded) | 50 | 25 slots |
| Storage (large) | 150 | 50 slots |
| Storage (warehouse) | 400 | 100 slots |

## Purchasing Power

How many hauls to afford key upgrades (using "forward push, mid, unvisited" as reference = 23g profit):

| Target | Buy Price | Hauls Needed | Days (~2 day round trip) |
|---|---|---|---|
| Food (1 day) | 9 | <1 | — |
| Tier 1 weapon/armor | 15 | 1 | 2 |
| Tier 2 medium gear | 40 | 2 | 4 |
| Tier 2 large gear | 80 | 3–4 | 6–8 |
| Storage (basic) | 10 | <1 | — |
| Storage (expanded) | 50 | 2–3 | 4–6 |
| Storage (large) | 150 | 7 | 14 |
| Storage (warehouse) | 400 | 18 | 36 |

## Key Properties

- **Everything is profitable.** Even the worst case clears food cost.
- **Depth is a quiet signal.** Same manhattan distance, depth 3 vs depth 14 = 11g difference.
- **Exploration bonus is decisive.** 8g tips the decision toward unvisited destinations.
- **Aldgate trips are thin.** Depth 0 = no depth bonus. You go home for storylets, not gold.
- **Frontier is where the money is.** Deep + unvisited + moderate distance = best gold/tile ratio.
