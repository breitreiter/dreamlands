---
kind: plan
title: "Process the scrub-biome arcs through Forge (safely)"
state: exploring
created: 2026-06-22
status: in-flight — procedure for running the 4 scrub arcs through the Forge pipeline without risking the (good) originals. ORDER SUPERSEDED 2026-07-27 by plans/finish_scrub_arcs.md; this doc remains authoritative for the per-arc procedure + safety guarantee. the_villa is integrated + verified and awaiting promotion; relay_post/signal_array need threads; foundry needs substrate. See "Current state" (verified 2026-07-26). Depends on plans/forge_dotnet_port.md (pipeline shipped, live-validated 2026-06-21).
touches:
  files:
    - text/encounters/arcs/scrub/the_villa/
    - text/encounters/arcs/scrub/relay_post/
    - text/encounters/arcs/scrub/signal_array/
    - text/encounters/arcs/scrub/foundry/
    - text/encounter-tool/Forge/
  features: [forge, pipeline, scrub-arcs]
provenance:
  author: claude
---

# Process the scrub-biome arcs through Forge

The Forge pipeline is shipped and live-validated (`plans/forge_dotnet_port.md`).
This plan is the **operational procedure** for running the four scrub arcs through
it — and, above all, doing so **without risk to the originals**, which are in good
shape and must not be damaged.

## Current state (verified 2026-07-26 — read this first)

Work paused early July. Verified against disk, not memory:

All four arcs have **check-clean `.enc` skeletons**; none but `the_villa` has been
through any Forge stage (no sidecars exist anywhere for the other three). The June
readiness table below **understated** `foundry` — corrected here.

| Arc | `.enc` | Forge substrate | Threads | Next action |
|---|---|---|---|---|
| `the_villa` | 5, prose done | full | 5, 39/39 covered | **promote** (step 9, gated) |
| `signal_array` | 10, check-clean¹ | **full** (4 bibles + 10 lens) | none | author threads |
| `relay_post` | 6, check-clean | **full** (4 bibles + 6 lens) | none | author threads |
| `foundry` | 7, check-clean | `_scenes` + 22KB sketch; **no bibles, no lens** | none | author bibles + 7 lens |

¹ one warning: `signal_array/Start.enc:7` is a bare `FIXME:` (no `(tone)` tag) whose
stub prose is also visibly unpolished (lowercase `kesharat`, PC-interiority musing).
It is the only check warning across the three unprocessed arcs. Fix at the substrate
layer before parse — an untagged beat gets no tone steer into color.

**`foundry` is much further along than "partial" implies.** Its sketch was rewritten
PC-POV (`c808cd6`) and is the worked example the `arc-sketch` skill itself teaches
from (the scene-5 lamp/press opening; the "he is watching the PC" blocking example).
Its skeleton was then *restructured* after decompose (`8015f15`): `The Question.enc`
and `The Switch.enc` were deleted and folded into `Control Room.enc`, with bodies
rewritten (-172/+102). So the narrative and structural work is done and deliberate.
What it lacks is only the **Forge color substrate** — `_cast`/`_set`/`_color` plus a
per-scene `*.lens.md` for each of its 7 files. That is a bounded authoring task
against an existing sketch, not a from-scratch arc-sketch effort.

Conversely `relay_post` and `signal_array` are *ahead* of foundry on exactly that
axis (every scene already has a lens) and behind it on nothing — their single
blocker is **threads**.

### The relay_post "blocking" problem — SOLVED, do not re-litigate

**Fictional blocking**: who is physically present in a scene, and whether the action
is plausible given that. The canonical failure: the PC and NPC 1 are in a room, NPC 2
enters and proposes betraying NPC 1 — *in front of NPC 1*.

This was found and fixed in the `relay_post` re-scaffold (`54685a8`), where the arc
was re-decomposed from scratch and walked beat by beat, each drift class encoded back
into the `arc-decompose` skill. The governing rule now lives there:

