# Dreamlands Developer Guide

## Build & Run

```bash
# Full solution (all libs + mapgen + CLI)
dotnet build Dreamlands.sln

# Encounter tooling only
dotnet build text/encounter-tool/Encounter.sln

# Generate a world
dotnet run --project mapgen -- generate production

# Run CLI harness
dotnet run --project ui/cli -- --map worlds/production/map.json --bundle /tmp/encounters.bundle.json

# Bundle encounters
dotnet run --project text/encounter-tool/EncounterCli -- bundle text/encounters --out /tmp

# Check encounter syntax
dotnet run --project text/encounter-tool/EncounterCli -- check text/encounters
```

## Solution Structure

```
lib/
  Map/            Shared map types (Node, Region, Poi, Terrain, Direction, MapSerializer)
  Encounter/      Parser + model for .enc format, bundle loader
  Rules/          Enums, action vocabulary, static balance data (BalanceData.Default)
  Game/           Stateless mechanic engine (Mechanics.Apply, SkillChecks, Conditions, Choices)
  Orchestration/  Bridges Game + Map + Encounter (GameSession, Movement, EncounterSelection, EncounterRunner)
  Flavor/         Static flavor text generation (partial — many stubs)
mapgen/           Map generation + SkiaSharp rendering + tile slicing
ui/cli/           Terminal play harness (integration testing, not a real client)
ui/web/           React + Leaflet map viewer (early stage)
text/
  encounters/     .enc encounter files, dungeon content, LLM generation tooling
  encounter-tool/ CLI for check/bundle/fixme/generate commands
  lore/           Locale guides (biome-specific world context for encounter generation)
```

## Project Documentation

All planning, design, and reference docs live in `project/`:

```
project/
  TODO.md             Master work tracker
  design/             Game design decisions, mechanics specs, balance notes
  architecture/       System architecture, tech stack, implementation plans
  screens/            UI screen designs (encounter, explore, inventory, settlement, trade, etc.)
  encounter-spec/     .enc format specification, philosophy, mechanics reference
  reference/          Historical reference (mapgen design, economy, hiking distances, etc.)
```

**Do NOT scatter loose markdown files in lib/, mapgen/, ui/, or other code directories.**
Design docs, specs, plans, and notes go in `project/`. The only markdown that belongs
alongside code is README.md (user-facing) and CLAUDE.md (developer context for that subproject).
If you catch the user creating a new .md file outside `project/`, `text/lore/`,
`text/encounters/generation/`, or `lib/Flavor/biomes/`, flag it and suggest moving it to `project/`.

## Key Conventions

- **Stateless engine**: Game library ops are `(state, args, balance, rng) -> (state, results)`. No side effects.
- **BalanceData.Default**: All balance values are C# constants. No YAML loading at runtime.
- **Encounter format**: Custom .enc token format. See `project/encounter-spec/format.md`.
- **World directories**: All game assets in `worlds/<name>/`. See `ui/web/CLAUDE.md` for details.
- **3-tier system**: Regions assigned Tier 1/2/3 by distance from city. Encounters organized in `{biome}/tier{n}/` directories.
- **21 dungeons**: Hand-crafted, roster-driven placement via `content/dungeons_roster.yaml`.

## Testing

No test projects exist yet. This is a known gap — see `project/TODO.md`.

## Dependencies

- .NET 8.0
- SkiaSharp (mapgen only — image rendering)
- YamlDotNet (mapgen only — dungeon roster, flavor names)
- Anthropic.SDK + Microsoft.Extensions.AI (encounter-tool only — LLM integration)
