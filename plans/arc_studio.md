---
kind: plan
title: Arc Studio — bibles-first pipeline with curation UI
state: exploring
created: 2026-05-31
updated: 2026-05-31
touches:
  files:
    - text/encounter-tool/EncounterCli/
    - text/encounter-tool/skills/arc-decompose/
    - text/encounters/arcs/
    - plans/arc_writer.md
  features: [authoring, llm, arcs, pipeline, ui]
---

# Arc Studio

Successor (tentative) to `plans/arc_writer.md`. Same goal — pipeline
that takes a brief to a shippable arc — but a different shape, born
from a real exercise of the existing pipeline on `scrub/relay_post`
exposed by the in-context decompose pass on 2026-05-31.

## Economic constraint (load-bearing)

Claude Code inference is **dramatically** subsidized vs. direct API
access — Opus 4.8 per-token API cost is bananas; the same Opus calls
inside Claude Code's harness are effectively free for this user.
This inverts the architecture: even when an API-batch model would be
the natural fit (independent per-beat calls, embarrassingly parallel
colorize/factual/voice passes), we lodge the work into **Claude Code
skills** instead. The skill model has Claude (the chat instance) do
the inference directly via tool calls — slower and clunkier than a
batch API but free at the relevant margin.

What this means for the pipeline below:

- **Every prose-generating pass is a skill, not a CLI-driving-API.**
  Colorize, factual, voice, critic — all implemented as skills that
  iterate over beats and emit content directly in the conversation,
  writing to `.enc` files via the Edit tool.
- **Sub-agent dispatch is the parallelism story.** A skill can
  spawn `Task` sub-agents (one per beat, or one per file) to keep
  context fresh and run independent work concurrently within a
  single Claude Code session. Same total inference, better context
  hygiene.
- **Cross-provider critic becomes intra-Claude-Code instead.**
  Independence comes from running the critic as a different model
  variant (e.g. Haiku-4.5 critic on Opus-4.8 author output) via
  the `model` parameter on `Agent`. Less independence than a true
  cross-provider check (same family, same training corpus, similar
  biases) but still meaningfully different — and it stays inside
  the subsidy.
- **Qwen / cross-provider API stays available as fallback.** Where
  the skill-shaped version genuinely doesn't work — long-running
  variant-grind jobs that can't be done in a single session, or
  cross-provider critics for high-stakes final passes — the
  existing `QwenClient` and Anthropic/Azure paths remain in
  `EncounterCli`. Default to skill; reach for API only when the
  skill version is unworkable.

This decision is **economics-driven, not architecture-driven**. If
the cost structure changes, several of these choices flip back to
API-shaped. The plan flags each such choice with `economic:` so
future-us can untangle them cleanly.

## Why this is a different plan

The existing pipeline (`arc_writer.md`) has the right *passes* but the
wrong *substrate*. The decompose pass reads the brief in-context and
fires the structural skeleton in one shot; subsequent passes (colorize,
factual, voice, critic) each re-read the brief and infer the world
from scratch. Drift compounds: the factual pass invents props the
scaffold didn't mention; the voice pass invents capabilities the
characters don't have; the critic checks against the brief but not
against what the *prior pass* established. Every issue caught in the
relay_post exercise — phantom stools, characters with implausible
expertise, hub bodies that re-establish arrival moments — traces back
to "the pipeline has no locked-in shared source of truth other than
the brief, and the brief is too loose to anchor against."

Arc Studio's load-bearing change is **shred the brief into structured
bibles before any prose-pass runs**. Every downstream pass reads the
bibles. Drift becomes the critic's job to catch *against the bibles*,
not against a fading memory of the brief.

## The pipeline

Seven stages. Human review at each ▼. Items in **bold** are new vs.
`arc_writer.md`; items in *italics* exist today and carry forward.

### 0. Brief (human + claude.ai)

Author drafts a monolithic `.md` brief. Messy and inexact is fine —
that is what the shred pass cleans up. The brief lives in the arc
directory as today (`text/encounters/arcs/<biome>/<arc>/<Name>.md`).

▼ no review — this is just the entry point.

### 1. **Shred (Claude Code, skill `arc-shred`)**

Read brief + locale guide + arc_patterns + relevant authored arcs.
Produce three sibling files in the arc directory:

- **`_cast.md`** — one entry per named character. Role, age,
  appearance, what they know, what they can do, what they cannot do,
  what they want, how they speak. Locked-in facts the scaffold and
  downstream passes both consult.
- **`_set.md`** — the physical environment. Rooms, props, doors,
  lighting, what is where, what state things start in. Time of day,
  weather, any environmental beats that shift.
