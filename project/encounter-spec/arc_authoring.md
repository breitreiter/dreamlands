ARC AUTHORING GUIDE
====================================

This guide covers structural techniques for writing multi-scene arc
encounters. It assumes you already know the .enc format (see format.md)
and the mechanic verbs (see mechanics_reference.md). This document is
about *how to arrange scenes*, not how to write prose.


SCENE TYPES
------------------------------------

Every scene in an arc serves one of three roles. Name your files by
what they are, not what happens in them.

**Hub** — A scene the player returns to repeatedly. Short body text,
many choices. The Plaza, Wrenbury, Captain Aldric.

**Wing** — A scene reachable from a hub. Longer body text, exploratory
choices that link back to itself or the hub. The Commons, The Exchange,
The Watchtower.

**Terminal** — A scene that ends the arc. Contains `+finish_dungeon`
or `+flee_dungeon`. The Letter, The Ghosts, Confrontation.

Most arcs are: Entry → Hub → Wings → Terminal. Some have two hub
layers (Grainway Station: Start → Captain Aldric hub → Assault/
Arrangement sub-hubs). Keep it to two layers max.


HUB SCENES
------------------------------------

A hub scene has a *short* body — one or two sentences of ambient
description. The body will be shown every time the player returns,
so it must read well on repetition. Don't put events in hub bodies.

Put the interesting description in the *choices that lead to wings*.
The choice text is a brief label; the choice body is where you paint
the scene. This way the description is read once (on first visit)
rather than on every return.

Example from The Plaza:

    The plaza is quiet and empty, save for Torben's sketching
    and occasional quiet mumbles.

    choices:

    * The commons = A wide passage with faded decoration on the walls
      The corridor turns twice and the dome-light thins to nothing.
      Torben strikes his lantern without comment...
      +open "The Commons"

The hub body is two lines. The corridor description lives in the
choice that navigates there. When the player returns to the hub,
they see the short body again — not the corridor description.


SELF-LINKING CHOICES (DIALOG AND MINOR EVENTS)
------------------------------------

For bits of dialog, observations, or minor events that don't warrant
their own scene file, make a choice that links back to its parent:

    * Ask Torben what he sees = His notebook is already open
      "Mess hall. Long tables, central kitchen, efficient..."
      +open "The Commons"

    * Explore the residential block = A doorway at the back
      The corridor is narrow enough that your shoulders nearly
      brush the walls...
      +open "The Commons"

    * Return to the plaza = Head back the way you came
      +open "The Plaza"

The first two choices contain dialog/exploration and return to the
same scene. The last choice navigates to a different scene. This
pattern lets a wing hold several conversations or observations
without creating a file for each one.

Rules for self-linking choices:
- The choice body should be self-contained. Don't assume the player
  read the other choices on this scene.
- End every self-linking choice with `+open "This Scene"`.
- Keep the last choice as the navigation exit (back to hub, or
  forward to the next scene).


TAG-BASED CHRONOLOGY WITHIN A SCENE
------------------------------------

When choices within a scene should become available only after the
player has done something else in that same scene, use tags to create
ordering without extra files.

Example from The Exchange:

    * Ask Torben what he sees = He's examining the desk partitions
      "Quartermaster's office. Classic layout..."
      +add_tag the_city_exchange_can_challenge
      +open "The Exchange"

    * Challenge his read = This isn't a quartermaster's office
      [requires tag the_city_exchange_can_challenge]
      "Torben." You keep your voice even. "I'm Guild..."
      +add_tag the_city_exchange_challenged
      +open "The Exchange"

The "Challenge" choice only appears after the player has heard
Torben's analysis. The second tag (`exchange_challenged`) is used
later to unlock a dialog option in The Letter — carrying consequence
across scenes without needing intermediate files.

This is the primary tool for creating multi-step sequences within a
single scene. You can chain several: step A adds tag X, step B
requires X and adds Y, step C requires Y.


QUALITY GATES FOR ONE-TIME CONTENT
------------------------------------

When a hub links to content that should only be experienced once,
use a quality as a visit counter:

    * Find the guide = A woman near the eastern wall is organizing
      a walking tour [requires quality wrenbury.tour_visits 0]
      ...
      +quality wrenbury.tour_visits 1
      +open "Wrenbury"

The `quality 0` condition is true when the quality hasn't been set
(defaults to 0). After the visit, incrementing it to 1 permanently
hides the choice.

Use this instead of tags when:
- The content is linked from a revisitable hub
- You want the choice to *disappear* after one use
- Tags are better for *unlocking* things; qualities are better
  for *locking out* things

For scenes where you just want to track "has visited" without
hiding choices, a tag is simpler.


GATING ARC PROGRESSION
------------------------------------

Use tags or qualities to control when deeper content becomes
available from a hub. Common pattern: a wing scene sets a tag,
the hub uses `[requires tag ...]` on the choice to the next layer.

