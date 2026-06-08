---
kind: plan
title: "Factual stage — color distribution & cross-beat contamination"
state: exploring
created: 2026-06-07
updated: 2026-06-07
status: notes — captured from the_villa first end-to-end run; not yet designed
touches:
  files:
    - text/encounter-tool/EncounterCli/FactualCommand.cs
    - text/encounter-tool/EncounterCli/DraftBlocks.cs
  features: [factual, colorize, pipeline]
provenance:
  author: claude
---

# Factual stage — color distribution & cross-beat contamination

Future-work notes from the `the_villa` first end-to-end run (2026-06-07). The
factual stage works (threads color, renders facts, preserves dialogue), but two
related failure modes surfaced that the per-beat architecture cannot fix on its
own. Both trace to the same root: **factual generates one independent GLM call
per FIXME beat** (`FactualCommand.ProcessFileAsync`), scene-aware in its *input*
(every call sees all beat stubs + the full color pool) but **blind in its
*output*** — no beat ever sees the prose the other beats already generated, and
there is no shared ledger of color already spent. See the mechanism writeup in
[[project_factual_stage_integration]] and the workflow doc §4 / §3 Stage 3.

## Note 1 — GLM echoes prior-beat phrasing, and anchors on the *weirdest* prose

GLM cheerfully echoes phrases from earlier beats. Worse, it preferentially
anchors on the most **AI-brained, novel constructions** — almost certainly
*because* those constructions are statistically unlikely, so they stand out and
the model latches onto them. The net effect is directional contamination: a weird
turn of phrase anywhere in a scene tends to **propagate downward** into the beats
below it.

Implication for the gate: this is another, sharper reason to **scrub every single
line** ([[feedback_factual_carries_bad_prose_verbatim]]). Weird language in any
one line does not stay contained — it poisons the lines beneath it. So the
line-by-line review is not just per-line hygiene; catching a bizarre phrase early
prevents it from seeding copies further down.

(Note this also interacts with per-beat *output blindness*: each call can't see
sibling FACTUAL prose, but the contamination still happens because the weird
phrasing lives in the **stub text** and the shared color pool, both of which every
call does see.)

## Note 2 — Color must be doled out carefully, not dumped

If you hand GLM the **whole** color pool on every beat, it **re-uses color many
times, often in genuinely bizarre ways** (the same "spine cracks open" / "ink
spatters" line threaded into 4 beats; a short abstract beat vacuuming the entire
remaining pool into one sentence — the "over-pack").

We deliberately moved to a **per-file (scene-level) color pool** (not per-beat)
because the opposite approach failed too: **many passages are too short to
compute color for individually**, and when too much color was supplied to a short
beat it turned into a **bloated mess**. So per-enc scene pools are the right unit
and should stay — but they are necessary, not sufficient. The pool is shared
across beats with no rationing, which is exactly what produces the re-use.

**The open problem:** keep the per-enc scene pool, but **guard against re-use
across beats.** Two candidate directions:

1. **Intelligent (deterministic) distribution** — partition or assign pool lines
   to beats up front (each line consumed at most once / capped), so a beat draws
   from its allotment rather than the whole bag. Needs a way to decide which lines
   "belong" to which beat.
2. **GLM self-report** — have the model report which pool lines it used for a
   beat, then withhold those from subsequent beats (a spent-color ledger threaded
   through the per-beat loop). Cheaper to build on the current architecture;
   relies on the model reporting honestly.

Both interact with the deferred **whole-scene-body generation** idea (generate the
scene in one pass so the model distributes each line once and rations naturally) —
that would dissolve both the re-use and the output-blindness at once, at the cost
of per-beat fact-traceability. Whether to fix distribution *within* the per-beat
design (1 or 2) or to switch to whole-scene generation is the real fork.

## Note 3 — no proper names in the color pool (hand-thread them instead)

When a color line carries a **proper name**, GLM dumps it **almost verbatim** —
e.g. it renders "the names appear in his shorthand: Ashcroft, Wren, Swinton" as a
bare list pasted into the prose, rather than weaving the names into the action.
The names themselves are legitimate flavor (throwaway buyer/seller names that
liven otherwise tedious narration — they have no story bearing and need no
consistency; see [[feedback_factual_carries_bad_prose_verbatim]]). The problem is
purely *rendering*: the tool can't place them well.

**Rule:** keep proper names **out of the color pool.** A name is not the kind of
"atomic observable" colorize/factual render well; fed as color it gets parroted.
Names belong **hand-threaded** into the text by the author (or, later, the voice
pass), woven into the sentence that needs them — not handed to factual as a line
to integrate.

## Note 4 — factual parrots color, it does not integrate it

The deeper quality problem behind the over-pack (Note 2): on many beats factual is
not *integrating* the color at all — it is **copy-pasting it back**. Roughly every
second paragraph is a blob of pool lines pasted in with minimal connective tissue.
The bar is damning: **you could get about the same quality by randomly spewing
color lines into the beat in under a second** — the model is adding little over a
random draw.

**Direction:** tune the GLM prompt (and/or add passes) so it **selects the color
appropriate to a beat and genuinely weaves it in**, rather than dumping the lines
it was given. **More passes / more clock time is acceptable** to get this right —
the current speed is no virtue if the output is parrot-quality. This is the
highest-leverage factual-stage prompt work. Candidate shapes:

- a **selection pass** (pick the 1-3 lines that actually fit this beat) separate
  from an **integration pass** (weave only those), instead of one dump-everything
  call;
- prompt the model to *use color sparingly and only where it earns its place*,
  with the over-pack explicitly called out as the failure to avoid;
- pair with the Note 2 rationing / spent-color ledger so a selected line is not
  re-selectable by a later beat.

## Status

Notes only. No design committed. Next time we work the factual stage, decide the
fork (per-beat rationing vs. whole-scene generation) before building, and treat
the **prompt-tuning for genuine color selection/integration (Note 4)** as the
first, highest-leverage lever — it may not even require the architectural fork.
