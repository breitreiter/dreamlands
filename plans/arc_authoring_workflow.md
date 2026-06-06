---
kind: plan
title: Arc-authoring workflow — working understanding (alignment checkpoint)
state: exploring
created: 2026-06-06
updated: 2026-06-06
status: ALIGNED (2026-06-06 session) — §5 flags worked through w/ user; live opens now in §7. Pending: author-centric rewrite (§5.2) + first end-to-end arc run (§5.1)
touches:
  files:
    - text/encounter-tool/EncounterCli/
    - text/encounter-tool/skills/arc-decompose/SKILL.md
    - text/encounter-tool/skills/arc-colorize/SKILL.md
    - text/encounters/arcs/
    - project/encounter-spec/arc_patterns.md
  features: [authoring, pipeline, arcs, workflow]
---

# Arc-Authoring Workflow — Working Understanding

> **Purpose.** A shared, corrected understanding of the arc-authoring flow as a single
> workflow, so we don't lose progress across a context clear and have a fixed surface to
> keep steering from.
>
> **Status: aligned, not finished.** The 2026-06-06 session walked the whole flow with the
> user and corrected it stage by stage — Stage 0 is AI-mediated (not hand-authored), Stage 1
> is the load-bearing structural/dramatic gate (now incl. slang scrub), voice is still
> research in `../voicer`, critic is built-but-unexercised, and the §5 alignment flags are
> resolved or merged. What remains is genuine open work, not guesswork, and lives in **§7**.
> The two biggest: the **author-centric rewrite** of this doc (§5.2) and the
> **unit/representation question** raised by revisitable hubs (§6b). Nothing here is frozen;
> the **first end-to-end arc run (§5.1)** is what will tell us where it's still wrong.
>
> Final home (this doc is a draft in `plans/`) is itself an open question — see §7.

## Maturity legend

Applied per stage so we don't present experiments as settled:

- **[PROVEN]** — exercised in this work / in active use, behavior observed.
- **[PARTIAL]** — exists and runs, but not validated end-to-end recently.
- **[SKETCH]** — designed or experimental; not confirmed in practice.

---

## 1. The spine (cross-cutting properties)

These hold across every stage and are the reason the thing is a *workflow* and not just a
pile of commands.

1. **Non-destructive accretion.** Every machine stage appends a `# --- KIND ---` … `# ---
   end ---` comment block adjacent to the `FIXME` beat it elaborates. The `.enc` parser
   **skips every `#` line** (`lib/Encounter/EncounterParser.cs`), so all generated output is
   invisible to the bundler and the file stays `check`-clean at every intermediate state.
   Stages re-run freely; nothing a tool emits ever ships on its own. **[PROVEN]**
2. **The `FIXME` beat is the slot.** Each beat accumulates a visible stack over its lifetime:
   `COLOR → FACTUAL → VOICED×N → CRITIC×N`. `DraftBlocks.ListAdjacentBlocks` already reads
   this stack (basis for an observability view — see §6). **[PROVEN]**
3. **The ship-gate is a manual promotion step.** Because all generated prose lives in
   comments, the human must lift the chosen passage *out* of its comment block into live
   outcome/body prose and delete the `FIXME` line. Nothing automates this today. **[PROVEN]**
4. **Per-stage human review gates.** Each stage's output is curated before the next consumes
   it; curation is **cull + repair**, not toggle-the-keepers
   (`feedback_curation_is_repair`). The pipeline's coherence machinery also *magnifies*
   upstream errors, so review must be front-loaded, not deferred
   (`feedback_pipeline_error_amplification`). **[PROVEN as principle]**
5. **Tools draft at every layer; the human steers and curates — including intent.** Even
   Stage 0 (the brief, bibles, lens) is AI-generated from a brainstormed sketch and reviewed,
   not hand-written. The human's job throughout is steering inputs and selecting/repairing
   outputs, not authoring prose. **[working hypothesis — see Alignment flag §5.2]**

---

## 2. Stage table

