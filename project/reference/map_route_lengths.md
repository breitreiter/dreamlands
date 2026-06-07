# Production Map — Route Lengths & Hazard Exposure

Graph-distance reference for the production map, so balance passes don't start
by rewriting map-analysis scripts. **Fourth time we've measured this; first time
writing it down.** Regeneration script in the appendix.

- **Map**: `worlds/production/map.json`, seed `2130085583`, 100×100, measured 2026-06-06.
  Stale the next time `mapgen generate production` runs — check the seed before trusting numbers.
- **Method**: BFS shortest path, 4-adjacency, passable = terrain ≠ Lake. Distances in
  steps (nodes entered). One step = one time period; **5 steps = 1 day**
  (Morning/Midday/Afternoon/Evening/Night). Nights on road ≈ `dist // 5`.
- **Inventory**: 63 settlements (26 T1, 37 T2, **zero T3** — every T3 leg launches
  from a T2 town), 19 placed dungeons/arcs (roster lists 20; one unplaced as of
  this measurement).

## Settlement spacing (graph dist to nearest settlement)

| | n | min | p25 | median | p75 | p90 | max |
|---|---|---|---|---|---|---|---|
| All | 63 | 5 | 9 | 11 | 18 | 18 | 24 |
| T1 | 26 | — | — | 6 | — | 9 | 13 |
| T2 | 37 | — | — | 14 | — | 18 | 24 |

Nearest-3 pool (the realistic "next hop" menu): median 14, p75 18, p90 21, max 32.

**Headline**: T1 hops are ~1 day. T2 hops are ~3 days. Nothing between adjacent
settlements exceeds ~5 days.

## Trade-graph legs (parent → tradeChildIds, the contract routes)

n=62 · min 5 · p25 9 · **median 13** · p75 18 · p90 19 · **max 28** (Thessani → Burnfoot, 5.6 days)

Longest legs:

| Route | dist | days | biome mix |
|---|---|---|---|
| Thessani → Burnfoot | 28 | 5.6 | fore 18, scru 10 |
| Orvast → Frostheim | 24 | 4.8 | plai 8, fore 7, moun 6, scru 2, swam 1 |
| Mëthrakel → Fort Redbank | 22 | 4.4 | plai 12, swam 10 |
| Eagle Roost → Dunkelberg | 22 | 4.4 | fore 18, moun 4 |
| Melori → Dunfield Watch | 20 | 4.0 | plai 13, scru 7 |

Highest hazard exposure (scrub+mountain steps — thirst/cold channels):

| Route | dist | hazard steps |
|---|---|---|
| Kalimur → Melori | 18 | 15 |
| Eagle Roost → Kalimur | 14 | 14 |
| Solîrekh → Hartzell | 19 | 14 |
| Fort Redbank → Thessani | 19 | 13 |
| Ironstone → Eagle Roost | 13 | 12 |
| Hohrthal → Brenngrat | 11 | 11 |

## Dungeon/arc final legs (nearest settlement → dungeon)

dC = distanceFromCity (depth from Aldgate). Sorted by region tier, then leg length.

| Arc | rT | dC | From | dist | days | leg biome mix |
|---|---|---|---|---|---|---|
| brides_cave | 1 | 26 | Kestrel Hollow | 8 | 1.6 | plai 6, moun 2 |
| relay_post | 2 | 64 | Corbie Knowe | 5 | 1.0 | scru 3, fore 2 |
| zahlenhaus | 2 | 80 | Eismark | 5 | 1.0 | moun 5 |
| grainway_station | 2 | 71 | Beacon Tor | 6 | 1.2 | plai 6 |
| halfway_house | 2 | 86 | Brenngrat | 6 | 1.2 | moun 6 |
| metal_beast | 2 | 73 | Windbreak Outpost | 6 | 1.2 | plai 6 |
| warrant_oak | 2 | 90 | Melori | 6 | 1.2 | scru 3, plai 2, fore 1 |
| ledgerhaus | 2 | 86 | Felsgrund | 7 | 1.4 | moun 7 |
| listening_blind | 2 | 86 | Dëmravesh | 7 | 1.4 | swam 7 |
| wellhead_station | 2 | 104 | Vashedi | 7 | 1.4 | scru 7 |
| foresters_post | 2 | 134 | Velashîr | 8 | 1.6 | fore 8 |
| tile_house | 2 | 105 | Hartzell | 11 | 2.2 | moun 9, swam 2 |
| census_house | 2 | 142 | Velashîr | 12 | 2.4 | fore 11, scru 1 |
| wrenbury | 2 | 112 | Belovar | 12 | 2.4 | fore 7, scru 4, plai 1 |
| the_lodge | 3 | 177 | Frostheim | 6 | 1.2 | moun 4, fore 2 |
| the_stift | 3 | 176 | Frostheim | 9 | 1.8 | moun 9 |
| the_revenakh | 3 | 159 | Orvast | 12 | 2.4 | plai 9, scru 2, swam 1 |
| the_city | 3 | 107 | Dryhope | 22 | 4.4 | fore 13, plai 9 |
| foundry | 3 | 124 | Brenngrat | 32 | 6.4 | plai 21, scru 6, moun 4, fore 1 |

