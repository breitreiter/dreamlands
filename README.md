# Dreamlands

An adventure RPG with procedural world generation and narrative encounters.

## Project Structure

```
dreamlands/
├── mapgen/          # World map generator (terrain, rivers, settlements, dungeons)
├── lib/
│   ├── Map/         # Shared map types (nodes, regions, terrain, POIs)
│   ├── Encounter/   # Parser for .enc encounter format
│   └── Rules/       # Game rules, skills, balance data
├── text/
│   ├── encounters/  # Encounter content by biome and tier
│   ├── encounter-tool/  # CLI for authoring, validating, and bundling encounters
│   └── enc-vscode/  # VS Code extension for .enc syntax highlighting
└── Dreamlands.sln
```

## Map Generation

Generates navigable world maps as sparse graphs over a terrain grid. Features include noise-based terrain generation, river systems, settlement placement, and roster-driven dungeon placement across biomes and difficulty tiers.

```bash
cd mapgen
dotnet run
```

Outputs a PNG map render and a JSON map file.

## Encounter System

Encounters are written in a custom indent-based format (`.enc`), validated, optionally expanded with LLM assistance, and bundled into JSON for the game runtime.

```bash
cd text

# Validate encounter files
dotnet run --project encounter-tool/EncounterCli -- check encounters

# Bundle for game runtime
dotnet run --project encounter-tool/EncounterCli -- bundle encounters --out ./output

# Generate encounters using oracle fragments + locale guides
cd encounters/forest/tier1
dotnet run --project ../../../encounter-tool/EncounterCli -- generate --out generated.enc
```

See [encounter-tool/README.md](text/encounter-tool/README.md) for the full CLI reference.

## Building

Requires .NET 8.0.

```bash
dotnet build Dreamlands.sln
```

## Status

Work in progress. The map generator produces playable world maps. The encounter authoring pipeline is functional. The game client is not yet implemented.
