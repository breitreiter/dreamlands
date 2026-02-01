# encounter-tool Developer Guide

## What This Is

CLI and library for authoring, validating, and bundling narrative encounters for the Dreamlands RPG. Encounters are written in a custom indent-based format (`.enc` files), validated, optionally refined with LLM assistance, and bundled into JSON for the game runtime.

## Project Structure

```
encounter-tool/
├── Encounter.sln
├── EncounterCli/
│   ├── Program.cs             # CLI dispatcher (top-level statements)
│   ├── CheckCommand.cs        # Validate .enc files
│   ├── BundleCommand.cs       # Parse + bundle to JSON
│   ├── FixmeCommand.cs        # LLM-expand FIXME stubs
│   ├── GenerateCommand.cs     # Oracle-driven encounter generation
│   └── LlmClient.cs           # Anthropic SDK wrapper
```

The parser library lives at `lib/Encounter/` (namespace `Dreamlands.Encounter`, zero dependencies). EncounterCli references it and adds the CLI + LLM integration.

## Build & Run

```bash
# From encounter-tool/
dotnet build

# From the text/ directory (parent of encounter-tool/ and encounters/)
dotnet run --project encounter-tool/EncounterCli -- check encounters
dotnet run --project encounter-tool/EncounterCli -- bundle encounters --out ./output
dotnet run --project encounter-tool/EncounterCli -- fixme encounters/swamp/The\ Hermit.enc --prompts-only

# Generate: run from a locale directory containing locale_guide.txt
cd encounters/forest/tier1
dotnet run --project ../../../encounter-tool/EncounterCli -- generate --out generated.enc
```

## CLI Commands

- **check** `[path] [--ext .enc,.txt]` — Validate .enc files for syntax errors. Exit 1 if any fail.
- **bundle** `<path> [--out dir] [--ext .enc,.txt]` — Parse all encounters, emit `encounters.bundle.json` with index (byId, byCategory) and parsed encounter data.
- **fixme** `<file.enc> [--config path] [--prompts-only]` — Find `FIXME:` lines, call LLM to expand them into prose, replace with `REVIEW:` lines.
- **generate** `[--template file] [--out file] [--config path] [--prompts-only]` — Generate an encounter from oracle fragments + locale guide via LLM. Looks for `locale_guide.txt` in cwd. Oracle fragments always at `text/encounters/generation/`.

## Encounter Format (.enc)

Full spec: `../notes/ENCOUNTER_FORMAT.md`

Quick summary: Title on line 1, prose body, then `choices:` at column 0. Choices at 2-space indent, outcomes at 4-space, branched content ([if]/[else]) at 6-space. Mechanics in brackets (e.g. `[skill_check persuade 15]`, `[give_item torch]`, `[branch other_encounter]`).

## Bundle Format

`encounters.bundle.json` contains:
- `index.byId` — encounter id (filename stem) -> `{ category, encounterIndex }`
- `index.byCategory` — directory name -> list of encounter ids
- `encounters[]` — parsed encounter objects

Categories derive from directory structure. The game uses category for random encounter selection (by biome/challenge level) and id for named POI encounters.

## LLM Configuration

`appsettings.json` in the project directory (copied to output on build):

```json
{
  "ActiveProvider": "Anthropic",
  "ChatProviders": [
    { "Name": "Anthropic", "ApiKey": "sk-ant-...", "Model": "claude-sonnet-4-5-20250929" }
  ]
}
```

Loaded from next to the executable by default. Use `--config <path>` to override.

## Encounter Authoring Pipeline

1. **Skeleton** — Author writes .enc file with structure, choices, and mechanics. Prose outcomes are stubbed with `FIXME: brief description of what happens`.
2. **Expand** — `fixme` command calls LLM to expand each FIXME into prose. Output is marked `REVIEW:`.
3. **Review** — Author reads REVIEW lines, edits as needed, removes the REVIEW prefix.
4. **Validate** — `check` command confirms syntax is clean.
5. **Bundle** — `bundle` command packages everything into JSON for the game build.

## Encounter Generation

Oracle-driven generation using the `generate` command:

- **Oracle fragments** — Three text files (Situation, Forcing, Twist) with ~30-40 one-line narrative seeds each, at `text/encounters/generation/`. A random combination of three fragments shapes each encounter.
- **Locale guide** — Region-specific context (setting, tone, scene palette, constraints). Always `locale_guide.txt` in the current working directory.
- **Prompt template** — `encounter_prompt.md` in the generation directory. Substitutes oracle picks + locale guide. LLM generates the encounter body + selects applicable player actions, then a second pass generates epilogues.

## Architecture Notes

- All files use `namespace EncounterCli;`. Program.cs has a standard `Main` entry point. Each command is a static class with `Run` or `RunAsync`.
- The parser is stateless — `EncounterParser.Parse(string)` takes raw text and returns a `ParseResult`. No file I/O in the library.
- LlmClient wraps the Anthropic SDK via Microsoft.Extensions.AI. Used by both fixme and generate commands.

## Adding a New Command

1. Create `FooCommand.cs` with `namespace EncounterCli;` and a static `Run`/`RunAsync` method returning `int`.
2. Add a case in the switch expression in `Program.cs`.
3. Add usage line to `PrintUsage()`.

## Future Work

- **Mechanic vocabulary validation**: The `check` command should validate bracket actions against a defined vocabulary. Currently any `[verb args]` is accepted.

## Dependencies

**Dreamlands.Encounter**: None (pure .NET 8.0)

**EncounterCli**:
- `Anthropic.SDK` 5.5.3
- `Microsoft.Extensions.AI` 9.9.1
- `Microsoft.Extensions.Configuration` + `.Json` + `.Binder` 9.0.0
