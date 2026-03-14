ENCOUNTER MECHANICS QUICK REFERENCE
====================================

FRONT-MATTER
------------------------------------
Optional metadata lines between title and body. Order doesn't matter.

[trigger road|settlement]       Where this encounter fires (default: road)
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

MAGNITUDE                       TIME PERIODS
  trivial                         morning
  small                           midday
  medium                          afternoon
  large                           evening
  huge                            night
  
CONDITIONS
  freezing  thirsty  irradiated  lattice_sickness
  exhausted  lost  injured  poisoned  disheartened


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

Gold                +give_gold <magnitude>
                    +rem_gold <magnitude>

Health              +damage_health <magnitude>
                    +heal <magnitude>

Spirits             +damage_spirits <magnitude>
                    +heal_spirits <magnitude>

Skills              +set_skill <skill> <level>
                    +increase_skill <skill> <amount>
                    +decrease_skill <skill> <amount>

Conditions          +add_condition <condition_id>
                    +remove_condition <condition_id>

Time                +skip_time <period> [no_sleep] [no_meal] [no_biome]

Dungeon             +finish_dungeon
                    +flee_dungeon

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