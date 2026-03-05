The current arbitrage-based trade system is not doing a great job of driving world exploration. If anything, it punishes
strategies other than looping early-game milk runs.

To course correct, I propose the Load System.

# Key changes
- Trade goods are no longer fungible commodities
- They are instead unique items with an intended destination
- The closest real-world analogy is a **load in trucking**: you pick one up, it fills a pack slot, you haul it toward its destination
- The player is essentially performing a distributed sorting function across the trade route graph — getting every load to its destination in as few hops as possible, constrained by personal inventory size
- The trade route graph exists to make this tractable by reducing total edges; without it, every settlement connects to every other (fully-connected graph) and the sorting problem is chaos

# Design

## Data Structure
Each load has:
- A name, which represents the object being delivered
- Origin biome
- A 1 sentence flavor story
- A destination settlement
- A 3x3 sector+biome based destination hint
- A delivery payment (blend of trade route hops and BFS tile distance — distant runs are harder, but so is deviating from the main trade arteries)
- A 1 sentence delivery flavor

Example:
- Exquisite Caftan
- Scrub
- A tailor in Irongate is certain caftans are coming into style next season.
- Irongate
- A mountain settlement in the southwest
- 50gp
- The tailor eyes the package eagerly. He hands you the payment and immediately unrolls the bundle.

## Market Interaction
Trade goods are gone. The market offers loads to claim (always free — the constraint is pack space) and accepts deliveries. You don't "buy" a load — you claim it, accepting the obligation to deliver it.

Optimal play becomes:
- Finding ways to cluster deliveries to group costs
- Deciding when to stash a long-haul load to pick up a short-haul load

## Storage
Deposit/withdraw is a first-class feature, not a pawn-shop hack. Any item in your pack (loads, gear, consumables) can be deposited at a settlement and picked up later. No pricing, no loss — just a locker.

- Settlement storage is a flat list of `ItemInstance` per settlement
- Open question: available everywhere, or only at hubs/waypoints? Limiting storage to connected settlements makes hub positioning matter more for the sorting game — you can't stash loads at dead-end leaves, forcing you to carry them further before dropping them.

Bank goes away as a separate feature — storage replaces it.

