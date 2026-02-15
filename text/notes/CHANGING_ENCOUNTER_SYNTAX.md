# Changing the encounter syntax

How-to guide for adding or modifying encounter language features.

## Architecture overview

The encounter system has four layers. Changes ripple through them in order:

1. **Parser** (`lib/Encounter/EncounterParser.cs`) — Reads `.enc` text, produces model objects. Stateless, no file I/O.
2. **Model** (`lib/Encounter/Choice.cs`, `Encounter.cs`, etc.) — Data classes the parser emits.
3. **Vocabulary & validation** (`lib/Rules/ActionVocabulary.cs` + sibling files) — Defines legal verbs and argument types. Used by `CheckCommand` at validation time, not at parse time.
4. **Serialization** (`EncounterCli/BundleCommand.cs`) — Converts parsed model to JSON for the game runtime.

There are also two secondary consumers:
- **Post-processor** (`EncounterCli/EncounterPostProcessor.cs`) — Converts raw LLM output into valid `.enc` format. Only relevant if the LLM generation pipeline should produce the new syntax.
- **Format spec** (`text/notes/ENCOUNTER_FORMAT.md`) — Human-readable grammar reference.

## Touch points by change type

### Adding a new `+command` (game mechanic)

Simplest case. Only the vocabulary layer needs a change.

1. **`lib/Rules/ActionVocabulary.cs`** — Add a new `ActionVerb` static field with `VerbUsage.Mechanic`, the verb name, and `ArgDef` entries. Add it to the `All` array.
2. **`text/notes/ENCOUNTER_FORMAT.md`** — Add a row to the "Game commands" table in section 5.
3. **Test**: `dotnet run --project encounter-tool/EncounterCli -- check encounters` from `text/`. Existing encounters should have the same error count as before (no regressions). Write a scratch `.enc` file using the new command and confirm it passes.

No parser, model, or bundle changes needed — `+commands` are stored as raw strings.

### Adding a new argument type

Example: adding a `Direction` type with values `north`, `south`, `east`, `west`.

1. **`lib/Rules/`** — Create a new file (e.g. `Direction.cs`) following the pattern in `Skill.cs` or `Magnitude.cs`. Define a static registry with `IsValidScriptName`.
2. **`lib/Rules/ActionVocabulary.cs`** — Add a new `ArgType` enum member. Add a validation case in `ValidateArg`.
3. **`text/notes/ENCOUNTER_FORMAT.md`** — Add a row to "Argument types" in section 5.

### Adding a new condition type

Conditions are used in `@if`/`@elif` blocks and `[requires]` tags. The parser is generic — any `@if <condition> {` parses, since it just captures everything between `@if` and `{` as the condition string. The change is purely in validation.

1. **`lib/Rules/ActionVocabulary.cs`** — Add a new `ActionVerb` static field with `VerbUsage.Condition`. The condition verb name (e.g. `has`, `tag`, `check`) is the first word; remaining words are validated as arguments.
2. **`EncounterCli/CheckCommand.cs`** — Already validates all branch conditions and `[requires]` against the vocabulary. No change needed unless the condition has special semantics.
3. **`text/notes/ENCOUNTER_FORMAT.md`** — Add a row to the "Conditions" table in section 5.

### Adding a new flow-control keyword

The current flow-control keywords are `@if`, `@elif`, and `@else`. Adding a fundamentally new keyword (not a new condition type) is rare and involves the parser's state machine:

1. **`lib/Encounter/EncounterParser.cs`** — Add `StartsWith` detection for the new keyword. The state machine tracks `braceDepth`, `inConditional`, `inFallback`, and the `branches` list.
2. **`lib/Encounter/Choice.cs`** — If the new keyword has fundamentally different semantics (not conditional branching), add a new model class.
3. **`EncounterCli/BundleCommand.cs`** — Add serialization for any new model classes.
4. **`text/notes/ENCOUNTER_FORMAT.md`** — Document in sections 2, 3, and 6.

### Modifying how choices or options work

1. **`lib/Encounter/EncounterParser.cs`** — Choice boundaries at `* ` prefix. Option text parsing (link/preview split, `[requires]` stripping) is in `ParseOptionLinkPreview` and `StripRequires`.
2. **`lib/Encounter/Choice.cs`** — Add/modify properties on `Choice`.
3. **`EncounterCli/BundleCommand.cs`** — Update `EncounterToJson` to serialize new fields.

## How to test changes

From the `text/` directory:

```bash
# Check all existing encounters for regressions
dotnet run --project encounter-tool/EncounterCli -- check encounters

# Check a single file
dotnet run --project encounter-tool/EncounterCli -- check encounters/path/to/file.enc

# Bundle and inspect JSON output
dotnet run --project encounter-tool/EncounterCli -- bundle encounters --out /tmp/bundle-test
# Then inspect /tmp/bundle-test/encounters.bundle.json
```

Compare error counts before and after your change. The existing encounters have some pre-existing validation errors (wrong arg types in older files) — those are a known baseline, not regressions.

## Key design decisions

- **The parser is deliberately dumb.** It recognizes four sigils (`*`, `@`, `}`, `+`) and braces. It doesn't validate argument values — that's the vocabulary layer's job. This means many new features only need vocabulary changes.
- **Conditions are raw strings.** The parser stores `"check negotiation medium"` or `"has torch"` (whatever follows `@if`/`@elif`), not a structured object. The game runtime and the validator each parse this string their own way. This keeps the parser simple but means the runtime must also understand the vocabulary.
- **One `@if` per choice.** The parser enforces this. Multiple branches within that `@if` are handled via `@elif`.
- **`@else` is optional.** An `@if` block without `@else` just has no fallback branch.
- **`[requires]` is choice-level gating, not outcome branching.** It controls whether the choice appears at all, while `@if`/`@elif`/`@else` controls what happens after the player picks the choice.