| # | Stage | Actor | Reads | Writes → resulting state | Manual prep before next stage | Maturity |
|---|-------|-------|-------|--------------------------|-------------------------------|----------|
| 0 | Intent | human ↔ AI (claude.ai → CC) | a seed idea | brief + `_cast`/`_scenes`/`_set`/`_color` bibles + per-scene `*.lens.md` | review generated files for correctness | [PARTIAL] |
| 1 | decompose | CC skill | brief, bibles, `arc_patterns.md`, rules | a **complete, playable, syntactically-correct** `.enc` arc: full choice/tag/quality structure + `FIXME(register)` beat stubs as bland-but-real prose | walk/sim/viz the graph; lock structure + drama (nothing downstream touches it) | [PROVEN] |
| 2 | colorize | `colorize` (GLM→Haiku) | lens + `_color` + beats | top-of-file `# --- COLOR ---` pool, `# []` checkboxes | cull + repair the pool (delete-to-curate) | [PROVEN] |
| 3 | factual | `factual` (GLM) | COLOR pool + beats + brief + locale | `# --- FACTUAL ---` per beat | read for invention/repetition; trim | [PROVEN] |
| 4 | voice | research in `../voicer` (not yet in EncounterCli) | FACTUAL | `# --- VOICED <author> <scene> ---` ×N (target shape) | pick/repair (slang already scrubbed at Stage 1) | [SKETCH] |
| 5 | critic | `critic` (Anthropic) | FACTUAL / VOICED vs ground truth | `# --- CRITIC … ---` `[CRIT]`/`[LOW]` notes (advisory) | decide which findings to act on | [SKETCH] — built, barely exercised |
| 6 | compile | **human, manual** | the stacked blocks | promote chosen passage → live prose; delete `FIXME` + draft blocks | — (this *is* the gate) | [PROVEN] |
| 7 | ship | `check` → `bundle` → `push` | curated `.enc` | `encounters.bundle.json` → `worlds/<world>/` | — | [PROVEN] |

---

## 3. Per-stage detail

### Stage 0 — Intent (AI-mediated; human steers + curates) [PARTIAL]
**Nothing here is hand-authored.** Stage 0 is itself a multi-step generative process, run
mostly on claude.ai and finished in Claude Code, with the human steering and reviewing at
every revision rather than writing prose. It has three sub-steps:

- **0a — Brainstorm the picture.** Iterate with the model over many revisions until there's
  a high-level picture of the arc that is both *interesting* and *thematically engaging*:
  a markdown document sketching the happy-path line and the themes to highlight. (claude.ai.)
