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
  App.tsx               # root component
  components/
    GameMap.tsx          # leaflet map, tiles from /world/tiles/
```

The app is early-stage — just the map viewer so far.
