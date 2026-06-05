# Dreamlands Developer Guide

## Build & Run

```bash
# Full solution (all libs + mapgen + CLI)
dotnet build Dreamlands.sln

# Encounter tooling only
dotnet build text/encounter-tool/Encounter.sln

# Generate a world
dotnet run --project mapgen -- generate production

# Run GameServer
dotnet run --project server/GameServer

# CLI integration test client (talks to GameServer over HTTP)
dotnet run --project ui/cli -- new                     # create game, save session
dotnet run --project ui/cli -- status                  # GET current state
dotnet run --project ui/cli -- move north              # move direction
dotnet run --project ui/cli -- choose 0                # pick encounter choice
dotnet run --project ui/cli -- inn                     # GET inn/chapterhouse info
dotnet run --project ui/cli -- rest                    # stay one night at inn
dotnet run --project ui/cli -- inn-recover             # full recovery at inn (costs gold)
dotnet run --project ui/cli -- chapterhouse            # free recovery at chapterhouse
dotnet run --project ui/cli -- market                  # GET market stock (when at settlement)
dotnet run --project ui/cli -- market-order '<json>'   # submit market buy/sell order
dotnet run --project ui/cli -- equip hatchet           # equip item from pack
dotnet run --project ui/cli -- unequip weapon          # unequip slot (weapon|armor|boots)
dotnet run --project ui/cli -- discard bodkin          # discard item from inventory

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
ui/cli/           HTTP client CLI for integration testing against GameServer
ui/web/           React + Leaflet map viewer (early stage)
text/
  encounters/     .enc encounter files, dungeon content, LLM generation tooling
  encounter-tool/ CLI for check/bundle/fixme/generate commands
  hauls/          Haul catalog, examples, generation oracle files + prompt template
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
`text/encounters/generation/`, `text/hauls/`, or `lib/Flavor/biomes/`, flag it and suggest moving it to `project/`.

## Key Conventions

- **Stateless engine**: Game library ops are `(state, args, balance, rng) -> (state, results)`. No side effects.
- **BalanceData.Default**: All balance values are C# constants. No YAML loading at runtime.
- **Encounter format**: Custom .enc token format. See `project/encounter-spec/format.md`.
- **World directories**: All game assets in `worlds/<name>/`. See `ui/web/CLAUDE.md` for details.
- **3-tier system**: Regions assigned Tier 1/2/3 by distance from city. Encounters organized in `{biome}/tier{n}/` directories.
- **20 dungeons**: Hand-crafted, roster-driven placement via `mapgen/content/dungeons_roster.yaml`.

## Testing

No test projects exist yet. This is a known gap — see `project/TODO.md`.

## Dependencies

- .NET 10.0
- SkiaSharp (mapgen only — image rendering)
- YamlDotNet (mapgen only — dungeon roster, flavor names)
- Anthropic.SDK + Microsoft.Extensions.AI (encounter-tool only — LLM integration)

## Project substrate (managed by `imp init`)

This repo uses a structured project-knowledge substrate split between
`imp/` (gnome-maintained) and root-level human-owned dirs.
Read substrate content before answering questions about design,
intent, or current behavior.

### What's where

**`imp/` — gnome territory** (imp writes directly under
`imp-gnome <noreply@imp.local>`):

- **`imp/concepts/<topic>.md`** — auto-generated narrative
  synthesis pages. Don't hand-edit; regenerated by `imp tidy`.
- **`imp/_index/`** — per-file/symbol/feature lookup pages.
  Read `imp/_index/by-file/<path>.md` before editing a
  source file for a digest of what to know first.
- **`imp/learnings/`** — discovered knowledge, why-decisions,
  gotchas. Authored by the gnome from notes.
- **`imp/reference/`** — archived external sources (URLs +
  local snippets). Authored by the gnome from notes.
- **`imp/note/inbox/`** — write target for `imp note`. The
  gnome processes captures here into structured entries on
  `imp tidy`.
- **`imp/log.md`** — append-only history.

**Repo root — human territory:**

- **`plans/`** — design intent, specs, in-flight work. Most new
  work starts as a plan in `state: exploring`.
- **`bugs/`** — bug reports.
- **`TODO.md`** — running list.
- **`rules/`** — hard project invariants. Substrate-shaped
  (frontmatter, drift tracking) but human-authored.

For drift semantics per kind, see `imp/_meta/conventions.md`.

### imp proposals

Imp writes its own dir directly. For changes touching root-level
human dirs (`rules/`, `plans/`, `bugs/`, `TODO.md`), imp produces
proposals at `dreamlands.imp-proposals/P-NNN-<slug>.md`. Review
and apply via `/imp-promote`. Auto-approval gradient when Claude
reviews on the user's behalf:

- **Always-safe** (auto-apply): `TODO.md` appends.
- **Claude-approvable**: plan edits and state-flips, new
  exploring plans.
- **Human-required**: any change to `rules/`, deletions, anything
  that loses information.