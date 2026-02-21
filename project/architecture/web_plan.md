# Web UI Plan

## Architecture

Thin server, dumb clients. Server owns all game state and mutations. Clients are presentation layers.

```
┌──────────────┐        fetch/JSON        ┌─────────────────┐       ┌───────────┐
│  React/Vite  │  ←───────────────────→   │  Azure Function  │  ←→  │  Cosmos   │
│  (browser)   │                          │  (GameServer)    │      │  (or file)│
└──────────────┘                          └────────┬────────┘       └───────────┘
                                                   │
┌──────────────┐        fetch/JSON                 │
│  CLI client  │  ←────────────────────────────────┘
│  (terminal)  │                          ┌─────────────────┐
└──────────────┘                          │  Orchestration   │
                                          │  Game / Rules    │
                                          │  Encounter / Map │
                                          └─────────────────┘
```

### Server responsibilities
- Load/save player state per request (Cosmos in prod, local file in dev)
- Rehydrate GameSession, call Orchestration, persist result
- Return structured game data — resolved, filtered, ready to render
- Log score events as side effect of state transitions (for scoreboard)

### Client responsibilities
- Send actions (`move north`, `choose 2`, `buy item`, `equip sword`)
- Render structured data however it wants (layout, animation, typography)
- No game logic, no rule knowledge, no state mutation

### Response shape: structured, not cooked
Server returns typed, filtered, resolved game state. Does NOT dictate layout or formatting.
Client decides presentation. Changing the UI is purely a frontend concern.

Rule: if the server needs game rules to produce it, the server sends it. If it's a presentation choice, the client decides.

## GameServer (Azure Function, isolated process .NET 8)

### Local dev
Azure Functions Core Tools (`func start`). File-based persistence. Vite dev server proxies API calls. No Azure account needed for local dev.

### Persistence interface
`IGameStore` — the one justified interface. Two implementations:
- `LocalFileStore` — JSON files on disk, for dev
- `CosmosStore` — for production

### Endpoints (draft, will refine)
- `POST /api/game/new` → create new game, return initial state view
- `POST /api/game/{id}/action` → perform action, return updated state view
- `GET /api/game/{id}` → current state view (reconnect/refresh)
- `POST /api/game/{id}/restore` → restore from code
- `GET /api/scoreboard` → casual arcade-style scores

### Action format
```json
{ "action": "move", "direction": "north" }
{ "action": "choose", "index": 2 }
{ "action": "buy", "itemId": "iron_sword", "quantity": 1 }
{ "action": "equip", "itemId": "instance_id" }
```

### Response shape
```json
{
  "mode": "exploring | encounter | encounter_outcome | settlement | trade | inventory | game_over",
  "status": { "health": 14, "maxHealth": 20, "spirits": 3, "gold": 42, "timeOfDay": "afternoon" },
  ...mode-specific structured data
}
```

## React App (Vite + TypeScript + shadcn/ui)

### What can start before Figma
- Vite + TypeScript + Tailwind + shadcn scaffolding
- API client module (typed fetch wrapper)
- TypeScript types mirroring server response shapes
- Router shell (mode-based, not URL-based — single page, mode drives what renders)
- Dev proxy config pointing at local func

### What's blocked on Figma
- All actual screen implementations
- Component styling and layout decisions

### What's blocked on Game lib stabilizing
- Detailed response type definitions (inventory shape, settlement services, trade format)
- Anything that assumes specific mechanics or action verbs

## Scoreboard
Server logs score events during state transitions (dungeon cleared, game completed, player died).
Casual 80s arcade cabinet vibe — high scores with initials, not a modern leaderboard.
Data is trustworthy because server computed all state.

## Alternate clients
API contract is the only coupling point. Any client that speaks HTTP + JSON is valid.
- Web: React renders JSON into components
- CLI: renders same JSON into ANSI terminal output
- Future: Discord bot, mobile, whatever

## Auth
Restore codes only. No accounts, no passwords. Start a new game, get a restore code. Keep it secret, keep it safe. Enter it on the splash screen to resume. Server validates the code and returns the session.

## Map rendering
Mapgen already produces gorgeous stippled pen-and-ink PNG maps. No need to redraw in the browser.

- **Map image**: generated at game creation time by mapgen, stored/served by the server. Players see the full raw map — POIs are baked into the image so players see interesting landmarks and want to explore toward them.
- **Graph overlay (TBD)**: non-diegetic overlay tracing the explored parts of the node graph. Shows where you've been and how tiles connect. Design and rendering approach not yet decided.
- **Coordinate mapping**: server provides node pixel positions (known from the hex/grid geometry), client maps these to image coordinates
- **No fog of war**: the map itself is always fully visible. The graph overlay shows exploration progress.

No fast travel for now. If movement and resupply feel painless, it's unnecessary. Revisit only if traversal becomes tedious in playtesting.

## Open questions
- Exact response shapes per mode (design as we build each screen)
- Scoreboard detail — what's the "score"? Dungeons cleared? Days survived? Treasure value?
- **Cold start splash screen** — Azure Functions have cold start latency. Need a fancy splash/loading screen so the first request doesn't feel broken. Could double as the game's title screen — show art, title, atmosphere while the function warms up in the background.

## Build order (rough)
1. GameServer scaffold — empty Azure Function, local dev working, IGameStore with LocalFileStore
2. First endpoint — new game + get state (exploring mode only)
3. React scaffold — Vite + shadcn + types + API client + dev proxy
4. First screen — exploring (after Figma design)
5. Movement endpoint — move action, return new exploring state
6. Encounter flow — encounter + outcome endpoints/screens
7. Expand from there as Game lib features land