Example from The Plaza / The Watchtower:

    # In The Watchtower:
    * That building in the center = A squat structure at the hub
      ...path description...
      +add_tag city.hub_explored
      +open "The Watchtower"

    # In The Plaza:
    * The archive = A reinforced corridor, wider than the others
      [requires tag city.hub_explored]
      ...
      +open "The Archive"

The player must visit the watchtower and examine the central
building before the archive choice appears on the hub. This
creates a natural exploration sequence without forcing linearity.

Naming convention: prefix arc-specific tags with the arc name
(`city.`, `metal_beast.`, `grainway_`). This prevents collisions
between arcs.


ITEM-GATED ENTRY
------------------------------------

Some arcs require an item obtained elsewhere in the world before
the player can enter. Gate this at the entry scene, not at the
arc POI.

Example from The City:

    * Go with him = Enter the city with Torben
      [requires has grid_cipher]
      ...
      +open "The Gauntlet"

    * Depart = This place is beyond you
      ...
      +flee_dungeon

Both choices are always visible. The player can visit the arc POI,
learn what they need, leave, find the item, and return. The entry
scene doubles as a teaser/hook.


BRANCHING WITHIN CHOICES
------------------------------------

Use `@if check` for skill-gated outcomes within a single choice.
Both branches should be narratively complete — the player doesn't
know which they'll get.

    * Overpower it = Kill it before it can attack
      @if check combat hard {
        ...success narrative...
        +add_item golem_armor
        +open "The Letter"
      } @else {
        ...failure narrative...
        +open "Defeated by the Envoy"
      }

Guidelines:
- Success and failure should both advance the story, not dead-end
- Failure can lead to an alternate path or a recovery scene
- Don't gate critical arc progression behind a single hard check
  with no alternative — always provide another route


MULTI-PATH CONVERGENCE
------------------------------------

Multiple narrative paths should converge on a shared terminal scene
when the final choice is the same regardless of how you arrived.

The City example: whether you approach the Envoy peacefully, fight
it successfully, or get defeated by it, all paths lead to The Letter.
The arrival method differs but the final decision (what to do with
the letter) is shared.

This works because:
- The terminal scene reads well regardless of prior context
- The choices in the terminal scene use tags from earlier to
  unlock path-specific options (e.g. `the_city_exchange_challenged`
  unlocks the persuasion option in The Letter)

When paths diverge in ways that make shared terminals awkward,
use separate terminal scenes (e.g. Grainway Station has
"The Legions Reward" and "The Scavengers Reward").


RECOVERY SCENES
------------------------------------

When a combat or skill check fails catastrophically, route to a
recovery scene rather than ending the arc:

    Defeated by the Envoy.enc:

    "Easy now, you took a hard hit."
    Torben leans over you...

    choices:
      * Take the letter
      +advance_time 4 no_meal
      +add_condition injured
      +add_item golem_armor
      +open "The Letter"

The player pays a cost (time, condition, spirits) but continues the
arc. This prevents frustrating dead ends while still making failure
meaningful.

Recovery scenes should:
- Be short — a few lines of narration, then move on
- Apply mechanical penalties (conditions, time, spirits loss)
- Rejoin the main path as soon as possible
- Not repeat the failed challenge


FACTION AND QUALITY CONSEQUENCES
------------------------------------

Arc terminal scenes are where faction quality changes land. Place
them on the final choices, not scattered through exploration.

    * Accept the legion's terms
      +quality faction.legion 4
      +quality faction.scavengers 1
      +finish_dungeon

    * Side with the scavengers
      +quality faction.scavengers 4
      +quality faction.legion -2
      +finish_dungeon

Don't spread faction changes across multiple scenes in the same
arc — it makes the net effect hard to reason about. If exploration
choices affect factions, keep the magnitudes small (+1/-1) and put
the big swings on the terminal choices.


TACTICAL ENCOUNTERS IN ARCS
------------------------------------

A .tac file can serve as the entry point or a transition within
an arc. The .tac's success/failure sections use `+open` to chain
into .enc scenes:

    success:
      ...prose...
      +open "The Plaza"

    failure:
      ...prose...
      +add_condition irradiated
      +open "The Plaza"

Both outcomes should advance the arc. The tactical encounter is an
obstacle, not a gate — use the failure section to apply penalties
while still moving the story forward.


STRUCTURAL CHECKLIST
------------------------------------

Before finalizing an arc:

- [ ] Every wing scene has a choice that returns to its hub
- [ ] Hub body text is short and reads well on repetition
- [ ] Descriptive prose lives in choice bodies, not hub bodies
- [ ] Arc-specific tags are prefixed with the arc name
- [ ] No critical progression is locked behind a single hard check
      with no alternative path
- [ ] Every `+open` target exists as a file in the arc directory
- [ ] Terminal scenes all have `+finish_dungeon` or `+flee_dungeon`
- [ ] The entry scene works both as a first visit and a return visit
- [ ] Self-linking choices are self-contained (don't depend on the
      player having read sibling choices)
- [ ] Quality gates use the `quality name 0` / `+quality name 1`
      pattern for one-time content on revisitable hubs
