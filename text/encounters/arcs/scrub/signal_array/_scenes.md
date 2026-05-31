# Scenes — The Signal Array

First draft. `[?]` = open question; `GAP:` = brief leaves to writer.

## Conventions

A *scene* is a narrative beat the player traverses as one unit
— not a `.enc` file. Decompose will fan these out into `.enc`
topology (hubs, branches, staged gates). Player activity *inside*
a scene (per-NPC chats inside a hub; rolls and forks inside a
clutch) lives as bullets under that scene's "what happens"
field.

Each scene is a `###` section. Fields:

- **Precursor:** tags / qualities / items that gate the scene.
- **What happens:** 3–6 bullets, factual. May include player
  activity (choices, rolls, branches) — those are decompose's
  problem, not the bible's.
- **Resulting state:** tags / qualities the scene can leave
  set, including the disjunction across player branches.
- **Leads to:** the next scene(s).

## Scene 1 — Arrival

- **Precursor:** none.
- **What happens:**
  - PC walks up on the mesa road and finds a Kesharat crew
    mid-work on a half-erected pylon.
  - The crew is on the schedule and the PC is an obstacle:
    routed around the cable runs, told to mind tools, sparse
    acknowledgment.
  - PC has time to clock the seams the uniform can't cover —
    Baret's clan band at the rolled cuff, Richard's grassland
    sunburn above the unbuttoned collar, Chorik holding the
    uniform too perfectly still.
  - The interval bell on Veran's belt rings: hard clap and
    sustained buzz. Tools down mid-motion. Three-second total
    flip from utilitarian-crew to pleasant-company. Rest
    interval begins.
- **Resulting state:** `tag arc_signal_array_started`,
  `tag bell_rung`.
- **Leads to:** Rest Interval.

## Scene 2 — Rest Interval

