# Colorize — Design Notes

First-principles notes on generating "color" for `.enc` encounter files.
Working doc; nothing here is locked. The gold-standard exemplars live in
`exemplars.md`; **this spec is derived from them.**

---

## 1. The goal and the pipeline

Given an `.enc` encounter file, have qwen (`imp:8080`) author **8–20
candidate fragments of color** — scene texture, impressions, ambient detail.

Color lines are **raw material, not finished writing.** They sit at the
front of a pipeline:

1. **Colorize (us)** → a *grab bag* of candidate color lines.
2. **Human curator** → reviews the grab bag, keeps any / all / none. Kept
   lines are the accepted set, and become *fact* for later passes (§6).
3. **Writer agent** → pulls from the accepted grab bag and writes *fairly
   bland* prose that incorporates the color where relevant.
4. **House-style agent** → rewrites the bland prose into the house voice.

This pins down what we own and what we do **not**:

- We own the **substance** — *what is worth noticing* in this scene.
- We do **not** own **prose voice** (the house-style agent) or
  **placement / integration** (the writer agent). The player never sees our
  lines verbatim.

So a color line should be a **clean, atomic, usable observable** — an
ingredient a downstream agent can pick up, place, expand, or combine. Plain
and unstyled is *correct, not lazy*: styling it pre-empts the house-style
pass, and over-writing it (the prior failure) buries the observable under
voice the writer would only have to strip back out. We were doing two later
agents' jobs, badly.

We are a **proposal engine**: maximize *usable candidates* and the *range*
they cover, not be right on every line. And because the writer pulls "as
appropriate," **our range is the writer's range** — over-produce across
registers, anchors, and senses so there's something relevant for whatever
beat they end up writing. A modest keep-rate with good spread is a win.

Color does four kinds of work (§5) and must never contradict established
fact (§6).

---

## 2. Taste anchor: gold vs. failed

The fastest way to see the target is to set the real exemplars beside the
prior failed-run output.

**Gold** (hand-authored, see `exemplars.md`):
- the sound of the wind whistling through the metal structure
- the oppressive heat and sun
- the metallic twang of a guy line as a worker torques it into place

**Failed runs** (the `# []` lines currently sitting in the sample files):
- "hatband dry and dust-pale at the inside fold, no sweat stain despite the heat"
- "Richard's voice shifting mid-sentence from coarse farmer to precise technician, the cadence snapping back like a snapped wire"

The gold is **plain, mostly about the place and the work, and trusts the
reader.** The failed runs are **ornate, fixated on per-character tells,
every line a horror clue, and padded with explanatory clauses.** Polish and
density are not the target. The right observable, at the right altitude, is.

Everything below is an attempt to name *why* the left column works.

---

## 3. What color is (and is not)

An `.enc` has two layers:

- **The beats** (`FIXME(...)` prose): the *skeleton*. What happens. Plot.
- **The color** (`# [] ...` lines): the *flesh*. What a present, attentive
  PC would **notice** while the beats happen.

> **Color is the camera, not the plot.** A beat says *a worker tightens a
> cable.* Color says *the metallic twang of a guy line as he torques it
> into place.*

From this:

- **One observable per fragment.** Something you could photograph, hear,
  smell, or feel.
- **Plain is good.** "the oppressive heat and sun" is a complete line. Do
  not mistake intricacy for quality — over-writing is itself a failure mode
  (it's how the prior runs went wrong). Aim for the exemplars' altitude:
  concrete and usable, but spare.
- **A held frame, not a little scene.** *Ambient or characteristic* motion
  is part of the frame — wind gusting, a guy line twanging, tea being
  poured. A *change of state* with a before-and-after is plot — a voice
  shifting mid-sentence, a habit starting then stopping. The first is
  texture; the second belongs in a beat.
- **Surface, not interior.** Show the outside of things. Not "Baret misses
  home" (invisible) but a hand that keeps returning to the worn clan band
  (visible — the player infers the rest).
- **People are agents in action — not props, not dossiers.** A worker is
  *alive* in color when the PC sees them doing real work with skill and
  intention: "a worker torques a guy line into place." That is a complete
  agent — a body, a craft, intention — conveyed entirely from the outside.
  Avoid **both** failures: the **prop** (a person reduced to motionless
  scenery) and the **dossier** (a person reduced to a biography or
  inner-life reveal — "the pig farmer," "overdue home," "two voices"). The
  PC, meeting strangers, cannot *see* a backstory in a glance; they see
  hands, posture, the way someone handles a tool. Stay there. The opposite
  of a prop is not interiority — it's agency-in-action. (See §10 for how the
  "treat them as complete people" instruction backfired.)

