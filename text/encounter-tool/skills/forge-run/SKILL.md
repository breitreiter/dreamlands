---
name: forge-run
description: Run one substrate-complete arc's skeleton `.enc` through the Forge prose pipeline (parse → categorize → color → weave → integrate → promote), with emphasis on the human-judgment PRE-KIMI color-review gate. The load-bearing skill is hand-critiquing the gemma color bank for hard misreads — referent/identity/medium/invented-entity SWAPS get flagged; interpretive/tonal/atmospheric invention is KEPT — so the paid weave is fed clean input and rubber-stamps instead of repairing-and-re-running. Use when an arc under `text/encounters/arcs/<biome>/<arc>/` has full substrate + (for the quality path) defined threads, and you are carrying it to finished prose. Covers the imp/box mechanics, the `forge review` gate, re-color vs accept decisions, and the manual-JSON curation bridge. Companion to `plans/scrub_arc_processing.md` (the per-arc procedure + safety guarantee) and `Forge/CLAUDE.md` (the pipeline internals).
---

# Forge run

Take one arc from FIXME-stub `.enc` to finished, promotable prose. The
pipeline (`parse → categorize → color → weave → integrate`) is mechanical;
**this skill is the human judgment around it** — above all the pre-kimi color
gate, where careful prep makes the paid assembler a rubber stamp.

The governing discipline (`feedback_pipeline_error_amplification`): **front-load
rigor.** kimi assembles beautifully *from clean input* and amplifies *dirty
input*. So we stop and verify on the last free artifact — the color bank —
**before** spending a cent on weave. We do not feed it slop and then repair +
re-run.

The safety guarantee is architectural (see the plan): every stage writes only
to the gitignored `X.enc.json` sidecar; the source `.enc` is untouchable until
a deliberate, branch-gated promotion. So the whole generation loop is low-risk
by construction — **all the rigor goes into the color gate and the final
promotion**, not into protecting the source.

## The loop (one arc)

Run from repo root. `A="text/encounters/arcs/<biome>/<arc>"`. `forge` =
`dotnet run --project text/encounter-tool/Forge --`.

