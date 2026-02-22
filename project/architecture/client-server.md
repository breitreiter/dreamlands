# Client-Server Architecture

## Overview

The game runs as a stateless REST API (ASP.NET 8.0 minimal API) with a React/TypeScript
frontend. Every player action is a single HTTP round-trip: the client sends an action, the
server loads state from disk, applies the action, saves, and returns a full state snapshot.

There is no WebSocket, SignalR, or server-push channel. The client never mutates game state
locally — it only renders whatever the server sends back.

## API Shape

### Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/game/new` | Create a new game. Returns `{ gameId, state: GameResponse }` |
| GET | `/api/game/{id}` | Reload current state (e.g. after browser refresh) |
| POST | `/api/game/{id}/action` | Dispatch any game action |
| GET | `/api/game/{id}/market` | Fetch settlement market stock + sell prices |

All game actions go through the single `/action` endpoint. The `ActionRequest` body
discriminates on the `action` field:

```
{ action: "move",             direction: "north" }
{ action: "choose",           choiceIndex: 1 }
{ action: "enter_dungeon" }
{ action: "end_encounter" }
{ action: "end_dungeon" }
{ action: "enter_settlement" }
{ action: "leave_settlement" }
{ action: "buy",              itemId: "sword_iron" }
{ action: "sell",             itemId: "rope" }
```

See `server/GameServer/GameResponse.cs` for the full DTO definitions and
`server/GameServer/Program.cs:303-546` for the action dispatch switch.

### Response Shape

Every action returns a `GameResponse` discriminated by `mode`:

```typescript
interface GameResponse {
  mode: "exploring" | "encounter" | "outcome" | "at_settlement" | "game_over";
  status: StatusInfo;        // always present: health, spirits, gold, time, skills, conditions
  inventory?: InventoryInfo; // present in all modes except game_over

  // mode-specific fields (only the relevant ones are populated):
  node?: NodeInfo;           // exploring, at_settlement
  exits?: ExitInfo[];        // exploring
  encounter?: EncounterInfo; // encounter
  outcome?: OutcomeInfo;     // outcome
  settlement?: SettlementInfo; // at_settlement
  reason?: string;           // game_over
}
```

The client uses the `mode` field to pick which screen component to render. See
`ui/web/src/api/types.ts` for the TypeScript mirror of these types.

### Market Exception

Buy/sell actions return a different shape — `MarketActionResponse` instead of `GameResponse`:

```typescript
interface MarketActionResponse {
  success: boolean;
  message: string;
  status: StatusInfo;
  inventory: InventoryInfo;
}
```

This is a partial update (status + inventory only, no mode/node/exits). The client patches
it into the current `GameResponse` via spread: `{ ...state, status: result.status, inventory: result.inventory }`.

See `ui/web/src/screens/Market.tsx:47` and `server/GameServer/Program.cs:505-511`.

## Why This Strategy

**Stateless request-response with full snapshots** was chosen because:

1. **Simplicity.** One response shape drives the entire UI. No delta protocol, no event
   sourcing, no conflict resolution. The client is a pure renderer.

2. **Cloud-ready.** The game engine is `(state, action, balance, rng) -> (state, results)`.
   No in-memory session objects survive between requests. This maps directly to Azure
   Functions + Cosmos DB if we ever move off the local file store.

3. **Crash recovery for free.** All state lives on disk. The server reconstructs a
   `GameSession` from `PlayerState` on every request (`BuildSession` in
   `server/GameServer/Program.cs:78-103`). A server restart mid-game loses nothing.

4. **Deterministic RNG.** `new Random(player.Seed + player.VisitedNodes.Count)` in
   `Program.cs:80` means the RNG can be reconstructed from persisted state without storing
   the generator itself.

## Key Files

| File | Role |
|------|------|
| `server/GameServer/Program.cs` | All endpoints, action dispatch, response builders |
| `server/GameServer/GameResponse.cs` | Request + response DTOs |
| `server/GameServer/GameStore.cs` | `LocalFileStore` — JSON file persistence |
| `lib/Orchestration/GameSession.cs` | Stateless session wrapper (player + map + bundle + balance + rng) |
| `lib/Orchestration/Movement.cs` | `GetExits`, `TryMove`, `Execute` |
| `lib/Orchestration/EncounterRunner.cs` | `Begin`, `Choose`, `EndEncounter` |
| `lib/Orchestration/EncounterSelection.cs` | `PickOverworld`, `GetDungeonStart`, `ResolveNavigation` |
| `lib/Game/Mechanics.cs` | 22 action verbs -> `MechanicResult` discriminated union |
| `lib/Game/Choices.cs` | `GetVisible` (filter by Requires), `Resolve` (conditional branches + skill checks) |
| `lib/Game/PlayerState.cs` | Complete mutable game state, JSON-serializable |
| `ui/web/src/GameContext.tsx` | Client state container — `gameId`, `response`, `doAction()` |
| `ui/web/src/api/client.ts` | HTTP client (`newGame`, `action`, `getMarketStock`, `marketAction`) |
| `ui/web/src/api/types.ts` | TypeScript DTOs mirroring the C# response types |

## State Sync Pitfalls

These are the places where client and server state can diverge. If something looks wrong in
the UI after an action, start here.

### 1. Market partial updates bypass the normal flow

Most actions return a full `GameResponse` that completely replaces client state. Market
buy/sell returns a `MarketActionResponse` with only `status` and `inventory`. The client
patches these into the existing `GameResponse` via object spread.

