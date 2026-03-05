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
Deposit/withdraw means settlement state grows with every item the player stashes. The current `SettlementState` tracks stock as `Dictionary<string, int>` (item counts), but storing deposited items with per-instance data (loads with destinations, equipment with modifiers) requires full `ItemInstance` lists per settlement. This is a structural change to persistence.

## Deadheading is stated but not solved
The doc flags empty-load trips as a problem but doesn't propose a solution. If loads are only available at the settlement you're standing in, the player must walk somewhere empty-handed to pick up the next load. The trade tree helps by clustering routes, but doesn't eliminate the problem — leaf nodes especially will strand the player.

## Trade tree construction depends on settlement density
The "connect to nearest closer node" algorithm assumes enough settlements exist that connections are reasonable distances apart. In sparse frontier zones (spacing ~35 nodes), a leaf settlement might connect to a parent 40+ tiles away with no intermediate stops. The player experience of hauling a load across 40 empty tiles is worse than the current system.

## Removing trade goods deletes 62 items of authored content
The current catalog has 62 biome-tiered trade goods with flavor text and per-settlement featured buy/sell dynamics. The redesign replaces all of this. The load name/flavor system needs comparable authored depth or the game feels thinner.

## Mercantile skill loses its purpose
The current Mercantile skill provides a 2% per point discount on purchases. If loads are free to acquire and deliver for a fixed payout, there's nothing for Mercantile to modify. The skill either needs a new role (better load selection, payout bonus, more loads offered) or should be cut.

# Open Questions

- **Relationship to courier_system.md**: The existing `courier_system.md` design doc describes a Dispatch system that overlaps heavily with this proposal (destination-based items, delivery payouts, haversack slots). Is this a replacement for that design, an evolution of it, or should they coexist (loads for big trade, dispatches for small courier jobs)?
- **Load generation**: Where do load names and flavor text come from? The current 62 trade goods are hand-authored in `ItemDef.BuildAll()`. Do loads reuse that content, get LLM-generated per settlement, or draw from a new authored pool?
- **How many loads per settlement?** The doc says hubs get "more loads" but doesn't specify numbers. Current market stock scales by `SettlementSize` (Camp=1 through City=5 max stock). Does the same progression apply?
- **Load transit/relay**: Yes — the player deposits loads at intermediate settlements and picks them up on later trips. The player is the only sorter, moving loads closer to their destinations over multiple passes through the network. The key question is UX: how do we make it clear which deposited loads at a settlement are "closer to done" vs. freshly arrived?
- **Storage availability**: Should deposit/withdraw be available at every settlement, or only hubs/waypoints? Restricting it makes the trade tree structure matter more for the sorting game but punishes players at leaf nodes.
- **Settlement size assignment**: Currently only the starting city gets `SettlementSize.City`; everything else defaults to `Camp`. The tree-derived hub/waypoint/leaf classification is a new sizing axis. Does it replace `SettlementSize`, map onto it, or sit alongside it?
- **SettlementSize vs hub status**: The trade tree doc says hub status comes from child count (3+ = hub). Does this feed into `SettlementSize` (hub = Town, waypoint = Village, leaf = Outpost)? Or is it a separate property?
- **Payout formula specifics**: Payout blends trade route hops and BFS tile distance. Exact weighting TBD — route hops reward staying on the arteries, tile distance rewards long hauls. Need to prototype both axes and find a ratio that makes on-route deliveries reliably profitable while off-route detours are risky-but-lucrative.

# Affected Systems

## Must Change

