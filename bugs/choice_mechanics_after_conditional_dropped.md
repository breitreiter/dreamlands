---
kind: bug
title: "Choice-level mechanics after an @if/@else block are silently dropped by the parser"
state: fixed
created: 2026-08-04
fixed: 2026-08-04
severity: high
status: fixed — parser now keeps choice-level mechanics (`ConditionalOutcome.Mechanics`)
  and `check` hard-fails on any mechanic it would discard. All 21 lines across 8 files
  in 5 arcs are live again with no content edits. See "Resolution".
touches:
  files:
    - lib/Encounter/EncounterParser.cs
    - text/encounters/arcs/forest/the_fugitive/Mareen.enc
    - text/encounters/arcs/forest/the_fugitive/The Camp.enc
    - text/encounters/arcs/forest/the_fugitive/The Outlanders.enc
    - text/encounters/arcs/forest/the_hermitage/Barn with Osric.enc
    - text/encounters/arcs/forest/the_hermitage/Meilin.enc
    - text/encounters/arcs/plains/grainway_station/Aldric on the Scavengers.enc
    - text/encounters/arcs/plains/grainway_station/The Assault.enc
    - text/encounters/arcs/scrub/signal_array/Observe.enc
  features: [encounter-parser, arcs, forge]
provenance:
  author: claude
  found_via: authoring signal_array's _threads.json; `forge thread --coverage`
---

# Mechanics after a conditional block are parsed away

## What happens

When a choice's outcome is an `@if/@else` block and mechanics follow the closing
brace at choice level, the parser discards them. They land in no branch, no
fallback, and no single outcome.

```
* Ask about the knife = The wrapped blade on the table
  @if tag fugitive.knife_truth {
    ...
  } @else {
    ...
  }
  +open "Mareen"          <-- dropped
```

Verified against the shipped bundle, not just by reading the parser:

```
choice: Ask about the knife
  branch mechanics: []
  fallback mechanics: []
  single: None
```

So in the game today that choice **navigates nowhere**.

## Why it happens

`EncounterParser.cs` routes a `+verb` line by parser state: inside a conditional it
appends to `branchMechanics` or `fallbackMechanics`; otherwise to `singleMechanics`.
After the conditional's closing brace, neither the branch nor the single accumulator
is the right target, and the model has nowhere to put it — `Choice` holds a
`ConditionalOutcome` **or** a `SingleOutcome`, with no slot for choice-level
mechanics alongside a conditional (`lib/Encounter/Choice.cs`).

The working convention elsewhere is to repeat the mechanic inside every branch —
`signal_array/Chorik.enc` puts `+open "The Rest Interval"` in both arms, and it
works. So the affected files are written against a reasonable-looking pattern the
parser never supported.

## Scope

21 lines, 8 files, 5 arcs. Detected by scanning for a `+verb` at choice level after
a brace that closes the conditional:

| File | Lines | Dropped |
|---|---|---|
| `the_fugitive/Mareen.enc` | 21, 44, 65, 84, 110, 135, 159, 178 | `+open "Mareen"` ×8 |
| `the_fugitive/The Camp.enc` | 36, 55, 72, 93 | `+open "The Camp"` ×4 |
| `the_fugitive/The Outlanders.enc` | 28, 55, 78 | `+open "The Outlanders"` ×3 |
| `the_hermitage/Barn with Osric.enc` | 71 | `+add_tag hermitage.quiet_soldier_seen` |
| `the_hermitage/Meilin.enc` | 78 | `+open "Tower"` |
| `grainway_station/Aldric on the Scavengers.enc` | 30 | `+remove_tag grainway_assault_possible` |
| `grainway_station/The Assault.enc` | 58, 92 | `+add_condition injured` ×2 |
| `signal_array/Observe.enc` | 17 | `+add_tag signal_array.observed` |

The count is a floor: the scanner reports the first dropped line per choice, so
`Observe.enc:18`'s `+open "The Rest Interval"` is real but uncounted.

Most of these are **hub returns** (`+open "Mareen"` and friends), so the likely
in-game symptom is a hub spoke that dead-ends instead of returning to its hub.

## The real defect

The silence. `encounter check` passes every one of these files. A dropped `+open` is
the difference between a working hub and a dead end, and nothing reports it.

## Fix options

1. **Parser support** — give `Choice` a mechanics list that applies whichever branch
   fires, and route post-brace `+verb` lines there. Matches what the eight files
   already assume, and reads better than duplicating a mechanic into every arm.
2. **Content fix** — push each dropped mechanic into every branch (21 lines becomes
   ~50). No engine change, but it re-asserts a pattern authors keep not writing.

Either way, **`check` must hard-fail (or at minimum warn on) a mechanic it is about
to discard.** That is the part that turns this from a one-time cleanup into a class
that cannot recur.

## Resolution (2026-08-04)

**Option 1, parser support.** No content was edited — all 21 lines work as written.

- `ConditionalOutcome.Mechanics` holds choice-level mechanics. The parser already
  accumulated them in `singleMechanics`; the conditional path in `FinishChoice` simply
  never read it. So the fix also picks up mechanics written *before* the `@if`, which
  were dropped by the same omission.
- They run **in addition to** whichever branch fires, appended after the branch's own,
  which is the order the author wrote. Applied in `Choices.Resolve` and in all three
  picker paths in `EncounterRunner` (success, `@else`, and the untrained pre-roll
  bypass — each builds its outcome separately and each needed it).
- Round-trips through the bundle (`BundleCommand` emits, `BundleLoader` reads).
- `Forge`'s `Thread.AllMechanics` includes them, which is what unblocked walking
  through `Observe`.

**The guard: `ValidateNoMechanicsDropped` in `CheckCommand`.** It counts `+verb` lines
in the source and mechanics in the parsed model and errors on a shortfall. Deliberately
a reconciliation rather than a check for this specific shape — it catches *any* future
drop, whatever its cause. Verified by stashing the parser fix and re-running: it
reports exactly 8 / 4 / 3 on the fugitive files, matching the original scan.

**Verification.** All 157 files pass `check` with zero dropped-mechanic errors. The
previously-lost mechanics are present in the bundle (`Mareen` → `open "Mareen"`;
`Observe` → `add_tag signal_array.observed` + `open "The Rest Interval"`). 510 tests
pass, including 7 new regression tests in `ChoiceLevelMechanicsTests` and
`ChoiceLevelMechanicsResolveTests`.

## Impact on Forge

`forge thread` could not walk through `Observe`, stranding its 4 beats. Resolved: the
`siesta` thread now takes the spoke, and `signal_array` coverage went 56/67 → 60/67.

The remaining 7 uncovered beats are `Grabbed.enc`, a **separate and still-open**
limitation: `Thread.Walk` takes the first branch's navigation, so `Completion`'s
`@else → Grabbed` is unreachable by any thread. Tracked in
`plans/finish_scrub_arcs.md` T5.