**What can go wrong:** If the server-side buy/sell has side effects beyond status and
inventory (e.g. a future feature that adds a tag, changes mode, or triggers a condition),
the client won't see it. The spread preserves stale values for every field not in the
partial response.

**Mitigation:** Either make buy/sell return a full `GameResponse`, or be very careful that
market actions never have side effects outside `status` and `inventory`.

### 2. Market stock is fetched separately and can go stale

`Market.tsx` fetches stock via `GET /api/game/{id}/market` on mount and again after each
buy/sell (`refreshStock`). Between the buy action response and the stock refresh, the UI
briefly shows old stock quantities. If `refreshStock` fails silently (the catch is empty
at `Market.tsx:38`), the stock stays stale until the user leaves and re-enters the market.

**What can go wrong:** A user could attempt to buy an item that's already out of stock on
the server, or see incorrect prices after a buy that should have changed featured-item
status.

**Mitigation:** Consider returning updated stock in the buy/sell response itself, or at
minimum don't swallow the refresh error.

### 3. Session mode is reconstructed, not stored

The server derives `SessionMode` from `PlayerState` fields each request
(`Program.cs:78-103`):
- `CurrentEncounterId != null` -> InEncounter
- `CurrentSettlementId != null` -> AtSettlement
- `Health <= 0` -> GameOver
- else -> Exploring

The client's `response.mode` string is what it received from the last action. If the user
refreshes the browser, `GET /api/game/{id}` reconstructs mode from the same fields, so
this stays consistent. But if `CurrentEncounterId` gets cleared without the client being
told (e.g. a bug in encounter cleanup), the next `GET` will show Exploring while the
client still thinks it's in an encounter.

### 4. No optimistic updates — but also no loading gates

`doAction` sets `loading: true` during the request (`GameContext.tsx:63`), but individual
screens don't consistently disable all interactive elements while loading. A fast double-
click on a choice button could fire two `choose` actions. The server will process the
second one against already-mutated state, which may produce an invalid choice index or
apply mechanics twice.

**Mitigation:** Disable all action buttons while `loading` is true. Some screens already
do this; audit the rest.

### 5. Encounter state lives in two places during a session

`GameSession.CurrentEncounter` is set in-memory on the server during request processing,
while `PlayerState.CurrentEncounterId` is persisted to disk. If the encounter object
lookup fails (e.g. the bundle was rebuilt and an ID changed), `BuildSession` silently
clears `CurrentEncounterId` (`Program.cs:94`) and drops the player back to Exploring.

**What can go wrong:** A mid-encounter player loads a game after a bundle rebuild. Their
encounter vanishes with no explanation.

### 6. RNG is only approximately deterministic

The seed is `player.Seed + player.VisitedNodes.Count`. Two requests that don't change
`VisitedNodes` (e.g. two consecutive encounter choices) get the same seed, meaning the
same RNG sequence. In practice this is fine because skill checks consume different numbers
of RNG calls depending on the encounter, but it's worth knowing that the "determinism" is
coarse-grained, not call-for-call reproducible.

### 7. File store has no concurrency protection

`LocalFileStore` does plain `ReadAllTextAsync` / `WriteAllTextAsync` with no locking.
Two concurrent requests for the same game ID could read the same state, apply different
mutations, and the last writer wins. This doesn't matter for single-player in a browser
(requests are sequential), but would matter if we add a mobile client or background
timers.

## Planned: Make Settlement UI Client-Side Only

`AtSettlement` is currently a server-side session mode tracked via
`PlayerState.CurrentSettlementId`. This is unnecessary — "is a settlement" is a property
of the current tile, and the server doesn't need to know which screen the client is
showing.

### What changes

**Remove from server:**
- `enter_settlement` / `leave_settlement` actions from the action dispatch
- `CurrentSettlementId` from `PlayerState`
- `AtSettlement` from `SessionMode` and mode reconstruction logic
- `at_settlement` from `GameResponse.mode`, `SettlementInfo` from the response

**Client derives settlement UI from tile data:**
- When `mode` is `"exploring"` and the current node has a settlement POI, the client
  offers settlement UI (market, services) as a local screen transition
- Navigating away dismisses the settlement screen naturally

**Market endpoints validate against tile, not mode:**
- `GET /market`, buy, and sell check that the player's current node has a settlement POI
  rather than checking `session.Mode == AtSettlement`
- `GET /market` takes over lazy-initialization of `SettlementState` (stock, pricing,
  featured items) and restocking — currently done in `SettlementRunner.Enter()`

**Move buy/sell out of the action endpoint:**
- Separate `POST /market/buy` and `POST /market/sell` endpoints instead of routing
  through the generic action dispatch
- Return updated `stock` alongside `status` and `inventory` in `MarketActionResponse`,
  eliminating the stale-stock window (pitfall #2)

**Condition cures move to movement:**
- Some conditions (e.g. cursed) cure on "enter a settlement." With no explicit enter
  action, this triggers during `Movement.Execute` when the destination node has a
  settlement POI. This is arguably more correct — arriving at civilization cures you,
  not opening the shop screen.

### What stays the same

- `PlayerState.Settlements` dictionary — still lazy-initialized per settlement, still
  persists stock/pricing/featured items. Just initialized on first `GET /market` instead
  of on `enter_settlement`.
- All market logic in `Market.cs` — unchanged, just called from different entry points.
- The rest of the action dispatch, mode reconstruction, and response shape.
