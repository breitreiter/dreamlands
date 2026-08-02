# Arc authoring flow — the complete reference

How an arc goes from an idea to finished prose in the game bundle.

This is the map. It does not restate the rules that live in the skills and specs it
cites — each of those stays the single source of truth for its own stage. Read this
to know **what the stages are, what each one costs, what it needs, and where you
are**; read the cited doc to actually run one.

> **If you have been away a while, jump to [§6 Cold start](#6-cold-start).**
>
> **Do not use `plans/arc_authoring_workflow.md`.** It describes the retired
> EncounterCli `colorize → factual → voice → critic` pipeline, which was deleted in
> the June 2026 Forge pivot. It is kept only for its two unresolved design questions.

---

## 1. Vocabulary

The terms the tooling assumes you know.

| Term | What it is |
|---|---|
| **Arc** | A multi-scene narrative unit under `text/encounters/arcs/<biome>/<arc>/`. 20 of them, placed by `mapgen/content/dungeons_roster.yaml`. |
| **Sketch / brief** | The Stage-0 `.md` document: premise, Canon (NPC interiority, motives, the fact-set sorted surface vs reserved), and every scene written as PC experience. |
| **Bible** | A shared reference file for the whole arc: `_scenes.md` (scene breakdown), `_cast.md` (characters), `_set.md` (physical staging), `_color.md` (**recurring** motifs only). |
| **Lens** | Per-scene `<Scene>.lens.md`. Stance, not facts: whose eye, what to notice, in what key. The highest-leverage steer into color. |
| **Beat** | One `FIXME:` stub in an `.enc` — the atomic unit of generated prose. An arc has tens (villa 39, signal_array 67). |
| **Tone tag** | `FIXME(dread):` — one of six: `dread`, `horror`, `wonder`, `mundane`, `action`, `revelation`. A soft steer into color. Written by hand or by `categorize`. |
| **Peer JSON / sidecar** | `X.enc.json` beside each `.enc`. Holds every beat's stub plus each stage's output under `stages[<phase>]`. **This is the work.** Gitignored. |
| **Color bank** | The gemma-generated pool of texture for a beat (`stages.color`). Vivid, produced *without* reasoning, so it asserts things that aren't true — which is what the review gate and the integrator exist to catch. |
| **Thread** | A named, ordered list of choice labels tracing one playthrough path through the arc's graph. Weave walks it and generates each beat with the prior finished prose fed forward. |
| **`final` / `approved`** | Per-beat fields in the sidecar. **No tool writes them** — they are the human curation layer. `integrate` splices only approved beats. |
| **Promotion** | The one destructive step: copying integrated output over the canonical `.enc`. |

---

## 2. Stage 0–1 — Substrate (the layer that leads)

**Governing rule: cheap `.md` leads, hard-to-reverse `.enc` follows.** Co-evolution
is fine, but land structural and dramatic decisions in the `.md` layer before baking
them into `.enc`. Reversing a bad decision costs minutes upstream and hours down.

| Produce | Skill |
|---|---|
| Sketch/brief — Canon + PC-POV scenes | `arc-sketch` |
| `_scenes.md`, `_cast.md`, `_set.md`, `_color.md` | `arc-sketch` |
| One `<Scene>.lens.md` per scene | `arc-sketch` |

Three things that reliably go wrong, all documented in the skill:

- **The governing physical condition gets established once and forgotten.** Dark
  factory with one lamp in scene 1, ordinary lighting by scene 4.
- **Environmental reality gets dropped.** Machines are loud, deserts are bright,
  ruins have vermin, markets have overheard talk.
- **`_color.md` accumulates one-offs.** It is for **recurring** motifs only;
  single-scene color belongs to the lens.

**Exit criterion:** every scene in `_scenes.md` has a lens, and all four bibles
exist. A missing lens means the color stage runs blind for that scene.

## 3. Stage 2 — Skeleton

**Skill: `arc-decompose`.** Turns the substrate into `.enc` files with FIXME-stub
prose. `_scenes.md` is the primary structural input.

Produces the **skeleton only**: legal syntax, no orphan tags, no infinite loops, hubs
wired to the patterns in `project/encounter-spec/arc_patterns.md`. Prose quality is
explicitly not its job.

Stubs are **factual**, not literary — no similes, no metaphor, no editorial framing.
The one exception: spoken lines are stubbed as actual quoted words. Downstream stages
write the prose; a stub that already contains prose contaminates them.

**Exit criterion:** `encounter check` passes, and every scene maps to a file.

## 4. Stage 3 — Forge (skeleton → prose)

**Runbook: `text/encounter-tool/skills/forge-run/SKILL.md`.**
**Internals: `text/encounter-tool/Forge/CLAUDE.md`.**
**Procedure + safety: `plans/scrub_arc_processing.md`.**

`forge` = `dotnet run --project text/encounter-tool/Forge --`

### The safety guarantee

**The inbound `.enc` is never modified.** Every stage writes only into the sidecar;
`integrate` splices approved prose into a *fresh* `.enc` under `out/`. Line numbers
are the splice key and stay stable precisely because the source is never rewritten —
`integrate` enforces this with a `source_sha` guard that aborts on drift.

So the generation loop is low-risk by construction. **All the rigor belongs at the
color gate and at promotion**, not at protecting the source.

### The stages

| # | Stage | Cost | Where | Notes |
|---|---|---|---|---|
| 1 | `parse` | free | local | `.enc` → sidecar. With nothing approved, `integrate` round-trips byte-identically. |
| 2 | `categorize` | free | GLM via router | Tags untoned beats with one of six tones. Router auto-loads `glmchat`; no `swap-model` dance. Tone is a soft steer — don't fuss marginal calls. |
| 3 | `color` | free | **imp loom, over ssh** | Gemma triplet enriches each beat. Needs the **whole box**: `ssh imp '~/.local/bin/swap-model stop all'` first. Smoke one beat (`--limit 1`) before the full run. Resumable, per-beat save. |
| 4 | **★ review ★** | free | local | **The gate. Do not proceed until it passes.** See below. |
| 5 | *threads* | free | authoring | Author the covering set. See below. |
| 6 | `weave` | **PAID** | Cloudflare via router | Threaded story-so-far synthesis, one thread at a time. ~58s/beat with kimi+thinking. Resumable. |
| 7 | *curate* | free | manual | Read the prose; cull and **repair**. Set `final` + `approved` by hand. |
| 8 | `integrate` | free | local | Approved beats spliced → `out/<arc>/*.enc`; unapproved keep their FIXME. |
| 9 | *promote* | free | manual | Copy over the source. The one risky step. |

`synthesis` (isolated per-beat, no story-so-far) also exists. **It is for smoke and
dry-checks only** — it produces prose we do not ship.

### The color gate (stage 4) — where the quality actually comes from

The governing discipline: **kimi assembles beautifully from clean input and amplifies
dirty input.** So stop at the last free artifact and verify it before spending.

`forge review "$A"` renders each beat's stub, tone, and color bank. Hand-critique it:

- **Flag** hard misreads — referent, identity, medium, and invented-entity *swaps*.
  The color model reasons not at all; it will confidently say the wrong thing.
- **Keep** interpretive, tonal, and atmospheric invention. That is the point of the
  stage — color exists to break the "Claude-tic" contamination that decompose bakes
  into the stubs, by injecting genuinely alien texture.

Then re-color or accept. Careful prep here makes the paid assembler a rubber stamp
instead of a repair-and-re-run loop.

### Threads (stage 5)

A thread is an ordered list of choice labels — one playthrough path. Weave walks it
and generates each beat **with the prior finished prose fed forward**. That
story-so-far is the dominant quality lever: it took the villa from ~50% rewrites to
ship-quality, and it is a stronger POV and fact anchor than any prompt rule.

**Every beat must be on at least one thread.** Threads therefore form a *covering
set*, and authoring them means adding paths until nothing is left out — alternate
endings, decline off-ramps, every hub sub-branch. The villa needed five, of which
`arson` exists purely as a grand tour to sweep up beats the other four missed. After
weaving, run the coverage check; if a beat is uncovered, **author another thread** —
do not fall back to isolated synthesis.

> **Current wart:** threads live in `Forge/Threads.cs` as hardcoded C#, so authoring
> content for a new arc means editing source and rebuilding. Moving them to a per-arc
> `_threads.json` is T3 of `plans/finish_scrub_arcs.md`.

### Two gotchas that will cost you money

- **Weave reuses any existing non-empty `synthesis_sofar` cell.** So if you edit a
  beat's color *after* it was woven, a plain re-weave silently `[reuse]`s the stale
  cell and your fix never lands. Re-weave affected beats with `--force`. **A render
  showing all `[reuse]` and no generation means nothing new was produced** — if you
  expected your edits to appear, that is the smell.
- **Daily caps are fatal, not transient.** minrouter enforces a per-upstream daily
  cap and returns HTTP 429 with `daily limit for '<upstream>' reached`;
  `RouterClient` raises `QuotaExhaustedException` so the run aborts rather than
  backing off into the same wall. Transient 429/5xx still retry with backoff.

### Curation (stage 7) is repair, not a checkbox

`final` and `approved` are written by **no tool** — the sidecar is an edit surface.
For each kept beat, set `final` to the chosen *or hand-repaired* prose and
`approved: true`. Culling alone is not curation; slightly-wrong beats get fixed by
hand. (`forge lock` is deferred until the UX is felt across a few arcs — we do this
~13 times, not 100.)

### Promotion (stage 9)

1. `encounter check out/<arc>`; diff each output against source — should be **only**
   FIXME→prose swaps.
2. Copy `out/<arc>/*.enc` over the canonical files on a branch.
3. `check` + `bundle` to confirm the game still loads it.
4. Commit. **Never push or merge without an explicit ask.**

---

## 5. Where state lives

The thing most likely to mislead you on resume.

| Artifact | Location | Tracked? |
|---|---|---|
| Source `.enc`, substrate `.md` | the arc dir | **yes** |
| Sidecars `*.enc.json` | **beside the arc path you pass** | no — gitignored |
| Integrated output | **`out/` relative to your cwd** | no — gitignored |
| Router token | `$MINROUTER_KEY` env var | never on disk |
| `appsettings.json` | next to the binary, optional | no — gitignored |

Neither sidecars nor `out/` has a fixed home, so an arc's real state can sit outside
this repo entirely while a stale copy sits where you would naturally look. This bit
the villa: live state was `~/repos/narr/forge/arcs/the_villa/` (the original Python
forge's own tracked corpus) at 39/39, while the in-repo sidecars held a stale 33/39
and the integrated output had landed under `text/encounter-tool/out/` because that
was the cwd — with an older `out/compare/` at the repo root too. Four plausible
locations, one current.

**So never trust a sidecar's existence as state.** Count beats with a non-empty
`stages.synthesis_sofar` and with `final` + `approved`, per file. Check `source_sha`
against the in-repo `.enc` — if it matches, the two copies' *sources* agree and
`integrate`'s guard will pass even when their sidecars differ wildly.

**Curated sidecars are an artifact, not a cache.** They hold paid weave output *and*
human judgment. Don't check whether they are under version control — check whether
the curated state is actually **committed**.

---

## 6. Cold start

Resuming an arc after a gap, in order:

1. **Locate the real state.** Find every `*.enc.json` for the arc, anywhere. Count
   woven and approved beats per file. Do not go by mtime.
2. **Check substrate completeness.** Four bibles? A lens per scene? A thread set?
3. **`encounter check`** the arc. Warnings are FIXME markers (expected until
   promotion); errors are not.
4. **Find the frontier** — the first stage with no output in the sidecar. Stages are
   independent and idempotent; a missing upstream key reads as a legible "run that
   phase first," never a silent skip.
5. **Resume.** Free stages can simply be re-run. Before any **paid** re-run, confirm
   the reuse rule above so you aren't paying for `[reuse]` or silently keeping stale
   cells.

### Per-arc checklist

```
[ ] sketch/brief written, PC-POV, governing condition carried through
[ ] _scenes / _cast / _set / _color present
[ ] one *.lens.md per scene
[ ] .enc skeleton; encounter check clean
[ ] every FIXME tone-tagged
[ ] parse    -> sidecars exist
[ ] categorize
[ ] color    (smoke 1 beat, then full; box to itself)
[ ] ★ review gate passed ★
[ ] threads authored; every beat covered
[ ] weave    (per thread; PAID)
[ ] curate   (cull + repair; final + approved)
[ ] integrate -> out/<arc>
[ ] verify   (check; diff is FIXME->prose only)
[ ] promote  (branch, copy, check, bundle, commit)
[ ] cleanup  (stale sidecars, stray out/ dirs)
[ ] archive  (curated sidecars committed somewhere)
```

---

## 7. Known open problems

Carried forward from the superseded workflow doc; neither was resolved by the pivot.

- **Observability.** You cannot currently trace *why* a passage came out as it did —
  which beat, which color lines, which lens fed it. The payoff would be
  hand-correction (you can't tell a dropped fact from a deliberate omission without
  the ground truth beside the output) and building authoring intuition by watching
  output-as-a-function-of-input. Proposed shapes: a side-by-side review surface, an
  `arc status` stage-stack dump, structural graph views. `forge review` covers part
  of the first, pre-weave only.
- **The unit-and-representation problem** (the largest). The authorable unit is
  probably the **branch-passage**, not the file. A revisitable hub like
  `forest/the_fugitive/Mareen.enc` packs many disjoint state-conditioned passages,
  re-entered at different points in a playthrough, each wanting its own prose and its
  own review. "Colorize this beat" is ill-defined when the beat is really N variants
  seen at N times. The peer-JSON sidecar addresses this better than the old comment
  blocks did, but keys on line number — still a file-position identity, not a
  state-conditioned one.

- **Cross-file prose continuity.** Transit prose of a `+open` contradicting the body
  it opens into; a hub body re-establishing an arrival that already played. No
  single-file check catches these — each file is individually valid and the defect
  exists only in read-order. A post-weave continuity critic was deferred until the
  first full arc shipped; the villa now meets that bar, so **check its woven output
  for arrival-restatement before building anything** — story-so-far may already
  suppress most of the class.

---

## See also

- `project/encounter-spec/format.md` — the `.enc` format
- `project/encounter-spec/arc_patterns.md` — hub/spoke shapes, state carriers, gating
- `project/encounter-spec/mechanics_reference.md` — the mechanic vocabulary
- `plans/finish_scrub_arcs.md` — current work: the four scrub arcs
- `plans/forge_dotnet_port.md` — how Forge got here