- **0b — Unpack into structure.** Expand that picture into an explicit happy path plus the
  interesting branches and decision points. The tension being managed here: give the player
  room (don't over-constrain) while controlling combinatorial complexity, authoring cost, and
  the risk of logic errors. Output is a high-level sketch / general outline. (claude.ai.)
- **0c — Generate the files + review.** Take the outline document and use it to generate the
  many Stage-0 artifacts below, then review them for correctness. (likely Claude Code,
  working from the 0b sketch.)

- **Artifacts produced (by 0c, not by hand):** the brief (e.g. `TheSignalArray.md`); bibles
  `_cast.md` (characters: know/can't-see/want/voice), `_scenes.md` (beat-by-beat scene
  breakdown + resulting tag/quality state), `_set.md` (physical staging, what the set does
  NOT contain), `_color.md` (arc-wide recurring color motifs + scene-scoped callbacks);
  per-scene `*.lens.md` (whose eye, what to notice, in what register).
- **Consumed by:** decompose reads brief + bibles + patterns; colorize reads lens + `_color`.
- **Maturity note:** the artifacts exist and are real ([PARTIAL]); the 0c *generation* step
  (outline → files) is not yet a built/exercised path ([SKETCH]).
- **Open:** which artifacts are truly required vs nice-to-have; the authoring order; and how
  much of 0a/0b should migrate from claude.ai into Claude Code.

### Stage 1 — decompose (Claude Code skill) [PROVEN]
- **Invoke:** the `arc-decompose` skill (`text/encounter-tool/skills/arc-decompose/SKILL.md`),
  interactively.
- **End state — this is the bar:** a **complete, playable, syntactically-correct encounter.**
  Structurally and dramatically *finished* — it can be pushed live to external beta testers
  as-is. It is simply written blandly: the `FIXME(register):` beat stubs are real, legible,
  playable prose, just not polished to the level we hold final work to. Everything that makes
  the arc *dramatically satisfying and fun* — the shape, the branches, the stakes, the
  payoffs — is locked in here.
- **Why it's load-bearing:** no stage after this improves structure or scripting. Stages 2–6
  only enrich prose. So if the drama, completeness, or wiring isn't right at the end of
  Stage 1, it never will be. This is the one stage where the *encounter* (not the writing) is
  authored, and it gets the heaviest human attention for that reason.
- **Output mechanics:** one `.enc` per encounter — `FIXME(register):` beat stubs + *full*
  structure (choices, `[requires]` gates, `@if` chains, mechanic verbs,
  `+open`/`+finish_dungeon`/`+flee_dungeon`, edge-transit prose). No color yet.
- **Promise / quality bar:** `check` clean; every choice routed; every tag/quality set has
  a reachable reader and vice versa; no unreachable terminals; no infinite loops; ≥1
  unconditional choice per encounter; no mechanics in the body.
- **Verification strategies (the gate is more than eyeballing):**
  - **Static analysis / `walk`** — reachability, termination, every ending hit.
  - **Interactive simulator** — play through to surface logic errors *and* danger-zone flags:
    structural shapes that read fine now but that downstream enrichment could inflate into
    something degenerate (e.g. a large body block on a hub that colorize/voice would balloon).
  - **Graph viz** — render the high-level flow so the human can reason through the arc's shape
    at a glance, not just node-by-node.
  - (Simulator + viz are partly aspirational tooling — see §6.)
- **Slang / levity cleanup (must clear before Stage 1 closes):** the brief and stubs are
  scrubbed for modern slang and whimsical turns of phrase. These creep in honestly — the
  brainstorming and tuning is exhausting, and a little levity helps push through the long
  hours — but any that survives Stage 1 *poisons every downstream layer*. The same steering
  that makes factual/voice avoid invention and avoid forgetting facts also makes them carry
  slang through unchallenged (`project_voicer_preserves_slang`). Stage 1 is the last point
  where the language is still the human's to fix cheaply, so it is the right gate: catch and
  rewrite the levity here, not after it has propagated.
- **Manual gate:** walk/sim/visualize the graph, lock the structure *and the drama*, fix any
  wiring the brief left ambiguous, scrub slang/levity. After this, structure is frozen and the
  prose is clean (if bland).

### Stage 2 — colorize (`EncounterCli colorize`) [PROVEN]
- **Invoke:** `colorize <arc-dir>` (GLM drain-loop → Haiku cut/rank). Steered by
  `*.lens.md` + `_color.md`. See `project_colorize_method`.
- **Output state:** one scene-level `# --- COLOR ---` pool at the top of each file, ranked,
  each line a `# []` curation checkbox.
- **Maturity in practice:** basically correct. The work is largely mechanical and it works
  well — but it still makes mistakes, and they are *hard to spot in isolation* because the
  output reads coherent on its own. Curate **against the inputs**, not just on its own merits.
- **Manual gate:** curate by **delete-to-cull + repair** slightly-bad lines (NOT just
  ticking boxes). Whatever survives in the pool is what factual will thread.

### Stage 3 — factual (`EncounterCli factual`) [PROVEN — rebuilt + gated this session]
- **Invoke:** `factual <arc-dir>` (GLM-4.5-Air via `LocalLlm`). Regression gate:
  `factual --parity`.
- **Reads:** the scene COLOR pool (top-of-file) + all the scene's beats (scene-aware,
  per-beat) + brief + locale.
- **Output state:** a `# --- FACTUAL ---` block per beat — plain, legible prose threading
  the color through the facts; quoted dialogue only where a beat attributes speech.
- **Maturity in practice:** basically correct, like colorize — mechanical and reliable in the
  main, but still errs. The good content quality is itself the trap: a fluent FACTUAL block is
  hard to fault in isolation, which is exactly when dropped upstream facts and quiet inventions
  go unnoticed. Review with the beat + brief + COLOR pool open beside it.
- **Manual gate:** read for invented facts (esp. dialogue content), modern slang, and
  cross-beat color repetition; trim. **Always against inputs, never output-only.** See §4.

### Stage 4 — voice [SKETCH — active research in `../voicer`, NOT yet integrated]
- **Status:** voice is still very much in the research phase. The work lives in a sibling
  research dir, `../voicer`, and has **not** been rebuilt into EncounterCli yet — it sits
  roughly where factual sat before its `../factual` → EncounterCli integration
  (`project_factual_stage_integration`). Everything below is the *target* shape, not a built
  command; treat command/flag/path specifics as provisional until integration lands.
