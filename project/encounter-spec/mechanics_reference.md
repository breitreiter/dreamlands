ENCOUNTER MECHANICS QUICK REFERENCE
====================================

NAMING & IDENTITY
------------------------------------
Each .enc/.tac file has two distinct names. Don't confuse them.

Filename (without .ext)         Identity / ID — used for all lookups
  "Road Toll.enc"               → ShortId "Road Toll", fully qualified "plains/tier1/Road Toll"

First line of the file          Display title — shown to the player in the UI
  "A Toll on the King's Road"   → Title text, purely cosmetic

Where each one is used:
  +open <target>                Matches against filename (ShortId), not the display title
  Storylet list / selection     Keyed by filename (ShortId), not the display title
  Encounter screen header       Shows the display title (first line of the file)

+open resolution order (same for both .enc and .tac bundles):
  1. Short-name match within the current category (arc/directory) — case-insensitive
  2. Fall back to fully qualified id (e.g. "arcs/plains/grainway_station/Captain Aldric")
  Targets only need to be unique within the arc. Two arcs can both have "Start".
  An .enc can +open a .tac target and vice versa — resolution checks both bundles.


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
                    +advance_time <N> [no_sleep] [no_meal] [no_biome]

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
    [stat <skill>]                  Governing skill (combat, cunning, negotiation, bushcraft)
    [tier 1|2|3]                    Tier restriction
    [requires <condition>]          Gate (same syntax as .enc requires)

    Prose body text describing the scene. Everything between front-matter
    and the first section marker.

    timers:
    openings:
    approaches:                     Optional — if omitted, defaults to aggressive
    success:                        Optional — prose + mechanics on victory
    failure:

ENCOUNTER SECTIONS
------------------------------------

### timers:

Two kinds of timers based on whether `resist` is present:

  **Sequential** (resist > 0): one active at a time, player depletes
  resistance to advance. These form the encounter's progression.

  **Ambient** (resist omitted or 0): tick every turn, can't be directly
  damaged, auto-cleared when all sequential timers are done.

Syntax:

    * Name [counter Text]: <effect> <amount> every <countdown> [resist <N>]

Effect types:
  `spirits <N>`              Drain N spirits when the timer fires, resets
  `resistance <N>`           Add N resistance to current timer, resets
  `condition <id>`           Add a pending condition check, resets
  `tick "<target>" <N>`      Decrement target timer's countdown by N, resets
  `fatal`                    Encounter fails when countdown reaches 0 (no reset)

Counter text is what the UI shows when the player stops this timer. Example:

    timers:
      * Flanking Maneuver [counter Block the flank]: spirits 2 every 4 resist 5
      * Pack Howl [counter Silence the alpha]: resistance 1 every 5 resist 6
      * Jagged Terrain [counter Find safer footing]: condition injured every 4 resist 4

Traverse example (ambient fatal master + sequential tick-timer waypoints):

    timers:
      * They're gaining on you: fatal every 20
      * Reach the creek [counter Slide down]: tick "They're gaining on you" 3 every 4 resist 6
      * Climb the ledge [counter Find handholds]: tick "They're gaining on you" 2 every 3 resist 5

Win condition: all sequential timers cleared → ambient auto-cleared.
Failure: spirits = 0 (SpiritsLoss) or fatal timer fires (TimerExpired).

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

### approaches: (optional)

If present, the player chooses an approach before the encounter starts.
If omitted, the encounter defaults to aggressive.

    * aggressive                    +2 momentum/turn, draw 1 card
    * cautious                      +1 momentum/turn, draw 2 cards

Example:

    approaches:
      * aggressive
      * cautious

### success: (optional)

Prose + mechanics applied when the player wins. Uses the same +verb syntax
as .enc mechanics.

    success:
      You push through. The bandits scatter.
      +add_gold 15

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

CARD ARCHETYPES (for openings)
------------------------------------

When authoring openings, use costs and effects that map to the standard
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


ITEM DEFINITIONS
====================================

Valid item_id values for +add_item, +lose_random_item, @if has, and [requires has].

WEAPONS
------------------------------------

Daggers (cancel-focused):
  bodkin              Bodkin              Combat +1   plains T1  15g
  jambiya             Jambiya             Combat +2   scrub T1   15g
  kukri               Kukri               Combat +3   scrub T2   40g
  hunting_knife       Hunting Knife       Combat +4   mountains T2  80g
  the_old_tooth       The Old Tooth       Combat +5   (arc/reward only)

Axes (aggro-focused, zero cancels):
  hatchet             Hatchet             Combat +1   forest T1  15g
  tomahawk            Tomahawk            Combat +2   forest T1  15g
  war_axe             War Axe             Combat +3   forest T2  40g
  broadaxe            Broadaxe            Combat +4   mountains T2  80g
  revathi_labrys      Revathi Labrys      Combat +5   (arc/reward only)

