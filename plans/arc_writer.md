---
kind: plan
title: Arc-writer authoring tool (peer to encounter-cli, fixme replacement)
state: exploring
created: 2026-05-30
updated: 2026-05-30
touches:
  files:
    - text/encounter-tool/EncounterCli/FixmeCommand.cs
    - text/encounter-tool/EncounterCli/
    - text/encounters/arcs/
    - project/encounter-spec/
  features: [authoring, llm, arcs, fixme-replacement]
---

# Arc-Writer

A peer tool to `EncounterCli` whose remit is the *whole arc*, not the single
encounter. Pipeline-shaped: brief → structural decomposition → color → factual
prose → voiced variants → adversarial pass. The final artifact is a *messy but
legal* set of `.enc` files the author curates by hand — accepting bits,
rejecting bits, deleting the rest. When this tool can produce a structurally
sound arc skeleton on its own, `fixme` mode in `EncounterCli` goes away.

## Why a peer tool and not more `fixme`

`fixme` is a single-stub expander. It has no model of:

- the surrounding arc (what other encounters exist, what tags/qualities are
  in flight, what the hub structure looks like),
- diegetic state-carrying machinery (qualities, tags, choice gating) that
  the eight hand-built arcs use to avoid dialog repetition and dead loops,
- author voice as something distinct from factual content,
- review-by-curation as an interaction shape — `fixme` writes one
  REVIEW: blob per FIXME and asks the human to clean it up.

Trying to bolt these onto `fixme` would warp it. A separate tool can be
opinionated about pipeline shape and review workflow.

## Prior art: `/home/joseph/repos/ngraph`

ngraph prototyped most of the individual passes we need; the *fact-graph
projection* (its core conceit) is not yielding amazing results, but the
surrounding research is directly reusable. Concretely:

- `experiments/prepass-02/run.sh` — bullet-proposal "texture pre-pass":
  qwen produces wiki-voiced, state-not-events, scope-tagged bullets that
  may contradict each other; the human curates. This is the prototype for
  our **colorize** mode. Key prompt rules to inherit: wiki voice only, no
  stylization, no narration of PC actions, ground every concrete detail in
  source material, contradictions allowed.
- `experiments/edge-writer-01/run.sh` — "factual writer" prompt:
  externally-verifiable fiction only, no PC interiority, do not invent
  beyond context, end at the moment the target state is established. This
  is our **factual writer** mode almost verbatim.
- `experiments/voice-rewrite-03/run.sh` — "ghost of Lovecraft" pass: a
  voiced draft revised against a canonical fact record, instructed to
  preserve voice while excising additions and PC interiority. This
  collapses voice-writer + adversarial-editor into a single call, which we
  may or may not want; see open questions.
- `plans/authoring-tool.md` — the broader design discussion, especially
  the resource ordering (`author wall-clock > author attention >
  cross-provider tokens > qwen compute > qwen tokens`) and the
  "background jobs while the author moves on" concurrency model.

What we do *not* inherit: the ngraph fact-graph projection itself. Arcs
stay in `.enc`. The pipeline operates on `.enc` files in place.

## Workflow

Six passes. Each pass reads `.enc` files in the arc directory and writes
back to those files, leaving prior-pass content in place (commented out or
labelled) so the curating author can compare. The tool never overwrites
without a backup; legal `.enc` syntax is preserved end-to-end so `check`
runs cleanly after every pass.

### 1. Decompose (Claude Code skill)

Input: a single markdown brief describing the arc — theme, setting,
factions, key beats, intended endings. (The existing arc `.md` files in
`text/encounters/arcs/*/*/` are the reference shape.)

Output: a set of `.enc` files realizing the arc's structure — hub(s),
spokes, terminal beats, qualities/tags wiring, choice gating, branch
mechanics, FIXME stubs in every prose slot. The result should `check`
clean and `walk` to every ending without infinite loops or dead choices.

