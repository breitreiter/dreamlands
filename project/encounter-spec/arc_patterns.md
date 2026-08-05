# Arc patterns

A working inventory of the structural and storytelling techniques used in
the eight authored arcs. This is the reference document the arc-writer
**decompose** pass (see `plans/arc_writer.md`) reads when turning a brief
into a set of structurally-sound `.enc` files.

It is deliberately *patterns, not prose advice*. Voice, register, and
flavor are out of scope here — those are the voice-writer pass's problem.
What this document captures is: how the arcs wire choices to qualities to
gating, how hubs hold the shape, how branches terminate, what makes a
loop a loop and not a trap.

**Status:** v0.2. Seeded from full reads of `plains/brides_cave`,
`forest/the_fugitive` (Start + Camp), `forest/the_hermitage` (full
skim), and `plains/wrenbury` (full read). Extended with patterns
surfaced by the first `arc-decompose` skill run on
`scrub/relay_post` (the §3.1 unconditional-choice floor, the §3.3
staged-gate pattern). The remaining four authored arcs
(`forest/the_lodge`, `plains/grainway_station`, `plains/metal_beast`,
`plains/the_city`) are inventoried at section depth only and need a
full pass — see "Still to mine" at the bottom of each section. Treat
findings as provisional until cross-arc confirmation.

**Authored arcs in scope** (eight):

- `forest/the_fugitive` — Mareen, oathbreaker brand, named outlanders
- `forest/the_hermitage` — pacifist former soldiers, deserters in the barn
- `forest/the_lodge` — the beast, Yana (in progress on `combat-pivot`)
- `plains/brides_cave` — ghost chamber, three endings
- `plains/grainway_station` — Aldric, the assault, two reward paths
- `plains/metal_beast` — Pyke, the buried machine
- `plains/the_city` — multi-location plaza+archive+watchtower
- `plains/wrenbury` — settlement-shaped arc

Mountains, scrub, and swamp arc directories exist but currently hold
`Start.enc` stubs only and are excluded from the inventory.

---

## 1. Arc shape: hubs, spokes, terminals

### 1a. Lightweight hub pattern

A hub encounter is one the player returns to multiple times during the
arc. Hubs do as little narrative work as possible on repeat visits — most
of their body is gated behind first-visit tags so the prose doesn't
re-fire every loop. The hub's job is **routing**, not telling.

The hub itself is short prose at the top of the file (two or three
sentences of ambient texture, no characters, no plot delivery) followed
by a `choices:` block. The hub-spoke distinction is structural, not
syntactic: a hub is just an encounter every spoke ends `+open`'s back to.

**Canonical example**: `forest/the_fugitive/The Camp.enc`.