Swords (hybrid):
  falchion            Falchion            Combat +1   plains T1  15g
  short_sword         Short Sword         Combat +2   plains T1  15g
  tulwar              Tulwar              Combat +3   scrub T2   40g
  scimitar            Scimitar            Combat +4   scrub T2   80g
  shimmering_blade    Shimmering Blade    Combat +5   (arc/reward only)

ARMOR
------------------------------------

Light (Cunning scaling, minor Freezing resist):
  tunic               Tunic                                        plains T1  (free)
  silks               Silks               Cunning +1               scrub T1   15g
  hunters_gear        Hunter's Gear       Cunning +2  Freezing +1  swamp T1   15g
  cartographers_cloak Cartographer's Cloak Cunning +3 Freezing +2  mountains T2  40g
  desert_scout_gear   Desert Scout Gear   Cunning +4  Freezing +2  scrub T2   80g
  robe_of_twilight    Robe of Twilight    Cunning +5  Freezing +3  (arc/reward only)

Medium (balanced Cunning + Injury + Freezing resist):
  leather             Leather             Cunning +1  Injured +1  Freezing +1  forest T1  15g
  hide_armor          Hide Armor          Cunning +1  Injured +1  Freezing +2  mountains T1  15g
  buff_coat           Buff Coat           Cunning +1  Injured +2  Freezing +3  forest T2  40g
  lamellar            Lamellar            Cunning +2  Injured +2  Freezing +3  mountains T2  80g
  mountain_regiment_armor  17th Mountain Regiment Armor  Cunning +2  Injured +3  Freezing +5  (arc/reward only)

Heavy (Injury resist scaling, no Cunning):
  gambeson            Gambeson            Injured +1  Freezing +1  mountains T1  15g
  chainmail           Chainmail           Injured +2               plains T1  15g
  scale_armor         Scale Armor         Injured +3               scrub T2   40g
  brigandine          Brigandine          Injured +4  Freezing +1  plains T2  80g
  golem_armor         Golem Armor         Injured +5  Freezing +2  (arc/reward only)

BOOTS
------------------------------------

  fine_boots          Fine Boots          Exhausted +1   plains T1  15g
  heavy_work_boots    Heavy Work Boots    Exhausted +2   mountains T1  15g
  riding_boots        Riding Boots        Exhausted +3   scrub T2   40g
  trail_boots         Trail Boots         Exhausted +4   forest T2  80g
  windstriders        Windstriders        Exhausted +5   (arc/reward only)

TOOLS
------------------------------------

Shopable:
  canteen             Canteen             Thirsty +2               forest T1  15g
  waterskin           Waterskin           Thirsty +3               scrub T2   40g
  letters_of_introduction  Letters of Introduction  Negotiation +2  scrub T1  40g
  peoples_borderlands A Guide to the Borderlands  Negotiation +3   mountains T2  80g
  cartographers_diary Cartographer's Diary  Bushcraft +2           mountain T1  40g
  ornate_spyglass     Ornate Spyglass     Bushcraft +3             scrub T2   80g
  cartographers_kit   Cartographer's Kit  Lost +5                  plains T1  80g
  sleeping_kit        Sleeping Kit        Exhausted +4             forest T2  80g
  brass_lantern       Old Brass Lantern   (light source)           plains T1  15g

Arc/dungeon-only:
  lattice_ward        Lattice Ward        Lattice_sickness +5
  sakharov_mask       Sakharov's Mask     Irradiated +5
  antivenom_kit       Antivenom Kit       Poison +5
  control_shaft       Control Shaft       (quest item)

FOOD
------------------------------------

  food_protein        Meat & Fish         (consumable, 3g)
  food_grain          Breadstuffs         (consumable, 3g)
  food_sweets         Sweets              (consumable, 3g)

MEDICINES
------------------------------------

  bandages            Bandages            Cures injured           3g
  siphon_glass        Siphon Glass        Cures lattice_sickness  scrub T2  40g
  pale_knot_berry     Pale Knot Berry     Cures exhausted         plains T2  15g
  shustov_tonic       Shustov Tonic       Cures irradiated        plains T2  40g
  mudcap_fungus       Mudcap Fungus       Cures poisoned          swamp T2  15g

TOKENS
------------------------------------

Skill tokens (+1 to a skill, found in encounters):
  ivory_comb          Ivory Comb          Negotiation +1
  lucky_buckle        Lucky Buckle        Combat +1
  knotwork_seed       Knotwork Seed       Bushcraft +1
  tarnished_key       Tarnished Key       Cunning +1

Capstone arc keys (no stats, unlock arc progression):
  hunters_journal     Hunter's Journal
  grid_cipher         Grid Cipher
  color_lens          Color Lens
  revathi_tile        Revathi Tile

HAUL
------------------------------------

  haul                Haul                (generic — identity comes from HaulDef on the instance)