- **`_scenes.md`** — the scene graph as a scannable table. Each row:
  scene id, precursor state (tags/qualities/items that gate this
  scene), what happens (3–6 beat bullets), resulting state, leads-to
  list. The shape that exposes causal/chronological issues at bullet
  scale, before any `.enc` exists.

All three prefixed with `_` so the encounter parser ignores them
(extends the existing `_*.enc` backup convention).

▼ **Human review.** Read three short files. Reject/refine anything
wrong. Casualty/chronology errors caught here cost minutes, not
hours.

### 2. **Decompose (Claude Code, skill `arc-decompose`, narrowed)**

The existing `arc-decompose` skill, but narrowed: it now reads the
locked bibles instead of inferring from the brief. Its job is
mechanical-syntactic: render the scenes from `_scenes.md` into
linked `.enc` files using the structural tricks inventoried in
`arc_patterns.md` (1a.ii hide-the-choice, §3.3 staged-gate, etc.).
No structural invention — the scene graph is already a graph.

The skill produces FIXME-stubbed `.enc` files that `check` clean.

▼ **Human review.** Read the `.enc` skeleton; check that gating
flows and the spoke shapes look right. The bibles caught the
content errors; this step catches the structural ones.

### 3. **Colorize (Claude Code, skill `arc-colorize`)**

`economic:` previously planned as qwen API; lodged into a skill to
stay inside the subsidy. For each FIXME beat above a minimum word-
count, the skill produces N texture bullets in the wiki-flat voice
and writes them inline as `# --- COLOR --- ... # --- end ---`
blocks. Bibles + brief + locale all in scope.

**Dispatch:** skill walks the arc dir top-down; for each `.enc`,
spawns a `Task` sub-agent per beat to keep context tight. Sub-
agent receives the bibles, the beat, the encounter body, and
returns the bullets. Parent skill writes them. Parallelism
bounded by however many sub-agents Claude Code will run
concurrently.

▼ **Human review.** Curate bullets in editor (delete rejects).

### 4. **Factual + embedded critic (Claude Code, skill `arc-factual`)**

`economic:` previously planned as qwen + cross-provider API. Now
two skill-shaped sub-passes per beat.

Sub-pass A (the writer): sub-agent receives bibles + beat + kept
color bullets + encounter body, produces a single externally-
verifiable passage per the existing `FactualCommand` system prompt
rules (rule 1a unpack NPC interiority, rule 8 render dialog). The
parent skill writes it as `# --- FACTUAL --- ... # --- end ---`.

