# Plan: CLI Play Harness — Map + Encounters

Plan file: `merry-coalescing-hickey` in `.claude/plans/`

## Context

We have the Game library (stateless mechanics engine), balance loader, encounter parser, and map generator. The next step is wiring them together into a playable CLI that lets you walk a generated map and trigger encounters. This is the "find the fun" harness — rest/food/conditions/trade/settlements are skipped for now.

Architecture: a new **Orchestration lib** (`lib/Orchestration/`) bridges Game + Map (both CLI and future web UI reference it), and a **CLI app at `ui/cli/`** provides the interactive console.

## Phase 0: Factor MapRenderer into Dreamlands.Map

`mapgen/MapRenderer.cs` is the ANSI terminal map renderer. It already only depends on `Dreamlands.Map` types and accepts `TextWriter`, `playerLocation`, and `visitedNodes` parameters — it's practically ready to share. Move it into `lib/Map/` so both mapgen and the CLI reference it without duplication.

**Move: `mapgen/MapRenderer.cs` → `lib/Map/MapRenderer.cs`** — change namespace from `MapGen` to `Dreamlands.Map`.

**Modify: `mapgen/MapGen.csproj`** — already references `Dreamlands.Map`, no change needed.

**Modify: `mapgen/Program.cs`** — remove `using MapGen;` for MapRenderer (now comes from `Dreamlands.Map`).

The SkiaSharp image renderer (`ImageRenderer.cs` + rendering passes) stays in mapgen — it has heavy graphics deps the shared lib shouldn't carry.

## Phase 1: Bundle Loader (in lib/Encounter/)

The Encounter library already owns the model types. The loader deserializes the bundle JSON back into them using `System.Text.Json` (no new deps).

**Modify: `lib/Encounter/Encounter.cs`** — add `Id` and `Category` init properties (parser leaves empty, bundle loader populates).

**New: `lib/Encounter/BundleLoader.cs`**

```
EncounterBundle
  IReadOnlyDictionary<string, BundleIndex> ById
  IReadOnlyDictionary<string, IReadOnlyList<string>> ByCategory
  IReadOnlyList<Encounter> Encounters
  Encounter? GetById(string id)
  IReadOnlyList<Encounter> GetByCategory(string category)

record BundleIndex(string Category, int EncounterIndex)

static BundleLoader.Load(string path) → EncounterBundle
```

Private DTO classes mirror the JSON shape from `BundleCommand.EncounterToJson()`, mapped to existing `Choice`/`ConditionalOutcome`/`SingleOutcome`/`OutcomePart` types.

## Phase 2: Orchestration Library

**New: `lib/Orchestration/Dreamlands.Orchestration.csproj`** — references Game, Map, Encounter, Rules.

### GameSession.cs
Holds loaded data and current state. Created at startup, passed around.

```
GameSession(PlayerState, Map, EncounterBundle, BalanceData, Random)
  SessionMode Mode  { Exploring, InEncounter, GameOver }
  Encounter? CurrentEncounter
```

No separate dungeon mode — `PlayerState.CurrentDungeonId != null` means we're in a dungeon.

### Movement.cs
Static methods using Map adjacency.

```
Movement.GetExits(session) → List<(Direction, Node)>
Movement.TryMove(session, Direction) → Node?
Movement.Execute(session, Direction) → Node   // updates X/Y + VisitedNodes
```

### EncounterSelection.cs
Category mapping + encounter picking.

```
GetCategory(Node) → string
  // Terrain→dir: Plains→plains, Forest→forest, Mountains→mountains, Swamp→swamp, Hills→scrub
  // + Region.Tier → "plains/tier1"

PickOverworld(session, Node) → Encounter?
  // Random from category, skip UsedEncounterIds, null if exhausted

GetDungeonStart(session, dungeonId) → Encounter?
  // "Start" from "dungeons/{dungeonId}"

ResolveNavigation(session, encounterId) → Encounter?
  // In dungeon: within "dungeons/{currentDungeonId}"
  // Otherwise: global ID lookup
```