1. **Branch.** `git switch -c forge/<arc>` off the clean baseline. (Pipeline
   tooling itself belongs on the base branch, not the arc branch — keep the arc
   branch's eventual diff purely prose.)
2. **Parse** (free, local). `forge parse "$A"` → one `*.enc.json` sidecar per
   `.enc`, gitignored. Confirm `git status` shows no sidecars.
3. **Categorize** (imp `glmchat`, free, ~fast). Load it yourself:
   `ssh imp '~/.local/bin/swap-model glmchat'` (blocks until healthy), confirm
   `curl -s http://imp:8080/v1/models`, then `forge categorize "$A"`.
   **Spot-check tones** (next section). Tone is a *soft* steer — don't fuss
   marginal calls.
4. **Color** (imp loom, free, box-to-itself). The loom is the gemma triplet
   `grind` script; it needs the whole box. Free it first:
   `ssh imp '~/.local/bin/swap-model stop all'`. **Smoke one beat**
   (`forge color "$A" --limit 1`), eyeball it, *then* the full run
   (`forge color "$A"` — does the remaining uncolored beats; resumable,
   per-beat save). On this hardware a full arc is minutes, not the "overnight"
   the older docs assume — but it still owns the box while running.
5. **★ COLOR GATE ★** (free, local) — the heart of this skill. `forge review`
   → hand-critique → re-color or accept. **Do not proceed to weave until this
   passes.** See "The color gate" below.
6. **Weave** (PAID Cloudflare/kimi). Only on vetted color.
   `forge weave "$A" --thread <name>` per thread; renders a continuous-read
   `.md` to `out/compare/`. Crash-safe/resumable (re-run reuses good cells,
   retries empties). Uncovered beats fall back to isolated `synthesis` (lower
   quality — flag them).
7. **Curate** (manual JSON for now). Read the woven prose; cull + **repair**
   weak beats (`feedback_curation_is_repair` — it's an edit surface, not a
   checkbox). For each kept beat, hand-edit its sidecar: set `final` to the
   chosen/repaired prose and `approved: true`. (`forge lock` is deferred until
   the curation UX is felt across a few arcs — same call as not building the
   color-critic tool: we do this ~13 times, not 100.)
8. **Integrate** (free). `forge integrate "$A"` → `out/<arc>/*.enc` (approved
   beats spliced; unapproved keep FIXME). `source_sha` guard aborts on drift.
9. **Verify the product.** `encounter check out/<arc>`; diff each `out` file
   against source — should be only FIXME→prose swaps.
10. **Promote** (the one risky step). Copy `out/<arc>/*.enc` over the source on
    the branch; re-run `check` + `bundle`; commit; open a PR for line-by-line
    human review. **Never `git push`/merge without an explicit ask**
    (`feedback_no_unprompted_push`). Rollback is trivial — the branch isolates
    everything; sidecars/`out/` are gitignored.

## Tone spot-check (step 3)

Render `forge review "$A"` and read each beat's tone against its stub. Tones
only nudge the color register, and kimi filters downstream, so the bar is low:
**re-tag only egregious misfits, not debatable ones.** Marginal calls
(a tense-confrontation tagged `revelation`, an obsession beat tagged `wonder`)
are fine — leave them. There is no per-beat manual override in `categorize`;
to force a tone, hand-edit `tone` in the sidecar.

## The color gate (step 5) — the load-bearing judgment

Generate the review surface:

```
forge review "$A"                    # every beat, per-file (full coverage)
forge review "$A" --thread <name>    # one path in reading order (what kimi sees)
```

Per beat it shows: stub, tone, register/length, the color's enriched text, and
**the facts the color asserts**. Read each color **against its stub** (the
ground truth) and against the arc's `_cast`/`_set` bibles.

### What you are looking for

The color is deliberately vivid-but-unreliable raw material; kimi is *supposed*
to mine it and filter it. So **do not over-clean** — stripping the alien
texture defeats the point of color (it exists to break decompose's "Claude-tic"
contamination). You are hunting one specific failure: **a concrete referent got
swapped for a wrong one.** That, and only that, is what propagates as
downstream confusion.

The test that separates the two kinds of error:

> Did the color **swap a concrete referent** (an entity's identity, its
> physical nature, the medium, the cast) for a wrong one? → **FLAG.**
> Or did it merely **color a correctly-identified referent** with mood,
> suspicion, metaphor, or sensory invention? → **KEEP.**

**FLAG (confuses downstream):**
- **Identity / physical-nature swap** — an entity rendered as the wrong kind of
  thing. *(the_villa: the page — a human boy, Jiri — rendered as living paper:
  "segmented head," "the vellum adjusted," "exhalation from the paper itself."
  The loom latched onto* page = sheet of paper*.)*
- **Medium error** — the wrong artifact. *(the_villa: skipping to the middle of
  a **journal** rendered as a **film reel**: "displacement of the reel… Frame
  374.")*
- **Wrong name for an established referent.** *(the_villa: the prior journal
  owner named "Silas Pruitt"; canon is Ferath Solan.)*
- **Invented named cast/agents that contradict the beat.** *(the_villa: solo
  Solan's notes rendered with two named assistants, "Bartholomew" and "Agnes,"
  running an inventory operation.)*
- **Gross continuity/scale break** — many seasons in a cellar, a corpse walking,
  etc.

**KEEP (inventive, useful — leave it for kimi to use or drop):**
- **Mood / suspicion / interpretation on a correctly-identified entity.**
  *(the_villa: the barrister rendered as faintly sinister — "the geometries…
  require refinement." He's the right person; the color only shades him. This
  is an* interesting *error — it enriches the ambiguity. Keep it.)*
- **Atmospheric metaphor, sensory invention, register.**
- **Flavor proper-nouns that don't collide with canon** (invented market
  rumors, place-names in passing) — intended flavor per
  `feedback_factual_carries_bad_prose_verbatim`, not errors.

### The soft third category: environmental-register drift

The loom gets **no setting context** (just `{id, tone, text}`), so it will
default the biome wrong — e.g. an arid desert mesa rendered as wet gothic
(honeysuckle, drizzle, elder trees, "weeping flagstones"). This recurs across
beats and a per-beat flag is just noise. Don't re-color for it. Trust
weave + story-so-far to override the environment (the prior finished prose
re-establishes the real setting), and **scrutinize it in the post-weave read**.
The real fix is upstream — feed the loom a one-line biome anchor — not a gate
action. *(Note for a future loom-input improvement.)*

### Not a flag

First-person `I/me` narration in the color is **expected** — the loom narrates
first person; weave's prompt recasts it into the beat's POV. Never flag it.

## Acting on flags

For each FLAGGED beat, choose:

- **Re-color** — delete that beat's `stages.color` object in the sidecar JSON,
  re-run `forge color "$A"` (it ships only uncolored beats). **Caveat (load-bearing):
  the loom is effectively deterministic per beat** — observed re-rolling a misread
  beat returned *byte-identical* output. So when the misread is locked by the stub's
  wording (identity/medium/cast ambiguity — the common case), re-color **cannot** fix
  it and just burns a box-committing run. Re-color is only worth trying if you suspect
  a one-off; for a stub-locked misread, **skip straight to hand-edit.**
- **Accept + lean on weave** — the synthesis SYSTEM prompt tells kimi the color
  is non-binding and "invents names, events, and claims," and the story-so-far
  carries the real facts forward. So kimi *should* drop Silas Pruitt, the film
  reel, etc. This is the architecture's intent and is safe for most beats.
  **Riskier for sparse beats** where the misread dominates the available
  texture — there, prefer re-color or a hand-edit, and mark them for a hard
  post-weave look.
- **Hand-edit the color** — surgically cut the offending clause from
  `stages.color.enriched` while keeping the good texture. Use sparingly; it's
  hand-work, but cheaper and more reliable than a re-color gamble for a
  one-clause swap.

### Source bugs are different

A typo in the **stub** itself (e.g. `the_villa` "gehind the desk") lives in the
source `.enc`, not the color. Fixing it changes the file's `source_sha`, and a
re-parse will reset that beat to a fresh stub (losing its tone/color via the
line+original match in `Peer.Merge`). So either fix it **at promotion** (kimi's
woven prose replaces the line anyway for approved beats), or fix-then-reparse +
re-color just that one beat. Note it at the gate; don't let it silently ride
into the promoted `.enc` on an *un*approved (FIXME-retained) beat.

## Hard rules (carry these)

- **The gate is pre-kimi.** Never weave un-reviewed color. The whole point is to
  not pay to assemble slop and then repair-and-re-run.
- **Flag referent swaps only.** Don't sand off the alien texture — that texture
  is *why* color exists. Mood, suspicion, and metaphor on a correct referent are
  features, not bugs.
- **imp is serialized and color owns the box.** `swap-model stop all` before a
  color run; smoke `--limit 1` before committing the full run.
- **Gateway spend is per woven beat** (paid even on `@cf` ids). Smoke a thread,
  review, then scale.
- **Key hygiene.** The Cloudflare key lives only in gitignored
  `appsettings.json`. Never commit it; never read a key from outside the repo.

## Pointers

- `plans/scrub_arc_processing.md` — per-arc procedure, safety guarantee, wave
  order, open questions (per-arc thread storage, `forge lock`).
- `Forge/CLAUDE.md` — pipeline internals, the synthesis logic-filter prompt, the
  hard-won gotchas (thinking-on, story-so-far beats isolated synthesis, color
  breaks Claude-tic contamination).
- Memories: `feedback_pipeline_error_amplification`,
  `feedback_curation_is_repair`, `feedback_subtle_errors_and_ambiguity`,
  `project_villa_pipeline_status`.