Sub-pass B (the critic): a different model variant — Haiku-4.5
sub-agent (set via `Agent`'s `model: 'haiku'` param) — reads the
just-written FACTUAL block + bibles and emits structured findings:

- new characters / props / locations not in `_cast.md` / `_set.md`
- characters acting on knowledge they don't have per `_cast.md`
- contradictions with `_scenes.md` precursor or resulting state
- PC interiority surviving the factual pass
- bare NPC interiority not unpacked into observable behavior

Severity routing:
- **Severe** (new character, capability invention, contradiction):
  parent skill re-dispatches the writer sub-agent with the critic
  finding folded into the prompt. Bounded retry (≤2) then flag.
- **Minor** (color lost, register drift): annotated inline as
  `# --- CRITIC factual sev=minor --- ... # --- end ---`.

The cross-model independence (Opus writer / Haiku critic) is
weaker than true cross-provider (Anthropic / Azure) but stays
inside the subsidy. `economic:` if budget changes, swap Haiku
sub-agent for an Azure gpt-mini API call via `EncounterCli`.

▼ **Human review.** Read FACTUAL blocks + critic findings.

### 5. **Re-voice + specialty critics (Claude Code, skill `arc-voice`)**

`economic:` same flip — was qwen + cross-provider, now skill-
shaped sub-agents.

Two changes from the existing voice pass beyond the skill flip:

- **Many candidates per beat (≈5)**, not 2. Picking from many is
  faster than hand-revising one (ngraph insight).
- **Multiple specialty critics** run on each candidate, emitting
  structured findings. Initial roster:
  - **physics** — nobody teleports, props don't move offscreen
    without acknowledgment, time passes when it should.
  - **knowledge-state** — characters only act on information they
    plausibly know per `_cast.md`.
  - **identity** — characters don't gain new capabilities mid-arc.
  - **plausibility** — actions track with human behavior given
    the character bible.
  - **continuity** — block reads as continuous prose with the
    *preceding* transit and the *next* scene body (three-block
    bundle critique). Carried forward from `arc_writer.md` open
    questions.

**Dispatch:** writer is the parent skill itself (or Opus sub-
agents per beat, generating all 5 candidates in one shot). Each
critic is a Haiku sub-agent with a focused system prompt — one
sub-agent per critic per candidate. Parallel where possible.
Findings attach inline; severe findings trigger one re-roll,
minor findings annotate the candidate.

▼ **Human review (the curation step).** Per beat, pick the chosen
variant from the annotated candidate list, delete the rest.

### 6. **Finalize (CLI, deterministic)**

`arc finalize <arc-dir>` strips all `# --- KIND --- ... # --- end
---` scaffold blocks, asserts exactly-one-chosen-variant-per-beat,
runs `check`. No model involvement. Last gate before ship. If the
asserts don't hold, the curation convention was loose — fix the
curation, re-run finalize.

▼ no review — the gate is mechanical.

## Curation UI (Arc Studio web app)

Steps 3–5 are where the human-attention bottleneck lives. The
ngraph studio already shipped most of the UI scaffolding we need.
Steal it.

### What ngraph studio has that we want

Located at `/home/joseph/repos/ngraph/src/Ngraph.Studio/`:

- **ASP.NET backend** on a fixed port (5174), filesystem-watched,
  websocket file-change notifications, no database — local files
  are the only source of truth. The architecture our authoring
  surface should also use.
- **Vite + React 19 frontend** with Monaco editor for the source
  pane and `@xyflow/react` + `elkjs` for the graph pane.
- **`GraphPane.tsx`** — ReactFlow + ELK layered left-to-right
  graph, custom node renderer, edge selection state. Direct fit
  for `_scenes.md` visualization. In ngraph this rendered the
  fact-state graph; in Arc Studio it renders the scene graph,
  which is structurally simpler and the curated source-of-truth
  (so the graph is sane out of the box, unlike ngraph's projected
  graph that needed shaping).
- **`EdgeDetailPane.tsx`** — inspector for variants of a single
  edge, with "author / generated / voiced" body piles. Exactly
  the variant-picker UX needed for step 5.
- **Pipeline runner pattern** — background job model with status
  streaming, "click button, work runs async, results stream back"
  (per ngraph `plans/authoring-tool.md` concurrency section).

The ngraph studio is ≈1,000 lines of frontend + a single-file
ASP.NET backend. Forkable in an afternoon.

### Arc Studio screens (rough)

- **Arc picker** — list arcs in `text/encounters/arcs/`, click to
  open. Same shape as the ngraph file picker.
- **Bibles pane** — three short scrollable views of `_cast.md`,
  `_set.md`, `_scenes.md`. Editable in Monaco. Save-on-blur with
  the existing debounce.
- **Scene graph** — `_scenes.md` rendered with `GraphPane`. Click a
  node to focus that scene in the next pane.
- **Beat inspector** — for the focused scene, list each FIXME beat
  with its color bullets, factual block, voiced candidates, and
  critic findings (color-coded by severity). Pick a variant per
  beat with a click. Edit Monaco-style inline.
- **Pipeline runner** — buttons to kick off colorize / factual /
  voice / critic passes against a selection (all-arc, one-scene,
  one-beat). Job-status panel with stream.
- **Finalize button** — runs `arc finalize`, surfaces assertion
  failures.

The .enc files remain the canonical artifact; the studio is just a
view + edit + pipeline-trigger surface over them. Authors can
always close the studio and edit `.enc` in `vim` — the file watcher
picks up external edits the same way.

## Open questions

- **Bible drift policy.** If the factual pass introduces a useful
  prop the human keeps, does it back-write to `_set.md`? My
  instinct: no, bibles lock at step 1 review; if the human wants
  to add a fact they re-edit the bible and re-run from there. But
  this means bibles can become stale relative to ship. Decide.
- **Critic findings schema.** Severity routing needs structured
  output `(severity, kind, file, beat, claim, what-violated)`. The
  existing `CriticCommand` emits prose. Prompt-engineering job.
- **Variant count + critic-on-every-candidate cost.** 5 variants ×
  5 critics × 20 beats × 6 files ≈ 3,000 critic calls per arc.
  Inside Claude Code subsidy this is free at the margin but slow
  if serialized; sub-agent dispatch should parallelize per beat at
  minimum, possibly per critic. Open question: how many concurrent
  sub-agents Claude Code will actually run before throttling /
  context-budget pressure. May need to throttle to a sensible
  concurrency cap (e.g. 8 at a time).
- **Sub-agent context budget.** Skill-shaped passes save inference
  dollars but spend Claude Code context. A 20-beat arc with 5
  critics each emitting findings is a lot of sub-agent results
  flowing back to the parent. Need a discipline: parent skill
  emits a one-line summary per beat to its own context, writes
  full critic findings to the `.enc` file as `# --- CRITIC ... ---`
  annotations, and never re-reads them. Otherwise the parent
  blows its context partway through the arc.
- **What happens when Claude Code is offline / down.** API
  fallback path exists (`EncounterCli` still has `QwenClient` +
  Anthropic SDK) but the skills are the default. Need a `--via api`
  flag or sibling CLI command for the fallback case.
- **One skill or two?** `arc-shred` and `arc-decompose` are
  different jobs. Two skills probably right, sharing input docs.
- **`_scenes.md` table schema.** Markdown table vs. YAML vs.
  something stricter. Table is most scannable; YAML round-trips
  better. Pick before building shred.
- **Coexistence with the current pipeline.** Cut over hard or
  let the old pipeline keep working until Arc Studio ships? My
  instinct: keep both until Arc Studio has shepherded one arc
  end-to-end, then deprecate.
- **Where does the Arc Studio process live?** Sibling to
  `EncounterCli` under `text/encounter-tool/`? Or a new top-level
  `arc-studio/`? Probably the latter — it's a peer tool, not a
  CLI subcommand.
- **Cross-provider critic budget.** Same question as
  `arc_writer.md` open questions (Haiku vs. Azure gpt-mini).
  Carries forward, but down-weighted: the default critic is now
  an in-harness Haiku sub-agent. The API path is for the
  fallback-to-true-cross-provider case only.
- **The studio UI and the skill model interact awkwardly.** Arc
  Studio (the web app) wants to "click a button, run the
  pipeline." But the pipeline is now Claude Code skills, which
  expect a chat-shaped interaction. Two options: (a) the studio
  button just emits a slash-command line the user pastes into
  their Claude Code session; (b) the studio shells out to
  `claude code --skill arc-colorize <args>` as a non-interactive
  invocation. Option (b) is cleaner but depends on Claude Code's
  non-interactive surface being usable. Investigate.

## Build sequence

1. Settle the `_scenes.md` schema (paper exercise on relay_post).
2. Write the `arc-shred` skill; produce `_cast.md`, `_set.md`,
   `_scenes.md` for relay_post and one other arc by hand-running
   the skill in chat.
3. Update `arc-decompose` skill to consume bibles instead of
   inferring from brief. Re-run on relay_post; compare quality.
4. Define the critic findings schema. Build the **Haiku-critic
   sub-agent prompt** (focused, structured output). Validate
   independence empirically by feeding it adversarial test cases.
5. Write the `arc-colorize` skill (per-beat sub-agent dispatch).
6. Write the `arc-factual` skill (writer sub-agent + Haiku
   critic sub-agent + retry loop). Settle the context-budget
   discipline: parent emits one-line summary per beat, full
   findings live in the file.
7. Write the `arc-voice` skill (multi-candidate generation +
   per-critic sub-agent dispatch). Concurrency cap TBD.
8. Implement `arc finalize` (CLI, deterministic, no model).
9. Fork ngraph studio; rename to Arc Studio; gut the
   ngraph-specific bits; wire the scene graph pane to
   `_scenes.md`.
10. Wire the beat inspector to the pipeline-pass annotations.
11. Settle the studio→skill bridge (slash-command paste or
    `claude code --skill <name>` shell-out, see open questions).
12. Retire `arc_writer.md` (or supersede with a `state:
    superseded` note pointing here).

Steps 1–8 are skill + CLI work and ship value independently.
Steps 9–11 are the studio UI on top; can land in any order after
the skills are stable.

## What this plan does not cover

- Migration of arcs already shipped through the current pipeline.
- A multi-arc / arc-graph editor — Arc Studio is single-arc, same
  scope discipline as ngraph studio.
- Combat / `.fight` authoring. Same exclusion as `arc_writer.md`.
- Browser-based playtest of the arc — the existing GameServer is
  the playtest path; Arc Studio focuses on authoring.

## Prior art and references

- **`plans/arc_writer.md`** — the plan this supersedes. Pipeline
  shape and ngraph inheritance carry forward; the bibles
  substrate is the headline change.
- **`/home/joseph/repos/ngraph/plans/authoring-tool.md`** — the
  ngraph studio plan. Concurrency model, "diverge and curate"
  framing, resource ordering, and research-agent pattern are all
  directly applicable.
- **`/home/joseph/repos/ngraph/src/Ngraph.Studio/web/src/`** —
  the actual UI implementation to fork: `GraphPane.tsx`,
  `EdgeDetailPane.tsx`, `App.tsx`, the file-watch + websocket
  scaffold.
- **`project/encounter-spec/arc_patterns.md`** — structural
  pattern inventory consumed by `arc-decompose`. No change in
  this plan.
- **Continuity feedback memories** (`feedback_hub_branching`,
  `feedback_scaffold_no_creative_writing`) — the lessons that
  motivated the bibles substrate. Each memory describes a class
  of drift the current pipeline cannot prevent.