- **Intended invoke (provisional):** something like `voice <arc-dir> [--authors HPL,REH]
  [--scene …]` (Qwen-class model), with per-(author, scene) optimized programs. Exact
  surface, model, and program format are open while research continues.
- **Reads:** each beat's `FACTUAL` block. Scene comes from the `FIXME(register)` tag;
  author selectable (e.g. HPL, REH).
- **Model quirk (load-bearing):** the voicer is currently most successful with **glm-base**,
  a *base* model used **nowhere else** in the pipeline (colorize/factual want an *instruct*
  model — see §4 preflight). Serving glm-base means a model swap on the server, and swaps cost
  ~a minute. This collides with the per-paragraph walking pattern (good for fact traceability)
  because alternating stages would thrash the server between base and instruct. Implication:
  **batch by model** — run all of an arc's voice work in one glm-base window rather than
  interleaving it beat-by-beat with instruct stages. See §4.
- **Output state (target):** one `# --- VOICED <author> <scene> ---` block per (author,
  scene), stacked after FACTUAL. Idempotent per pair; delete a block to regenerate.
- **Known constraint:** the voicer **preserves modern slang / anachronism in its input**
  unless individual words are explicitly banned (`project_voicer_preserves_slang`). This is
  exactly why slang is scrubbed at the **end of Stage 1** — by the time input reaches the
  voicer it is too late and too expensive to catch. The voicer assumes clean input.

### Stage 5 — critic (`EncounterCli critic`) [SKETCH — built but barely exercised]
- **Status:** the command is fully implemented and wired (`CriticCommand.cs`, ~345 lines,
  Haiku via `LlmClient`; `Program.cs` dispatches `critic`). But we have **done little actual
  work on the critic stage** — the prompts are untuned against real output, its findings
  haven't been validated for usefulness (signal vs. noise, false-positive rate), and its role
  in the workflow is unsettled. So: code maturity is real, *stage* maturity is sketch. Don't
  read the behavior below as proven; read it as what the current code attempts.
- **Invoke:** `critic <arc-dir> [--config <path>] [--phase factual|voice|both] [--force]
  [--prompts-only]` (Anthropic).
- **Reads / checks (as currently coded):** factual phase — FACTUAL vs brief + COLOR + beat,
  flags invention / PC interiority / contradiction / scope leak. voice phase — each VOICED vs
  FACTUAL, flags invention / interiority / contradiction / loaded embellishment (does NOT flag
  legit voice signatures).
- **Output state:** `# --- CRITIC … ---` blocks with `[CRIT]`/`[LOW]` findings, skipped if
  already present (use `--force` to regenerate). **Advisory, non-blocking** — the human
  decides.
- **Open:** whether the critic earns its place at all, or whether the side-by-side review
  surface (§6) plus the human gate covers the same ground more reliably. Needs real
  exercise before we can say.

### Stage 6 — compile / promote (human, manual) [PROVEN]
- For each beat: read the stacked blocks (FACTUAL, VOICED×N, CRITIC notes), pick the
  winning passage, **lift it out of the comment block into live prose**, delete the `FIXME`
  line and the remaining draft blocks. The `.enc` stays `check`-clean throughout because
  the parser ignores `#` lines, so this can be done incrementally.
- **No tooling for the collapse today.** This is the irreducible human step.

### Stage 7 — ship (`check` → `bundle` → `push`) [PROVEN]
- `check <path>` — validates syntax, vocabulary, structure; bans em-dashes; warns on
  `FIXME:`/`REVIEW:` (gap: only the bare `FIXME:` form, not `FIXME(register):` — §6).
- `bundle <path>` — parses (skipping `#` draft comments) → `encounters.bundle.json`.
- `push [--world production]` — check + bundle + reload local GameServer; writes the bundle
  to `worlds/<world>/`.
- `walk <arc-dir>` — interactive playthrough to verify reachability/gates before shipping.

---

## 4. Hazards & gates learned (keep these in the workflow, not in our heads)

- **Model preflight — and it's per-stage, not global.** colorize/factual need the GLM
  *instruct* model; imp was once found serving a *base* model, producing template-echo
  garbage (the factual `--parity` gate caught it). **But voice is the exception:** it wants
  **glm-base** (a base model). So the preflight isn't "is imp serving instruct?" — it's "is
  imp serving the model *this stage* expects?" Right model per stage, checked before the run.
  ([PROVEN this session] — see `project_factual_stage_integration`.)