### EncounterRunner.cs
UI-agnostic encounter flow. Returns step objects the CLI renders.

```
abstract record EncounterStep
  record ShowEncounter(Encounter, List<Choice> VisibleChoices)
  record ShowOutcome(ResolvedChoice, List<MechanicResult>)
  record Finished(FinishReason)

enum FinishReason { Completed, NavigatedTo, DungeonFinished, DungeonFled, PlayerDied }

EncounterRunner.Begin(session, encounter) → ShowEncounter
EncounterRunner.Choose(session, choiceIndex) → EncounterStep
```

Choose flow: Resolve branch → Apply mechanics → check for Navigation/DungeonFinished/DungeonFled/death → return appropriate step.

## Phase 3: CLI Application

**New: `ui/cli/DreamlandsCli.csproj`** — console exe, references Orchestration.

```
Usage: dreamlands-cli --map map.json --bundle encounters.bundle.json --balance path/to/balance/
```

### Program.cs
Load map (`MapSerializer.Load`), bundle (`BundleLoader.Load`), balance (`BalanceData.Load`). Create `PlayerState.NewGame()` at starting city. Run main loop.

### ExploreMode.cs
- Render: region name, terrain, coords, available exits with terrain hints, POI info, status bar
- Input: `n`/`s`/`e`/`w` for movement, `status`, `inv`, `look`, `quit`
- On `PoiKind.Encounter` node: auto-trigger encounter (if pool not exhausted)
- On `PoiKind.Dungeon` node: prompt "Enter {name}? (y/n)", if not already completed
- On `PoiKind.Settlement`: display name, no interaction yet

### EncounterMode.cs
- Render encounter title + body, numbered visible choices
- Read choice number → EncounterRunner.Choose()
- Render outcome text + mechanic result summaries ("Health: -3 (now 17)")
- Handle Navigation (loop to next encounter), DungeonFinished/Fled (back to explore), PlayerDied (game over)

### Display.cs
Shared rendering: word wrap, status bars, mechanic result formatting.

## Phase 4: Solution Integration

- Add `Dreamlands.Orchestration` to `Dreamlands.sln` under `lib/`
- Add `DreamlandsCli` to `Dreamlands.sln` under `ui/`

## File Summary

| File | Action |
|------|--------|
| `mapgen/MapRenderer.cs` → `lib/Map/MapRenderer.cs` | Move + change namespace to `Dreamlands.Map` |
| `mapgen/Program.cs` | Modify: update MapRenderer references |
| `lib/Encounter/Encounter.cs` | Modify: add Id, Category |
| `lib/Encounter/BundleLoader.cs` | New |
| `lib/Orchestration/Dreamlands.Orchestration.csproj` | New |
| `lib/Orchestration/GameSession.cs` | New |
| `lib/Orchestration/Movement.cs` | New |
| `lib/Orchestration/EncounterSelection.cs` | New |
| `lib/Orchestration/EncounterRunner.cs` | New |
| `ui/cli/DreamlandsCli.csproj` | New |
| `ui/cli/Program.cs` | New |
| `ui/cli/ExploreMode.cs` | New |
| `ui/cli/EncounterMode.cs` | New |
| `ui/cli/Display.cs` | New |
| `Dreamlands.sln` | Modify: add 2 projects |

## Scope — NOT building yet

- Rest/food/conditions, trade, settlement services, save/load, flavor text integration

## Verification

1. `dotnet build Dreamlands.sln` — all projects compile
2. Generate a bundle: `dotnet run --project text/encounter-tool/EncounterCli -- bundle text/encounters --out /tmp/test`
3. Use existing or generate a map JSON
4. Run: `dotnet run --project ui/cli -- --map map.json --bundle /tmp/test/encounters.bundle.json --balance lib/Rules/balance`
5. Walk around, hit an encounter node, make choices, see mechanic results
6. Enter a dungeon, navigate rooms, finish or flee