| Layer | File | Impact |
|-------|------|--------|
| Rules | `lib/Rules/ItemDef.cs` | Remove or repurpose 62 `TradeGood` entries. Add load item type. |
| Rules | `lib/Rules/TradeBalance.cs` | Gut and replace. Featured buy/sell, cross-biome pricing, same-biome penalty, jitter — all gone. New constants for load payout scaling, max loads per settlement size. |
| Rules | `lib/Rules/SettlementBalance.cs` | Remove `storage` service tiers (bank is gone). Possibly add hub/waypoint/leaf classification. |
| Game | `lib/Game/Market.cs` | Major rewrite. Remove buy/sell pricing engine, trade good stocking, featured items, restock logic for trade goods. Replace with load pickup and delivery mechanics. |
| Game | `lib/Game/SettlementState.cs` | Add load inventory. Add deposit/withdraw storage (full `ItemInstance` lists, not just counts). Track trade tree parent/children if needed at runtime. |
| Game | `lib/Game/ItemInstance.cs` | Add per-instance load fields (destination, hint, payout, delivery flavor) — similar to `courier_system.md`'s proposed `DestinationSettlementId` etc. |
| Game | `lib/Game/PlayerState.cs` | Remove `ClaimedFeaturedBuys`. Possibly adjust pack/haversack semantics for loads. |
| Orchestration | `lib/Orchestration/SettlementRunner.cs` | Add delivery-on-arrival hook (scan inventory for loads matching current settlement). Rename market references to "market." |
| MapGen | `mapgen/SettlementPlacer.cs` | Build trade tree after placement. Derive hub/waypoint/leaf from child count. Assign `SettlementSize` from tree position. |
| MapGen | `mapgen/` (new or existing render pass) | Render trade tree edges as debug lines on the map image. |
| Map | `lib/Map/Poi.cs` or new DTO | Serialize trade tree edges (parent settlement ID per settlement) into `map.json`. |
| Server | `server/GameServer/Program.cs` | Update market endpoint to return loads instead of trade goods. Add delivery results to settlement entry response. Build settlement lookup from map for load generation. |
| Server | `server/GameServer/GameResponse.cs` | Replace `MarketOrderResultInfo` trade good fields with load fields. Add delivery notification DTOs. |
| Tests | `tests/Dreamlands.Game.Tests/MarketTests.cs` | 18 tests, all invalidated. Full rewrite against new load mechanics. |

## May Change

| Layer | File | Impact |
|-------|------|--------|
| Rules | `lib/Rules/Skill.cs` | Mercantile skill needs a new role or removal. |
| Rules | `lib/Rules/CharacterBalance.cs` | `CostMagnitudes` may need rethinking if loads are free. |
| Game | `lib/Game/Mechanics.cs` | Any action verbs tied to trade goods need updating. |
| Web | `ui/web/src/screens/Market.tsx` | Rewrite buy/sell UI for load pick-up and item deposit. |
| Web | `ui/web/src/screens/Settlement.tsx` | Delivery notification on arrival. "market" branding. |
| Web | `ui/web/src/screens/Inventory.tsx` | Load display with destination hints. |
| Web | `ui/web/src/api/types.ts` | New DTOs for loads, deliveries, deposited items. |
| CLI | `ui/cli/Program.cs` | Update `market` and `market-order` commands for new response shapes. |
| Design | `project/design/trade_economy.md` | Obsoleted — archive or replace. |
| Design | `project/design/trade_goods.md` | 62 trade goods catalog — obsoleted or repurposed as load name source. |
| Design | `project/design/trade_rumors.md` | Per-good rumors — obsoleted or folded into load flavor. |
| Design | `project/design/courier_system.md` | Overlapping design — reconcile or supersede. |
| Design | `project/screens/trade.md` | UI spec needs full rewrite for load-based interaction. |
| Orchestration | `tests/Dreamlands.Orchestration.Tests/SettlementRunnerTests.cs` | Settlement entry tests need delivery hook coverage. |

---

# Implementation Audit (2026-03-03)

## What's Done
- **Trade tree** — built (`TradeRouteBuilder.cs`), sized by child count (3+ = Town, 1-2 = Village, 0 = Outpost), rendered on the map (`TradeRoutePass.cs`)
- **Trade tree serialized** in map.json as coordinate-pair edges, round-trips through `MapSerializer`
- **152 haul catalog entries** authored in `text/hauls/haul_catalog.md` with origin biome, destination biome, origin flavor, delivery flavor
- **Bank deposit/withdraw** exists in `Bank.cs` (could evolve into general storage)
- **`haul-generate` authoring tool** fills blank catalog entries via LLM

## What's Not Started
- No `Haul` item type — `ItemType` enum still only has `TradeGood`
- `ItemInstance` has no destination/payout/hint/delivery fields (4 fields total)
- No code reads the haul catalog at runtime — it's pure markdown
- `Market.cs` is 100% arbitrage (buy/sell/pricing/restock) — zero load mechanics
- No delivery-on-arrival hook in `SettlementRunner`
- No payout formula, no load generation balance constants
- The 71 old `TradeGood` ItemDefs are still in `BuildAll()`
- No server endpoints for load claiming or delivery results

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