- **Model-swap cost vs. per-paragraph granularity.** Walking one paragraph at a time is great
  for fact traceability, but voice's glm-base requirement (unique in the pipeline) means any
  interleaving of voice with instruct stages forces a server model swap — ~a minute each — and
  thrashes. Resolve by **batching by model**: do all instruct work (colorize/factual) for the
  arc, then all glm-base work (voice) in one window, rather than alternating beat-by-beat.
  Per-paragraph traceability is recovered *within* each model's batch, not across stages.
- **Slang contamination ("clock").** Modern slang and whimsical levity enter at Stage 0/1 —
  often honestly, as relief during exhausting brainstorming — and propagate; the voicer
  preserves it, and the same steering that suppresses invention/fact-loss carries slang
  through unchallenged. Fix it at the **end of Stage 1**, the last point where the language is
  cheaply the human's to rewrite (see §3 Stage 1). Factual *can* de-slang ("say what is
  MEANT") but reliability is unverified, so it is a backstop, not the gate.
- **Curation = cull + repair**, never toggle-the-keepers (`feedback_curation_is_repair`).
- **Per-beat color repetition.** factual re-threads signature color across a character's
  beats and the body beat over-packs the pool. Curator trims; the real fix (whole-scene-body
  generation / pool-partitioning) is deferred. ([PROVEN this session].)
- **Prompt-example contamination.** Models lift example phrases out of the prompt into
  output; never put quotable phrases in a prompt.
- **Subtle errors seep through** (`feedback_subtle_errors_and_ambiguity`): plausible-but-
  wrong invented facts in dialogue, ambiguity resolved wrong then amplified.
- **Coherence hides the defect — never review output-only.** colorize/factual content is good
  enough that a generated block reads *fine on its own*. That fluency is the hazard: the two
  failure modes that matter most — **dropped upstream info** and **invented facts** — are
  invisible unless you read the output *against its inputs* (beat, brief, COLOR pool). The
  errors aren't loud; they're omissions and quiet additions. Loud inventions are easy (a voice
  test once went full isekai, stepping the PC out of a magic portal — caught instantly); the
  dangerous ones are subtle and only surface in the diff against ground truth. The review
  surface must therefore put inputs beside the generated block (see §6).

---

## 5. Alignment flags — **TARGET CORRECTIONS HERE**

The places Claude suspects this model diverges from the user's:

1. **Maturity, flattened → and no end-to-end run yet.** The table risked reading all stages as
   equally settled; the per-stage maturity tags now fix that (1–3 exercised, 4–5 experiments,
   6–7 existing tooling). The real gap underneath: **no complete arc has been taken start to
   finish through the whole pipeline.** Signal_array is the closest and it stalls at Stage 3.
   - **Next concrete step:** push one arc all the way through, end to end, and learn from what
     breaks. That run is the only thing that will tell us whether the staging is even right.
   - **Posture (this session):** don't be paralyzed waiting for perfect signal. Make educated
     guesses from good UX judgment and the signals we already have, ship them, and
     **course-correct as we learn.** The doc is allowed to be wrong in places; the end-to-end
     run is how we find out where. Build-and-learn beats analysis-paralysis here.
2. **Tool-centric vs. author-centric framing.** **RESOLVED (this session): author-centric.**
   The real doc's spine is the human's loop and decisions; tools are servants to that loop, not
   the organizing principle. Two guardrails on the rewrite, so it doesn't overcorrect:
   - **Stay observable.** Author-centric does *not* mean hand-wavy. Every step still surfaces
     *this specific run accepted X and yielded Y*, presented transparently (§6a). The author's
     loop is the spine; the input→output trace is what makes each turn of it legible.
   - **Build a flywheel, not a bag of tools.** The goal is a process that *steers* the author
     toward good output — defaults, gates, and visibility that make the right move the easy
     move. It is **not** a pile of commands the author must memorize and sequence correctly to
     avoid failure. If avoiding a failure mode depends on the author remembering to run the
     right tool in the right order, that's a design miss: fold the safeguard into the flow.
   - **Current position is always cheaply knowable.** Where each unit sits in the pipeline must
     be something the author can *directly check* or Claude can *trivially calculate* from the
     artifacts — never something to track in their head. The author should never have to worry
     that they're about to feed Stage-1 input through Stage 4 and break something; the flow
     should know the stage and refuse (or guide) accordingly. (This is the steering job of the
     `arc status` / stage-stack visibility in §6a, turned into a guardrail rather than a tool
     the author must remember to consult.)