- Title prose is two lines of low-information texture ("Voices carry
  across the clearing…"). No characters introduced; no plot delivered.
- Eight choices, each one a spoke to a named character or sub-area, plus
  the terminal "Wait for dawn".
- Three of the eight spokes use `[requires tag …]` so they only appear
  once the player has unlocked them (knife_truth → Explain the knife;
  rumor_corrected → Talk to Edda, Talk to Silla).
- Every spoke ends `+open "The Camp"` so the player returns to the same
  hub for the next pick.
- Each spoke wraps its prose in `@if tag fugitive.talked_<x> { short
  recap } @else { full scene + add_tag }` so re-entering the same spoke
  costs one line of text instead of a re-run.

This is one of two canonical mid-arc hub shapes. It generalizes:

```
* <Choice that opens a scene>
  @if tag arc.flag_<spoke> {
    <one-line acknowledgement that you've been here>
  } @else {
    <full scene prose>
    +add_tag arc.flag_<spoke>
  }
  +open "Hub"
```

The `+open "Hub"` after the conditional ensures both branches return
home; the tag toggles full-vs-recap on the next entry.

> **This pattern was silently broken until 2026-08-04.** The parser
> discarded choice-level mechanics written outside the `@if`/`@else`, so
> the `+open "Hub"` above never reached the engine and the spoke ended the
> encounter instead of returning. It hit 18 choices across `the_fugitive`
> (15), `the_hermitage` (2), and `signal_array` (1) — all of them written
> correctly against this doc. Fixed in the parser (no content edits), and
> `check` now hard-fails on any dropped mechanic. See
> `bugs/choice_mechanics_after_conditional_dropped.md` and `format.md` §3.2 E.

### 1a.ii. Hide-the-choice variant

The other common shape — used in `forest/the_hermitage/Tower.enc` and
`Garden.enc` — gates the choice itself with `[requires !tag <flag>]`
so the option disappears from the menu after first use. The body has no
recap because there's nothing to recap to:

```
* "Ask Osric about the hermitage" = How did this place come to be? [requires !tag hermitage.asked_history]
    <full scene prose>
    +add_tag hermitage.asked_history
    +open "Tower"
```

**Choosing between 1a (recap-in-place) and 1a.ii (hide-the-choice):**

- Use **1a** when revisiting the spoke has narrative meaning — the
  player's mood, the camp's mood, a passing reminder of what was said.
  `The Camp.enc`'s "Watch Gault" recap ("He's still talking. The
  audience has thinned…") is doing real work the second time.
- Use **1a.ii** when the spoke delivers a one-shot piece of
  information — a question Osric answers once, an introduction to a
  character. There's nothing to recap so hiding the option keeps the
  hub menu trim as the player makes progress.

The two patterns can coexist in one arc — `the_fugitive` uses 1a almost
exclusively; `the_hermitage` leans on 1a.ii throughout.

### 1a.iii. Mutually-exclusive routes via stacked negation

Hermitage's `Tower.enc` uses a third variant on top of 1a.ii: when
multiple spokes lead to *exclusive* downstream paths, each spoke's
`[requires]` negates the tags the other paths would set:

```
* Go to the barn alone
    +add_tag hermitage.met_edric +open "Barn Alone"

* Ask Osric to come with you to the barn [requires tag hermitage.asked_history && !tag hermitage.edric_cracked && !tag hermitage.barn_with_osric && !tag hermitage.meilin_accompanies]
    ...

* Go to the barn with Meilin [requires tag hermitage.meilin_accompanies && !tag hermitage.edric_engaged && !tag hermitage.barn_with_osric]
    ...
```

The pattern: each exclusive route disappears once the player has
committed to a different one. Once Meilin is accompanying you
(`meilin_accompanies` set), the "Osric comes along" option vanishes;
once you've already done the Osric run (`barn_with_osric` set), both
alternatives vanish.

This is the multi-clause pattern's headline use case. See §2c for the
compound-condition syntax in `[requires]` and `@if`.

### 1b. Start hub vs. mid hub

`Start.enc` and mid-arc hubs are not the same shape.

- **`Start.enc`**: arc entry point. Always referenced from outside via
  the arc-roster system. Prose is dense — sets premise, introduces the
  inciting incident, frames the moral stakes. Choices are the first
  meaningful decision; the player has no qualities/tags from this arc
  yet, so no gating beyond an obvious early `has <item>` check.
  Examples: `brides_cave/Start.enc` (single choice, lantern gate);
  `the_fugitive/Start.enc` (Stay vs. Walk away — Walk away is an arc
  bailout via `+flee_dungeon`).
- **Mid hub** (`The Camp.enc`, `Tower.enc`, `Garden.enc`,
  `Wrenbury.enc`, `Meilin.enc`): see §1a, 1a.ii, 1a.iii.

The two shapes can co-exist in the same arc. `the_fugitive` has both:
`Start.enc` (narrative-heavy, one-way) → `The Camp.enc` (routing hub).

### 1b.ii. Hub replacement / staged hubs

Some arcs use multiple mid-hubs, with the *current* hub changing as the
arc progresses. The arc's "where do I return to" target shifts mid-flow.

**`the_hermitage`** is the cleanest example:

- `Start.enc` → one decision (enter or leave) → `Cart.enc`.
- `Cart.enc` is a one-shot bridge: it has one choice that runs a long
  prose scene and `+open`s `Tower.enc`. Cart is never re-entered.
- `Tower.enc` is the primary mid-hub for the central act. All
  conversation spokes (Osric, Garden/Aldous, Meilin) `+open` back to
  `Tower`.
- Once the player commits to a barn route, the hub changes:
  - `Barn Alone.enc` and `Barn with Osric.enc` are *themselves* small
    hubs (their spokes loop back to them, not to Tower) — but only for
    the duration of the barn conversation.
  - `Barn Meilin.enc` is a *terminal* hub: most choices end the arc.
    One choice ("Return to the tower") routes to a state-specific
    intermediate, `Meilin Acts.enc`, which is a *new* hub for the
    violent ending only.

This staged-hub structure is how `the_hermitage` carries a
three-character (Osric / Aldous / Meilin) investigation alongside a
three-faction (devoted / Kesharat / imperials) moral choice without any
single encounter file getting unwieldy.

**`brides_cave`**, by contrast, has no mid-hub at all: it's a strict
linear chain Start → Record → Ghosts, with the branching all happening
at the terminal scene.

The skill needs to support both shapes — and to *pick* between them
based on brief shape. Rule of thumb: if the brief names more than two
characters or two locations the player must visit before the arc
resolves, use a mid-hub. Below that, a linear chain is cleaner.

### 1c. Terminal beats

Every authored arc terminates via one of two verbs:

- `+finish_dungeon` — the arc succeeded; the player gets the reward
  hooked elsewhere (rewards arrive on the return-to-settlement summary,
  not in the .enc).
- `+flee_dungeon` — the arc was abandoned; no reward, but the arc
  state clears.

In `brides_cave/The Ghosts.enc`, all three player choices terminate the
arc — `+finish_dungeon` for all three outcomes, regardless of how the
fight went. The "fail" branch of the combat check still ends the arc
(via `+damage_spirits 2 + skip_time morning + finish_dungeon`). The arc
terminates because the *scene* is over, not because the player won.

In `brides_cave/Start.enc`, the lantern-less branch terminates via
`+flee_dungeon` — the arc never really started, so it doesn't count as
finished.

**Inference**: pick `+flee_dungeon` when the player turned back before
engaging the arc's core conflict; pick `+finish_dungeon` when they
engaged it, regardless of outcome.

### 1c.ii. Same reward, multiple ending routes

`the_hermitage` rewards `+add_item scarecrow_boots` on three distinct
ending routes:

- `Barn with Osric.enc` — peaceful resolution; player vouches for the
  monastery with a guild writ.
- `Barn Meilin.enc` "Propose an alliance" with negotiation check
  success — armed-compound resolution.
- `Barn Meilin.enc` "Let it run" — Meilin scouts under cover; the
  player witnesses the violence offstage.

Other ending paths in the same arc (Meilin Acts, Barn with Osric "Keep
silent", Barn Meilin alliance check-fail) do not grant the boots.

**Pattern**: an arc may have multiple ending routes that all qualify as
the *good* ending in different terms, and the item reward attaches to
all of them. The skill should not assume "one reward = one path"; the
brief should declare which ending shapes earn the artifact and the
decomposer should hang the `+add_item` on each.

### 1c.iii. Terminal scenes routed by player commitment

`the_hermitage/Meilin Acts.enc` is only reachable from a *specific*
return path: `Barn Meilin.enc`'s "Return to the tower" choice routes
there, not back to `Tower.enc` — but only when the player has earlier
learned Meilin's plan (`meilin_plan_known`), and has *not* used the
negotiation success path. Player commitment in an earlier scene rewires
where "Return to the hub" actually goes.

This is the "you made a choice you can't take back" structural beat:
the choice's prose reads as a normal return-to-hub, but the destination
is a terminal scene the player can't escape.

### Still to mine (§1)

- Full survey of mid-hub shapes across `the_hermitage`,
  `grainway_station`, `metal_beast`, `wrenbury`, `the_city`.
- Whether any arc uses two mid hubs (e.g., a hub-of-hubs). `the_city`'s
  file list (Plaza, Archive, Watchtower, Commons, Exchange) suggests yes.
- Whether any arc terminates from a hub rather than a leaf scene.

---

## 2. State carriers: tags and qualities

### 2a. Tags = arc-local boolean flags

Every authored arc namespaces its tags with the arc identifier as a
prefix: `fugitive.night_passed`, `fugitive.saw_gault`,
`fugitive.knife_truth`, `fugitive.rumor_corrected`,
`fugitive.talked_edda`, `fugitive.talked_silla`. The dot is
convention, not parser syntax — the parser treats the whole id as
opaque. The prefix prevents accidental collision with global tags
(`brides_cave_known`) and other arcs.

**Two distinct uses of tags inside an arc:**

1. **Scene-visited markers**: `fugitive.talked_silla`,
   `fugitive.saw_gault`, `fugitive.night_passed`. Set on first entry,
   read on re-entry to swap to a recap. Pattern in §1a.
2. **Earned-knowledge markers**: `fugitive.knife_truth`,
   `fugitive.rumor_corrected`. Set when the player learns or accomplishes
   something, read by `[requires tag …]` on choices elsewhere to
   surface new options.

Authored arcs do not appear to mix global and arc-local tags in the
same condition. Global tags (e.g. `brides_cave_known`) live at the
encounter-level `[requires]` for arc gating; arc-local tags live at
choice level and `@if` inside the arc.

### 2b. Qualities = arc-local numeric state

Qualities are used for things tags can't express cleanly: accumulating
counters, scaled outcomes, or multi-state values. They default to 0,
take signed adjustments via `+quality <id> <amount>`, and read via
`@if quality <id> <threshold>` or `[requires quality <id> <threshold>]`.

**Threshold semantics** (per `lib/Game/Conditions.cs:EvaluateQuality`):

- Positive threshold (`quality x 3`): true iff `value ≥ 3`.
- Negative threshold (`quality x -2`): true iff `value ≤ -2`.
- Threshold zero (`quality x 0`): true iff `value ≥ 0`, i.e.,
  **always true** for monotonically-incremented qualities.

**`brides_cave` and `the_fugitive` do not use qualities at all** — pure
tag arcs. `the_hermitage` also has no quality use; everything is tags.

**No inventoried arc currently uses qualities.** `plains/wrenbury`
originally used them for visit-tracking but the threshold-zero gate
didn't close on re-entry; the arc was rewritten to tag negation
(`!tag wrenbury.toured`, `!tag wrenbury.worker_met`). See §9 for the
trap. The "countdown from N" pattern (below) remains useful but no
authored arc has needed it yet.

The "countdown from N" pattern (worth equipping the skill with even
though wrenbury's specific instantiation is incorrect):

> Start the quality at N (initialize in the entry encounter via
> `+quality <id> 0`). Allow the player to perform some bounded action
> M times; increment the quality on each use. Gate the action on
> "remaining tries > 0" — i.e. `!quality <id> N` (which evaluates
> `value < N`, i.e. "fewer than N uses recorded"). After N uses, the
> gate closes and an alternative branch can fire ("the guide has
> finished for the day; come back tomorrow").

This is more expressive than a boolean tag: you can let the player
tour twice, then have the guide tell them to come back tomorrow; you
can let the player ask Edda three follow-ups; you can let the player
buy from the vendor up to a stock cap. Tags max out at "happened / did
not happen"; counters max out at "how many times."

Use a tag when binary suffices (the vast majority of cases). Use a
quality when degree is structurally important — accumulating
reputation, bounded tries, stage advancement that needs more than two
states.

### 2c. Compound conditions in `[requires]` and `@if`

**Canonical reference**: `rules/encounter_mechanics.md` is the living
spec for vocabulary and condition syntax. `project/encounter-spec/format.md`
predates the rules substrate and is partial — when the two disagree,
the rules doc wins. The skill should read both, prefer the rule.

Per the rule and verified at `lib/Game/Conditions.cs:26` (||), `:38`
(&&), `:52` (! prefix), `[requires]` and `@if` accept full boolean
expressions:

```
[requires tag hermitage.asked_history && !tag hermitage.edric_cracked && !tag hermitage.barn_with_osric && !tag hermitage.meilin_accompanies]
```

Rules:

- `!` may prefix any stateless atom (`has`, `tag`, `quality`).
- `check` and `meets` cannot be negated or appear in compound
  expressions (per rules/encounter_mechanics.md). They stand alone.
- Precedence: `!` (tightest) > `&&` > `||`. No parentheses; refactor
  if precedence is awkward.

The hermitage arc relies on compound `[requires]` to gate exclusive
routes (§1a.iii).

### 2d. Naming conventions

From the seen arcs:

- Arc-local tag prefix: lowercase arc directory name + `.` + descriptor.
  `fugitive.talked_silla`, `fugitive.knife_truth`.
- Talked-to-character markers: `<arc>.talked_<character>` (fugitive),
  `<arc>.<character>_<topic>` (hermitage: `meilin_whoami`,
  `aldous_fought`, `edric_pitch`).
- Witnessed-event markers: `<arc>.saw_<thing>`,
  `<arc>.met_<character>`, `<arc>.<event>_passed`.
- Earned-knowledge markers: `<arc>.<topic>_<state>` —
  `knife_truth`, `rumor_corrected`, `quiet_soldier_seen`,
  `osric_story_heard`.
- Commitment markers (gate exclusive routes): `<arc>.<character>_<action>` —
  `meilin_accompanies`, `meilin_plan_known`, `meilin_plan_hidden`,
  `edric_cracked`, `barn_with_osric`.
- Global per-arc presence tag (read by external storylets): bare arc
  name like `brides_cave_known` — no dot, no prefix.

Two slot-shapes worth distinguishing because the skill will mix them up:

- **`<arc>.<verb>_<noun>` (state-of-the-world)**: `asked_history`,
  `met_edric`, `rumor_corrected`. Reads as "this thing has happened."
- **`<arc>.<character>_<topic>` (per-character knowledge)**:
  `meilin_whoami`, `aldous_fought`, `edric_kesharat`. Reads as "we have
  had this specific conversation about this specific topic." Scales
  better than `<arc>.talked_<character>` when each character has
  multiple addressable topics.

### Still to mine (§2)

- Quality naming conventions and threshold idioms.
- Whether any arc uses `+remove_tag` or quality decrements (recoverable
  state) vs. only monotonic accumulation.
- How the arc-roster system pre-seeds any tags before `Start.enc`.

---

## 3. Choice gating with `[requires …]`

The `[requires <condition>]` trailing on a choice line *hides* the
choice from the UI when the condition fails. This is distinct from `@if`
inside an outcome, which gates the *content* of a choice already
visible.

**Use `[requires]` when**:
- The choice would make no narrative sense before some prior event
  (`The Camp`: "Explain the knife" requires the player has heard the
  knife's truth from the Outlanders).
- The choice is a follow-up to a chain (`The Camp`: "Talk to Edda" and
  "Talk to Silla" both require `rumor_corrected` — they only show up
  *after* the player has done the work the Edda/Silla scenes react to).

**Use `@if` inside the outcome when**:
- The choice is always offered but the result depends on player state
  (`brides_cave/Start.enc`: "Explore the cave" — lantern check is in
  the outcome, not the choice gate, because the player needs to
  *learn* they should have brought a lantern).

A choice with no `[requires]` always shows. A choice with `[requires]`
hides cleanly with no UI artifact; players don't see options they
haven't earned.

### 3.1. At least one choice per encounter must be unconditional

`EncounterCli check` hard-rejects encounters where *every* choice
carries a `[requires]` trailer with the error:

```
All choices are gated with [requires] — at least one must be unconditional
```

Rationale: the runtime can otherwise reach a state where the player
sees no choices and gets stuck. The check enforces a structural
floor — every hub must have at least one always-visible exit.

The fugitive arc satisfies this with "Speak with the outlanders"
(unconditional). The relay_post arc satisfies it with "Leave at dawn
without acting" (unconditional in the midnight hub) and by converting
Devra and Ossal spokes from pattern 1a.ii (hide-the-choice) to 1a
(recap-in-place) in the evening hub. Pattern 1a always-visible spokes
are the cleanest way to satisfy this rule for character-introduction
beats — they revisit cleanly *and* count as unconditional.

### 3.2. Multi-condition gating

`[requires]` accepts compound conditions via `&&`, `||`, and `!` (see
§2c). `the_hermitage` uses multi-clause requires throughout to gate
exclusive routes (§1a.iii). For state-machine-heavy arcs this is the
right tool; for simpler arcs, keep gates single-clause to ease review.

### 3.3. Staged-gate pattern: "first OR Nth-but-not-after-climax"

A recurring shape in arcs with a re-readable scene whose meaning
shifts as the player learns context: the scene should be visible on
first visit, visible again to deliver a richer beat once a precursor
condition holds, then hidden once the climactic beat has fired.

Canonical example: relay_post's "Read the message file" spoke. First
read sets `saw_wire` and shows the strange-orders body. Second read
(after talking to Devra) reveals the predicted-arrival message and
sets `predicted_arrival`. After that, the choice hides.

```
* Read the message file [requires !tag relay_post.saw_wire || tag relay_post.met_devra && !tag relay_post.predicted_arrival]
    @if tag relay_post.saw_wire {
        <deep-read body that fires only after met_devra>
        +add_tag relay_post.predicted_arrival
    } @else {
        <first-read body>
        +add_tag relay_post.saw_wire
    }
    +open "Hub"
```

The gate parses by precedence (`!` > `&&` > `||`) as:

```
(!tag saw_wire) || ((tag met_devra) && (!tag predicted_arrival))
```

Meaning the choice shows if:
- the precursor hasn't fired yet (first visit), OR
- the precursor has fired AND the climactic beat hasn't yet AND the
  enabling-context tag is set.

This is the natural extension of §1a.iii's stacked-negation pattern
into a re-readable beat that loops twice and then closes. Use it
when a scene needs to be revisited specifically because its meaning
changes between visits — not for general repeat-clickable content,
which can stay always-visible with pattern 1a.

### Still to mine (§3)

- Whether any arc uses `meets <skill> <tier>` in a `[requires]`
  trailing tag (the parser accepts it; haven't seen an example yet).
- Patterns for `[requires]` on settlement-stocked encounters (the
  `brides_cave/The Old Man's Story.enc` file uses
  `[requires tag brides_cave_known]` at the encounter level — is
  that pattern reused?).

---

## 4. Branch and check patterns

### 4a. Picker check is terminal + `@else`

Per the format spec (§3.3): a `check <skill> correct:X wrong:Y` is only
legal as the terminal branch of an `@if`/`@elif` chain, and the chain
must end with `@else`. Authored arcs respect this; no creative
violations to inventory.

### 4b. Stacking static gates before a picker

`@if has <item> → @elif tag <flag> → @elif meets <skill> <tier> →
@elif check <skill> correct:X wrong:Y → @else { fail }` is the legal
maximal chain. Pattern seen in the format-spec example for "Pick the
lock"; not yet confirmed in authored arcs.

`brides_cave/Start.enc` uses the simplest case: `@if has brass_lantern
{ … } @else { … }` — one static gate, no picker, no fallback chain.

`brides_cave/A Record in Stone.enc` uses the second simplest:
`@if check bushcraft correct:plan wrong:push { … } @else { … }` —
picker check standalone, no static prefix.

`brides_cave/The Ghosts.enc` mixes outcomes: one choice has a picker
check, one is unconditional prose, one is a long unconditional scene
with `+add_level`. No mid-encounter chained gates.

### 4c. "Approach with open hands" — non-check resolution path

A recurring pattern: a choice that resolves the encounter without a
check, often as the *thematically correct* answer the arc rewards. In
`brides_cave/The Ghosts.enc`, "Approach with open hands" has no `@if`
at all — it's a long unconditional prose block ending in `+add_level`.
The combat check, by contrast, can succeed *or fail* and the arc still
gives `+heal_spirits 3` on the win or damages spirits on the fail. The
non-check path is the "you got it" outcome.

**Inference**: an arc may have multiple endings: some skill-gated
(uncertain outcome, big payoff on win, soft fail on loss), some
choice-gated by player wisdom (deterministic, also rewarding, no risk).
Both endings can be "good" in different ways.

### 4d. Choice preamble = prose before `@if`

The pattern from format spec §3.2D ("Mixed" outcome) — prose, then a
gated branch — is used in `brides_cave/The Ghosts.enc`'s "Draw your
weapon" choice. The first paragraph ("You draw steel. The blade rings
clear in the dark.") renders unconditionally, then the picker resolves
the rest. This lets the choice's *committed* action read the same on
win and fail, with the divergence happening in what comes after.

### Still to mine (§4)

- Survey of which skill each arc reaches for in pickers and what
  approaches (`correct:X wrong:Y`) they pair with. Working theory: each
  arc has a "house skill" matching its biome/theme.
- Whether arcs use the four-skill picker symmetrically or favor 1–2.
- Patterns for `meets <skill> <tier>` as a deterministic gate before a
  picker check (the "if you're already good enough, skip the roll"
  shape).

---

## 5. Edge-transit prose

How an encounter acknowledges the *between* — the time and ground the
player crosses between scenes — without inventing new state.

**`brides_cave/A Record in Stone.enc` win branch** (lines 31–35) is the
canonical example: a paragraph of detail about *how* the player threads
the passages, what they see and smell, ending with arrival at the next
chamber. The prose advances geography without introducing characters,
choices, or qualities — it bridges the cut between two scenes.

**`brides_cave/A Record in Stone.enc` fail branch** is the *short*
edge-transit: three sentences, `+skip_time morning`, then `+open "The
Ghosts"`. Same destination as the win branch, weaker prose, and a
time-cost via `skip_time` to make the failure tangible without
narratively changing the destination.

**Pattern**: edge-transit prose belongs in the *outgoing* outcome of a
choice, not the *incoming* preamble of the next encounter. The next
encounter assumes the player is already in place. This keeps each
encounter file readable as a self-contained scene.

**`the_fugitive/Start.enc` Walk-away outcome** (lines 52–54) demonstrates
edge-transit out of the arc entirely: "The argument is still audible
behind you for a quarter mile, voices rising and falling through the
trees, getting quieter with distance." A sound-fade transitions the
player from inside the arc to outside it, then `+flee_dungeon`.

### 5b. Intro-in-transit pattern

A structural variant for hub-spoke shapes that emerged from the
`signal_array` decompose exercise (2026-05-31): the *transit prose* of
a hub's spoke choice can carry significant scene content — the
introduction of an NPC, the opening exchange, a first revealed beat
— leaving the spoke encounter itself to carry only the meaningful
decision the PC is about to make.

The hub-side transit carries the *scene*; the spoke encounter
carries the *decision*.

```
# In the hub's choices: block

* Walk over to the foreman = He has the head of the rest circle and the teapot. [requires !tag arc.met_veran]
    FIXME(mundane): You cross to Veran and sit down. He pours you tea.
    FIXME(mundane): Veran is in his mid-fifties, weathered, foreman of the crew.
    FIXME(mundane): He talks about the route from the last site. He names the landmarks.
    FIXME(dread): The route does not close.
    +open "Veran"

# In Veran.enc

FIXME(mundane): Veran is across from you at the rest circle, finishing his tea. He waits for what you might say.

choices:
* Press him on the geometry = ...
* Step back to the circle = ...
```

**When to use:**

- The spoke has a meaningful decision after a load-bearing intro
  or first-beat. Hub transit carries the intro; spoke carries the
  decision.
- The spoke would otherwise contain a "if you press him..."
  hypothetical (arc-decompose skill continuity rule §10): uplift
  the first beat into the transit, make the press a real choice.
  This is the natural shape for "press or step back" forks.

**When NOT to use:**

- The intro and the decision are inseparable (the PC walks in,
  sees something, must choose immediately). Keep the whole beat in
  the spoke body.
- The spoke is pure information-delivery hide-after-visit (§1a.ii)
  with no decision — body carries everything, the only choice is
  "Return to the hub." No reason to fragment.
- The spoke is itself a sub-hub with its own multi-spoke menu.
  Standard hub shape applies; intro in spoke body.

**Canonical instances:**

- `signal_array/The Rest Interval.enc` (2026-05-31) — four of the
  five named-NPC spokes (Veran / Baret / Richard / Chorik) use
  this shape. The Observe spoke does not, because it has no
  separate decision to delegate to.

Pairs naturally with the bibles + many-small-files emergent
pattern (see `plans/arc_studio.md`): each spoke `.enc` stays
small, single-purpose, and easy to read in isolation.

### Still to mine (§5)

- Examples of long inter-scene travel (multi-day, biome-change) and how
  they pair narrative with `+skip_time` or `+damage_health` ambient
  costs. `the_hermitage` and `metal_beast` likely have these.

---

## 6. Loop avoidance and graceful termination

An arc must not allow the player to spin indefinitely in a hub without
the option to move forward. Two structural protections:

### 6a. The "Wait for dawn"-style forward exit

`the_fugitive/The Camp.enc`'s last choice is "Wait for dawn", gated by
`[requires tag fugitive.night_passed]` — and `night_passed` is set the
*first time* the player picks any spoke that takes them outside the hub
(Outlanders or Mareen). So:

- First entry to the hub: only the three first-pass options are visible
  (Outlanders, Mareen, watch Gault).
- After visiting Outlanders OR Mareen, `night_passed` is set, and "Wait
  for dawn" appears.
- After learning the knife truth from Outlanders, "Explain the knife"
  appears.
- After explaining it, "Talk to Edda" and "Talk to Silla" appear.
- The player can never spin a hub without the option to advance — the
  forward exit gates *on* at the same moment new content appears.

The pattern is: **set the forward-exit tag in the first scene that
delivers required information; gate the hub's terminal option on that
tag.** No arc-end is ever locked behind "you must have explored
everything."

### 6b. Re-entry recap prevents prose fatigue

The `@if tag talked_<x> { short recap } @else { full scene }` pattern
from §1a is the second protection: even if the player revisits every
spoke twice, the second visit costs one line of text. Hub spinning is
*cheap* in attention as well as available — the player can re-enter a
spoke they vaguely remember without paying the cost of re-reading the
whole scene.

### Still to mine (§6)

- Whether any arc uses a "you've exhausted the hub" terminal — a choice
  that appears only after every spoke has been visited (logically: an
  `[requires]` on a tag set by the last unvisited spoke). Probably not,
  but worth confirming.
- How arcs handle the player picking the terminal exit early —
  presumably the unvisited spokes are simply un-experienced and the
  arc resolves with less context. `the_fugitive/Dawn.enc` will show
  whether dawn-scene behavior shifts based on what tags are set.

---

## 7. Front matter conventions

From the seen arcs, the front-matter idioms are stable:

- Arc-internal encounters use `[trigger none]` — they're only reachable
  via `+open`-chains from the arc's `Start.enc`.
- `[vignette dungeons/<arc_name>]` is set on every encounter in the
  arc to keep the image consistent across the whole arc rather than
  flipping to per-terrain art.
- `[tier]` is not set on arc-internal encounters (the arc inherits its
  tier from the roster).
- The settlement-stocked storylet that introduces the arc to the player
  (`brides_cave/The Old Man's Story.enc`) uses `[trigger settlement]`
  and `[requires tag <arc>_known]`. This is the surface that hooks the
  arc into the wider game.

### Still to mine (§7)

- Confirm `[trigger settlement]` storylets exist in every arc.
- Document the `<arc>_known` tag convention (where it's set, and by
  what mechanism). Likely: world-init or first-visit-to-relevant-region.

---

## 8. Per-arc fingerprints

A one-line note on each arc's distinguishing structural choice, to be
filled in as the inventory passes complete.

- **forest/the_fugitive** — Mid-hub (`The Camp`) with eight spokes, six
  gated on tags; arc resolves at `Dawn.enc` regardless of completeness.
  Single mid-hub. Pattern 1a (recap-in-place) throughout. State is pure
  tags, no qualities.
- **forest/the_hermitage** — Staged hubs: Start → Cart (one-shot bridge)
  → Tower (central hub, three character spokes: Osric / Aldous via
  Garden / Meilin) → biome-shifted barn hubs (Barn Alone, Barn with
  Osric, Barn Meilin) → terminal scenes. Three distinct good endings
  (peaceful via Osric, armed-compound via Meilin alliance, violent via
  Meilin Acts) all granting `scarecrow_boots`. Heavy use of pattern
  1a.ii (hide-the-choice) and 1a.iii (stacked negation for exclusive
  routes). Compound `&&`/`!` conditions throughout. The largest, most
  intricate arc.
- **forest/the_lodge** — In progress on `combat-pivot`. TBD.
- **plains/brides_cave** — Three-scene linear chain (Start → Record →
  Ghosts) with three endings at the terminal, no mid-hub. The simplest
  authored arc shape.
- **plains/grainway_station** — Two-reward fork ("Legions Reward" vs.
  "Scavengers Reward"). TBD.
- **plains/metal_beast** — Pyke is a named NPC across multiple scenes.
  TBD.
- **plains/the_city** — Largest spread (8 named locations). Likely a
  hub-of-hubs structure. TBD.
- **plains/wrenbury** — Single mid-hub (`Wrenbury.enc`) with three
  spokes (village ruins / market / guide tour / worker conversation).
  Pure-tag arc (rewritten from a buggy quality-threshold version; see
  §9).
- **scrub/relay_post** — Decompose-skill exemplar (2026-05-30). Two
  staged hubs (evening `The Relay Post.enc`, then `Midnight.enc`)
  bridged by a tag-gated "Wait for midnight" advance. Five terminal
  routes across three Dawn files, with `Dawn Ossal.enc` carrying
  three sub-routes via tag-branched prose (open / stealth-win /
  stealth-fail). First arc in the inventory to use the staged-gate
  pattern for re-readable context shifts (§3.3); first to use pattern
  1a (recap-in-place) specifically to satisfy the §3.1 "at least one
  unconditional choice" rule. Pure tags, no qualities, single picker
  (cunning). Two terminal scenes: "Take her to Cardenio" (story; +100g)
  and "Take her to the pits" (truth; +15g). The reward is more gold for
  the story path and less for the harder truth, which is a structural
  signal worth noting — the arc *materially rewards* the easier ending,
  trusting the player to find the harder one its own reward.

---

## 9. Anti-patterns and rejected shapes

Seed entries plus mistakes observed in the authored corpus:

- **Re-running full prose on hub re-entry**: rejected. Use pattern 1a
  (recap-in-place) or 1a.ii (hide-the-choice). Never blast the same
  body on every loop.
- **`[requires]` without a corresponding setter**: orphans the choice.
  The decompose skill must verify every `[requires tag X]` has at
  least one `+add_tag X` reachable from a prior scene; every
  `[requires quality X N]` (positive N) has at least one
  `+quality X <positive>` setter reachable.
- **Picker check without `@else`**: parser rejects this. Hard rule.
- **`quality X 0` as a hide-after-visit gate** *(written as such in
  `plains/wrenbury`)*: per `Conditions.cs:109` and
  `ConditionsTests.Quality_ZeroThreshold_TrueWhenZeroOrPositive`,
  threshold `0` evaluates `value >= 0`, which stays true after any
  positive increment. To get the intended "visible at 0, hidden after
  increment" behavior, the gate must read `!quality X 1` (i.e.
  "value < 1") — or simply use a tag. Originally observed in `plains/wrenbury` (since rewritten to
  tag-negation). The skill should reject `quality X 0` as a hide-gate
  pattern and offer the working alternatives: a tag, `!quality X 1`,
  or — if the brief actually calls for "N tries" — the countdown
  pattern in §2b.
- **Tag-namespace collision across arcs**: not yet observed, but the
  skill must enforce the `<arc>.<flag>` convention so two arcs that
  both want a `met_<character>` tag don't clobber each other.
- **Mutually-recursive `+open` with no advancing state**: not yet
  observed, but the obvious failure mode of a hub that contains a
  choice that opens a spoke that contains only a "Return to hub"
  choice. The decompose skill must verify every spoke either advances
  state (`+add_tag` / `+quality` / `+add_item`) or terminates
  (`+finish_dungeon` / `+flee_dungeon`).
- **Flow control or mechanic verbs in the body** *(observed in the
  first draft of `relay_post/Dawn Ossal.enc` and `Dawn Refuse.enc`
  before fix)*: the body renders as raw prose at runtime, so an
  `@if … { … }` in the body shows literally as "@if tag X {" to the
  player. The parser silently swallowed this and `check` initially
  didn't catch it. `check` now rejects it. If a terminal scene needs
  per-route prose branching, put the entire branched block inside
  the single closing choice's outcome — the body should be static
  prose true on all routes.

---

## Open questions for the inventory itself

- Should this doc live at `project/encounter-spec/arc_patterns.md`
  (chosen for now, sibling to `format.md` and `mechanics_reference.md`)
  or at `project/design/arc_patterns.md`? Spec-adjacent feels right —
  this is a recipe book for the format, not a game-design doc.
- How does this stay current as new arcs ship? The decompose skill
  reads it; if the authored corpus drifts the skill produces stale
  shapes. Probably: anytime a new arc ships, do a quick "did this arc
  introduce a pattern not in the inventory?" pass.
- Should there be code that *enforces* the patterns the skill is
  supposed to honor — e.g., `EncounterCli check` learns "every
  `[requires tag X]` has an `+add_tag X` setter reachable in the arc"?
  Probably yes, eventually. Different plan.