Quality bar: **structure, not text**. The prose can be FIXME stubs or
one-line placeholders. The structural promise is:

- legal `.enc` syntax,
- every choice has a destination,
- every quality/tag set has a reader,
- every quality/tag read has a setter,
- no unreachable terminals, no infinite back-and-forth loops,
- choice gating with `[requires …]` is consistent with the qualities/tags
  the rest of the arc establishes,
- hubs are lightweight (per the inventory below),
- transit prose between encounters is acknowledged but not over-written.

Implemented as a **Claude Code skill** (not a CLI subcommand), because the
decomposition step benefits from interactive back-and-forth: the model
will sometimes anchor on a bad reading of the brief, miss an ending the
author cares about, or wire qualities in a way that doesn't fit the
established conventions. Skill UX lets the human redirect mid-flight
without restarting.

#### Prerequisite: inventory of authored techniques

Before the skill can be effective it needs a digest of the patterns the
eight hand-built arcs already use. This is a deliverable of this plan, to
live at `project/encounter-spec/arc_patterns.md`. Things to inventory by
reading every arc:

- **Edge-transit prose** — how an encounter acknowledges "the road
  between" without inventing new state. Examples in `the_hermitage`
  Start.enc → Garden.enc transitions; brides_cave Start.enc body.
- **Lightweight hubs** — Start.enc as a low-state, high-affordance
  branching point; `Yana.enc` / `The Lodge.enc` as mid-arc hubs.
- **Qualities for state without dialog repetition** — how arcs set
  `+quality knows_X 1` on first telling and gate later mentions on
  `quality knows_X 1`. Survey actual quality names in use, naming
  conventions, scoping.
- **Tags for permanent character marks** — when an arc uses `+tag
  fugitive_helped` vs a quality. (Tags = lifelong, qualities = arc-scoped
  numeric.)
- **Choice gating with `[requires …]`** — patterns for hiding choices
  the player can't sensibly take vs. presenting them grayed-out.
- **Branch fork conventions** — `@if check <skill>` placement, fallback
  branches, the `meets <skill> <tier>` gate.
- **Terminal beats** — arc reward delivery, settlement-return cues,
  the "wrap" idiom that gives the arc a felt ending.
- **Loop avoidance** — how repeat visits to a hub differ from the first,
  what qualities/tags are doing that work.
- **Conventions per biome** — register, voice, recurring imagery; what
  travels and what doesn't.

The inventory is *the brief Claude reads* when running the skill. Treat it
as a working document we revise as the skill produces (and the human
rejects) bad structural patterns.

### 2. Colorize (qwen, bullet pre-pass)

