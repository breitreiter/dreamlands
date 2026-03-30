ENCOUNTER MECHANICS QUICK REFERENCE
====================================

FRONT-MATTER
------------------------------------
Required metadata lines between title and body. Order doesn't matter.

[trigger road|settlement|none]  Where this encounter fires (required)
[tier 1|2|3]                    Which tier this encounter belongs to (default: any)
[vignette <path>]               Override vignette image (path relative to assets/vignettes/, no .png)
[requires <condition>]          Gate the entire encounter (multiple lines AND together)

Conditions in [requires] use the same syntax as @if and choice-level [requires]:
  [requires has <item_id>]
  [requires tag <tag_id>]
  [requires quality <quality_id> <threshold>]
  [requires check <skill> <difficulty>]


SKILLS                          DIFFICULTY        DC
  combat       fighting            trivial         5
  negotiation  persuasion/social   easy           10
  bushcraft    survival/travel     medium         15
  cunning      trickery/awareness  hard           20
  luck         fortune             heroic         30
  mercantile   trade/appraisal

TIME PERIODS
  morning
  midday
  afternoon
  evening
  night
  
CONDITIONS
  freezing  thirsty  irradiated  lattice_sickness
  exhausted  lost  injured  poisoned


ACTION VERBS
------------------------------------

Flow control        @if check <skill> <difficulty> { ... } @else { ... }
                    @if meets <skill> <target> { ... } @else { ... }
                    @if has <item_id> { ... } @elif check <skill> <difficulty> { ... } @else { ... }
                    @if tag <tag_id> { ... } @else { ... }
                    @if quality <quality_id> <threshold> { ... } @else { ... }

Choice gating       * Option text [requires has <item_id>]
                    * Option text [requires tag <tag_id>]
                    * Option text [requires quality <quality_id> <threshold>]

Navigation          +open <encounter_id>

World state         +add_tag <tag_id>
                    +remove_tag <tag_id>
                    +quality <quality_id> <amount>       (signed int, e.g. +quality guild 1, +quality clans -1)

Items               +add_item <item_id>
                    +add_random_items <count> <category>
                    +lose_random_item

Gold                +give_gold <amount>
                    +rem_gold <amount>

Health              +damage_health <amount>
                    +heal <amount>

Spirits             +damage_spirits <amount>
                    +heal_spirits <amount>

Skills              +increase_skill <skill> <amount>
                    +decrease_skill <skill> <amount>
                    +set_skill <skill> <level>

Conditions          +add_condition <condition_id>
                    +remove_condition <condition_id>

Time                +skip_time <period> [no_sleep] [no_meal] [no_biome]

Dungeon             +finish_dungeon
                    +flee_dungeon

Return to pool      +repool

TACTICAL ENCOUNTERS (.tac format)
====================================

Tactical encounters are card-based encounters for combat and traversal. They
live alongside .enc files in text/encounters/{biome}/tier{n}/ and use the
.tac extension. A .tac file is either an **encounter** or a **group** (branch
point), never both.

FILE STRUCTURE
------------------------------------

    Title                           First line, plain text
    [variant combat|traverse]       Required for encounters, not groups
    [intent <tag>]                  Optional intent tag (violence, stealth, etc.)
    [stat <skill>]                  Governing skill (combat, cunning, negotiation, bushcraft)
    [tier 1|2|3]                    Tier restriction
    [requires <condition>]          Gate (same syntax as .enc requires)

    Prose body text describing the scene. Everything between front-matter
    and the first section marker.

    stats:
    timers:
    openings:
    path:                           Traverse only
    approaches:                     Combat only
    failure:

ENCOUNTER SECTIONS
------------------------------------

### stats:

    resistance <N>                  Hit points — progress needed to win

### timers:

    draw <N>                        How many timers are active at once
    * Timer Name [counter Counter Text]: <effect> <amount> every <countdown>

Effect types:
  `spirits <N>`          Drain N spirits when the timer fires
  `resistance <N>`       Restore N resistance when the timer fires
  `condition <id>`       Add a pending condition check (resolved at encounter end)

Counter text is what the UI shows when the player stops this timer. Example:

    timers:
      draw 2
      * Flanking Maneuver [counter Block the flank]: spirits 2 every 4
      * Pack Howl [counter Silence the alpha]: resistance 1 every 5
      * Jagged Terrain [counter Find safer footing]: condition injured every 4

