---
name: arc-decompose
description: Turn an arc brief (markdown sketch of a Dreamlands narrative arc — premise, characters, beats, endings) into a structurally sound set of `.enc` files with FIXME-stub prose. Produces the SKELETON only — legal syntax, no orphan tags, no infinite loops, hubs wired per established arc patterns. Prose quality is left to later pipeline passes (colorize, factual, voice). Use when the user has written or pointed at a brief like `text/encounters/arcs/<biome>/<arc>/<Name>.md` and wants the bones of the arc on disk before any writing-pass happens.
---

# Arc decompose

Turn an arc brief into a skeleton of `.enc` files: legal syntax, every
choice routed, every quality/tag setter has a reader and vice versa,
every terminal reachable, no infinite loops, hubs wired per the
established patterns. Prose is FIXME stubs throughout — quality writing
is handled by later pipeline passes (see `plans/arc_writer.md`).

You are not a writer here. You are a structural engineer with a taste
for game flow. The acceptance bar is **does this play sanely from end
to end**, not **is this prose good**.

## Inputs

Before doing anything else, read all of the following, in order:

1. **The brief.** Always a markdown file in the arc directory, usually
   named after the arc (e.g. `TheRelayPost.md`, `the_hermitage.md`,
   `README.md`). The user will name it or pass a path.
2. **`project/encounter-spec/arc_patterns.md`** — the cumulative
   inventory of structural techniques across the authored arcs. Treat
   every numbered section as a tool in the box. Decompose by *picking*
   patterns that fit the brief, not by inventing.
3. **`rules/encounter_mechanics.md`** — the canonical, living
   vocabulary spec. Every verb, every condition shape, every front-
   matter field. When this disagrees with anything else (including
   `project/encounter-spec/format.md`), this wins.
4. **The biome's locale guide.** Located at
   `text/encounters/<biome>/tier<n>/locale_guide.txt`. The brief lives
   under `text/encounters/arcs/<biome>/<arc>/`; the tier comes from the
   brief itself (it's named, e.g. "Scrub Tier 2"). Skim for register
   and the scene palette — you won't write prose, but the FIXME
   summaries should sound like they belong in this biome.
5. **One or two reference arcs.** Pick from `arc_patterns.md` §8 the
   fingerprint that's closest in shape to what you're about to build
   (linear chain → `brides_cave`; staged hubs → `the_hermitage`;
   single mid-hub with conversation spokes → `the_fugitive` or
   `wrenbury`). Read the actual `.enc` files in the matching arc
   directory. Pattern-match the shapes, not the prose.

## Process

**Plan before writing. Do not write any `.enc` file until the user has
signed off on the plan.** Writing first and fixing later costs the user
more attention than a five-minute plan review.

### Step 1 — Read everything in §Inputs.

### Step 2 — Draft the structural plan.

Produce a short plan in chat (~half a screen) covering, in order:

1. **Arc shape.** Linear chain / single mid-hub / staged hubs / hub-of-
   hubs. Cite which §8 fingerprint you're modeling on and why this
   brief warrants that shape. If the brief has more than two
   characters or more than two locations the player must visit before
   resolution, default to a mid-hub or staged-hub shape.
2. **File list.** One `.enc` per encounter, with a one-line role per
   file (Start / mid-hub / character scene / terminal). Include
   filename conventions used by the arc (`Title Case.enc` with spaces
   is normal; see existing arcs).
3. **State map.** Every arc-local tag and quality you'll introduce.
   - Tags: `<arc>.<name>` with a one-line role (scene-visited,
     earned-knowledge, commitment marker — see `arc_patterns.md` §2d).
   - Qualities only if degree is structurally required; otherwise use
     a tag. Threshold-zero gates do not work (see §9). Use
     `!quality X 1` or a tag instead.
4. **Choice gating.** For each `[requires]` you'll author, name the
   condition and the setter scene that satisfies it. Every
   `[requires tag X]` and every `[requires quality X N]` (N≥1) must
   have a reachable `+add_tag X` / `+quality X N` setter from a
   prior scene.
5. **Endings.** Each terminal route: its trigger condition, its
   `+finish_dungeon` vs. `+flee_dungeon` verb, and any per-route
   rewards (`+add_item`, `+add_level`, gold). If the brief implies
   multiple "good" endings, the same artifact reward can hang off
   several routes (see `arc_patterns.md` §1c.ii).

   **Reward conventions** (apply unless the brief explicitly says
   otherwise):
   - **Default reward is `+add_level`** on any route that resolves
     the arc's central situation. This is the floor: if the player
     saw the arc through and made a real call, they get a level.
     Gold may ride along where it's diegetically motivated (the
     NPC pays them, they loot a body), but the level is the
     baseline.
   - **T3 arcs grant a unique piece of end-game gear** instead of
     (or alongside) the level — a one-of-a-kind `+add_item` named
     in the brief or chosen from the arc-only items in
     `rules/encounter_mechanics.md` (e.g. `the_old_tooth`,
     `shimmering_blade`, `robe_of_twilight`). T1 and T2 arcs do
     not grant unique gear by default. The arc's tier is named in
     the brief's title line ("Scrub Tier 2", "Forest Tier 3").
   - **Reward on resolution.** Any ending that engages the arc's
     core conflict and resolves it — even bleakly, even via the
     "wrong" choice — earns the default reward. Resolution is the
     trigger, not moral correctness.
   - **No reward for bailing out.** Routes that walk away before
     the arc's central choice is faced get `+flee_dungeon` and no
     reward. Brief language like "the PC declines to engage" or
     "this isn't their problem" signals a bailout route.
   - **No reward for making things worse.** Routes that destroy
     the arc's premise (kill the informant, burn the evidence,
     hand the captive to the wrong party out of spite) terminate
     via `+finish_dungeon` but grant no level — the arc resolved,
     but in a way the game does not celebrate.
   - **No reward for failed obvious gambles.** If a picker check
     is framed as "you tried something risky and it didn't work"
     (the deception fails, the bluff is called), the wrong branch
     skips the reward. Reward on the right branch only. Pure
     uncertainty pickers (combat outcome, ambient skill check)
     can reward both branches at different magnitudes per the
     `brides_cave/The Ghosts.enc` precedent — distinguish by
     whether the failure is a *consequence of the player's bad
     read* (no reward) vs. *bad luck on a fair check* (reduced
     reward okay).
