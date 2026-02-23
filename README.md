# Dreamlands

An adventure RPG with procedural world generation, narrative encounters, and a stateless game engine. Explore a tiled world of biomes, dungeons, and settlements through a branching encounter system.

## Project Structure

```
dreamlands/
├── server/GameServer/       # HTTP game server (ASP.NET Core)
├── lib/
│   ├── Map/                 # World representation (nodes, regions, terrain, POIs)
│   ├── Encounter/           # Parser + model for .enc encounter format
│   ├── Rules/               # Items, conditions, skills, balance constants
│   ├── Game/                # Stateless mechanic engine (22 action verbs)
│   ├── Orchestration/       # Session bridge (movement, encounters, settlements, camp)
│   └── Flavor/              # Biome-aware flavor text generation
├── mapgen/                  # World map generator + tile renderer
├── ui/
│   ├── cli/                 # CLI client for integration testing
│   └── web/                 # React + Leaflet map viewer (early stage)
├── text/
│   ├── encounters/          # .enc encounter files by biome and tier
│   ├── encounter-tool/      # CLI for check/bundle/generate commands
│   └── lore/                # Biome locale guides for LLM-assisted authoring
├── tests/                   # Unit tests (Encounter, Game, Rules)
├── worlds/                  # Generated world assets (maps, tiles, bundles)
└── project/                 # Design docs, architecture, specs, reference
```

## Getting Started

Requires .NET 8.0.

```bash
# Build everything
dotnet build Dreamlands.sln

# Generate a world (map, tiles, encounter bundle)
worlds/production/build.sh

# Start the game server
dotnet run --project server/GameServer

# Play via the CLI client (auto-starts server if needed)
dotnet run --project ui/cli -- new
dotnet run --project ui/cli -- status
dotnet run --project ui/cli -- move north
dotnet run --project ui/cli -- choose 0
```

## Game Server

The server exposes an HTTP API for all gameplay. State is persisted as JSON save files.

**Gameplay loop:** explore the map, trigger encounters at POIs, visit settlements to trade and rest, manage equipment and conditions, delve into dungeons.

Supported actions: movement (6 directions), encounter choices, dungeon entry/completion, settlement entry/exit, buying and selling at markets, equipping/unequipping/discarding items, and camping (food, medicine, rest).

## Map Generation

Generates navigable world maps as sparse graphs over a terrain grid. Features include noise-based terrain, river systems, settlement placement with trade economies, encounter slot placement, and roster-driven dungeon placement across 5 biomes and 3 difficulty tiers. Output includes a JSON map, a PNG render, and a Leaflet-compatible tile pyramid.

```bash
dotnet run --project mapgen -- generate production
```

## Encounter System

Encounters are written in a custom indent-based format (`.enc`) with branching choices, skill checks, conditional logic, and game mechanic actions. Content is organized by biome and tier, with LLM-assisted authoring tooling.

```bash
# Validate encounter syntax
dotnet run --project text/encounter-tool/EncounterCli -- check text/encounters

# Bundle for game runtime
dotnet run --project text/encounter-tool/EncounterCli -- bundle text/encounters --out /tmp
```

See [encounter-tool/README.md](text/encounter-tool/README.md) for the full CLI reference.

## World Assets

All generated assets live in named world directories under `worlds/`. Each world contains a map, tile pyramid, encounter bundle, and game assets. The web UI serves from the active world via symlink.

```bash
# Build a world (mapgen + asset copy + encounter bundle)
worlds/production/build.sh

# Rebuild without regenerating the map
worlds/production/build.sh --skip-map
```

## Status

The game engine, server, and content pipeline are functional. Map generation produces complete worlds with settlements, dungeons, and encounter slots. The CLI client supports full gameplay loops. The web UI has screen scaffolding but is not yet integrated with gameplay.
