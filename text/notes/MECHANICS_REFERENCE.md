ENCOUNTER MECHANICS QUICK REFERENCE
====================================

SKILLS                          DIFFICULTY        DC
  combat       fighting            trivial         5
  negotiation  persuasion/social   easy           10
  bushcraft    survival/travel     medium         15
  stealth      stealth/evasion     hard           20
  perception   awareness           very_hard      25
  luck         fortune             heroic         30
  mercantile   trade/appraisal

MAGNITUDE                       TIME PERIODS
  trivial                         morning
  small                           afternoon
  medium                          evening
  large                           night
  huge

CONDITIONS
  diseased  injured  thirsty  cold  infested
  exhausted  haunted  hungry  lost


ACTION VERBS
------------------------------------

Flow control        @if check <skill> <difficulty> { ... } @else { ... }
                    @if has <item_id> { ... } @elif check <skill> <difficulty> { ... } @else { ... }
                    @if tag <tag_id> { ... } @else { ... }

Choice gating       * Option text [requires has <item_id>]
                    * Option text [requires tag <tag_id>]

Navigation          +open <encounter_id>

World state         +add_tag <tag_id>
                    +remove_tag <tag_id>

Items               +add_item <item_id>
                    +add_random_items <count> <category>
                    +lose_random_item
                    +get_random_treasure

Gold                +give_gold <amount>
                    +rem_gold <amount>

Health              +damage_health <magnitude>
                    +heal <magnitude>

Spirits             +damage_spirits <magnitude>
                    +heal_spirits <magnitude>

Skills              +increase_skill <skill> <magnitude>
                    +decrease_skill <skill> <magnitude>

Conditions          +add_condition <condition_id>

Time                +skip_time <period> [no_sleep] [no_meal] [no_biome]

Dungeon             +finish_dungeon
                    +flee_dungeon