The through-line: **color shows; the player infers.**

---

## 4. The PC's frame of reference (the lens)

This is the axis the prior runs missed entirely, and the exemplars lean on
hard (#2: a signal tower is "nearly magical").

The PC comes from a **renaissance-era imperial society** (the world of the
Aldgate calendar and timeline — steel, sail, black powder). Kesharat
civilization runs on **rail, steam, and signal infrastructure** that sits
at or past the edge of what the PC can even name. A lattice signal array
is, to them, a near-magical metal construction; a torqued guy line, a
machined tool, the brass interval bell — all of it is *strange technology*.

So a primary source of color is the **gap between the PC's world and what
they're looking at.** Render the PC's authentic reaction to industrial
artifacts that are mundane to us:

- It powers **aliveness** — the world feels real because the PC genuinely
  *reacts* to it, with wonder or unease, instead of narrating it flatly.
- It feeds **dread** — Kesharat order and machinery are alien, and the
  alien-ness is free dread that doesn't require naming the horror.

Most potent on: the tower, cables and guy lines, machined tools and parts,
the bell, anything precise, metallic, or scheduled.

Two cautions:
- The PC has *some* read ("a signal tower, by the look of it" — the bibles
  let them guess). Render **naive wonder, not total blankness.**
- Stay in the PC's vocabulary. No "radio," "antenna," "electricity,"
  "signal frequency." They'd see metal, cable, hum, a box that does
  something. (The author's note in exemplar #2 says "radio tower" as a
  *gloss for us* — that word never reaches the page.)

---

## 5. The four jobs (the palette)

Color does four kinds of work. These are a **palette to paint from**, not a
checklist to satisfy on every run. A scene makes some available and others
not; the spread of candidates should track what the scene actually
supports.

1. **Grounding** — ties the scene to what the player knows of the world (and
   to the PC's own frame, §4); rewards attention; makes the world cohere.
2. **Aliveness** — ambient sensory texture; the incidental things one notices
   if actually present. *This is the bulk of good color* (4 of 5 exemplars).
3. **Dread** — unsettling, discordant, wrong details, when the scene reaches
   for that mood.
4. **Warmth** — humanizing detail, when the scene reaches for that mood.

**Register is keyed to the beat — it is not a fixed property of color.** The
`.enc` tags its beats `mundane` vs `dread`; pitch each candidate to the *job*
of the beat (and the scene) it attaches to. There are two distinct jobs, and
which one is in play decides the whole balance:

- **Establishing baseline (the opening's job).** When the scene's work is to
  ground the reader — and the *first* thing the player sees always is, by the
  logic of horror: you can't feel the wrongness until you know what normal
  was — color connects them to the familiar (a tower, workers building, a hot
  day). Here dread is a sparse *seam* the attentive PC catches in passing, and
  innocent normalcy dominates. The Signal Array opening runs ~4:1, and the one
  unsettling note lands *because* it is outnumbered. **That ratio is a
  consequence of the beat's position, not a law of color.** When every line is
  a clue (the prior runs' deepest failure), wrongness becomes wallpaper and
  the player is *told* what to feel.

- **Substantiating a stated wrong (the deeper beats' job).** When a scaffolded
  beat already *asserts* something unsettling — say *"Torgo is distracted and
  can't talk"* — color's job flips. It is no longer outnumbering the dread; it
  is giving the bare assertion **observable flesh:** Torgo repeating a phrase
  from a technical manual under his breath; staring past you into the middle
  distance; humming — not a melody, but in tune with the wind-driven vibration
  of the pylon. (That humming only lands if the tower's noise was established
  earlier — it *recontextualizes* an innocent fact rather than introducing a
  new one: a **callback**, §6, the strongest yes-and move.) This is the
  **same render-from-facts engine** (§6) — the
  unsettling beat *is* the fact, and color renders observable instants of it —
  and the **same show-don't-argue discipline** (§7): render the observable
  (Torgo talking low and steady to the empty air beside him), not the
  interpretation ("in conversation with an unseen entity"), which only *names*
  the wrongness. Here dread candidates are the point, and the innocent
  majority does not apply.

So don't hand the curator a fixed innocent-to-dread ratio. Hand them a palette
pitched to **what the beat is doing** — grounding the reader, or fleshing out
a wrong the beat has already stated.

The strongest fragments do **double duty** (the metal tower is wonder *and*,
faintly, alien-dread) but each has one **primary** register so the curator
can read the palette.

---

## 6. Render from facts → provenance (the anti-contradiction engine)

The hard constraints:
- Color never contradicts **world fact** (no one's from Chicago; no
  synthetics in a circa-1800s setting; no pig on the mesa).
- Color never contradicts **encounter fact** — and **accepted prior color
  is now encounter fact** (improv "yes-and": build on it, enrich it, call
  back to it, set up a later subversion; never break it).

We enforce both the same way, positively: **every fragment must have
provenance.** Good color is a *fact, rendered as an observable* — the bibles
and beats *describe* the world; a fragment renders one observable instant of
that description. We don't ask "is this forbidden?"; we ask "where did this
come from?" A fragment with no source in the established material is the
thing to reject.

This is the cure for the don't-list (§10): **invention is where
contradictions come from.** A detail derived from a fact cannot contradict
the world, because it came from the world. We invent the *lens* (how steam
coils, the exact pitch of the twang), not the *subject*.

Grounding is **necessary but not sufficient** — see §7. A fragment can be
perfectly derived from a fact and still be bad color.

"Yes-and" in practice — three moves, in rough order of power:

- **Enrich** — add a facet to an already-established fact (another sense on
  the tower, a second detail of the work).
- **Call back / recontextualize** — re-notice something already established,
  in a way that *changes its meaning.* **This is the strongest move we have,**
  and it is why the baseline matters (§5): the innocent normalcy we plant in
  the opening is **ammunition.** Establish the tower's noise as plain
  aliveness; later, Torgo humming *in tune with it* rereads that same noise as
  wrong — dread manufactured from material the player already accepted, with
  nothing new introduced. A callback poisons (or warms, or deepens) a fact
  already in the player's head, so it lands harder than fresh detail and can't
  be shrugged off as set-dressing. Improv's core trick: the laugh — or the
  chill — is in the *return*, not the introduction.
- **Set up** — plant something the author can subvert later; never pay it off
  ourselves (that's plot).

Two consequences of leaning on callbacks:

- **A callback is only as good as what it refers to**, so the generator must
  *know what's already established* — accepted prior color and prior beats fed
  in as fact (§8). This is the concrete payoff of a per-vignette accepted-fact
  store and cross-`.enc` continuity (§11): a fact planted in `Start.enc` is
  exactly what a later `.enc` recontextualizes.
- **A callback carries a dependency.** It only lands if its referent was
  actually established *to the player* — and we propose, we don't place (§1).
  So tag a callback with the fact it calls back to (provenance, §9), making
  the dependency legible to the curator; if the referent was never placed, the
  callback dangles and they drop it.

---

## 7. Show, don't argue (the line-level restraint)

The signature failure of the prior runs was the **explanatory clause** — a
subordinate clause whose only job is to point at the meaning. The
grammatical tells are **"despite," "not X but Y," and "as if":**

- ✗ "no sweat stain **despite the heat**" → ✓ (the absence, shown plainly,
  in a context where the heat is already established)
- ✗ "the raw pink of sunburn, **not** the dry red of mesa wind"
- ✗ "fingers curled **as if** still holding invisible tools"

The clause is the author leaning in to make sure you get it. Cut it; keep
the noun phrase. A good fragment reads like a **caption you could put under
a photograph** — it names what's in frame and stops.

**But:** a *light perception word* is fine and often right — "the
**strange** way the tools were laid out," "the **oppressive** heat." That's
the PC's honest one-word read (§4), not a constructed argument. The line:

- One word of PC-perception coloring the observable → **fine.**
- A clause that *reasons about* an anomaly to flag it as wrong → **out.**

In this encounter the PC *is* the one who can see the seams (the bibles say
so), so registering quiet unease is legitimate POV — just don't *explain*
it, and don't do it on every line.

---

## 8. What the generator gets fed (inputs)

To render from facts, the model needs the facts. The `.enc` ships alongside
its bibles (`_set.md`, `_cast.md`, `_scenes.md`, the vignette doc) inside a
biome (`scrub.md`) and a shared world (`timeline.md`, `imperial_calendar.md`).

Tentative input bundle per generation:

1. **The target `.enc`** — beats (with mundane/dread tags) and choices.
   **Strip the existing `# []` color block** unless a line is marked
   *accepted* (below): the unaccepted proposals are failed-run output, and
   leaving them in invites the model to imitate exactly the style we're
   escaping. Pass accepted color separately, as fact, not as a sample to
   continue.
2. **The vignette / set / cast bibles** for that `.enc` (resolved from the
   `[vignette ...]` directive — e.g. `dungeons/signal_array`).
3. **The biome lore** (here, scrub).
4. **The PC's frame** (§4) — who the PC is and what reads as alien tech to
   them. Likely a short standing brief, not re-derived per call.
5. **Shared world facts** as needed (calendar, timeline) — probably curated
   slices, to control context size.

"Established facts" in priority order: (1) world lore, (2) set/cast bibles,
(3) the encounter's beats, (4) **accepted** prior color.

Note that (4) is only known *after* the curator signs off, so callbacks to it
(§6) force the generate→approve loop to interleave: process `.enc` files in
play order and accumulate the accepted set, so each call's prior color is
already canonical. See §11 item 8 for the loop structure.

Also: **don't duplicate** color already present, and prefer candidates that
attach to under-served anchors.

---

## 9. Output shape

- **Count:** 8–20 fragments. Over-produce within reason.
- **Altitude: raw material, not prose.** Match the exemplars — concrete,
  usable, *spare*. Unstyled is correct (§1): the line is an ingredient for
  the writer agent, not text the player reads. Over-writing buries the
  observable; styling pre-empts the house-style pass.
- **Format:** match the `.enc` block — one terse line each, noun-phrase-led,
  static, no verbs of punctual action.
- **Coverage / variety:** spread across
  - **register** (mostly aliveness/grounding, dread in the minority, warmth
    where supported),
  - **anchor** (environment, the tower/tech, the work, the crew, objects —
    don't pile six lines on one NPC),
  - **sense** (sound, smell, touch, thermal — not sight-only).
- **One detail per line.** Two observables = two fragments.
- **Optional metadata, to aid the pull.** A downstream *agent* selects lines
  by relevance, so a light tag helps it match color to the beat it's
  writing: **register** (aliveness / grounding / dread / warmth) and
  **anchor** (environment / tower-and-tech / the work / a named crew member
  / an object), optionally the **fact it renders** (provenance, §6) — and for
  a **callback**, the prior established fact it recontextualizes, so the
  curator can see the dependency (§6).
  Exemplar #2's bracketed *why* is an instance of this. Keep tags out-of-band
  (bracketed) so they never leak into prose, and keep them **descriptive,
  not prescriptive** — do not dictate placement ("use in the arrival beat");
  the writer owns that. Tags also let us see whether qwen is grounding or
  inventing. (See §11.)

---

## 10. The pathology we're escaping

The prior prompt grew into a long list of "don't do X." That is the failure
mode of correcting a generator with negative rules: they **don't compose**
(the list grows forever), **don't generalize** ("no pig on the mesa" doesn't
prevent the next invention), and **fight symptoms, not causes** — nearly
every "don't" was an instance of the model **inventing** instead of
**rendering** (§6), or **arguing** instead of **showing** (§7).

The cure is not a better blocklist. It's a few positive principles plus
strong grounding, so bad outputs become **unreachable** rather than
**forbidden.** If we ever find ourselves adding the fourteenth "don't,"
that's the signal we've missed a principle — go find it instead.

**A case study in overcorrection (and the deeper lesson).** An earlier pass
noticed qwen was treating the workers as **props** — flat set-dressing — and
corrected with "treat humans as complete agents, with a life and identity."
That steered it wrong: it produced the **dossier-color** above (per-NPC
biographies, inner lives, two-voices reveals). The instruction named a
*goal* ("make them real") but not a *channel*, so the model cashed it out
through the nearest channel it had — exposition — which is exactly the
channel color must never use (§3, §7). The fix is to name the channel:
people are made real **through observed embodied action** (§3), not through
stated identity. The general lesson, and a rule for writing this prompt:
**correct toward an observable behavior, never toward an abstract value the
model can only express by telling.** Every abstraction we hand qwen
("complete agents," "make it atmospheric," "feel alive") will be cashed out
as the thing it most knows how to do — describe and explain — unless we
ground it in what to *observe*.

---

## 11. Open questions

1. ~~Altitude / granularity — seeds vs. finished?~~ **Resolved:** color is
   raw material — spare, unstyled ingredients for a downstream writer agent
   and a house-style pass, never player-facing (§1). Lean seed-like.
2. **Accepted vs. proposed marking.** Confirm a convention (`# [x]`
   accepted / `# []` unaccepted, or whatever the real one is). Decides what
   we feed as fact vs. strip. (§8)
3. **Output metadata — what and how.** Leaning yes on light, descriptive
   tags (register, anchor, provenance) to aid the writer's pull and our
   debugging (§9). Open: exact format (bracketed inline vs. structured), and
   how much tagging risks over-constraining a grab-bag the writer is
   supposed to range over freely.
4. **Context budget.** Whole biome + bibles per call, or curated slices?
   (§8)
5. **Beat-level vs. scene-level color.** One pool per `.enc`, or target
   specific beats (a `dread` beat gets dread candidates)? Beat-targeting
   might raise the keep-rate.
6. **Cross-`.enc` continuity.** When color is accepted in `Start.enc`,
   should it feed forward as fact into `The Rest Interval.enc`? (Shared
   vignette and cast.) Argues for a per-vignette accepted-fact store. This is
   also where the strongest callbacks live (§6) — see item 8.
7. **Self-evaluation.** Have the model (or a second pass) score its own
   candidates against §3–§7 and drop the weak ones before the human sees
   them? Trades tokens for keep-rate.
8. **Generation/approval interleaving (the callback consequence).** Callbacks
   (§6) need to refer to *canonical* color, but canonicity comes only from the
   human curator's sign-off — so generation and approval can't be two clean
   sequential phases. Working resolution (not locked):
   - **Callbacks are second-order.** First-order color (grounding, aliveness,
     fresh dread) is a function of *beats + bibles alone* — no prior-color
     dependency, fully parallelizable. A callback is a function of the
     *accepted set*. So the dependency is a two-layer DAG, not a tangle:
     generate first-order → approve → generate callbacks against what's now
     canonical → approve.
   - **Process `.enc` files in play order; accumulate the accepted-fact store
     (item 6).** Then a callback from a later `.enc` to an earlier one is just
     first-order generation against canonical input — the earlier `.enc` is
     already approved. The strongest callbacks (most narrative distance) are
     cross-`.enc`, so they fall out *without* interleaving inside a call. Only
     *same-`.enc`* callbacks need a genuine second pass within one encounter —
     and those are the weaker case, so make them an optional per-`.enc` second
     pass, not a core requirement.
   - **The loop is `.enc`-granular and ordered, but adds no rounds.** The
     curator already reviewed each `.enc`'s grab bag one at a time; we just
     *order* those reviews by play order and feed each forward. What's lost is
     parallelism across `.enc` files — mild.
   - **Callback referents must be accepted, never merely proposed** (the
     guardrail from item 2): speculative callbacks against unapproved color
     build on sand. Callbacks point only into the canonical store.
   - **Staging:** ship **first-order only as v1** (no callbacks, no ordering
     constraint, the simple linear pipeline of §1). Add callbacks as v2 once
     the accepted-fact store + play-order processing exist. Isolates and defers
     the hard part; v1 is not blocked on it.
