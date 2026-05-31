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

  **Voiceprint (borrowed from Ali:Chat — see references).** The
  "how they speak" field carries the descriptive register *and*
  one or two verbatim example lines: actual sentences in the
  character's voice, not adjectives about it. The RP-card
  community's load-bearing finding is that a couple of example
  lines pin voice more reliably, and more token-efficiently, than
  any amount of description — structured fields are for facts,
  example lines are for feel. This is the dialog writer's anchor.
  We defer dialog to the voice pass (step 5), so the bible owes
  that pass *something to write against*: it generates its ~5
  candidates per beat against the example lines, and the
  voice/identity critics measure drift against them rather than
  re-deriving the register from adjectives every candidate. The
  example lines are *reference in the bible*, never prose decanted
  into the `.enc` — `feedback_scaffold_no_creative_writing` still
  holds. The current signal_array `_cast.md` has descriptive Voice
  fields but no example lines; add them.
- **`_set.md`** — the physical environment. Rooms, props, doors,
  lighting, what is where, what state things start in. Time of day,
  weather, any environmental beats that shift.
- **`_scenes.md`** — the scene graph, one scene per heading. For
  each scene: id, precursor state (tags/qualities/items that gate
  this scene), a physical-state ledger (below), what happens (3–6
  beat bullets), resulting state, leads-to list. The shape that
  exposes causal/chronological issues at bullet scale, before any
  `.enc` exists.

  **Granularity warning (load-bearing).** A scene is a *narrative
  beat* — a coherent chunk of the arc the player traverses as
  one unit — **not a `.enc` file.** Decompose is the step that
  fans scenes out into `.enc` topology (hub-and-spoke,
  branched choice, sequential beats, staged gates). If
  `_scenes.md` ends up with ~15 entries and visibly maps 1:1
  to expected `.enc` files, the shred has decomposed early
  and the bible no longer sits above the substrate it is
  supposed to govern. A typical arc has **3–6 scenes** plus
  short outro-state stubs (end states, not scenes — a few
  lines each). The signal_array arc, for example, is three
  scenes (arrival, rest interval, completion-through-
  decision), one optional side-scene (a Chorik aside), and a
  small set of terminal-state outros.

  Per-NPC chats inside a hub are *player activity inside a
  scene*, not scenes of their own. They live as bullets under
  the hub scene's "what happens" field. Same for branch
  resolution beats inside the clutch: cunning roll, combat
  roll, success/fail forks — all bullets under the clutch
  scene, not their own scenes. The bibles operate one level
  of abstraction above `.enc`. Keep them there.

  **Physical-state ledger (borrowed from RP "tracker"
  extensions — see references).** The `resulting state` field
  is *mechanical* — the tags/qualities that gate flow. It does
  not track physical continuity, so the physics/continuity
  critics (step 5) have nothing concrete to check against. Each
  scene therefore also carries a short prose ledger: who is
  present, time of day / light, the state of the salient props,
  what the PC is carrying that matters, and a one-line **Δ** of
  what this scene changes. The critics then check two
  invariants — the prose only touches things in the ledger, and
  anything the prose changes shows up in Δ — which turns "does
  this read continuously" from a vibe into a checkable assertion.
  The "who is present" line doubles as the key set that drives
  per-beat bible scoping (see "Bible scoping" below).

  **Keep the ledger prose, not a state machine.** It is
  continuity-checking scaffolding for the human and the critics:
  *document what changes,* do not decant it into tags/qualities
  or a build-ready state table. The same pull that makes a scene
  list want to become a 1:1 `.enc` map (granularity warning
  above) makes the ledger want to become a covert state machine.
  Resist it. The mechanical state lives in `resulting state` and
  ultimately in the `.enc` tags authored at decompose; the
  ledger sits a level above, in prose, and stays there.

**GAP triage.** The brief always leaves things unanswered.
Shred resolves them rather than punting — downstream passes
treat unmarked gaps as license to invent, and an unresolved
gap is a drift vector. The skill picks a reasonable default
for every gap and annotates it inline as `GAP: ...`, then
sorts the gaps into three tiers:

- **Cosmetic** (the brass-button design, a character's exact
  age, boot height, what the next-site is named): default
  silently with a `GAP:` annotation in the relevant bible
  field. Author skims, almost always accepts, edits inline
  if they care.
- **Local** (a side-scene's placement, a tag's downstream
  consumer, a roll's target tier): default with annotation
  in-field. Expect a quick author look but rarely a deep
  revisit. Cheap to override.
- **Load-bearing** (anything that, if defaulted wrong, would
  cascade through downstream passes and be expensive to
  undo): default *and* surface at the top of the bible as a
  **"Decisions to confirm"** block before the human-review
  gate. Examples seen so far: whether the crew is uniformed,
  whether a deflection scene is allowed, the save-skill
  choice for an automatic mechanical beat, scene granularity
  itself when the brief is structurally ambiguous.

The triage is the skill's job; the author's job at the
review gate is to confirm or override. If the skill cannot
tell which tier a gap belongs to, it goes load-bearing —
false-positives on the "Decisions to confirm" block are
cheap (author waves them through), false-negatives are
expensive (drift compounds through colorize/factual/voice).

**Arc invariants (borrowed from RP "author's note" / depth
injection — see references).** Some facts must not drift
*anywhere* in the arc: Mareen's name taboo, Veran cannot
recognize the recruitment signal, the farmer and the
technician cannot both be true. These are not gaps to resolve
and not mechanical state — they are settled facts whose
violation cascades. Shred lifts them into a short
**Invariants** block at the top of `_cast.md`, distinct from
the "Decisions to confirm" block: that block is decisions to
*make*; invariants are decisions already *made* and locked.
Every downstream pass and every critic is handed this block
verbatim, and crucially it survives the scoping prune (see
"Bible scoping") — even a sub-agent that only sees one
character's entry still sees the invariants. Cross-arc
invariants belong in `rules/`; arc-local ones live at the top
of `_cast.md`.

All three prefixed with `_` so the encounter parser ignores them
(extends the existing `_*.enc` backup convention).

**Emergent property: many small files become viable.** The
shipped arcs (`the_fugitive`, `the_hermitage`, `relay_post`)
fought hard to minimize `.enc` count, packing branching into
single-file `@if` trees and end-scenes-with-multiple-routes
(see `relay_post/Dawn Ossal.enc`, which carries three terminal
sub-routes via tag-branched prose). The reason was substrate-
shaped: managing state across files was painful without a
view onto the whole graph, so authors merged scenes into
"giga-files" to keep state local. Decompose then had to honor
that discipline.

The bibles + scene-graph view flip this. Cross-file state is
legible at a glance via the graph; orphan setters get caught
by the audit step (every `+add_tag X` paired against a reader
or removed); per-scene `.enc` files stay small and single-
purpose. The signal_array decompose exercise produced 10
files at 15–38 lines each, with no file carrying more than
one Cunning/Combat/Negotiation picker — markedly more
legible per-file than the shipped arcs.

The default-arc skeleton should be **more files, smaller
each**, not the old giga-file shape. Reach for in-file
branching only when the branches are tiny outcome variants
of a single choice (the Dawn Ossal pattern is still valid for
small per-route prose tweaks); for anything heavier, factor
the branch into its own `.enc`. Easier to read, easier to
debug, easier for downstream prose passes to keep context
tight on a single beat.

**Formatting rule (applies to all three bibles): avoid markdown
tables — for now.** The author reviews these in Sublime, where
md tables render as a wall of gunk — hard to read, hard to
edit, hard to diff. Prefer nested bullet lists, `###`-per-entity
sections, or plain paragraphs.

**Sunset clause: once the graph viewer ships** (the Arc Studio
scene-graph pane reading `_scenes.md`), the author will not
read `_scenes.md` directly anymore — the graph view becomes
the read surface, the file becomes machine-shaped. At that
point switch `_scenes.md` to JSON (or whatever the graph
viewer parses natively); no need to keep it human-pretty.
`_cast.md` and `_set.md` stay prose-shaped — those are read
directly in the bibles pane regardless.

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

## Bible scoping (agentic RAG)

The per-beat prose passes (colorize, factual, voice) and their
critics each run as a sub-agent. The naive move is to hand every
sub-agent everything in scope. Don't — that is both a
context-budget problem (see open questions) and a *quality*
problem: a knowledge-state critic that can see the entire cast
can always rationalize "well, someone here could know this."

**The corpus is not three small files.** It is tiered, and the
tiers want different retrieval treatment:

- **Per-arc bibles** (`_cast.md`, `_set.md`, `_scenes.md`).
  Small, and for a given beat *mostly* all-relevant. The scoping
  problem here is narrow — prune to present cast/props.
- **Biome lore guides** (`text/lore/<biome>.md`). This is the
  load-bearing addition. They are **big** — `scrub.md` is ~6.3k
  words, the whole `text/lore/` corpus ~27k — and cleanly
  sectioned (`## Identity`, `## Peoples` → `### Tashkari` /
  `### Kesharat Administration` / `### The Lattice`,
  `## Material Culture`, `## Distance Tiers`). A beat touching
  the Kesharat crew needs the Kesharat + Lattice sections, not
  the whole guide. Dumping a full biome guide into every beat is
  the exact context-blowout the open questions warn about.
- **Cross-cutting lore** (`timeline.md`, `imperial_calendar.md`,
  `swamp_tongue.md`, the per-guide Lattice sections). Pulled by
  topic, not by biome — a beat that dates an event or speaks the
  swamp tongue needs these regardless of which arc it is in.
- **Sibling arcs.** The cross-arc notes in `_cast.md` (Baret's
  Reshîd clan links to `the_villa`) are retrieval edges into
  other arcs' bibles, used rarely but real.

This tiering is why the **agentic RAG through-line** is real and
not over-engineering. The two poles, now mapped onto the tiers:

- **Static / ledger-driven** handles the per-arc bibles cleanly:
  scope is a deterministic function of the scene ledger's
  "present" line plus one transitive hop. Cheap, no model call,
  the right v1 for cast/props.
- **Retrieval (keyed → semantic → agentic)** is what the biome
  guides and cross-cutting lore actually need, because relevance
  there is *topical*, not "who is on stage." A beat does not
  announce "I touch the Lattice section." Start with keyed
  injection over the guide's `###` sections (lorebook-style),
  graduate to embeddings + reranking if keyed misses, and reach
  for an agentic retrieval step when a beat needs a fact no
  keyword or ledger line predicts (a callback, a sibling-arc
  detail).

Anthropic's **Contextual Retrieval** (see references) is the
lodestar for *how the corpus is written and indexed*, and it
matters most for the biome guides precisely because they are
long. Each chunk — a `###` lore section, a cast entry — must
survive being pulled out of its document: prepend enough
situating context that a `### The Lattice` chunk still carries
"this is the scrub biome's account of the Lattice" when injected
alone. That is the same "comprehensive, standalone entry" rule
the lorebook guides reached independently, and the article's
Contextual Embeddings + Contextual BM25 + reranking stack is the
concrete recipe if we go past flat keyed lookup. The through-line:
**the bibles and `text/lore/` together are a retrieval corpus;
the passes are its consumers; per-beat scope is a retrieval
query, not a fixed prompt dump.**

Scoping is also the constraint, not just an economy — a
knowledge-state critic that cannot see an off-stage character's
entry literally cannot launder that character's knowledge into
the scene. The arc invariants block is the one tier that is
*never* pruned: every sub-agent sees it regardless of what the
retrieval returns.

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
  graph, custom node renderer, edge selection state. Used in Arc
  Studio **only during the shred/decompose stages** for
  `_scenes.md` visualization. See "Scope of the graph view"
  below — this is a *story-shape sketching tool*, not a view onto
  the `.enc` files. In ngraph it rendered the fact-state graph
  (often messy because projected from facts); here it renders
  the human-curated scene graph, which is sane out of the box.
- **`EdgeDetailPane.tsx`** — inspector for variants of a single
  edge, with "author / generated / voiced" body piles. Exactly
  the variant-picker UX needed for step 5.
- **Pipeline runner pattern** — background job model with status
  streaming, "click button, work runs async, results stream back"
  (per ngraph `plans/authoring-tool.md` concurrency section).

The ngraph studio is ≈1,000 lines of frontend + a single-file
ASP.NET backend. Forkable in an afternoon.

### Scope of the graph view

The graph viewer is a **shred/decompose-stage tool only.** `.enc`
files do not map cleanly to a graph: hubs (a single encounter
returned to from many spokes) and self-looping edges (a spoke that
`+open`s back to its own hub) produce a "graph" that is technically
correct but visually unhelpful — every hub becomes a dense star,
self-loops clutter, and what the player actually *experiences* is
nothing like the rendered topology.

**Where this would have helped on shipped arcs.**
`forest/the_hermitage` (staged hubs with three character spokes
and three distinct good endings, all granting the same artifact)
and `plains/grainway_station` (two-reward fork with many branching
paths between intro and resolution) were both authored painfully
without an up-front picture of how the story flowed. The author
held the shape in their head and re-discovered missing connections
the slow way — by reading `.enc` files top to bottom and noticing
gaps. A `_scenes.md` graph view at the sketching stage would have
surfaced reachability holes and ending coverage in seconds.

So the graph is bounded:

- **Available during shred (step 1) and decompose (step 2).** This
  is where the author is figuring out the *shape* of the story —
  what scenes exist, what precursors gate them, what leads to
  what. A bullet list of scenes plus a graph view is the right
  pairing here. Move nodes around, see the flow, catch
  unreachable terminals.
- **Hidden during colorize / factual / voice / critique (steps
  3–5).** These are per-beat prose passes. The author is reading
  text, picking variants, fixing wording — graph topology is
  noise. Replace with a file-tree + per-beat inspector flow.
- **Optionally available read-only at finalize.** A quick "does
  this still look like the arc I planned?" check before shipping.
  Same `_scenes.md`-derived view, not a `.enc`-derived view.

The rule: the graph reflects the **author-curated scene graph**
(`_scenes.md`), never the **rendered click-graph** (.enc files).
The latter is best viewed as text + walk-through, not as a node-
edge diagram.

### Arc Studio screens (rough)

- **Arc picker** — list arcs in `text/encounters/arcs/`, click to
  open. Same shape as the ngraph file picker.
- **Bibles + graph pane** (shred / decompose mode) — three
  scrollable views of `_cast.md`, `_set.md`, `_scenes.md` in
  Monaco, with the graph view rendered alongside `_scenes.md`.
  This is where the author lives during structural sketching.
- **File tree + beat inspector** (prose-pass mode) — flat list of
  `.enc` files in the arc; click one to open. For the selected
  file, list each FIXME beat with its color bullets, factual
  block, voiced candidates, and critic findings (color-coded by
  severity). Pick a variant per beat with a click. Edit Monaco-
  style inline. **No graph here** — beats are the unit of work,
  not nodes.
- **Pipeline runner** — buttons to kick off colorize / factual /
  voice / critic passes against a selection (all-arc, one-file,
  one-beat). Job-status panel with stream. Available in both
  modes.
- **Finalize button** — runs `arc finalize`, surfaces assertion
  failures.

Mode switch is per-arc, not global — the studio can be in
shred/decompose mode for one arc and prose-pass mode for another
in adjacent tabs.

The .enc files remain the canonical artifact; the studio is just a
view + edit + pipeline-trigger surface over them. Authors can
always close the studio and edit `.enc` in `vim` — the file watcher
picks up external edits the same way.

## Open questions

- **Lore retrieval index.** The "Bible scoping" section treats
  `text/lore/` + the per-arc bibles as a retrieval corpus. How do
  we index and query it? In all cases Qwen does the prep — free
  instruct + embeddings to chunk the corpus into clean standalone
  pieces with contextual-retrieval situating prefixes, and to
  build embedding vectors if/when we go semantic — so that side is
  cheap and off the Claude Code subsidy. The fork is *where the
  index lives.* Corpus scale (low-thousands of chunks) makes both
  legs fine on performance — brute-force cosine is instant at this
  size, so we are nowhere near needing ANN — which means the
  decision is **operational, not perf**:

  - **Leg A — Lucene.NET in-process.** A BM25/keyed index over the
    `###`-section chunks, embedded directly in the arc-studio
    backend or an `EncounterCli` subcommand. No new runtime, no
    service boundary, no Java. Cost: Lucene.NET is pinned to the
    Java 4.8 (2014) codebase, so there is **no native vector
    search** — going semantic means storing Qwen vectors as a
    `byte[]` DocValues sidecar and hand-rolling a cosine rerank
    over the BM25 candidate set in C#. Trivial at our scale, but
    code we own and maintain.
  - **Leg B — a Lucene-backed search *service* on imp.** Note that
    bare Java Lucene is a *library*, not a server — the "raw binary
    service + .NET client" shape is **OpenSearch** (Apache-2.0,
    self-hostable, the natural pick), or Elasticsearch/Solr.
    OpenSearch ships native Lucene-engine HNSW kNN and an official
    .NET client (`OpenSearch.Client` / `OpenSearch.Net`), so the
    C# tooling talks to it over HTTP and treats vectors as a
    first-class feature — no hand-rolled rerank. Footprint is
    nothing on the 128GB box (a small single-node instance idles
    around 1–2 GB; the index + ~8 MB of vectors live off-heap in
    page cache) and, crucially, it runs on CPU + RAM and **does
    not draw from the unified-memory pool the Qwen KV cache
    fights over** — it buys context-window headroom with cheap
    RAM. Cost: another service to supervise and a network hop.

  Lean: **Leg A to ship lexical-only fast** (prove retrieval scopes
  a beat at all before adding machinery), with **Leg B as the
  graduation** if/when we want semantic without owning the vector
  code, or want the retrieval load permanently off the
  context-window budget. Either way settle: chunk granularity (one
  `###` section, or finer?) and build-once-and-watch (the studio
  backend is already file-watched) vs. rebuild-per-pass.
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
  blows its context partway through the arc. The complementary
  lever is **bible scoping** (see section above): each sub-agent
  receives only the present cast/props plus invariants, not the
  whole bible.