Condition timers don't resolve immediately. Each firing adds one pending
resist check. When the encounter ends (win or lose), all pending checks are
rolled. Multiple firings of the same condition stack — 3 firings = 3 resist
rolls. Any single failure applies the condition. Known condition IDs:
freezing, thirsty, irradiated, lattice_sickness, exhausted, poisoned,
lost, injured.

### openings:

Filler cards drawn into the player's deck. These supplement the player's
collection cards (from skill + equipment).

    * Card Name: <cost> -> <effect>
    * Card Name: <cost> -> <effect> [requires <condition>]

Cost types:      free | tick | momentum <N> | spirits <N>
Effect types:    damage <N> | momentum <N> | stop_timer

Gated openings (with [requires]) are added first, then ungated. Example:

    openings:
      * Wade Carefully: free -> damage 1
      * Brace and Push: momentum 1 -> damage 2
      * Find Footing: free -> momentum 1
      * Trap Line: free -> damage 4 [requires has bear_trap]

With the default UI size, openings max out at around 60 characters.

### path: (traverse only)

Authored sequence of cards the player must work through. Same syntax as
openings but order matters — these define the terrain/route.

    path:
      * Wade Carefully: free -> damage 1
      * Brace and Push: momentum 1 -> damage 2
      * Strong Stroke: spirits 1 -> damage 3

### approaches: (combat only)

Three approach options that set starting conditions:

    * scout: momentum <N>, timers <N>, openings <N>
    * direct: momentum <N>, timers <N>
    * wild: momentum <N>, timers <N>

`momentum` = starting momentum, `timers` = how many timers begin active,
`openings` = bonus filler openings drawn. Example:

    approaches:
      * scout: momentum 0, timers 2, openings 3
      * direct: momentum 3, timers 2
      * wild: momentum 5, timers 3

### failure:

Prose + mechanics applied when the player loses. Uses the same +verb syntax
as .enc mechanics.

    failure:
      The current takes your legs out. You wash up downstream, bruised.
      +damage_spirits 2
      +lose_random_item
      +add_condition exhausted

GROUP FILES
------------------------------------

A group is a branch point that routes to other .tac encounters. It has
`branches:` instead of encounter sections.

    Title
    [tier 2]

    Prose body.

    branches:
      * Label [intent tag] -> path/to/Encounter Name
      * Label [intent tag] -> path/to/Encounter Name [requires <condition>]

Example:

    Bandit Roadblock
    [tier 2]

    The road narrows between two rocky outcrops.

    branches:
      * Fight through [intent violence] -> plains/tier2/Bandit Roadblock Fight
      * Sneak past [intent stealth] -> plains/tier2/Bandit Roadblock Stealth [requires has light_armor]
      * Talk your way out [intent negotiation] -> plains/tier2/Bandit Roadblock Parley

CARD ARCHETYPES (for openings and path)
------------------------------------

When authoring openings/path, use costs and effects that map to the standard
archetypes. See card_archetypes.md for the full list. Common patterns:

    free -> damage 1              free_progress_small
    momentum 1 -> damage 2       momentum_to_progress
    momentum 2 -> damage 3       momentum_to_progress_large
    momentum 3 -> damage 5       momentum_to_progress_huge
    spirits 1 -> damage 3        spirits_to_progress
    free -> momentum 1            free_momentum_small
    free -> momentum 2            free_momentum
    momentum 2 -> stop_timer      momentum_to_cancel
    spirits 1 -> stop_timer       spirits_to_cancel

Note: `_to_cancel` openings don't need names. At draw time, the engine
pairs each stop_timer card to the most urgent active timer and renames
it to that timer's [counter] text automatically.
    tick -> damage 2              threat_to_progress


# Factions
## Continental
- faction.empire
- faction.tradeguild
## Plains
- faction.legion
- faction.scavengers
## Scrub
-  faction.clans
-  faction.kesharat
## Forest
-  faction.exiles
## Mountain
-  faction.miners
-  faction.company
-  faction.scholars
-  faction.renegadescholars
## Swamp
-  faction.revathi
-  faction.revivalists
-  faction.collectors

# Arcs
## Scrub
- arc.tomak
## Plains
- arc.torben 
## Mountain
- arc.regula
## Forest
- arc.briarcommons

role:
seed
hint
complication
choice
reveal
resolution
aftermath
ambient