Per the ngraph `prepass-02` prototype. For each prose block in each `.enc`
file above a minimum word-count threshold (e.g. ≥40 words — skip "Bob
awaits your reply" stubs), qwen proposes N texture bullets:

- Wiki voice, flat. No stylization. No horror register.
- State, not events. No narration of PC actions.
- Bullets may contradict each other; that's desired diversity. The author
  curates at pick-time.
- Each bullet carries a scope label so the author can see which prose
  block(s) it belongs to.

Bullets are written into the `.enc` file as commented-out blocks under
the relevant prose block, in a syntax `check` ignores (TBD: leading `#`
lines? a dedicated `[colorize]` sub-block? — see open questions). The
human reviews in an editor, deletes rejected bullets, leaves accepted
bullets in place to feed pass 3.

### 3. Factual writer (qwen, per-block)

For each prose block that has accepted color bullets *and* a FIXME beat
summary, qwen produces a single block of factual prose:

- Integrates the accepted color bullets and the FIXME beat into one
  passage.
- Externally-verifiable fiction only. No PC interiority.
- Does not invent beyond color + beat + arc brief.
- Matches the structural slot (length, register-neutral).

Output replaces the FIXME line with a `FACTUAL:` block (analogous to
fixme's `REVIEW:` convention) so the author can still see what was
generated from what.

### 4. Voice writer (qwen w/ author programs)

For each `FACTUAL:` block, qwen produces M voiced variants using the
existing `voices/<author>_<scene>_program.json` programs (HPL, REH, CAS
× mundane/action/horror/dread/wonder/revelation). Voiced variants are
written as adjacent `VOICED-<author>-<scene>:` blocks. The arc's `.md`
brief or per-encounter front matter declares the intended author/scene
defaults so the tool doesn't need a flag-per-block.

### 5. Adversarial editor (cross-provider critic)

Two runs:

- **5a.** After factual writer, run a cross-provider model (haiku or
  gpt-mini, whichever is cheaper that month) over each `FACTUAL:` block
  with the arc brief + color bullets as ground truth. Critic flags
  invention, scope leak, contradiction, PC-interiority. Findings attach
  inline as `[critic: …]` annotations.
- **5b.** After voice writer, run the same critic over each `VOICED-…:`
  block with the matching `FACTUAL:` block as ground truth. Voice
  variants that score below a threshold (TBD; metric TBD — likely just a
  count of severity-weighted findings) get a regeneration with the
  critic findings folded into the prompt (à la ngraph's
  `voice-rewrite-03` "ghost of Lovecraft" pass: preserve voice, strip
  additions).

Adversarial output is non-blocking. The author sees the findings; the
author decides.

### 6. Compile + curate

End state per encounter file: legal `.enc` syntax, multiple proposed
prose passages per slot labelled by provenance (FACTUAL / VOICED-HPL-dread
/ VOICED-REH-action / …), critic annotations inline. The author opens
each file in an editor, picks the best passage per slot, deletes the
rest and the labels, runs `check`, ships. The "ideally still-compiling"
property is a hard requirement — at no point should a partially-curated
file fail `check` on syntax alone (semantic warnings allowed).

## CLI surface (proposed)

Sibling to `EncounterCli`. Working name `ArcCli` until something better
surfaces.

```
arc decompose <brief.md> --out <arc-dir>     # skill-driven; CLI is a thin entry point
arc colorize <arc-dir> [--min-words 40] [--per-block 6]
arc factual <arc-dir>
arc voice <arc-dir> [--author HPL] [--scene dread] [--variants 3]
arc critic <arc-dir> [--phase factual|voice] [--provider haiku|gpt-mini]
arc check <arc-dir>                           # re-export of EncounterCli check
```

Each pass operates on the arc directory as a unit and is idempotent: it
skips blocks already at-or-past its stage unless `--force` is passed.

## Coexistence with `fixme`

`fixme` keeps working until `arc decompose` + `arc factual` together can
replicate its current value at equal or better quality. The retire test:
take a representative selection of FIXMEs from existing arcs, run the new
pipeline cold, compare. When the new tool's output is ≥ `fixme` output in
the author's judgement, delete `FixmeCommand.cs`, `ExpandClient.cs`,
`LoraClient.cs`, the `voices/` programs (or move them to the new tool),
and the `--expand-url` plumbing.

## Build sequence

1. **Pattern inventory** — read every existing arc, write
   `project/encounter-spec/arc_patterns.md`. Independent of any code.
2. **Decompose skill** — Claude Code skill that reads the pattern
   inventory + a brief, produces a structurally-sound `.enc` skeleton.
   Iterate on real briefs until the structural quality bar holds.
3. **Colorize pass** — port `ngraph/experiments/prepass-02` into a CLI
   subcommand that operates on `.enc` blocks in place. Settle the
   in-file annotation syntax (see open questions).
4. **Factual writer pass** — port `ngraph/experiments/edge-writer-01`
   with color-bullet integration.
5. **Voice writer pass** — wire the existing `voices/` programs into the
   per-block voicing call.
6. **Adversarial editor** — cross-provider critic, post-factual and
   post-voice variants.
7. **`fixme` retirement** — once the bar is met, delete the old code.

Steps 3–6 can land in any order after the syntax is settled, and each is
independently shippable.

## Open questions

- **In-file annotation syntax.** Colorize bullets, FACTUAL blocks,
  VOICED blocks, and critic findings all need to coexist inside `.enc`
  files without breaking `check`. Options: `#` line comments (need to
  extend the parser); a dedicated `[draft]` sub-block (parser change);
  or a sidecar file per encounter (`.enc.drafts`) keyed by line number
  (no parser change but harder to curate in an editor). Sidecar feels
  cleanest but loses the in-editor "everything in one place" property.
- **Block addressability.** Pass 2+ need a stable way to refer to "the
  prose block under choice 3 of encounter X". Filename + line range is
  fragile across edits. Choice-id + beat-id is more stable but requires
  giving every beat an id.
- **Decompose skill vs. subcommand.** The plan says skill. Is there a
  case for a CLI-driven decompose that calls Claude as a subprocess via
  the existing `LlmClient`? Skill lets us use Opus and interactive
  steering; subprocess is reproducible and scriptable.
- **Critic provider.** Haiku (metered Anthropic spend) vs. Azure
  gpt-mini ($50/mo MSDN credit, hard ceiling). Project-configurable, but
  what's the default?
- **Voice variant count.** ngraph found 1 voiced variant was usually
  enough if the voice program was good. The brief asks for "several."
  Start with 2 and let the author flag if they want more.
- **Scoring rubric for adversarial pass.** What numeric threshold
  triggers regeneration? Probably "any high-severity finding" rather
  than a count, but TBD after we see real critic output.
- **Working directory layout under an arc.** Do FACTUAL/VOICED/critic
  go inline in `.enc`, in a sidecar `.drafts/` dir, or in a single
  `<arc>/draft.json`? Trades editor ergonomics against parser cleanliness.
- **Skill name.** `/arc-decompose`? `/arc-skeleton`? `/arc-write`?
- **Tool name.** `ArcCli` is a placeholder.
- **Continuity-critic pass (post-voice, investigate).** The existing
  critic (5a/5b) checks each block against the brief + color for
  invention, scope leak, PC interiority. It does not cross-check
  prose against the *adjacent text the player will actually read*.
  Real failures observed in the `relay_post` decomposition pass:
  the transit prose of a `+open` choice and the body of the next
  encounter contradicting each other; the hub body re-establishing
  an arrival moment that already played in the transit; bare NPC
  interiority surviving the factual pass. A post-voice continuity
  critic could catch these by walking the runtime graph and, for
  each prose block, bundling the deterministically-derivable
  immediate context for the critique prompt:
    - **preceding encounter body** (the static frame the player
      read most recently — the body of whichever encounter `+open`ed
      to this one),
    - **transit prose** (the outgoing-outcome block of the
      `+open`-bearing choice that just fired — the prose
      *immediately* before the block under critique),
    - **the block under critique** (the FACTUAL or VOICED prose to
      evaluate).
  These three pieces are derivable from the parsed encounter graph
  modulo skill-check branches (which fan out non-deterministically;
  treat each branch as a separate critique input). Critic prompt
  becomes: "given that the player just read X then Y, does Z
  follow as continuous prose without restating, contradicting, or
  re-establishing the same beat?" Worth prototyping after the
  pipeline ships its first full arc end-to-end. Related: the
  arc-decompose skill's Step 4 continuity rules (which the skill
  owns at the stub level) and the FactualCommand HARD RULE 1a
  (which the factual pass owns at the per-block level) — both
  upstream of this, neither catches cross-file inconsistency.

## What this plan does not cover

- Migration of existing arcs through the new pipeline (they're already
  shipped; no reason to rerun).
- Generation of locale guides, archetype pools, or other corpus material
  consumed by the pipeline; those stay in their current locations.
- A web UI. This is a CLI + skill. A web UI is a separate later plan if
  the editor-based curation step is too painful.
- Combat / `.fight` file authoring. Out of scope; arcs reference fights,
  they don't author them.