3. **Stage-0 intent layer.** ~~Asserted brief + 4 bibles + per-scene lens are all required
   hand-authored prerequisites.~~ **CORRECTED (this session):** Stage 0 is AI-mediated
   (brainstorm → unpack → generate files → review), not hand-authored — see §3 Stage 0.
   Remaining opens: which artifacts are truly required, the order, and how much of the
   brainstorm/unpack migrates from claude.ai into Claude Code.
4. **Deeper premise (the unit question).** *Merged into §6b* — "is the unit per-beat / is it
   linear or looping?" turns out to be the same problem as the authoring-surface representation
   strain, so the two are now treated together there (grounded in the `Mareen.enc` hub).

---

## 6. Two unsolved design problems

The earlier version of this section was a flat list of small "tooling gaps." That undersold
two real, open design problems. Both are unsolved; neither is just a missing command.

### 6a. Observability — "this looks like X because the inputs looked like Y"

What "observable" means here is **visibility into prior states and the supporting artifacts**:
the author should be able to look at any generated passage and trace *why it came out the way
it did* — which beat, which COLOR lines, which brief slice, which lens fed it.

The crucial nuance: **the author will almost never fix an error by repairing the upstream
artifact.** Repairing the brief and re-running is not the loop. The visibility earns its keep
two other ways:

1. **Hand-correction.** To fix the current passage you have to see the ground truth it was
   supposed to honor — otherwise you can't tell a dropped fact from a deliberate omission, or
   an invention from a legitimate inference (§4, "coherence hides the defect").
2. **Building authoring intuition.** Seeing *output-as-a-function-of-input*, repeatedly, is how
   the author learns to write upstream content that behaves — content that doesn't leak weird
   phrases downstream and doesn't get facts lost. This is the long-game payoff: better Stage-0
   craft, learned from watching what the machine does with it.

Concrete shapes this could take (recommended, not built):
- **Side-by-side review surface (inputs ‖ generated block).** Render the generated block next
  to the beat stub, relevant brief slice, COLOR pool, and lens — so dropped facts and
  inventions read as a diff against ground truth instead of living in the reviewer's head.
- **`arc status <dir>`** — print each beat's completed-stage stack (from
  `DraftBlocks.ListAdjacentBlocks`) so progress and gaps are visible at a glance.
- **Structural views (Stage 1).** A graph viz of the arc's flow, and an interactive simulator
  that flags danger-zone structure likely to enrich badly downstream (e.g. an oversized hub
  body block colorize/voice would inflate into something degenerate). Same observability
  spirit, aimed at structure rather than prose.

### 6b. The unit-and-representation problem (merged with the old §5.4 premise flag)

Two questions that looked separate are actually one: **what is the authorable/reviewable unit,
and how is it represented on disk?** The current answers — "the unit is roughly a file/beat"
and "drafts live as `FIXME` + `# --- KIND ---` comment blocks; just edit the file" — were both
shaped by a **single one-shot cleanup pass**. They break together on complex hubs.

**Concrete breakage: `text/encounters/arcs/forest/the_fugitive/Mareen.enc`.** One file, but a
revisitable hub with ~9 choices, each carrying its own `@if/@elif/@else` branches gated on a
large *accumulating* tag-state space (`fugitive.knife_truth`, `maren_engaged`, `maren_defies`
/ `maren_accepts` / `maren_self_marks` / `maren_steps_down`, `loophole`, `saw_gault`, …). It is
re-entered as state changes (`+open "Mareen"`). So a single visit is not the unit — the file
packs many **disjoint, state-conditioned passages**, each effectively its own beat with its own
prose that wants its own draft stack.

Why this breaks the current model, on three entangled axes:

- **The unit (old §5.4).** "Per file" is wrong and "per beat" is ambiguous: the real authorable
  unit is the **branch-passage — a state-conditioned view** — and there are many per hub file.
- **Time / looping (old §5.4).** The flow is not linear. A hub is re-entered over a playthrough
  and holds states reached at *different times*. "Colorize this beat" is ill-defined when the
  beat is really N variants seen at N points, each wanting its own treatment and its own
  review-against-inputs (§6a).