## Flavor Changes
The market is still a market — you go there to claim loads and deliver them. Storage is a separate service at the settlement (warehouse, storehouse, whatever fits the settlement's flavor).

## Deadheading
We want to avoid sticking the player with empty-load trips.

## Settlement Graph
We want to build "trade routes" that encourage players to transit packages in predictable routes. This ensures that players can
"work an area," shuffling packages closer to their final destinations.

Place settlements first with your density falloff. Then sort every settlement by Manhattan distance to the capitol, farthest first. Each settlement connects to the nearest settlement that is closer to the capitol than itself. That's it. That's the whole tree construction.
What falls out of this:
Clusters form organically from spatial proximity. If five villages are scattered in a region, they'll mostly connect to the same nearer settlement, which becomes a hub by virtue of having several children. You didn't designate it as a hub — it just sits in the right spot. The further from the capitol, the sparser settlements are, so outer leaves connect over longer distances while inner areas form tighter clusters. The density gradient you already wanted does double duty as economic geography.
After building the tree, derive hub status from child count. Any settlement with 3+ children is a hub. Give it a more impressive name, a bigger factor operation, more loads. A settlement with 1-2 children is a waypoint. Zero children is a leaf. The game's economic hierarchy is an emergent property of the map rather than a separate authored layer.
One tuning lever you'll want: when a settlement picks its parent, you might add a slight bias toward nodes that already have children. Without this, you can get situations where two nearby settlements of roughly equal distance to the capitol split a cluster between them — five villages and two competing "hubs" with 2-3 children each instead of one clear hub with 5. A small attractiveness bonus for existing connections produces cleaner hierarchy. Think of it as "merchants prefer established routes."
The opposite tuning lever: if hubs get too dominant (one settlement hoovers up every connection in a region), cap the child count and force overflow to the next-nearest parent. This creates secondary hubs, which is more interesting for gameplay — two competing market towns in a region rather than one monopoly.
The thing I'd prototype first is just the raw "connect to nearest closer node" pass on a few random maps and see what the trees look like. My guess is it'll be 80% right out of the box and you'll spend your tuning time on the edge cases — weird long-distance connections that skip over logical intermediate stops, or clusters that don't coalesce cleanly.

The trade route graph should live in mapgen as a first-class generation step. Generate settlements, then build the trade tree, then use tree position to assign settlement size/services. This is cleaner than fitting a graph to an already-sized map after the fact.

Let's start with baking this trade route graph into mapgen, so we can generate very ugly blue lines between graph-connected settlements. These are temporary, but useful for understanding the state of connectivity.

# Risks

## Payout balance is hard to get right
Load payouts are a blend of trade route hops and BFS tile distance, but the player's actual travel cost depends on terrain, encounters, health, and food consumption. If payouts are too generous, loads become free money. If too stingy, the player can't sustain themselves. The arbitrage system at least had market forces providing some self-correction; flat payouts don't. The two-axis payout (route hops + tile distance) gives us more tuning room than a single metric, but also more ways to get it wrong.

## Storage grows settlement state unboundedly
Deposit/withdraw means settlement state grows with every item the player stashes. `SettlementState.Bank` is already `List<ItemInstance>` per settlement, so the structural change is done. The risk is unbounded growth in serialized player state — may need a per-settlement storage cap or deposit fee to constrain it.

## Deadheading is stated but not solved
The doc flags empty-load trips as a problem but doesn't propose a solution. If loads are only available at the settlement you're standing in, the player must walk somewhere empty-handed to pick up the next load. The trade tree helps by clustering routes, but doesn't eliminate the problem — leaf nodes especially will strand the player.

## Trade tree construction depends on settlement density
The "connect to nearest closer node" algorithm assumes enough settlements exist that connections are reasonable distances apart. In sparse frontier zones (spacing ~35 nodes), a leaf settlement might connect to a parent 40+ tiles away with no intermediate stops. The player experience of hauling a load across 40 empty tiles is worse than the current system.

## ~~Removing trade goods deletes 62 items of authored content~~ (resolved)
Replaced with 152 haul catalog entries — more content than the old trade goods, with origin/delivery flavor per entry.

## Mercantile skill loses its purpose
Mercantile still provides a 2% per-point discount on market buys (food/medicine/equipment). Haul claiming is free, so mercantile doesn't affect trade income. Needs a new haul-related role (payout bonus, better offers, more offers) or removal.

# Open Questions

## Resolved
- ~~**Relationship to courier_system.md**~~: Superseded. The haul system replaces the courier/dispatch concept entirely.
- ~~**Load generation**~~: 152 haul entries authored in `text/hauls/haul_catalog.md`, transcribed to static C# in `HaulDef.*.cs`. LLM-assisted authoring via `haul-generate` command.
- ~~**How many loads per settlement?**~~: Non-leaf = 2, leaf = 1. Fills on visit, persists until claimed.
- ~~**Settlement size assignment / hub status**~~: Trade tree child count maps to `SettlementSize` (3+ = Town, 1-2 = Village, 0 = Outpost). City reserved for starting settlement.
- ~~**Payout formula**~~: Manhattan distance × 3. Simple, tunable later.

## Still Open
- **Storage availability**: Should deposit/withdraw be available at every settlement, or only hubs/waypoints? Deposit fee as a balancing lever? See "Storage on leaves" section below.
- **Load transit/relay UX**: How to make it clear which deposited loads at a settlement are "closer to done" vs. freshly arrived?
- **Mercantile skill role**: Currently just discounts market buys. Needs a haul-related role (better offers, payout bonus, more offers) or removal.
- **Payout tuning**: Manhattan × 3 is the starting formula. Needs playtesting to see if it sustains the player economy across different map positions.

# Affected Systems

## Done

| Layer | File | Change |
|-------|------|--------|
| Rules | `lib/Rules/ItemDef.cs` | ✅ 71 `TradeGood` entries deleted. `TradeGood` removed from `ItemType` enum. `Haul` type exists. |
| Rules | `lib/Rules/TradeBalance.cs` | ✅ Gutted. Only `PriceJitter`, `MercantileDiscountPerPoint`, `MaxStock`, `RestockPerDay` remain. |
| Rules | `lib/Rules/HaulDef.*.cs` | ✅ 152 haul entries transcribed to static C#, partitioned by biome. |
| Rules | `lib/Rules/ActionVocabulary.cs` | ✅ `get_random_treasure` verb removed. |
| Game | `lib/Game/Market.cs` | ✅ Rewritten. Buy-only (food/medicine/equipment). `ClaimHaul()` added. Sell/featured/arbitrage deleted. |
| Game | `lib/Game/SettlementState.cs` | ✅ `FeaturedSellItem`/`FeaturedBuyItem` removed. `HaulOffers` list added. |
| Game | `lib/Game/ItemInstance.cs` | ✅ Haul fields: `HaulDefId`, `DestinationSettlementId`, `DestinationHint`, `Payout`. |
| Game | `lib/Game/PlayerState.cs` | ✅ `ClaimedFeaturedBuys` removed. |
| Game | `lib/Game/HaulGeneration.cs` | ✅ Picks destination settlements from trade tree (2-hop distance), generates haul offers. |
| Game | `lib/Game/HaulDelivery.cs` | ✅ Scans player pack on arrival, auto-delivers matching hauls, pays out gold. |
| Game | `lib/Game/Mechanics.cs` | ✅ `ApplyGetRandomTreasure` removed. |
| Orchestration | `lib/Orchestration/SettlementRunner.cs` | ✅ Calls `GenerateHauls()` on settlement visit. |
| MapGen | `mapgen/TradeRouteBuilder.cs` | ✅ Tree built, hub/waypoint/leaf sizing from child count. |
| MapGen | `mapgen/TradeRoutePass.cs` | ✅ Trade tree edges rendered on map. |
| Map | `lib/Map/` | ✅ Trade tree serialized per-settlement in map.json (parent + children). |
| Server | `server/GameServer/Program.cs` | ✅ `GET /market` returns stock + hauls. `market_order` is buy-only. `claim_haul` action added. Auto-delivery on move. |
| Server | `server/GameServer/GameResponse.cs` | ✅ `SellLine` removed. `OfferIndex` added. `DeliveryInfo` DTO exists. |
| Tests | `tests/Dreamlands.Game.Tests/MarketTests.cs` | ✅ 13 tests: stocking, buy, ClaimHaul, restock. |
| Tests | `tests/Dreamlands.Game.Tests/HaulDeliveryTests.cs` | ✅ Delivery tests exist. |

## Remaining

| Layer | File | Impact |
|-------|------|--------|
| Rules | `lib/Rules/Skill.cs` | Mercantile skill needs a new role or removal. Currently just discounts market buys. |
| Game | `lib/Game/Bank.cs` | Rename/rework to general-purpose storage. Open question: deposit fee, leaf restrictions. |
| Web | `ui/web/src/screens/Market.tsx` | Rewrite UI for haul claiming + buy-only shop (separate task). |
| Web | `ui/web/src/screens/Settlement.tsx` | Delivery notification on arrival. |
| Web | `ui/web/src/screens/Inventory.tsx` | Haul display with destination hints. |
| Web | `ui/web/src/api/types.ts` | Update DTOs for new market/haul response shapes. |
| Design | `project/design/trade_economy.md` | Obsoleted by haul system. |
| Design | `project/design/trade_goods.md` | Obsoleted — trade goods deleted. |
| Design | `project/design/trade_rumors.md` | Obsoleted or folded into haul flavor. |
| Design | `project/design/courier_system.md` | Superseded by haul system. |

---

# Implementation Audit (2026-03-05)

## What's Done

### Infrastructure
- **Trade tree** — built (`TradeRouteBuilder.cs`), sized by child count (3+ = Town, 1-2 = Village, 0 = Outpost), rendered on the map (`TradeRoutePass.cs`)
- **Trade tree serialized** per-settlement in map.json (parent + children), round-trips through `MapSerializer`
- **152 haul catalog entries** authored in `text/hauls/haul_catalog.md`, transcribed to static C# in `HaulDef.{Plains,Mountains,Forest,Scrub,Swamp}.cs`
- **`haul-generate` authoring tool** fills blank catalog entries via LLM

### Haul lifecycle (complete end-to-end)
- **Haul generation** — `HaulGeneration.Generate()` picks concrete destination settlements from the trade tree (2-hop distance, fallback to 1 then 3). Cap: 2 offers at non-leaf settlements, 1 at leaves. Payout = Manhattan distance × 3.
- **Haul offer display** — `SettlementRunner.GenerateHauls()` populates `SettlementState.HaulOffers` on settlement visit. `GET /api/game/{id}/market` returns hauls array with name, destinationHint, payout, originFlavor.
- **Haul claiming** — `Market.ClaimHaul()` validates index/pack capacity, moves haul from offers to player pack. Server exposes via `claim_haul` action with `offerIndex` parameter.
- **Auto-delivery** — `HaulDelivery.Deliver()` scans player pack on settlement arrival, matches by `DestinationSettlementId`, pays out gold, returns delivery results. Wired into `SettlementRunner`. Server includes `Deliveries` in GameResponse.

### Market rewrite (complete)
- **Trade goods removed** — all 71 `ItemType.TradeGood` entries deleted from `ItemDef.BuildAll()`. `TradeGood` removed from `ItemType` enum.
- **Arbitrage pricing removed** — `GetSellToSettlementPrice`, `Sell`, `FeaturedSellItem`, `FeaturedBuyItem`, `FeaturedSellDiscount`, `FeaturedBuyPremium`, `SameBiomeBuyPenalty`, `CrossBiomeFlatBonus` all deleted.
- **Market is buy-only** — `Market.cs` sells food/medicine/equipment. `ApplyOrder` is buy-only. `MarketOrder` has only `Buys` (no `Sells`/`SellLine`).
- **`ClaimedFeaturedBuys`** removed from `PlayerState`.
- **Server endpoints updated** — `GET /market` returns stock + hauls (no sellPrices/featured). `market_order` is buy-only. New `claim_haul` action.
- **Market tests rewritten** — 13 tests covering food/medicine/equipment stocking, buy, ClaimHaul, and restock. Old 18 arbitrage tests deleted.
- **`get_random_treasure`** verb and handler removed from `ActionVocabulary` and `Mechanics`.
- **Bank deposit/withdraw** exists in `Bank.cs` (could evolve into general storage)

## What's Not Started
- **Market screen UI** (`ui/web/src/screens/Market.tsx`) — needs rewrite for haul display and claim interaction
- **Bank → Storage rename/rework** — `Bank` list on `SettlementState` works but naming and UX need updating. Open question: storage at all settlements vs hubs only, deposit fee
- **Dynamic haul generation** — when bespoke hauls exhaust for a given route, generate generic fallback hauls so trade never dead-ends
- **Encounter-sourced hauls** (`add_haul` verb) — encounters awarding hauls directly, design written but no code
- **Mercantile skill rework** — still provides 2% per-point discount on market buys; new haul-related role TBD

## Design Decisions (resolved 2026-03-04)

### 1. Settlement identity → coordinate-based IDs
Settlements are identified by grid coordinates (e.g. `"16,5"`), not flavor names. Flavor names are display-only. Player state, haul destinations, storage — everything keys on the coordinate ID. This is stable across flavor regeneration and map rerenders.

### 2. Trade tree structure → serialized per-settlement in map.json
Drop the raw edge list. Each settlement gets its parent coordinate and children list baked into map.json. The tree is generated once in mapgen and read many times at runtime. No re-derivation needed — the function just deserializes and goes. This matters because the final server form is Azure Functions, where every cold start pays the cost.

### 3. Haul catalog → static C#, partitioned by biome
Haul definitions live in C# like every other content type (`ItemDef.BuildAll()`, `ConditionDef.All`, etc.). No markdown parsing or JSON loading at runtime. Partitioned into `HaulDef.Plains()`, `HaulDef.Mountains()`, `HaulDef.Forest()`, `HaulDef.Scrub()`, `HaulDef.Swamp()` — each in its own file or partial class (~30 entries each) for context-window friendliness. `BuildAll()` concatenates the five lists. The markdown catalog remains the authoring surface; transcription to C# is a separate step (manual now, automatable later).

### 4. Destination resolution → dynamic at market spawn time
`HaulDef` specifies destination *biome*, not a specific settlement. When the market generates hauls, it picks a concrete destination settlement from that biome. This decouples authored content from map generation and gives us tuning levers for the "feel" of available runs. The generation logic will be churny and full of knobs — that's where the game design lives.

### 5. Load availability → dumbest possible starting system
Start simple, tune from observed play:
- **Non-leaf settlements**: generate until they have exactly **2** hauls when the player checks the market.
- **Leaf settlements**: generate up to **1** haul. This prevents the player from delivering to a leaf and getting stuck with a full inventory and no room to pick up the return cargo.
- **Destination**: both hauls go exactly **2 hops away** on the trade tree.
- **Persistence**: hauls stick around until claimed, not on a timer. No market volatility.
- **Refresh**: new hauls appear when existing ones are claimed, filling back to the cap.
- **Generation sees player state**: inventory doesn't exist until you look at it, so generation can quietly factor in what the player is carrying, where they've been, and how broke they are. Biggest lever for the "getting lucky" vibe.

# Package Placement Math

Since the trade route graph is a tree (acyclic, undirected traversal), and the player moves freely along it, the problem has clean structure.

The key concept is **edge crossing number**. For any edge in your tree, removing it partitions the graph into two subtrees. Count how many packages have their origin in one partition and destination in the other — that's the crossing number for that edge. Every one of those packages *must* be carried across that edge at some point. With capacity *k*, the player must traverse that edge at least ⌈crossings / k⌉ times in each direction. That gives you a tight lower bound on optimal play for any given package configuration.

This puts you squarely in the territory of the **k-delivery TSP on trees**, which is well-studied and has the nice property of being polynomially solvable (unlike the general graph case). The optimal strategy is essentially: do a DFS-like traversal, but at each edge, batch your deliveries to maximize how many crossing-packages you carry per traversal. The total optimal cost ends up being roughly proportional to the sum across all edges of ⌈crossings(e) / k⌉ × 2 × weight(e).

For your design purposes, the terms and concepts I'd focus on:

**k-delivery TSP on trees** — the direct formulation. Frederickson, Hecht, and Kim did foundational work here. Sometimes called "capacitated delivery on a tree."

**Multi-commodity flow on trees** — each package is a commodity with a source and sink. The tree structure means each commodity has a unique path, so flow decomposition is trivial and you can reason about edge loads directly.

**Edge congestion / load** — the ratio of crossing number to capacity at each edge. This is your single most useful design metric. Edges where this ratio exceeds 1 are where the player is forced into repeated traversals, which is where the game becomes a planning puzzle rather than a simple walk.

**Separator edges / bridge analysis** — in your tree, every edge is a bridge. The edges closest to the root with high crossing numbers are your natural "trade route bottlenecks." The deeper leaf-adjacent edges with low crossings define your "remote outposts." You're essentially designing a congestion gradient.

The practical balancing lever: you control difficulty by tuning how many packages cross each edge relative to capacity, and how *conflicting* the directions are. An edge where 3 packages go left-to-right and 3 go right-to-left is much harder to plan around than one where all 6 go the same direction, because the player can't batch them efficiently. That directional conflict ratio on bottleneck edges is probably the most interesting knob you have.

## Levers we have
- Limited information - since you have to visit a settlement to see the market inventory, that means the inventory doesn't exist until you look at it. We can do some limited correction to avoid bad experiences.
- NPC traders - not a formal concept, but if goods move between markets, or disappear from markets, it's not unreasonable to assume some other trader took the job.
- Trade graph - We now have a trade route tree, which means we can create clusters of delivery
- Rumors - We can communicate to the user the health of a node or branch and whether it's worthwhile to continue working it

## Gameplay to avoid

### Deadheading
We'll want to avoid players making a delivery to a settlement and having no pickups.

### Market volatility
If players see something in the market, they should have a reasonable expectation for how long it will be there. If they make a side trip to deliver another package, then return to the market, it will be frustrating if their planning is undermined by random inventory shuffles

### Mandatory inventory exhaustion
We should keep in mind that the delivery system is primarily a way to nudge players to explore new parts of the world, or to make treks to distant locations profitable/desirable. We should not expect or require players to sort the entire tree.

### Infinite milk runs
We need to be careful with this one. We absolutely do want to nudge players to keep moving, and one way to do that is to communicate that local work is drying up. We also have a limited number of predefined packages. Looping short runs will exhaust that pool and force us into bland flavorless deliveries.

We'll need to balance this with mechanics that keep the player engaged. If we guarantee subsistence income (to prevent bricking) we risk some players deciding the game is no fun because subsistence grinding seems to be a viable strategy.

Hopefully out-of-branch deliveries can help with this, but we'll want to keep a close eye on it.

## Production Map Graph Profile (2026-03-05)

78 settlements in the trade tree. Node distribution by edge count:

| Edges | Count | Role |
|-------|-------|------|
| 1     | 25    | Leaves (no children) |
| 2     | 34    | Pass-through (1 parent, 1 child) |
| 3     | 15    | Minor hubs (1 parent, 2 children) |
| 4     | 4     | Major hubs (1 parent, 3 children) |

Max connectivity is 4 — no settlement has more than 3 children. The four 4-edge hubs are Grassford (s20_7), Dustford (s40_16), Grassford (s14_36), and Oakford (s21_69). The tree is relatively flat: most nodes are leaves or simple pass-throughs, with a thin layer of branching hubs.

Surprisingly, the root (Aldgate) is only a 2-edge node — not a major hub. The four 3-child hubs sit at hops 1, 4, 8, and 10 from origin, spread through the tree depth rather than clustered near the root. This means the tree's branching structure is driven more by settlement density pockets than by proximity to the capital.

Tree depth is 17. Settlement count by depth:

| Depth | Count | Settlements |
|------:|------:|-------------|
| 0 | 1 | Aldgate |
| 1 | 2 | Grassford, Grasshollow |
| 2 | 5 | Dusthollow, Grasshollow, Sunhollow, Sunwatch, Thornhollow |
| 3 | 7 | Granitehollow, Grasshollow, Sandwatch, Sandwatch, Thornhollow, Thornhollow, Wheatwatch |
| 4 | 5 | Dustford, Sandwatch, Sunhollow, Sunwatch, Windhollow |
| 5 | 6 | Elmwatch, Flinthollow, Oakhollow, Thornhollow, Wheathollow, Windhollow |
| 6 | 6 | Grasshollow, Oakhollow, Sunhollow, Sunwatch, Wheathollow, Wheathollow |
| 7 | 6 | Frosthollow, Mosshollow, Sunwatch, Thornhollow, Wheatwatch, Windhollow |
| 8 | 8 | Dustwatch, Elmhollow, Elmhollow, Flinthollow, Granitewatch, Grassford, Grasshollow, Oakwatch |
| 9 | 7 | Granitewatch, Ironwatch, Oakhollow, Sandhollow, Sandhollow, Sandwatch, Thornhollow |
| 10 | 6 | Flinthollow, Granitehollow, Oakford, Oakhollow, Thornhollow, Wheatwatch |
| 11 | 7 | Dusthollow, Dustwatch, Elmhollow, Elmwatch, Murkwatch, Sunwatch, Thornhollow |
| 12 | 4 | Flinthollow, Mosswatch, Sunhollow, Thornhollow |
| 13 | 3 | Sandhollow, Sunwatch, Wheathollow |
| 14 | 2 | Elmhollow, Thornwatch |
| 15 | 1 | Elmhollow |
| 16 | 1 | Sunhollow |
| 17 | 1 | Grasswatch |

The bulk of settlements (depths 2–11) form a wide band in the middle of the tree. The long tail from depth 12–17 is a single chain with no branching — a deep linear spine reaching into the frontier.

### Storage on leaves — open tension

The original plan restricts storage to non-leaf settlements to make hub positioning matter for the sorting game. But deep chain endpoints like Grasswatch (depth 17) are natural base camps for frontier exploration — players will want to stage gear there. Denying storage at leaves punishes exploration-mode play that has nothing to do with trade.

Charging for storage (per-deposit gold cost) may thread the needle: leaves can have storage without undermining the sorting game, and the cost also discourages a market-sweeping anti-pattern — arriving at a new settlement, claiming every haul, immediately dumping them into storage to force a market refresh, and cherry-picking from the larger pool. If storage is free, that's a dominant strategy wherever hauls are plentiful. A deposit fee makes it a real tradeoff: you're paying gold now for optionality later.

## Encounter-sourced hauls

Encounters could award hauls directly — "You found an old journal belonging to Jorgo Borgins, you should return it." This gives encounters a way to create delivery obligations organically, tying exploration rewards to the trade network.

### Why this is low-disruption

The delivery pipeline doesn't care where a haul came from. `HaulDelivery.Deliver()` scans the player's pack for any `ItemInstance` with `HaulDefId != null` and a matching `DestinationSettlementId`. Auto-delivery on settlement arrival, payout, flavor text — all of it works identically regardless of whether the haul came from a market or an encounter. No model changes needed; `ItemInstance` already carries all the haul fields.

### What it needs

A new mechanic verb (e.g. `add_haul`) in `Mechanics.cs` that creates an `ItemInstance` with haul fields populated. Two authoring approaches:

1. **Reference a HaulDef by ID**: `+ add_haul exquisite_caftan` — ties the encounter to a catalog entry, reuses its name/flavor/destination biome. Destination settlement resolved at runtime the same way `HaulGeneration` does it.
2. **Inline definition**: encounter specifies name, payout, and destination hint directly. More flexible for one-off narrative hauls that don't belong in the catalog.

### Quest hauls vs market hauls

Quest hauls (encounter-sourced) are structurally different from market hauls and need separate handling in three areas:

1. **Spawn exclusion**: Quest hauls must never appear in market generation. Use a distinct origin biome (e.g. `"quest"`) so `HaulGeneration` naturally skips them — it filters by origin biome matching the settlement's terrain, and no settlement has terrain `quest`.

2. **Payout**: Quest hauls aren't on the trade graph, so the market payout formula (Manhattan distance × 3) doesn't apply. Payout should be set by the encounter author or by a separate quest-specific formula. Likely higher than equivalent market hauls to reward off-road exploration.

3. **Destination selection**: Market hauls pick destinations by walking the trade tree (2 hops). Quest hauls happen in the wilderness, off the graph. Destination should be the **closest settlement by cartesian distance** from the encounter location. This keeps delivery feel natural — "the nearest town" — rather than routing through trade graph topology the player can't see.

### Design implications

- Encounter hauls are a natural reward for exploration, complementing market hauls rather than competing with them. A player deep in the frontier finds a thing worth carrying back — that's the fantasy.
- Encounter hauls don't count against the market cap, so they layer on top of the base system without disrupting market generation.
- No new UI needed — a haul is a haul in the inventory screen regardless of source.