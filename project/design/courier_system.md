# Courier System

Simple concept:
Markets sell Dispatches

A Dispatch
- Description is a target city by name, maybe also a clue for where it spawns
- Has a short flavorful name
- Has an id of a destination city
- Has a payout for delivery

When the player arrives at the destination, the dispatch is removed from their inventory and they are paid

Target description is:
- Settlement name
- Biome
- 3x3-slice map segment (north-west, north, midlands, etc )
- Settlement size
- "Deliver to Grassgate, a plains town in the west"

In the market, dispatches should be named "Delivery Contract: Sealed Personal Letter"
In the inventory, "Sealed Personal Letter"

Tier 1 markets only generate dispatches for tier 1 settlements
Tier 2 markets only generate dispatches for tier 2 settlements

Dispatches consume 1 haversack slot

There should be a significant spread between buy price (you're just buying a contract to deliver it) and final delivery price.

Payout should be either:
- Scaled by bfs to destination
- If that's hard, random or set by dispatch type

# Dispatch names

## Local / Trivial–Small Contracts

- Sealed personal letter — Plains 1 — Trivial
- Merchant invoice packet — Plains 1 — Small
- Church tithe receipt — Plains 1 — Small
- Minor court summons — Plains 1 — Small
- Trade order confirmation — Plains 1 — Small
- Apprenticeship indenture — Plains 1 — Small

## Commercial / Medium Contracts

- Mining claim renewal — Mountain 2 — Medium
- Shipment authorization writ — Scrub 2 — Medium
- River toll exemption pass — Swamp 2 — Medium
- Guild debt certificate — Plains 2 — Medium
- Land transfer deed — Plains 2 — Medium
- Grain futures confirmation — Plains 2 — Medium

## Industrial / Large Contracts

- Rail spur survey folio — Scrub 2 — Large
- Patent filing dossier — Mountain 2 — Large
- Engineering blueprint set — Mountain 2 — Large
- Blasting rights authorization — Mountain 2 — Large
- Industrial machinery order contract — Plains 2 — Large
- Assay certification results — Mountain 2 — Large

## Political / Military / Significant Contracts

- Troop movement dispatch — Plains 2 — Huge
- Encrypted military cipher packet — Plains 3 — Huge
- Railway expansion master plan — Scrub 3 — Huge
- Industrial sabotage report — Mountain 2 — Huge
- Arrest warrant for regional leader — Scrub 2 — Huge
- Treaty draft between factions — Scrub 2 — Huge
- Scholar's suppressed manuscript — Mountain 3 — Huge
- Mesa weapons prototype schematics — Scrub 3 — Huge

---

# Implementation Addendum

## New Item Type

Add `Dispatch` to the `ItemType` enum in `lib/Rules/ItemDef.cs`. This keeps dispatches distinct from consumables and trade goods so the UI can identify them. `IsPackItem` returns false for Dispatch (haversack item, 1 slot).

Dispatch `ItemDef` entries go in `ItemDef.BuildAll()` alongside trade goods. Each dispatch name from the list above becomes a def with `Type = ItemType.Dispatch`, a `Biome`, `ShopTier`, and `Cost` (the buy price — Trivial/Small/Medium/Large/Huge). The def represents the contract type, not a specific destination.

## New Fields on ItemInstance

`lib/Game/ItemInstance.cs` needs per-instance dispatch data since the same def ("Sealed Personal Letter") can target different settlements:

- `string? DestinationSettlementId` — the settlement POI name (used for delivery matching)
- `string? DestinationDescription` — the flavor string shown to the player ("Deliver to Grassgate, a plains town in the west")
- `int? DeliveryPayout` — gold paid on delivery

These are nullable so non-dispatch items are unaffected.

## Market Stock Generation

`Market.InitializeSettlement()` in `lib/Game/Market.cs` needs a new block after the existing trade goods logic that adds 1–3 dispatch items to the catalog. It needs access to a list of all settlements in the world (names, biomes, tiers, sizes, positions) to:

1. Filter to same-tier settlements (tier 1 market → tier 1 destinations, etc.)
2. Exclude the current settlement as a destination
3. Pick a random dispatch def whose biome matches the destination
4. Create stock entries with destination-specific pricing

This means `InitializeSettlement` needs a new parameter: something like `List<SettlementInfo> allSettlements` built from the map at server startup. The map already has everything needed — `node.Poi.Name`, `node.Region.Terrain`, `node.Region.Tier`, `node.Poi.Size`, and `node.X/Y` for computing the 3x3 map slice.

`Market.Buy()` also needs a tweak: when purchasing a dispatch, it must stamp `DestinationSettlementId`, `DestinationDescription`, and `DeliveryPayout` onto the `ItemInstance` before adding it to the haversack. A factory function (similar to the existing `createFood` delegate) would keep this clean.

## Delivery on Settlement Entry

`SettlementRunner.Enter()` in `lib/Orchestration/SettlementRunner.cs` is the delivery hook. After the restock call, scan `player.Haversack` for items where `DestinationSettlementId` matches the current settlement's POI name. For each match:

1. Remove the item from haversack
2. Add `DeliveryPayout` gold to player
3. Collect delivery results for the response

`SettlementData` (same file) needs a new field: `List<DeliveryResult> Deliveries` (dispatch name + gold earned). This flows up to the server response.

## Server Changes

**`server/GameServer/GameResponse.cs`**: Add a `Deliveries` list to `SettlementInfo` so the client knows what was delivered on entry.

**`server/GameServer/Program.cs`**: The `enter_settlement` action handler already maps `SettlementData` → `SettlementInfo`. Just wire the new deliveries field through. The market endpoint needs no changes — dispatches appear as normal stock items.

Server startup needs to build the settlement lookup from `map.json` and pass it into `GameSession` or `Market.InitializeSettlement`. The map is already loaded at startup.

## Map Slice Computation

For the destination description ("a plains town in the west"), compute the 3x3 slice from the destination node's X/Y on the 100x100 map grid:

- Columns: 0–32 = west, 33–65 = midlands, 66–99 = east
- Rows: 0–32 = north, 33–65 = centre, 66–99 = south
- Combine: "north-west", "centre", "south-east", etc. Drop "centre-" prefix for just "midlands" when both are centre.

This is a pure function on node coordinates — no new data needed.

## Payout Pricing

Buy price uses the existing `CostMagnitudes` (Trivial=3, Small=15, Medium=40, Large=80, Huge=200). Delivery payout should be significantly higher — maybe 3–5x the buy price, optionally scaled by BFS distance from origin to destination if that data is available at purchase time. If BFS is too complex to wire in, a flat multiplier per cost tier works fine.

## Web UI Changes

**No new screens needed.** Dispatches fit into existing screens:

- **Market screen** (`src/screens/Market.tsx`): Dispatches appear in the buy list as "Delivery Contract: Sealed Personal Letter". The item description shows the destination clue. May want to visually group dispatches separately from trade goods. The `MarketItem.type` field from the server already distinguishes item types — filter on `"Dispatch"`.
- **Inventory screen** (`src/screens/Inventory.tsx`): Dispatches appear in haversack as "Sealed Personal Letter" with the destination description. They should not be equippable or discardable (or at least warn before discarding). The `ItemInfo` type in `src/api/types.ts` already has a `description` field that can carry the destination clue.
- **Settlement screen** (`src/screens/Settlement.tsx`): On entry, if deliveries occurred, show a notification ("Delivered Sealed Personal Letter — earned 45 gold"). This needs a new `deliveries` field on `SettlementInfo` in `src/api/types.ts`.

## Affected Files Summary

| Layer | File | Change |
|-------|------|--------|
| Rules | `lib/Rules/ItemDef.cs` | Add `ItemType.Dispatch`, add dispatch defs to `BuildAll()`, update `IsPackItem` |
| Game | `lib/Game/ItemInstance.cs` | Add `DestinationSettlementId`, `DestinationDescription`, `DeliveryPayout` |
| Game | `lib/Game/Market.cs` | Generate dispatch stock in `InitializeSettlement`, handle dispatch creation in `Buy` |
| Orchestration | `lib/Orchestration/SettlementRunner.cs` | Delivery scan in `Enter()`, extend `SettlementData` |
| Server | `server/GameServer/GameResponse.cs` | Add deliveries to `SettlementInfo` DTO |
| Server | `server/GameServer/Program.cs` | Wire delivery data through, build settlement lookup from map |
| Web types | `ui/web/src/api/types.ts` | Add `deliveries` to `SettlementInfo`, possibly `destinationDescription` to `ItemInfo` |
| Web UI | `ui/web/src/screens/Settlement.tsx` | Delivery notification on entry |
| Web UI | `ui/web/src/screens/Market.tsx` | Visual grouping for dispatch items (optional) |
| Web UI | `ui/web/src/screens/Inventory.tsx` | Show destination description, prevent discard (optional) |