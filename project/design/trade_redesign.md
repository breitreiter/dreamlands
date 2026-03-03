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

Place settlements first with your density falloff. Then sort every settlement by Manhattan distance to the capitol, farthest first. Each settlement connects to the nearest settlement that is closer to the capitol than itself. That's it. That's the whole DAG construction.
What falls out of this:
Clusters form organically from spatial proximity. If five villages are scattered in a region, they'll mostly connect to the same nearer settlement, which becomes a hub by virtue of having several children. You didn't designate it as a hub — it just sits in the right spot. The further from the capitol, the sparser settlements are, so outer leaves connect over longer distances while inner areas form tighter clusters. The density gradient you already wanted does double duty as economic geography.
After building the tree, derive hub status from child count. Any settlement with 3+ children is a hub. Give it a more impressive name, a bigger factor operation, more loads. A settlement with 1-2 children is a waypoint. Zero children is a leaf. The game's economic hierarchy is an emergent property of the map rather than a separate authored layer.
One tuning lever you'll want: when a settlement picks its parent, you might add a slight bias toward nodes that already have children. Without this, you can get situations where two nearby settlements of roughly equal distance to the capitol split a cluster between them — five villages and two competing "hubs" with 2-3 children each instead of one clear hub with 5. A small attractiveness bonus for existing connections produces cleaner hierarchy. Think of it as "merchants prefer established routes."
The opposite tuning lever: if hubs get too dominant (one settlement hoovers up every connection in a region), cap the child count and force overflow to the next-nearest parent. This creates secondary hubs, which is more interesting for gameplay — two competing market towns in a region rather than one monopoly.
The thing I'd prototype first is just the raw "connect to nearest closer node" pass on a few random maps and see what the trees look like. My guess is it'll be 80% right out of the box and you'll spend your tuning time on the edge cases — weird long-distance connections that skip over logical intermediate stops, or clusters that don't coalesce cleanly.

The trade route graph should live in mapgen as a first-class generation step. Generate settlements, then build the DAG, then use DAG position to assign settlement size/services. This is cleaner than fitting a graph to an already-sized map after the fact.

Let's start with baking this trade route graph into mapgen, so we can generate very ugly blue lines between graph-connected settlements. These are temporary, but useful for understanding the state of connectivity.

# Risks

## Payout balance is hard to get right
Load payouts are a blend of trade route hops and BFS tile distance, but the player's actual travel cost depends on terrain, encounters, health, and food consumption. If payouts are too generous, loads become free money. If too stingy, the player can't sustain themselves. The arbitrage system at least had market forces providing some self-correction; flat payouts don't. The two-axis payout (route hops + tile distance) gives us more tuning room than a single metric, but also more ways to get it wrong.

## Storage grows settlement state unboundedly
Deposit/withdraw means settlement state grows with every item the player stashes. The current `SettlementState` tracks stock as `Dictionary<string, int>` (item counts), but storing deposited items with per-instance data (loads with destinations, equipment with modifiers) requires full `ItemInstance` lists per settlement. This is a structural change to persistence.

## Deadheading is stated but not solved
The doc flags empty-load trips as a problem but doesn't propose a solution. If loads are only available at the settlement you're standing in, the player must walk somewhere empty-handed to pick up the next load. The DAG helps by clustering routes, but doesn't eliminate the problem — leaf nodes especially will strand the player.

## DAG construction depends on settlement density
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
- **Storage availability**: Should deposit/withdraw be available at every settlement, or only hubs/waypoints? Restricting it makes the DAG structure matter more for the sorting game but punishes players at leaf nodes.
- **Settlement size assignment**: Currently only the starting city gets `SettlementSize.City`; everything else defaults to `Camp`. The DAG-derived hub/waypoint/leaf classification is a new sizing axis. Does it replace `SettlementSize`, map onto it, or sit alongside it?
- **SettlementSize vs hub status**: The DAG doc says hub status comes from child count (3+ = hub). Does this feed into `SettlementSize` (hub = Town, waypoint = Village, leaf = Outpost)? Or is it a separate property?
- **Payout formula specifics**: Payout blends trade route hops and BFS tile distance. Exact weighting TBD — route hops reward staying on the arteries, tile distance rewards long hauls. Need to prototype both axes and find a ratio that makes on-route deliveries reliably profitable while off-route detours are risky-but-lucrative.

# Affected Systems

## Must Change

| Layer | File | Impact |
|-------|------|--------|
| Rules | `lib/Rules/ItemDef.cs` | Remove or repurpose 62 `TradeGood` entries. Add load item type. |
| Rules | `lib/Rules/TradeBalance.cs` | Gut and replace. Featured buy/sell, cross-biome pricing, same-biome penalty, jitter — all gone. New constants for load payout scaling, max loads per settlement size. |
| Rules | `lib/Rules/SettlementBalance.cs` | Remove `storage` service tiers (bank is gone). Possibly add hub/waypoint/leaf classification. |
| Game | `lib/Game/Market.cs` | Major rewrite. Remove buy/sell pricing engine, trade good stocking, featured items, restock logic for trade goods. Replace with load pickup and delivery mechanics. |
| Game | `lib/Game/SettlementState.cs` | Add load inventory. Add deposit/withdraw storage (full `ItemInstance` lists, not just counts). Track DAG parent/children if needed at runtime. |
| Game | `lib/Game/ItemInstance.cs` | Add per-instance load fields (destination, hint, payout, delivery flavor) — similar to `courier_system.md`'s proposed `DestinationSettlementId` etc. |
| Game | `lib/Game/PlayerState.cs` | Remove `ClaimedFeaturedBuys`. Possibly adjust pack/haversack semantics for loads. |
| Orchestration | `lib/Orchestration/SettlementRunner.cs` | Add delivery-on-arrival hook (scan inventory for loads matching current settlement). Rename market references to "market." |
| MapGen | `mapgen/SettlementPlacer.cs` | Build settlement DAG after placement. Derive hub/waypoint/leaf from child count. Assign `SettlementSize` from DAG position. |
| MapGen | `mapgen/` (new or existing render pass) | Render DAG edges as debug lines on the map image. |
| Map | `lib/Map/Poi.cs` or new DTO | Serialize DAG edges (parent settlement ID per settlement) into `map.json`. |
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

