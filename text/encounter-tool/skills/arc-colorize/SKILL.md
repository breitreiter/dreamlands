---
name: arc-colorize
description: Generate "color" (scene texture as raw, atomic observables) for a decomposed arc's `.enc` files, one scene-level grab-bag pool per file, for the author to curate. Two-stage tool: a per-scene LENS steers GLM-4.5-Air to over-generate, then a Haiku critic culls the slop. Runs via `EncounterCli colorize`. Use after `arc-decompose` produces a structurally-sound skeleton and before `arc-factual` writes verifiable prose.
---

# Arc colorize

Generate **color** — scene texture as short, raw, *atomic observables* — for
each `.enc` in an arc. Output is **one scene-level grab bag per file** (8-20+
candidate lines), written as a `# --- COLOR ---` pool at the top of the file
body. The author curates by keeping the good lines; kept color feeds the
factual pass.

Colorize owns **substance** (what is worth noticing), never **voice** or
**placement**. Lines are raw material, never player-facing. The downstream
pipeline is colorize -> curator selects -> factual integrates -> house-style
applies voice.

This skill is a thin operator's guide to a **tool**, not a task you perform by
hand. The method, prompts, and the discipline live in code
(`EncounterCli/ColorizeCommand.cs`) and were validated in the `../colorize`
spike. See that repo's `FINDINGS.md` and the `project_colorize_method` memory
for the full provenance. Do **not** revert to hand-walking beats or the old
single-stage "wiki voice" prompt — that approach was abandoned for poor output.

## Position in the pipeline

Reads the output of `arc-decompose` (FIXME-stubbed `.enc` files + the bibles
`_cast.md` / `_set.md` / `_scenes.md` + the brief) plus a **per-scene lens**.
Produces the same `.enc` files with one scene-level `# --- COLOR ---` pool each.
`arc-factual` downstream consumes the curated pool as a grab-bag. Step 3 of the
arc pipeline; see `plans/arc_studio.md`.

## How the tool works (two stages)

```
per-scene LENS ┐
arc facts      ┼─► GLM-4.5-Air (v3 prompt, thinking off, temp sweep, over-generate)
the .enc beats ┘        → candidate pool (deduped)
                        → Haiku critic (one cached call/file; cuts simile / argue /
                          invention / anachronism / generic / ornate; light dedup)
                        → scene-level # --- COLOR --- pool of `# []` lines
```

Why two stages: GLM's vivid-writing prior resists prompt prohibition at
generation time, so we **over-generate and gate** rather than fight it in one
prompt. The per-scene lens is the highest-leverage steer — it makes the model
render out-of-frame tech *in the PC's words* instead of a filter deleting it.

## Prerequisites

- **GLM-4.5-Air served at `imp:8080`** (the `LocalLlm` provider in
  `appsettings.json`). Serve with `--jinja`, repeat-penalty off, top-p 1.0,
  min-p 0.01. Confirm: `curl -s http://imp:8080/v1/models`.
- **Anthropic (Haiku) key** in the `Anthropic` provider of `appsettings.json`
  (the filter stage). Without it, the tool writes the raw unfiltered pool and
  says so.
- Generation/sampling knobs live in the `Colorize` block of `appsettings.json`
  (`Temperatures`, `SamplesPerTemp`, `TopP`, `MinP`, `MaxOutputTokens`,
  `EnableThinking: false`, `TimeoutSeconds`). The defaults are the settled
  config; don't tune generation further (thinking-on tested worse).

## Author a lens first (the highest-leverage step)

Before running, write a lens for each scene — *whose eye, what to notice, in
what key*. The tool injects it first and loudest. Two homes, in priority order:

- **`<EncounterName>.lens.md`** sidecar beside the `.enc` (preferred — per scene).
- **`_lens.md`** peer in the arc dir (arc-wide fallback).

A lens has two facets. It is **stance, not facts** (facts are in the bibles/beats):

```markdown
# Lens — <scene>

## Perceptual
Whose eye, with what expertise, looking at what. What they pick out; how they
rename machinery they have no word for, in their own renaissance-era vocabulary.

## Social / tonal
How the world treats the PC here, and the scene's key — the register (warm
pastoral, social dread, human strain, supernatural wrongness) and whether the
off-key note is a rare seam (opening) or the thing being substantiated (a beat
that already asserts a wrong).
```

Worked example (a warm-then-uneasy rest break):

```markdown
## Perceptual
A renaissance-era traveller at a Kesharat work camp on bare scrub rock, the
interval bell just rung. Look at the kettle, the cups, the crew's hands and
postures. Plain materials: tin, canvas, rope, iron. Name any machinery in the
traveller's own words.

## Social / tonal
The crew has just turned warm, making room for a fifth. The off-key register is
a faint seam under the warmth, only where a beat plants it. Mostly aliveness
and warmth here.
```

## Run it

```bash
# inspect the assembled prompt(s) without calling any model (no GLM needed):
dotnet run --project text/encounter-tool/EncounterCli -- \
  colorize text/encounters/arcs/<biome>/<arc> --prompts-only

# generate for the whole arc (GLM + Haiku):
dotnet run --project text/encounter-tool/EncounterCli -- \
  colorize text/encounters/arcs/<biome>/<arc> --audit
```

Flags:
- `--force` — regenerate; removes an existing COLOR pool first. (Default is
  idempotent: a file that already has a pool is skipped.)
- `--prompts-only` — print the assembled prompts; no model calls.
- `--no-filter` — write the raw GLM pool without the Haiku cull (for tuning).
- `--audit` — print each cut line with its reason.
- `--config <path>` — alternate appsettings.json.

A backup `_<file>.enc` is written beside each modified file (gitignored).
`check` stays green after a run (COLOR lines are `#` draft comments).

## Curate the pool

The author keeps good lines and drops the rest. Convention: every line is
written as `# []` (proposed); toggle to `# [x]` to keep. Expect ~70% to be
keepers. Known cull candidates the filter sometimes misses: an anachronism on a
long list (e.g. "laminated activation card" instead of a renaissance phrasing),
a stray generic, a borderline argued line. Dropping a couple of strays from a
20-line grab bag is a curator's job, not a tool bug — the pool is kept wide on
purpose (light dedup, variations preserved).

## Handoff

After a run, tell the author:
- Per file: candidates generated, kept, cut.
- Any file the lens was missing for (color quality drops sharply without one).
- Files where the pool came back thin (suggests the bibles are thin there;
  may warrant a bible patch before factual).

## Follow-ons / notes

- **`arc-factual` reads COLOR per-beat today** (`DraftBlocks.ExtractBlock`).
  The scene-level pool this skill now writes needs factual to be adapted to
  pull from the file-level pool as a grab-bag. Flagged, not yet done.
- The old `colorize.sh` Qwen rig and `inputs/` are gone; the prompt and filter
  rubric now live inline in `ColorizeCommand.cs`.
- Reference + spec preserved at `project/encounter-spec/colorize/` (method,
  taste anchor, and an eval set of strong style exemplars for a future critic).