- **What happens when Claude Code is offline / down.** API
  fallback path exists (`EncounterCli` still has `QwenClient` +
  Anthropic SDK) but the skills are the default. Need a `--via api`
  flag or sibling CLI command for the fallback case.
- **One skill or two?** `arc-shred` and `arc-decompose` are
  different jobs. Two skills probably right, sharing input docs.
- **`_scenes.md` schema.** Resolved (2026-05-31): one `###`-per-
  scene section with nested bullet fields (id, precursor, beats,
  resulting state, leads to). Decided after the relay_post and
  signal_array exercises showed md tables are unreadable in
  Sublime. Open sub-question: should the schema be machine-
  parsable (so the decompose skill can iterate scenes
  programmatically), and if so, do we lean on field-prefix
  conventions (`- id:`, `- beats:`) or just trust the LLM to
  read prose? Default for now: prose-readable, prompt-driven
  parsing. Revisit if decompose drifts.
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
- **AI roleplay character-card conventions.** The SillyTavern
  tooling community has spent years on exactly the in-scene
  consistency problems the bibles substrate fights, in a harder
  setting — live context eviction at play-time, which our static
  authored prose does not have. The borrows are authoring-time
  structure, not runtime features. Sources:
  - Keyed injection — lorebook / World Info:
    <https://docs.sillytavern.app/usage/core-concepts/worldinfo/>
  - Persistent scene-state diff — the "tracker" extension:
    <https://github.com/kaldigo/SillyTavern-Tracker>
  - Example-line voicing — Ali:Chat:
    <https://rentry.co/alichat>
  - Always-injected invariants — author's note / depth
    injection, and the general card schema:
    <https://docs.sillytavern.app/usage/core-concepts/characterdesign/>
  - The facts-vs-feel split (structured PList for facts,
    example lines for voice) — PList + Ali:Chat:
    <https://rentry.co/kingbri-chara-guide>
- **Anthropic, "Introducing Contextual Retrieval"**
  (<https://www.anthropic.com/engineering/contextual-retrieval>)
  — the lodestar for the "Bible scoping" section. Prepend
  chunk-specific situating context so each entry retrieves
  standalone; Contextual Embeddings + Contextual BM25 +
  reranking cut the top-20 retrieval-failure rate by 67%, and
  prompt caching makes the context-generation pass cheap. The
  agentic-RAG through-line for treating the bibles as a
  retrieval corpus rather than one monolithic prompt dump.