- **Representation overload (old §6b).** One `FIXME` per file + one top-of-file COLOR pool
  cannot key N branch-passages, each accreting `COLOR → FACTUAL → VOICED×N → CRITIC×N`. Cram a
  per-branch revision history into the comment channel and it becomes an unreadable
  pseudo-database it was never meant to be.

**Open (the workflow's largest):** the authorable unit is probably the **branch-passage, not
the file**, and the flow **loops**, not lines up — which forces a representation that can
address sub-file units and carry a draft stack per unit (structured sidecar / out-of-band
revision store) rather than the flat `FIXME` + comment convention. A near-term symptom worth
fixing regardless of the bigger decision: **`check` should warn on `FIXME(register):`** stubs,
not only bare `FIXME:`, or a forgotten stub ships to players un-warned.

---

## 7. Open decisions

- **Final location/shape of this doc** (currently a draft in `plans/`; candidates were
  `project/encounter-spec/arc_authoring_workflow.md` or a tool README section). Shape is now
  decided to be **author-centric** (§5.2): the eventual rewrite reorganizes around the human's
  loop with tools as servants, replacing the current command-organized stage table. Not done
  yet — this draft is still command-organized.
- **Stage-1 slang/levity cleanup:** built as a tool (lint/critic) or left a manual gate
  item? halt vs. auto-rewrite? (Relocated earlier from a pre-voice check — see §3 Stage 1, §4.)
- **Per-beat repetition fix:** whole-scene-body generation as a follow-up?
- **Observability (§6a):** build the visibility tooling (side-by-side surface, `arc status`,
  structural views) now or defer? Note its payoff is hand-correction + authoring intuition,
  *not* upstream repair.
- **Unit + representation + looping (§6b — the workflow's largest open question):** is the
  authorable unit the branch-passage rather than the file, given revisitable hubs like
  `Mareen.enc` that hold many state-conditioned passages reached at different times? And does
  that force replacing the `FIXME` + comment-block substrate with one that addresses sub-file
  units and carries a per-unit draft stack (structured sidecar / out-of-band revision store)?
- **Voice integration:** when the `../voicer` research settles, rebuild it into EncounterCli
  (as `../factual` → `factual` was done) and pin the real command/model/program surface.
- **Disposition of the scrub arc(s).** Concretely, there is **one** arc that has actually
  been pushed through the pipeline and now sits in disrepair: **`scrub/signal_array`** (the
  proving ground for earlier pipeline versions). The other 18 roster arcs carry *no* pipeline
  markers — they are skeleton stubs (10) or flesh-out prose (8), i.e. unstarted, not damaged.
  So this decision is really just about signal_array. Its disrepair is internal:
  - **Two parallel file sets.** 10 live `X.enc` (regenerated Jun 5) and 10 orphaned `_X.enc`
    scratch copies (Jun 2–3). Non-comment content is byte-identical across a pair; they differ
    only in generated comment blocks. The `_X` set is stale scratch from an earlier colorize
    pass — redundant cruft, safe to delete once confirmed.
  - **Ragged pipeline front.** All 10 live scenes have a COLOR pool (Stage 2 ran); only
    `The Rest Interval` reached FACTUAL (Stage 3, 17 beats). No VOICED, no CRITIC anywhere.
  - **Stage-2 curation never applied.** Every COLOR line is still `# []` (unticked) — the
    cull+repair gate hasn't run even where colorize completed, so the pools are raw GLM output.
  - **Stage-0 substrate is healthy.** brief (`TheSignalArray.md`), 4 bibles
    (`_cast`/`_scenes`/`_set`/`_color`), 10 per-scene `.lens.md` all present.

  Open: repair signal_array back to a clean gate, or restart its prose layers from the (good)
  brief + bibles? The error-amplification principle (§4, `feedback_pipeline_error_amplification`)
  and the "coherence hides the defect" hazard (§4) both lean toward **restart-from-a-clean-gate**:
  a half-repaired arc can carry subtle, fluent-looking upstream corruption forward invisibly.
  The actionable middle path here is unusually clean because Stage 1 structure is sound and
  identical across the `_X`/`X` pairs: **(1)** delete the `_X.enc` scratch set; **(2)** keep the
  `check`-clean Stage-1 structure in the live `X.enc`; **(3)** discard the raw COLOR/FACTUAL
  comment blocks below that line and re-run colorize→factual fresh under the current method,
  rather than hand-curating month-old uncurated pools. Don't repair prose you can't cheaply
  re-verify against inputs.

---

## 8. Author UX (proposed)

The author-centric framing (§5.2) made concrete: how the human actually moves through the
work. This is a proposal, not built — it's the target the eventual rewrite (§7) organizes
around. It assumes much of Stage 0 happens **entirely outside our ecosystem** (claude.ai,
research, idle thinking) and that the only thing crossing the boundary is a freeform outline.

### 8.1 The spine — one verb, a cockpit that recommends and waits

The author mostly isn't choosing a command. They're answering the question the system keeps
asking: *"here's the next thing that needs your judgment, and everything you need to judge it —
go."* Everything mechanical runs underneath.

That means **one surface the author returns to** — the *cockpit* (`arc` with no verb, or
`arc status` grown a spine). It shows every unit's stage at a glance (position always knowable,
§5.2), recommends the single sensible next action, and refuses out-of-order moves. It
**recommends and waits — it never autopilots.** The author keeps the wheel.

### 8.2 Review time is generation time; keep chunks small

A load-bearing principle, not a nicety:

- **Pipeline review against generation.** Reading, tweaking, and annotating is slow human time.
  Use it: while the author works chunk N, the system generates chunk N+1 in the background.
  Review and generation overlap instead of taking turns. Composes with batch-by-model (§4) —
  the long instruct/base batches run *while* the author curates earlier cards.
- **Small chunks.** The unit of review is one small card the author can finish in a sitting —
  never "review 10 variations across 7 files before anything moves." Progress stays continuous,
  and the author is never blocked behind a giant review wall.

### 8.3 The loop, sitting by sitting

- **Sitting 0 — outside our world.** Brainstorm on claude.ai over many revisions → a high-level
  picture (happy path + themes) → unpack into explicit happy path + branches + decision points.
  Out comes **one freeform outline doc.** No tooling of ours. The only contract is the handoff:
  it lands in the arc dir (or the author points the tool at it). We do *not* demand a rigid
  format — structuring it is our job, not theirs.
- **Front loop — structure and substrate co-evolve (NOT a gated sequence).** Decompose and the
  bibles grow *together*. Brainstorming a new scene while decomposing is exactly when the author
  adds durable color (`_color`), character detail (`_cast`), and scene steering (`_scenes`).
  There is no "finish bibles → gate → decompose" order; it's one iterative co-authoring loop,
  the cockpit surfacing each generated/edited artifact beside its justification (§6a). *(This
  softens the §3 Stage-0 `0c` → Stage-1 boundary — in practice they are one loop.)*
- **The one hard gate — structure frozen.** The load-bearing sign-off (§3 Stage 1): complete,
  playable, dramatically satisfying, bland; slang scrubbed; walk/sim/viz clean. The cockpit
  **refuses prose work until the author signs off here.** That single enforced gate is what
  guarantees the author can never feed Stage-1 input through Stage 4 — the system won't offer it.
- **Prose loop — driven, batched by model, pipelined.** Author says "go." The system runs all
  the instruct work (colorize → factual) across the arc in one batch, then hands back a **review
  queue**: one small card per branch-passage, each showing generated output beside its inputs
  (beat + color + brief + lens) so dropped facts/inventions read as a diff (§4, §6a). Author
  works the queue: cull + repair. Only when curated does the system run the **separate** glm-base
  voice batch and queue those cards. Background generation keeps the queue fed while the author
  curates (§8.2). The author experiences two work phases, not a model-swap storm.
- **Promote.** Per card, the author picks the winning passage; the system lifts it into live
  prose and clears the drafts. (The §6b frontier — UX designed around the branch-passage even
  though the storage to back it cleanly is unsolved.)
- **Ship.** `arc ship` = check + bundle + push, and offers a final walk. Never hand-assembled.

### 8.4 What this leans on (open questions)

- **Review unit = branch-passage** (§6b). Right unit, blocked on the representation. Interim:
  per-file cards with branch sub-sections; tighten to true per-branch when the substrate lands.
- **The cockpit *is* §6a observability + the §5.2 position-knowable guardrail**, fused into the
  one surface the author already looks at — not a separate tool to remember (the flywheel, §5.2).