6. **Pickers.** Each `check <skill> correct:X wrong:Y` you intend to
   use, in which scene, with which `correct:`/`wrong:` approaches
   from the canonical pairs in `rules/encounter_mechanics.md`. Picker
   checks are terminal in their `@if` chain; `@else` is mandatory.
7. **Choice-gating pattern per hub.** **Default to pattern 1a.ii
   (hide-the-choice) for every spoke.** A spoke that has been
   resolved should disappear from the menu, not present a
   summary the player has to click through to learn there is
   nothing new. Use `[requires !tag <arc>.<flag>]` on the choice
   line and set the flag inside the outcome.

   **Do not use pattern 1a (recap-in-place) as a default**, even
   though `arc_patterns.md` calls it canonical. The recap pattern
   has narrow legitimate uses: a hub where revisiting a spoke
   *delivers genuinely new content* on the second visit (the
   spoke's prose responds to a tag the player set elsewhere in
   the interim), or a beat where the recap itself does meaningful
   diegetic work (mood-shift, time-of-day change, NPC reaction).
   "I want the choice to still be visible so the hub looks full"
   is not a reason — the hub is allowed to drain as the player
   makes progress.

   §3.3 staged-gate (the wire-spoke pattern) is the exception
   that proves the rule: the choice is visible across two visits
   because the *second visit fires different content gated on a
   precursor tag*. The recap pattern would be lazy here; the
   staged-gate is precise. Reach for it when a scene's meaning
   genuinely shifts across visits.

   **§3.1 floor** (every encounter needs at least one
   unconditional choice) is satisfied by **adding an always-
   visible exit**, not by converting a spoke to 1a. The exit can
   be the forward-advance choice ("Wait for midnight", "Walk
   out"), an idle/decline choice that ends the scene without
   committing, or — at terminals — the route-out itself. Never
   keep a stale spoke visible just to satisfy §3.1.
8. **Staged gates for context-shifted re-reads.** When a scene's
   meaning changes across visits (e.g., a clue reread after talking
   to the character it concerns), use the §3.3 staged-gate pattern:
   `[requires !tag A || tag B && !tag C]`. First visit fires; second
   visit fires only after precursor B; the choice hides after the
   climax tag C lands.

### Step 3 — Present the plan, then proceed.

Send the plan to the user. **Default to writing the files
immediately after** — the plan is a chance for the user to redirect,
not a gate. The scaffold is cheap to regenerate; waiting on
sign-off for every routine arc burns the user's attention.

**Only stop and wait for sign-off if the brief is genuinely
broken** in a way the plan can't paper over:

- The brief is incoherent or contradicts itself (one paragraph says
  the NPC dies, a later one has them traveling with the PC).
- The brief is a recipe for brownies / clearly the wrong document.
- The brief ends part-way through (no endings described, hub
  unresolved).
- The brief calls for mechanics the vocabulary doesn't support
  (charm, paralyze, insta-kill, disarm — see the
  `no-agency-negation` rule).
- The brief implies a structural shape the inventory doesn't
  cover and you'd be inventing patterns wholesale.

If none of those apply, ship the plan and the files together. The
user reviews the plan as a heads-up for the diff they're about to
read, and redirects via revisions after rather than gating before.

When the user does redirect, redraft the plan **and** the files in
the same turn.

### Step 4 — Write the files.

Write one `.enc` per file in the file list. For each:

- First line: display title (the title as players see it).
- Front matter: `[trigger none]` for arc-internal encounters,
  `[vignette dungeons/<arc>]` for consistency. No `[tier]` on arc-
  internal encounters (the roster sets it).
- Body: prose is FIXME stubs **one line each**, using the
  `FIXME(<scene>): <one-sentence beat summary>` form that
  `EncounterCli fixme` expects. Optional `(<register>)` annotation
  hints the later voice pass — pick from `mundane | action | horror
  | dread | wonder | revelation`. Do not write multi-paragraph prose.
- Choices: full structural form. `* Link = Preview` split where the
  brief gives both a terse hook and a longer flavor. `[requires …]`
  trailers on the choice line for hide-the-choice gating. Outcome
  blocks with proper `@if` chains, mechanic verbs, and `+open` /
  `+finish_dungeon` / `+flee_dungeon` terminators.

  **Choice labels describe what the PC does**, not what is happening
  in the world. "Wait out the rest interval" is right because the
  PC's action is waiting; "Finish the pylon" is wrong because the
  *crew* finishes the pylon. The PC is the subject of the verb, even
  when the action is "wait" or "watch" or "do nothing." World events
  go in the preview text after `=`, or in the outcome prose. Common
  red flags: world-state changes as choice labels ("The fire dies"),
  imperatives whose implied subject is someone else ("Finish the
  pylon"), NPC actions as choice labels.

  **Vary the action verb across parallel spokes.** When a hub has
  multiple spokes (one per NPC, one per location), five rows of "Talk
  to X" reads as a generated menu. Distinct verbs carry the same
  gating with more texture: Walk over to, Chat with, Meet, Check in
  on, Sit back and watch. Pick the verb that fits the character or
  beat the spoke delivers — the foreman gets "walk over to" because
  he is the figure of authority; the quiet one gets "check in on"
  because he is the one you would worry about.
- Edge-transit: per `arc_patterns.md` §5, edge-transit prose lives in
  the *outgoing* outcome of a choice. Drop a FIXME line for it where
  needed; the next encounter assumes the player is already there.

**Continuity rules for the skeleton:**

1. **Facts must be broadly consistent across the arc.** If the
   Start.enc transit says someone closes the door behind the PC,
   the hub body cannot describe that same someone as having been
   sitting across the room the whole time. If the brief says two
   characters are mid-argument when the PC arrives, decide *where*
   that argument lives diegetically and stage it once — do not
   re-establish the same arrival moment in the hub body. The
   skill owns continuity at the beat-summary level; the factual
   pass will reproduce whatever contradictions the stubs encode.

2. **Transit prose and the next scene's body read adjacently.**
   At runtime the player sees the outgoing outcome of the choice
   they picked, followed immediately by the next encounter's
   body — two blocks across two files rendered as one continuous
   scene. Write them as continuous prose. The transit should
   *land* the PC in the next scene; the next scene's body should
   pick up *after* the landing, not re-narrate the arrival. The
   common failure is the transit ending with "you step inside"
   and the body opening with "you step inside" again.

3. **Arrival/welcome belongs in the incoming transit, not the
   hub body.** The hub body is re-rendered on every spoke return.
   Anything that should only happen once — a door slamming, an
   NPC standing up to greet the PC, the smell of tea hitting them
   for the first time — goes in the transit, not the body. The
   body is static ambient texture true on every visit.

4. **Character tension surfaces through observed reactions in
   spokes, not as body fixtures.** If two NPCs are in a long-
   running disagreement, don't write "two people are clearly
   mid-argument" in the hub body. Stage the tension through
   small cross-room beats inside the spokes — when the player
   talks to A, B exhales/snorts/makes a dry sound from across
   the room; when the player talks to B, A returns the gesture.
   The disagreement registers diegetically, not as an ambient
   "argument aura."

5. **Bare interiority in stubs is okay; the factual pass owns
   unpacking it.** It is fine to write `FIXME(mundane): She is
   quietly terrified` or `FIXME(dread): He is afraid of what it
   means if she is right` in a stub — the beat-summary form is
   allowed to compress to character state. The factual pass is
   responsible for rendering that state as observable phenomenon
   (a hand going still, a tea cup set down without finishing,
   the way someone phrases a question) rather than direct
   interiority. The skill does not need to pre-unpack; just do
   not leave interiority in the *final* prose.

5b. **Stub spoken dialogue as actual words, not reported summary.**
   When a character speaks, write the line they say, even at the
   skeleton stage: `FIXME(mundane): "You're not guild," the page
   says. "There's a man dead up at the villa."` Do NOT compress
   speech to its content (`the page explains a merchant is dead`).
   The downstream passes — factual especially — are *eager to eat
   dialogue*: they flatten a spoken line back into bare facts and
   the voiced moment is lost, because the stub handed them facts to
   preserve, not a voice. If a human is saying words, the stub
   carries the words. This is the one place a skeleton stub is
   allowed to read like prose — a rough quoted line is a placeholder
   the voice pass refines; a *summary* is a placeholder the factual
   pass keeps flat. `the_fugitive/Mareen.enc` is the reference: it
   stubs full quoted dialogue throughout. (Exception to rule 7's
   witness-statement discipline, which still governs narration.)

6a. **Staging must be physically plausible.** Track where every
   NPC actually is across the arc's timeline. A visitor needs
   lodging — they don't just "withdraw to the back storeroom"
   unless someone offered it. Humans sleep at night — if an NPC
   is awake through a midnight scene, the stub must justify it
   (night shift, can't sleep, expecting a late transmission,
   keeping watch). Private conversations in a shared space need
   a privacy mechanism — proximity (whispered low), distance
   (stepped outside under the eaves), or a closed door (beckoned
   into the storeroom). Two NPCs cannot have separate private
   conversations with the PC in a one-room building unless the
   staging accounts for it.

   **Transit prose carries the lead-in facts.** When a `+open`
   bridges scenes that imply offstage logistics — going to bed,
   one character leaving the room, weather changing, a meal
   eaten — the transit must establish them. The next scene's
   body should not have to handwave them. If "Wait for midnight"
   bridges evening to night, the transit handles: who slept
   where, who took which shift, who agreed to what. The
   midnight body is then a snapshot of the *resulting* state,
   not a re-staging of it.

   **Transits must acknowledge state changes that happen during
   them.** If a state changes between the source scene and the
   destination scene — *anything* the player would otherwise
   notice as a jump cut — the transit narrates the change. Time
   is the most common case: a transit from a midnight scene to
   a dawn scene must acknowledge the six hours that passed and
   sketch what the PC and the NPCs did with them (slept,
   waited, packed, kept watch). Other examples: weather
   changing (storm subsides), a character departing (one NPC
   leaves the room offstage), an item moving (the wax-paper
   sleeve goes from her bedroll to her coat pocket), a tag
   being set whose narrative content has to land somewhere. If
   the state delta isn't on the page, the player feels the
   discontinuity. Never jump-cut between two timed or staged
   moments without bridging the delta.

7. **No Intro to Creative Writing in scaffolds.** Stubs are for
   *factual information*, not for clever phrasing. Avoid:
   - Similes and metaphors. "She pours tea with the manic warmth
     of someone alone with the wire too long" → "She pours tea."
     "He moves the way old men move who have decided which
     motions are worth the cost" → "He is old. He moves slowly."
     "He chooses his words the way a man chooses them who has
     written reports his whole life" → "He chooses his words
     precisely."
   - Cute generalized observations. "as if he had been expecting
     the door to open and had set the question of who would be
     on the other side of it aside" → "He looks up without
     surprise."
   - Editorial framing. "you will come to see" / "the easiest
     thing you have done all night" / "the silence of two people
     deciding what to do without you" — all defer to downstream.
   - Elided objects. If a character offers a second cup, the
     stub must first establish there is tea. Do not lean on
     implication; downstream readers (the factual pass, the
     voice pass, the eventual critic) need the *facts* to render
     well. "She pushes a second cup at you" is wrong if no cup
     was established. "She has a teapot at her desk. She pours
     you a cup." is right.
   - Jargon-as-style and ambiguous referents. "She frames the
     recent traffic as entertainment" → "She shows you the recent
     wire messages she has transcribed." "Traffic" is telecom
     jargon the model reaches for because it sounds professional;
     it also collides with foot-traffic, road-traffic. Use the
     plainest concrete noun for the referent. If the brief calls
     a thing X, the stub calls it X. Downstream has access to
     the brief, but should not have to disambiguate the stub
     against it.

   Factual stubs should read like a competent witness statement:
   short declarative sentences, named props, named relations,
   no flourishes. "Bob is an old man." "Bob has been here for
   ten years." "Bob knows the price of everything." The downstream
   passes are the ones that turn those facts into prose with
   texture. The decompose pass writes *the truth*; later passes
   write *the writing*.

8. **Hedge in-world jargon.** The player may not know terms like
   "lattice tower," "the Lattice," "siphon glass," "the Stand,"
   "alignment with the Lattice," "rest interval." The first time
   a stub names one, the same sentence or the next adds a brief
   plain-language gloss or a "locals call it X" frame. Downstream
   passes tighten and re-voice; they do not *add explanation that
   wasn't in the stub*. If the stub names a jargon term cold, the
   rendered prose names it cold, and the player has no frame.
   Example fix from `signal_array/Start.enc`: "lattice tower" →
   "tower of crossed metal struts five meters tall, guyed with
   cable…Kesharat work; you have seen the scaffolding of these
   going up across the mesa over the past months; the locals call
   them signal arrays, but nobody outside the Administration is
   clear on what they signal to." Hedging in-line is preferred to a
   separate paragraph; the voice pass can compress the hedge if the
   rendered context makes the term clear.

9. **The narrator does not name the strangeness.** Stubs put
   factual events on the page; the player makes sense of them. A
   beat where a Lattice signature is visible should be parseable
   by a reasonable reader as something ordinary (disciplined work
   crew, polite host, careful clerk) and by an attentive reader as
   something off. The stub provides both readings simultaneously
   by writing events, not diagnoses. "Three seconds. The flip is
   total" is wrong — it names the wrongness. "The crew downs tools
   and breaks for rest. The foreman turns to you, smiling, and
   beckons you over" is right — same beat, no diagnosis. Avoid
   "she has the focused purposefulness of someone protecting
   equipment," "the warmth is wrong," "they are someone else
   behind the eyes," and similar diagnostic-narrator constructions
   that take the discovery away from the player. The horror in
   these arcs is the slow build of "wait, that doesn't quite add
   up." If the narrator italicizes it, the discovery is gone.

10. **No hypothetical player actions in stubs.** Second-person
    narration must not reference player actions the player did not
    perform. "If you press him on the geometry he has a second
    explanation" is wrong: the reader has no way to engage the
    hypothetical, and the second explanation is being shown to
    them without their having earned it. Reach for one of four
    shapes instead:

    - **Just narrate the ask** (default for natural curiosity) —
      if any reasonably engaged player would ask, just say "you
      ask him..." and continue. Saves clicks; load-bearing reveal
      lands for every player. The signal_array Baret and Richard
      spokes use this shape.
    - **Promote into a choice** (when the press is a real
      decision) — if the press is confrontational, costly, or one
      a careful player might reasonably not do, make it an
      explicit choice with its own outcome. Optionally a self-
      looping hub spoke (1a.ii hide-after-press). The signal_array
      Veran spoke uses this shape.
    - **Move the first beat into transit, make the encounter a
      small hub** — uplift introduction + first explanation into
      the spoke's transit text; leave the spoke encounter to
      carry just the meaningful decision. See `arc_patterns.md`
      §5b (intro-in-transit pattern).
    - **Drop the second beat entirely** — if the deeper reveal
      isn't earning its keep, cut it.

    A hypothetical in the body ("if you press, if you ask") is
    always wrong. Pick one of the four shapes above. Default to
    "just narrate the ask."

11. **Match prose severity to mechanical condition weight.** When
    a stub applies `+add_condition X`, the prose must match what
    the engine actually models. `injured` is a serious wound —
    without treatment, the PC dies in four days. Writing "bruised
    ribs" with `+add_condition injured` underspecifies the
    consequence; write "a wound that is not closing" plus
    `+skip_time morning` to model the PC being non-functional.
    Consult `rules/encounter_mechanics.md` for each condition's
    mechanical meaning before deciding the prose register. Common
    conditions and prose registers:

    - `freezing` — cold/exposure: shivering, blue lips, slowing
      limbs.
    - `thirsty` — water scarcity: dry mouth, headache, cracking
      lips.
    - `irradiated` — long-term degradation: ambient nausea, hair
      loss, sores.
    - `lattice_sickness` — Lattice exposure: dissociation,
      schematic intrusions, memory gaps.
    - `exhausted` — sleep deprivation: unfocused, dropping things,
      can't think.
    - `injured` — serious physical wound: a wound that isn't
      closing; **4 days to death** without a Medical Kit; pairs
      naturally with `+skip_time` for unconsciousness.
    - `poisoned` — toxin: sweats, cramps, taste of metal.

    If the brief calls for a scrape, some bruises, or a sore
    shoulder, do not reach for `+add_condition injured` — the
    narrative damage doesn't match the mechanical damage. The
    right verb for a minor wound is often nothing at all (a
    narrative-only beat) or `+damage_spirits` if the cost is
    morale/composure rather than flesh.

12. **Picker preambles frame the approach without telegraphing
    the answer.** The prose between a choice's `* Option text`
    line and its `@if check` is shown alongside the picker, before
    the player commits. Its job is to give the player layout,
    stakes, and what is at hand. Its job is NOT to advise. Phrases
    that lean the reader toward the correct approach ("the crew
    is starting to turn," "you don't have much time," "best to
    move now") clobber the choice — the player no longer feels
    they are picking between three real approaches. Put the hint
    in the *layout*, not the recommendation: "The crew is on the
    work side of the housing, not coordinated yet. Veran is the
    closest" frames a rush-vs-outlast choice without telling the
    reader which to pick. Wrong approaches are punished by the
    `@else` branch, not by an unfair preamble. The preamble stays
    neutral; the outcome teaches.

Write the files in dependency order (Start.enc last, since it links
forward; or write all and verify at the end — either works).

### Step 5 — Verify.

Run:

```sh
dotnet run --project text/encounter-tool/EncounterCli -- check text/encounters/arcs/<biome>/<arc>
```

If any file fails, fix the specific syntax error and re-run. Common
breakages: unclosed `@if` braces, a picker check without `@else`, a
`[requires]` condition referencing a verb not in the vocabulary.

### Step 6 — Walk the graph.

Produce a short walk-through in chat:

- **Reachability**: list every `.enc` file and confirm a path exists
  from `Start.enc` to it. Files reachable only via dead branches are
  bugs.
- **Tag/quality balance**: list every `+add_tag X` / `+quality X N`
  setter and every `[requires tag X]` / `@if tag X` / `[requires
  quality X M]` reader. Every reader must have at least one setter
  that fires before it in some play sequence. Orphan readers and
  unused setters are bugs.
- **Termination**: list every spoke and confirm each one either
  (a) advances arc state via `+add_tag` / `+quality` / `+add_item`,
  or (b) terminates via `+finish_dungeon` / `+flee_dungeon`. A spoke
  that does neither is an infinite-loop risk.
- **Endings**: list every reachable `+finish_dungeon` and
  `+flee_dungeon` and the play sequence that triggers it. Confirm
  this matches the endings the brief promised.

### Step 7 — Hand off.

Tell the user:

- File list shipped.
- Number of FIXME beats by encounter (gives them a sense of the work
  the later passes have to do).
- Any structural questions the brief left ambiguous and you resolved
  one way — they may want to flip the call.
- Any patterns you reached for that aren't in `arc_patterns.md` yet
  — those are inventory deepening opportunities.

## Output contract

The skill's deliverable is a directory of `.enc` files satisfying
**all** of the following at the moment of hand-off:

1. `EncounterCli check` passes with zero errors. Warnings are okay if
   the user accepts them.
2. Every `.enc` has the right front-matter for its role.
3. Every choice has a destination (a `+open`, a `+finish_dungeon`, a
   `+flee_dungeon`, or a clear single-outcome resolution).
4. Every `[requires]` and `@if` condition references a tag/quality
   that is set by a reachable prior scene, or a `has`/`meets`/`check`
   the player can plausibly satisfy.
5. Every spoke advances state or terminates.
6. Every terminal is reachable.
7. Every ending route promised by the brief has a path to it.
8. Prose is FIXME stubs only. No multi-paragraph generated prose. No
   improvised flavor text masquerading as final.

## Hard rules

These are violations the skill must never produce. They are the
non-negotiables from `arc_patterns.md` §9 and `rules/encounter_mechanics.md`:

- **At least one choice per encounter must be unconditional.**
  `check` hard-rejects encounters where every choice carries a
  `[requires]` trailer (see `arc_patterns.md` §3.1). Default to
  satisfying this via pattern 1a (recap-in-place) on the first
  character-introduction spoke of each hub — that gives clean
  revisit behavior *and* an always-visible exit.
- **No `@if`, `@elif`, `@else`, `+verb`, or `}` in the body.** The
  body (everything between the title/front-matter and the `choices:`
  line) renders as raw prose at runtime; flow-control tokens display
  literally. `check` rejects them. If a terminal scene needs route-
  branched prose, put the whole branched block inside the single
  closing choice's outcome.
- **No `quality X 0` as a hide-after-visit gate.** It evaluates to
  "value ≥ 0", which is always true after init. Use a tag, or
  `!quality X 1`.
- **No picker check without `@else`.** Parser rejects this.
- **No picker check mid-chain.** Picker is terminal in its `@if`
  chain; static gates (`has`, `tag`, `quality`, `meets`) may precede
  it but `@elif` may not follow it.
- **No `[requires]` referencing a tag with no setter.** Every gate
  must have a path that opens it.
- **No spoke that loops back without state advance.** A choice that
  `+open`s back to the hub must either set a tag/quality or grant an
  item; otherwise the player can spin indefinitely.
- **No em-dashes (—) in prose stubs.** `EncounterCli check` hard-rejects them.
- **No `+remove_tag` unless the brief explicitly calls for
  reversible state.** Default to monotonic accumulation; arcs rarely
  need to forget.
- **No global tags from arc-internal encounters.** Use the
  `<arc>.<flag>` namespace. The only bare-name tag an arc touches is
  the global `<arc>_known` presence tag, and that's set by the
  storylet that hooks the arc — see if the brief implies one, but
  default to not authoring it in this pass.
- **No combat encounters (.fight files).** The skill produces `.enc`
  only; `.fight` files are out of scope. If the brief calls for
  combat, leave a FIXME and let the user wire the `.fight` later.
- **No invented vocabulary.** Every verb in `+verb args` and every
  condition in `@if` / `[requires]` must exist in
  `rules/encounter_mechanics.md`. If the brief implies a mechanic
  that doesn't, flag it for the user — do not improvise.

## Interaction model

- Plan first, write second. Always.
- Briefs are partial maps, not specifications. Read them once for
  beats, once for endings, once for state. Ask before resolving any
  ambiguity that changes the arc shape (e.g. "the brief gives two
  endings but a third feels implied — should I include it?").
- The user will sometimes anchor on a structural choice that doesn't
  fit (e.g. "make every conversation a picker check"). When this
  happens, push back once with the pattern reference; if the user
  insists, comply. They know their game.
- After writing, the **user is the final reviewer**. The skill walks
  the graph; the user okays the walkthrough. Do not declare the arc
  done until the user has read the walkthrough and accepted.

## Notes

- The brief format is freeform structured markdown. Briefs vary in
  what they specify — some give explicit choice text and reward verbs
  (`brides_cave/README.md`), some give pure prose sketches
  (`TheRelayPost.md`). The skill adapts: extract structure where
  present, infer where absent, and surface anything that needed
  inference in the hand-off (Step 7).
- The skill operates inside the arc directory. The brief, the .enc
  files, any sidecar `scenes.md` or `context.md` all live together.
- The skill does not run `bundle`, `worlds/<world>/build.sh`, or any
  asset-rebuild step. That's the author's call once the arc is
  prose-complete.
- This skill is the first stage of the broader arc-writer pipeline
  documented in `plans/arc_writer.md`. The subsequent passes
  (colorize, factual, voice, adversarial) operate on the output of
  this stage. Producing a clean structural skeleton is what makes
  those passes viable.
