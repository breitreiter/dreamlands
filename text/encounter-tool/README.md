# Encounter tool

CLI and library for the encounter fragment format (Format C). See `../encounters/ENCOUNTER_FORMAT.md` for the format spec.

## Layout

- **EncounterLib** — Parser and model. Use in other projects (game, editors) to parse `.enc` files.
- **EncounterCli** — Console app: `check` (validate syntax), `bundle` (emit JSON for runtime), and `fixme` (replace FIXME epilogues with REVIEW using nb).

## File extension

Use **`.enc`** for encounter files. The CLI scans for `*.enc` by default. You can include other extensions during migration, e.g. `--ext .enc,.txt`.

## CLI

From the repo root (parent of `encounter-tool` and `encounters`):

```bash
# Validate all .enc under ./encounters
dotnet run --project encounter-tool/EncounterCli -- check encounters

# Validate with custom path and extensions
dotnet run --project encounter-tool/EncounterCli -- check path/to/encounters --ext .enc,.txt

# Bundle encounters into a single JSON (index + encounters)
dotnet run --project encounter-tool/EncounterCli -- bundle encounters --out ./output

# Print proposed prompts for each FIXME (no API call)
dotnet run --project encounter-tool/EncounterCli -- fixme encounters/03_swamp/The\ Hermit\ of\ Sallow\ Fen.enc
```

Output: `encounters.bundle.json` in the given `--out` directory.

### fixme

Finds lines that start with `FIXME: <summary>` in an encounter file. For each one, calls the Anthropic API to expand the summary into epilogue prose, then replaces the `FIXME:` line with a `REVIEW:` line in the file. Requires an `appsettings.json` with API credentials (same format as nb).

Use `--prompts-only` to preview the prompts without calling the API. Use `--config <path>` to point at a specific config file.

## Bundle format

`encounters.bundle.json` contains:

- **index.byId** — Map from encounter id (filename without extension) to `{ category, encounterIndex }`. Use for direct lookup (e.g. POI: "this map POI is gorgashs_tomb" → `index.byId["gorgashs_tomb"]`).
- **index.byCategory** — Map from category (directory path relative to encounters root, e.g. `00_intro`, `03_swamp`, `poi`) to a list of encounter ids. Use to randomly pick (e.g. "challenge 3 swamp" → `index.byCategory["03_swamp"]` → pick one id, then load by id).
- **encounters** — Array of parsed encounter objects (id, category, title, body, choices with optionText and either branched or single outcome).

Category is derived from the directory: `00_intro/01_Docks_1.enc` → id `01_Docks_1`, category `00_intro`. So you can place POIs in a `poi/` directory and name files by id (e.g. `poi/gorgashs_tomb.enc`).

## Runtime usage

1. **Embed the bundle** — Add `encounters.bundle.json` to your game project as an embedded resource:
   ```xml
   <ItemGroup>
     <EmbeddedResource Include="path\to\encounters.bundle.json" LogicalName="encounters.bundle.json" />
   </ItemGroup>
   ```
2. **Load once at startup** — Read the stream from your main assembly (e.g. `Assembly.GetManifestResourceStream("encounters.bundle.json")`), deserialize to a bundle type that has `index` and `encounters`.
3. **Lookup by id** — `index.byId["gorgashs_tomb"]` gives you `encounterIndex`; `encounters[encounterIndex]` is the encounter.
4. **Random by category** — `index.byCategory["03_swamp"]` gives you a list of ids; pick one at random, then look up by id as above.

You can reference **EncounterLib** in the game only if you need to parse raw `.enc` at runtime (e.g. mods). For bundled content, the JSON is already parsed; you only need a small DTO and the index.

## Library usage

```csharp
using EncounterLib;

var source = File.ReadAllText("encounter.enc");
var result = EncounterParser.Parse(source);
if (!result.IsSuccess)
{
    foreach (var err in result.Errors)
        Console.WriteLine(err);
    return;
}
var encounter = result.Encounter;
// encounter.Title, encounter.Body, encounter.Choices (OptionText, Branched or Single)
```