- **Precursor:** `tag bell_rung`.
- **What happens:**
  - Crew sharing tea around the rest circle, warm and ribbing
    each other, treating the PC as a guest.
  - PC can engage Veran (the route from the last site that
    doesn't close), Baret (family, "just this one more job"),
    Richard (the pig, the brother, the farm — register shifts
    audibly when the pylons come up), or sit and observe.
  - The Chorik aside is available here as a heavier beat —
    see optional side-scene below.
  - GAP: the brief invites 3–4 separable conversations plus
    ambient; the spokes can be freely chosen or partially
    gated to encourage at least one before the interval ends.
  - When the PC has had enough or prompts it, the bell rings
    the interval out — a softer signal, no flip, just a
    return to work.
- **Resulting state:** `tag interval_traversed` plus any
  per-NPC tags the writer wants to carry forward (e.g. for
  the Chorik aftermath leverage).
- **Leads to:** Completion (default) or Early Depart (outro).

### Scene 2b — Chorik aside (optional)

- **Precursor:** inside Rest Interval.
- **What happens:**
  - PC attempts to engage Chorik directly.
  - Chorik declines, deflects, changes subject.
  - The other workers patch his discomfort with on-his-behalf
    lies ("he's just tired," "don't ask him about that") —
    inventions Chorik neither confirms nor denies.
  - PC sees Chorik is the only one whose flip was not total.
  - This beat is the leverage the Aftermath Negotiation roll
    will reach for. Whether to fold it into Scene 2 as a
    spoke or surface it as a discrete side-scene during
    decompose is the author's call.
- **Resulting state:** `tag chorik_engaged`.
- **Leads to:** back into Rest Interval.

## Scene 3 — Completion and Choice

The clutch. One scene, even though the player-facing
activity has internal structure (roll, branch, sub-roll). All
of it lives at the same narrative beat: *the pylon goes live
and the PC has to decide what to do about it.*

- **Precursor:** `tag interval_traversed`.
- **What happens:**
  - Crew rises. Twenty minutes of focused work — cables
    checked, top piece fitted, housing closed, activation
    sequence read from the laminated card.
  - The click. The cycling-up. The pylon is live.
  - The recruitment signal hits the PC: blueprints,
    schematics, coordinates, purpose. PC resists. A
    save-shaped roll (Cunning, per
    `project_cunning_damage_saves.md`; Bushcraft as alt)
    determines how much the PC catches as the grip firms.
  - The PC surfaces. Crew mid-transition, present in body,
    absent behind the eyes. The activator housing is at the
    base of the pylon. The window will not stay open.
  - PC chooses to act or walk away. Acting is a Cunning check
    against the housing; on failure, Veran gets a hand on the
    PC and a Combat check follows to either crack the housing
    anyway or get caught.
- **Resulting state (disjunction):**
  - `tag pylon_broken` (acted, succeeded — clean or
    after-the-grab)
  - `tag pylon_intact` + `tag pylon_walked_away` (chose walk)
  - `tag pylon_intact` + `+add_condition injured` (acted,
    fully failed)
  - Plus `quality signal_caught` (0 = clean resist, 1–3 =
    fragments caught).
- **Leads to:** the matching outro state.

## Outro states

Short post-decision fragments. End states, not scenes — a few
lines of resolution each, the crew's gap filling in around
whatever the PC did.

- **Walked away.** PC alone on the mesa with the humming
  pylon. Crew gone synchronized down the road. Coordinates
  still in the PC's head.
- **Broken clean.** Crew piecing together the "weirdo
  merchant with heat sickness" narrative. Includes the
  Aftermath Negotiation roll: not to convince them, but to
  put one crack in the plaster wide enough for Chorik to
  notice. On success Chorik goes quiet, looks at his hands,
  Veran calls his name, he goes. PC walks away not knowing if
  anything landed.
- **Broken with Chorik cracked.** Variant of broken-clean
  with `tag chorik_cracked`. Same outward beat. Different
  weight downstream [?].
- **Caught.** PC injured, signal still live in the crew,
  crew preparing to move toward the next site. Worse-than-
  walk-away ending.
- **Early depart.** PC left during the rest interval before
  completion. Pylon half-built behind them. No reward, no
  flag, arc closed via `+flee_dungeon`. GAP: do we allow this
  at all? Default yes.

All terminate via `+finish_dungeon` (or `+flee_dungeon` for
early-depart) and set `tag arc_signal_array_completed`.

## Open structural questions

- **Spoke gating on Completion.** Should Scene 3 require the
  PC to have engaged at least one spoke in Scene 2, or be
  freely available? Default: free, but the writer should
  prefer a soft prompt over a hard gate so the interval
  doesn't feel sticky.
- **Chorik aside placement.** Spoke of Scene 2 or its own
  side-scene? Going side-scene (Scene 2b) for the bibles to
  flag the narrative weight; decompose may collapse it back
  into the hub.
- **Skill choice for the resist roll.** Cunning per
  `project_cunning_damage_saves.md`. Not Combat (no fight).
  Not Negotiation (no interlocutor). Bushcraft as alt.
- **`signal_caught` as a quality.** Cheap to set, and lets
  downstream content (later sites, other scrub arcs) read
  fragment count. GAP: any *current* downstream consumer, or
  speculative?
- **Combat encounter at the failed-act path.** Roll check
  (Combat skill + picker), not a full `.fight` — these are
  workers protecting equipment, not a combat encounter.
- **Aftermath Negotiation as roll vs. choice.** Roll. Target
  tier [?].
- **Cross-arc Chorik fitting.** GAP from `_cast.md`. Default:
  written as a stranger, swap-in-able later when a named
  scrub NPC registry exists.

## Scope check

Three scenes, one side-scene, five outro states. If decompose
fans this into the expected `.enc` topology (hub-and-spoke for
Scene 2, sequential-with-branches for Scene 3), the rough
count is ~10–12 `.enc` files. If shred ever produces a scenes
file with more entries than that, it has decomposed early.