> Staging must be physically plausible: **track NPC location, justify awake-at-night,
> supply a privacy mechanism for "private" convos in shared spaces.**

`Midnight.enc` implements all three deliberately — Ossal's location is tracked (closed
storeroom door), his being awake is justified and *used* as a reveal ("fully dressed;
he was not asleep"), and privacy is mechanical: he pulls the PC into the storeroom and
closes the door ("Devra cannot hear you in here"), then returns them to the chair.
Devra's approach mirrors it (crouches at the chair, speaks low, glances at the door).

`signal_array` got the same discipline: `The Rest Interval` makes earshot an explicit
constraint ("the circle is small enough that anything said carries") and then *uses*
it — the crew answers on Chorik's behalf across the rock — while `Chorik.enc` moves him
"a few meters from the group" to buy privacy, and pays it off (he is "afraid of being
heard"; another worker calls him back).

**So this is not an outstanding blocker on either arc.** Both are staging-clean.

Residuals found and patched 2026-07-26: `Midnight.enc`'s unconditional hub body
asserted "Ossal is asleep on the cot inside" and re-rendered on every spoke return,
contradicting the reveal the PC had just seen — reworded to "Ossal turned in behind
it," which holds in both states while preserving the sleep inference the reveal
depends on. (A conditional body was rejected: **no body-level `@if` exists anywhere in
the corpus** and the format spec documents `@if` only inside choice outcomes — don't
introduce the construct in a Forge-bound file.) Also fixed a typo in `Chorik.enc`.

### The still-open continuity class (different problem)

Distinct from blocking, and **not** fixed: the cross-file prose-continuity class at
`plans/arc_writer.md:298` — transit prose of a `+open` choice contradicting the body it
opens into; a hub body re-establishing an arrival that already played in transit; bare
NPC interiority surviving the factual pass.

No single-file check catches these; each file is individually valid and the defect
exists only in read-order. The proposed fix (a post-weave continuity critic walking the
runtime graph, critiquing each block against the preceding encounter body + the transit
prose that fired into it) was deferred until "the pipeline ships its first full arc
end-to-end." **`the_villa` has now met that condition.** Before building it, check the
villa's woven output for arrival-restatement — weave's story-so-far may already
suppress much of this class, which would make the critic unnecessary.

**Where the_villa's live state actually lives — the thing that will trip you up:**
the working copy is `~/repos/narr/forge/arcs/the_villa/` (**outside this repo**).
*Not* because the run used Tier-2 isolation — that was a wrong inference made on
2026-07-26 and corrected 2026-07-27. `~/repos/narr/forge` is the **original Python
forge**, its own git repo (`.git` inside `forge/`, with `weave.py`, `synthesis.py`,
`thread.py`, `color.py` still in place), and `arcs/the_villa/` is **tracked there**
— it was the corpus forge was developed *against*, which is why its history reads as
the development of the method itself (seed spine → categorize/color → synthesis →
threaded story-so-far → kimi integrator). The villa ran in the Python tool's own
working directory because that was the natural place at the time. Its sidecars are
the current ones (39/39 woven, curated, `final`+`approved`). The **in-repo** sidecars at
`text/encounters/arcs/scrub/the_villa/*.enc.json` are **stale** (Jun 28, 33/39
woven) — do not read them as state. Integrated output is
`text/encounter-tool/out/the_villa/*.enc` (cwd was `text/encounter-tool/`, so
`out/` landed there, not at repo root; an older `out/compare/` also exists at repo
root from earlier runs).

The in-repo `.enc` are the tracked **FIXME-stub baseline** (39 FIXMEs). Promotion
overwrites them, so it produces a reviewable git diff — it is not a new-file drop.
`source_sha` matches between narr and the repo for all 5 files, so the integrate
guard is satisfied and promotion is unblocked.

Verified at pause: 5 threads (`vastand`, `cave`, `decline`, `giveover`, `arson`)
covering 39/39 beats with 0 uncovered; `check` clean on all 5 integrated files;
0 FIXME remaining; diff vs source is pure FIXME→prose swaps. Curation applied 3
hand repairs (`The Decision:5`, `The Decision:11`, `The Early Pages:24`); the
`arson`-thread contamination risk flagged in `Threads.cs` for `The Decision:28`
was checked and **did not manifest** — that beat reads independent.

**Durability risk — RESOLVED 2026-07-27.** The 39 curated beats and every paid kimi
call were sitting **uncommitted** in narr/forge's working tree. (They were never
"gitignored with no version history" as first written — narr/forge tracks `arcs/`;
only `out/`, `__pycache__/`, `*.pyc` are ignored there. The dreamlands-side
statements about gitignored sidecars, below, remain correct — that is a different
repo with different rules.) Committed as `2ae9d9a` in `~/repos/narr/forge`: 12 new
`synthesis_sofar` weaves plus `final`+`approved` on all 39 beats. Verified before
committing that all 39 `original` stubs were byte-identical to the prior commit, so
there was no source drift; the rest of that diff was JSON re-serialization noise.
Losing the dir no longer costs the weave spend or the curation judgment.

**Also uncommitted:** the minrouter migration in `text/encounter-tool/Forge/`
(new `RouterClient.cs`; `GlmClient`/`GatewayClient` deleted). Builds clean. This is
pipeline tooling and belongs on the base branch — keep the villa's eventual diff
purely prose. Router is live and authenticating at `imp:8086`, but `GET /v1/models`
404s; use `GET /help` (with bearer) to list upstreams, and confirm the exact path
Forge calls before spending on a weave.

## The safety guarantee (why this is low-risk by construction)

Forge's peer-JSON model is built so the source `.enc` is **never modified**:

- `parse`/`categorize`/`color`/`synthesis`/`weave` write **only** to the `X.enc.json`
  sidecar (gitignored) — never to the `.enc`.
- `integrate` is the only thing that emits a finished `.enc`, and it writes to
  **`out/<arc>/`** (gitignored), never over the source. With nothing approved it
  reproduces the source byte-for-byte (proven round-trip).
- `integrate` verifies `source_sha` and **aborts** if the source drifted since parse.
- `*.enc.json`, `out/`, `*.key` are all gitignored.

So the entire generation pipeline cannot touch a tracked file. **The only operation
that mutates a canonical `.enc` is the final promotion** (landing finished prose into
the repo) — and that is a deliberate, branch-gated, human-reviewed git step. All the
rigor goes there.

Two tiers of isolation; pick per appetite:

- **Tier 1 (default, in-place):** run the pipeline in the real arc dir. Sidecars
  accrue beside the originals but are gitignored; originals are read-only by
  construction. Simplest; safety is architectural.
- **Tier 2 (paranoia):** `cp` the arc to a gitignored scratch dir (e.g.
  `/tmp/forge-work/<arc>/` or a throwaway git worktree) and run everything there;
  the real repo is never even opened for write until promotion. Recommended for the
  first production arc until we trust the flow, then drop to Tier 1.

## Readiness assessment (scouted 2026-06-22 — historical; see Current state above)

| Arc | enc | substrate | threads | Pipeline-ready? |
|---|---|---|---|---|
| `the_villa` | 5 | full (4 bibles + 5 lens) | **vastand, cave** (in `Threads.cs`) | ✅ proven (live smoke) |
| `relay_post` | 6 | full (4 bibles + 6 lens) | none yet | substrate yes; **needs threads** |
| `signal_array` | 10 | full (4 bibles + 10 lens) | none yet | substrate yes; **needs threads** |
| `foundry` | 7 | **partial — only `_scenes.md` + brief** | none yet | **NO — needs lens + `_cast`/`_set`/`_color`** |

Originals are all committed clean; `the_villa`'s dreamlands `.enc` are byte-identical
to forge's authored copies.

## Prerequisites (gaps to close before/within processing)

1. **Foundry substrate. → DECIDED: separate later effort.** *(2026-07-26: the
   "needs arc-sketch" framing below is too pessimistic — foundry's sketch and
   skeleton are done and deliberate; only the bibles + lenses are missing. See
   Current state. Re-decide whether it still belongs outside wave 1.)* foundry has no lens files
   and is missing the `_cast`, `_set`, `_color` bibles. Color's highest-leverage steer
   is the per-scene lens; without it, color quality drops. foundry is **excluded from
   wave 1**; its bibles + per-scene lenses are authored as their own task
   (arc-sketch), and it feeds back into this procedure once ready.

2. **Thread definitions per arc.** `weave` (the quality path — threaded story-so-far)
   walks named threads; only `the_villa` has them (`Threads.cs`). relay_post and
   signal_array need thread plans authored — a covering set of ordered choice-label
   lists whose union touches **every beat** (each beat must appear on ≥1 thread, or it
   gets no woven prose). Author these against each arc's choice graph (use
   `forge thread <arc> --thread <name>` to dry-check a plan's spine before generating).
   - *Coverage check:* after defining threads, diff the union of their beat spines
     against the arc's full beat set; any uncovered beat falls back to isolated
     `synthesis` (lower quality — flag it).
   - *Open:* `Threads.cs` is currently a compiled-in dictionary. For N arcs we likely
     want threads loaded from a per-arc file (e.g. `_threads.md`/`.json` in the arc
     dir) rather than recompiling. Decide before relay_post.

3. **Curation / approve step (missing tool). → DECIDED: manual JSON for now.**
   `weave` writes `stages.synthesis_sofar[<model>]`; `integrate` only splices beats
   with `final` set + `approved:true`. Nothing bridges that yet. **For the first wave
   we do it by hand-editing the sidecar JSON** (set each approved beat's `final` to
   the chosen/repaired prose and `approved:true`), with the steps documented as we go;
   a `forge lock` helper gets built later once the curation UX has been felt in
   practice. Per `feedback_curation_is_repair`, curation is cull **+ repair** (an edit
   surface, not a checkbox) — so expect to edit prose in `final`, not just copy it.
   This remains the one real gap for end-to-end completion; the manual step is the
   interim bridge, not the destination.

## Per-arc procedure (the loop)

For each arc, on its own branch, Tier-1 or Tier-2 working copy:

1. **Branch.** `git switch -c forge/<arc>` off the clean baseline.
2. **Parse.** `forge parse <arc>` → sidecars. (Free, local.)
3. **Categorize.** `forge categorize <arc>` → tones. (GLM on imp; needs `glmchat`
   loaded. ~20s/beat.) Spot-check tones; `--force` re-tags outliers if needed.
4. **Color.** `forge color <arc>` → gemma color bank. (imp loom; **overnight per
   arc**, box-to-itself. loom is a script — free the box with `swap-model stop all`,
   then `color` drives `grind` over ssh.) Resumable; `--dry-run` first to verify the
   beats-job. Smoke one beat with `--limit 1` before committing the box overnight.
5. **Weave.** For each thread: `forge weave <arc> --thread <name> --config <cfg>`.
   (PAID Cloudflare gateway, ~1 min/beat with kimi+thinking; key in gitignored
   `appsettings.json`.) Crash-safe/resumable (per-beat save; re-run without `--force`
   reuses good cells, retries empties — verified). Renders a continuous-read `.md`
   per thread to `out/compare/` for review. Run uncovered beats through `synthesis`.
6. **Review & lock (curation).** Read each thread's continuous-read; cull + **repair**
   weak beats; set `final`+`approved` per beat (via the `forge lock` helper from
   prereq 3). This is the human quality gate — front-loaded rigor, no automated
   end-to-end (`feedback_pipeline_error_amplification`). Watch for the known subtle
   failures (`feedback_subtle_errors_and_ambiguity`): plausible-but-wrong facts,
   POV slips, ambiguity resolved wrong.
7. **Integrate.** `forge integrate <arc>` → `out/<arc>/*.enc` (FIXME lines replaced
   by approved prose; unapproved beats keep FIXME). `source_sha` guard protects
   against drift.
8. **Verify the product, not the source.** `forge check out/<arc>` (syntax clean,
   no FIXME left if fully locked). Diff `out/<arc>/<name>.enc` against the source to
   eyeball exactly what changed — should be only FIXME→prose line swaps.
9. **Promote (the one risky step).** Copy `out/<arc>/*.enc` over the canonical
   source on the branch. Re-run `encounter check` + `encounter bundle` to confirm
   the game still loads it. Commit; open a PR for line-by-line human review **before**
   it lands (never `git push`/merge without an explicit ask —
   `feedback_no_unprompted_push`).
10. **Rollback is trivial:** the branch isolates everything; sidecars/`out/` are
    gitignored; the baseline `.enc` is untouched until the PR merges.

## Order (DECIDED)

> ⚠️ **SUPERSEDED 2026-07-27 by `plans/finish_scrub_arcs.md`.** That plan owns the
> order, the prerequisites, and the finishing work. **This document remains
> authoritative for the per-arc procedure and the safety guarantee** — the steps
> above, not the sequencing below. The order is now
> **`the_villa` → `signal_array` → `relay_post` → `foundry`**, and the thread-file
> format is its own blocking task rather than a decision folded into `relay_post`.
> The historical wave-1 plan is kept below for provenance.

**Wave 1** *(historical)* — the three substrate-complete arcs:
1. **`the_villa`** — first real promotion. Already proven through weave; smallest with
   full threads. Use it to nail the manual-curation + promotion procedure end-to-end.
   ~~Tier-2 isolation for this first run.~~ *(Moot: the villa's weave + curation
   already happened, in the Python forge's own arc dir — see Current state. Only
   step 9, promotion, remains.)*
2. **`relay_post`** (6 enc) — author threads, run the now-proven loop. ~~Decide the
   per-arc thread-file format here.~~ *(Now T3 of the superseding plan — it blocks
   every remaining arc, so it cannot wait for arc #2.)*
3. **`signal_array`** (10 enc) — largest; same loop.

**Later** — `foundry`: excluded from wave 1. Author its substrate (bibles + per-scene
lenses) as a separate task, then run it through this same procedure. *(2026-07-27:
still last in the order, but for the opposite reason — not "not ready", just the
largest remaining authoring gap. Its sketch and skeleton are finished and
deliberate.)*

## Cost & resource notes

- **imp is serialized.** categorize (glmchat) and color (gemma triplet) both want the
  box, and color is overnight-per-arc and needs it to itself. Plan color as a
  kick-off-at-end-of-session job; don't block other work on it. Batch arcs so the box
  isn't thrashed between profiles.
- **Gateway spend** is per woven beat (paid even on `@cf` ids). `the_villa` vastand was
  19 calls; a 10-enc arc with several threads is materially more. Smoke `--limit 1`,
  review, then scale (`feedback` discipline from the port plan).
- **Key hygiene:** Cloudflare key lives only in gitignored `appsettings.json`; never
  commit it, never read from a path outside the repo.

## Open questions

- Per-arc thread storage (`_threads.*` in the arc dir) vs. compiled `Threads.cs`? Lean
  per-arc file before relay_post.
- `forge lock` curation UX — minimal CLI accept/edit vs. richer surface? Minimal first.
- Single integrator model, or A/B several per arc (the deferred `compare`/Phase 6) and
  pick? Lean single (kimi) for the first wave; revisit if quality wobbles.
- Does foundry belong in this plan at all, or a separate "finish foundry substrate"
  plan that feeds back here when ready? Lean: separate, referenced from here.