**Headlines**:

- T2 final legs are short: 5–12 steps, mostly single-night trips from a nearby town.
- T3 splits into two classes:
  - **Doorstep T3** — the_lodge / the_stift / the_revenakh: 6–12 steps from a T2
    town (Frostheim serves two of them). Hazard is biome exposure (the_stift = 9
    mountain steps), not distance.
  - **Expedition T3** — the_city (22 steps, hazard-free but 4+ nights each way)
    and foundry (32 steps, 10 hazard steps, 6+ nights each way; round trip ≈ 12
    nights). These are the legs that should price-gate on Bushcraft.
- Frostheim is the deep-north basecamp (dC ~176 arcs at its doorstep); Brenngrat
  serves both halfway_house (6) and the foundry expedition (32).

## Appendix: regeneration script

Run against any world's `map.json`. Last run 2026-06-06 (python3, stdlib only).

```python
import json, collections

m = json.load(open('worlds/production/map.json'))
nodes = {(n['x'], n['y']): n for n in m['nodes']}
regions = {r['id']: r for r in m['regions']}

def passable(p):
    n = nodes.get(p)
    return n is not None and n['terrain'] != 'Lake'

def nbrs(p):
    x, y = p
    for q in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
        if passable(q): yield q

settlements, dungeons = [], []
for p, n in nodes.items():
    poi = n.get('poi')
    if not poi: continue
    if poi['kind'] == 'Settlement': settlements.append((p, poi, regions[n['regionId']]))
    elif poi['kind'] == 'Dungeon': dungeons.append((p, poi, regions[n['regionId']], n.get('distanceFromCity')))

def bfs(start):
    dist, prev, q = {start: 0}, {}, collections.deque([start])
    while q:
        p = q.popleft()
        for nb in nbrs(p):
            if nb not in dist:
                dist[nb], prev[nb] = dist[p] + 1, p
                q.append(nb)
    return dist, prev

def path_to(prev, start, end):
    path = [end]
    while path[-1] != start: path.append(prev[path[-1]])
    return list(reversed(path))

def biome_mix(path):
    c = collections.Counter()
    for p in path[1:]: c[nodes[p]['terrain']] += 1
    return c

sett_bfs = {p: bfs(p) for p, _, _ in settlements}

# Settlement spacing: nearest-k settlement distances per settlement
for p, poi, reg in settlements:
    dist, _ = sett_bfs[p]
    nearest3 = sorted(dist.get(q, 10**9) for q, _, _ in settlements if q != p)[:3]

# Dungeon final legs: nearest settlement, path, biome mix
for p, poi, reg, dfc in dungeons:
    d, sp, spoi = min((sett_bfs[sp][0].get(p, 10**9), sp, spoi) for sp, spoi, _ in settlements)
    mix = biome_mix(path_to(sett_bfs[sp][1], sp, p))

# Trade-graph legs: parent -> tradeChildIds
sett_by_id = {poi['settlementId']: (p, poi, reg) for p, poi, reg in settlements}
for p, poi, reg in settlements:
    for cid in poi.get('tradeChildIds', []):
        if cid in sett_by_id:
            cp = sett_by_id[cid][0]
            d = sett_bfs[p][0].get(cp)
            mix = biome_mix(path_to(sett_bfs[p][1], p, cp))
```
