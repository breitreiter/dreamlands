# Click-to-Move

We've been deferring click-to-move for a while now because of the inherent complexity of the task.

We've had some design changes which simplify this, so it's worth revisiting. Also leaflet has been making stepwise movement very clunky and unpleasant.

# The Ideal Experience

- Player clicks on a map tile without dragging
- We show a confirmation dialog explaining how long the trip will take
- If the player cancels, we close the dialog and perform no further actions
- If the player approves, we close the dialog
- We draw a ghost line on the map showing the route
- We add map pins to the start and end of the route
- We animate the player's progress along the route (smooth marker movement or animated line)
- Encounters are front-loaded: check the full path before animation starts, truncate the route at the encounter tile
- If there is an encounter, you are ejected from the move flow and returned to the bare map when the encounter is resolved
- If the PC gains the Lost condition during a camp, they are ejected from the move flow (flavor: you got lost on your journey)
- When the PC reaches their destination, clean up the move line and map markers

# Confirmation Dialog

The dialog gates travel with three pre-checks:

1. **Path exists** - A* finds a walkable route avoiding water
2. **Food sufficient** - player has enough food for the journey's camps (3 food per night, journey length / 5 moves per day)
3. **Condition survivable** - pre-simulate the journey's camps given current conditions and inventory (bandages, medicine). If the PC would die en route, block travel: *"You are injured and don't have enough supplies to survive this journey. Select a closer destination."*

If all checks pass, show: estimated travel time (X days, Y moves), food warning if supplies are tight but sufficient.

If the journey is not survivable, the dialog shows the block reason and an **"Abandon Hope"** button. This triggers the existing rescue/respawn flow (teleport to chapterhouse, drop items) — same outcome as dying on the road, without the tedious walk. The travel dialog is the natural place players discover they're stranded, and the place they'll try clicking progressively closer settlements before giving up. No need to surface this elsewhere.

# Decisions

- **Pathing**: A* over the walkable grid. Lakes stay — route around them. If the pathfinding result looks bad in practice, revisit.
- **Encounter check**: front-loaded. One check before animation, truncate path at encounter tile. Simpler implementation, same UX.
- **Night/condition handling**: pre-simulated at the gate. No mid-journey ejection for conditions (Lost is the exception — that's flavor for "your journey was interrupted"). This avoids grinding a dying PC on the road.
- **No mid-journey cancellation**: once confirmed, the journey plays out. Keeps the flow simple — one decision point, one animation, one outcome.

# Complications

Leaflet is brittle and difficult to work with. If we get incredibly lucky and this is a first-class feature of leaflet, let's use it.

If this requires hacking leaflet, consider a more brute-force approach like:
- Zooming the map out so that the full path is visible
- Centering the center of the route
- Locking zoom and panning
- Drawing our own custom layer for the path tracking

This brute-force approach may honestly be the better first approach — skip fighting Leaflet's marker animation API.

---

# Implementation Sketch

## Overview

Three layers of work: pathfinding (lib), journey simulation (server), and animated travel UI (web).

## 1. Pathfinding — `lib/Map/Pathfinding.cs`

A* over the dense grid. Simple, no dependencies.

```
static List<Node>? FindPath(Map map, Node from, Node to)
```

- Heuristic: manhattan distance (no diagonals)
- Neighbor lookup: `map.GetNeighbor(node, dir)` for all 4 cardinals, filter `CanTraverse`
- Returns ordered list of nodes from start to destination (inclusive), or null if unreachable
- Grid is 100x100 — A* over 10k nodes is trivial, no optimization needed

## 2. Journey Pre-Simulation — `lib/Orchestration/JourneyPlanner.cs`

Takes a path and current player state, simulates the journey without mutating anything.

```
static JourneyPlan Plan(PlayerState player, List<Node> path, BalanceData balance, Random rng)
```

**JourneyPlan** contains:
- `Path` — the full node list
- `MoveCount` — total moves
- `Nights` — number of camps (moves / 5, based on time-of-day math)
- `FoodNeeded` — camps that need food
- `FoodAvailable` — food items in haversack + pack
- `Survivable` — bool, would the PC survive?
**Encounter check**: internal only, never exposed to the player. Walk the path, incrementing a shadow copy of `MoveCount` and `NextEncounterMove`. If the cadence triggers, record the move index. This is purely so the server knows the actual journey length and can return the correct `journeyPath` (truncated at the encounter tile) for the client to animate.

**Survival check**: simulate each night's condition drain against current HP and inventory. Conservative estimate (worst-case ambient rolls). If HP hits 0, `Survivable = false`.

**Food check**: count camps, count food items. Straightforward.

## 3. Server Endpoint — New `travel` Action

New action type alongside the existing `move`:

```
POST /api/game/{id}/action
{ "action": "travel", "targetX": 42, "targetY": 17 }
```

**Handler flow**:
1. Look up target node, validate it's on the map and not water
2. `Pathfinding.FindPath(map, currentNode, targetNode)`
3. `JourneyPlanner.Plan(player, path, balance, rng)`
4. If not survivable or no path, return error with reason
5. If survivable, execute: loop through `EffectivePath`, calling `Movement.Execute` + time advancement + EndOfDay for each step. This is the real game loop, same as clicking move N times.
6. Return response with journey summary (nodes visited, encounters hit, camps resolved, final state)

The server does the full execution — no intermediate client round-trips during travel. The client gets back the final state plus a log of what happened, and animates it.

**Response additions** (new fields on GameResponse or a TravelResult DTO):
- `journeyPath` — list of `{x, y}` for the route taken
- `journeyEvents` — ordered list of notable stops (camps, encounters) with move index
- Final state is the normal GameResponse fields (mode, node, exits, encounter, camp, etc.)

## 4. Web UI — Travel Flow in `Explore.tsx`

### Click Detection

Change `DirectionIndicator`'s click handler. Currently it computes a cardinal direction from the click. New behavior:

- Click on map → convert click LatLng to grid coordinates
- If the clicked tile is within 2 tiles (manhattan distance), use existing single-step `move` repeated as needed (preserves quick tap-to-move for nearby tiles without a confirmation dialog)
- If the clicked tile is 3+ tiles away, enter travel flow with confirmation dialog

### Confirmation Dialog

New component: `TravelConfirmDialog`. Before sending the `travel` action:

Send a plan request to the server that returns path validity, distance, food/survival status — but NOT encounter info (encounters are hidden from the player). On confirm, send the real `travel` action.

Dialog shows:
- Destination name (settlement name or terrain + coordinates)
- Estimated travel: "3 days, 14 moves"
- Food status: "You have enough food" or "Warning: you'll run low on food by day 2"
- Block message if not survivable

### Animation

On travel confirmation + server response:

1. Lock map interaction (disable clicks, keyboard movement)
2. Zoom/pan to fit the full route in view
3. Draw the ghost line (Leaflet polyline, dashed, muted color) along `journeyPath`
4. Animate progress:
   - Move player marker along the polyline nodes, one tile per ~200ms
   - Draw a solid line behind the marker showing completed path
   - Pause briefly at journey events (camps, encounters) with a small indicator
5. On encounter: stop animation, transition to encounter screen. Route cleanup happens when encounter resolves.
6. On arrival: brief pause, clean up ghost line and pins, unlock interaction, show destination state

### State Machine

The travel flow is a simple linear state machine in Explore.tsx:

```
idle → confirming → traveling → idle (or encounter)
```

- `idle`: normal map interaction
- `confirming`: dialog open, waiting for player input
- `traveling`: animation playing, input locked
- Transition back to `idle` on arrival, or to encounter/camp screen if interrupted
