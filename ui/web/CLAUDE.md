# ui/web Developer Guide

## World Directories

All game assets live in named world folders under `worlds/`. The web app serves from the active world via a symlink:

```
ui/web/public/world  ->  ../../worlds/production/
```

A world folder contains everything the client needs:

```
worlds/production/
  map.json          # topology, regions, POIs — tracked in git
  map.png           # full rendered image (gitignored)
  tiles/            # leaflet tile pyramid (gitignored)
  encounters/       # bundled encounter JSON (slot, not yet wired)
  assets/           # future: sprites, audio, etc.
```

**Do not copy loose files into public/.** Everything the app needs comes through the `public/world` symlink. To switch worlds, repoint the symlink.

**Generating a world:**
```bash
dotnet run --project mapgen -- generate production
```

This writes map.json, map.png, and tiles/ into `worlds/production/`. The seed is recorded in `worlds/worlds.yaml` so regeneration is deterministic.

## Stack

- React 19 + TypeScript 5.7, Vite 6
- Leaflet (react-leaflet) for map rendering
- Dev server on port 3000

## Build & Run

```bash
npm install
npm run dev        # vite dev server on :3000
npm run build      # tsc + vite build
```

## Structure

```
src/
  main.tsx              # mount point
  App.tsx               # root: GameProvider + mode-based router
  GameContext.tsx        # game state, doAction(), partial-response merging
  api/
    client.ts           # fetch wrappers for GameServer HTTP API
    types.ts            # TypeScript interfaces matching server DTOs
  calendar.ts           # Imperial calendar formatting
  screens/
    Splash.tsx          # new game / title screen
    Explore.tsx         # map + side panel (movement, POI interaction, inventory toggle)
    Encounter.tsx       # encounter narrative + choices
    Outcome.tsx         # skill check results + mechanic effects
    Settlement.tsx      # settlement services menu
    Market.tsx          # buy/sell with projected inventory/gold
    Camp.tsx            # end-of-day: auto-balanced meal, medicine, threats
    Inventory.tsx       # overlay: equip/unequip/discard actions
    GameOver.tsx        # death screen
    StatusBar.tsx       # top bar: health, spirits, gold, conditions
  components/
    GameMap.tsx          # leaflet map, tiles from /world/tiles/
    CompassRose.tsx      # directional movement + inventory button
    SegmentedBar.tsx     # health/spirits bar with discrete segments
```

## Key Patterns

- **GameContext** provides `doAction()` which merges partial server responses (stripping nulls) so fields like `exits` aren't clobbered by inventory-only responses.
- **Market** uses `isPackType()` to match the server's `IsPackItem` logic (weapon, armor, boots, tool, tradegood → pack; everything else → haversack).
- **Camp** auto-selects a balanced meal (1 protein + 1 grain + 1 sweets) or nothing; no manual food selection UI.
