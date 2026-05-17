---
kind: rule
title: Encounter Mechanics & Game Commands
created: 2026-02-21
updated: 2026-05-17
status: current
touches:
  files:
    - lib/Rules/ActionVocabulary.cs
    - lib/Rules/ConditionDef.cs
    - lib/Rules/ItemDef.cs
    - lib/Rules/Skill.cs
    - lib/Rules/Difficulty.cs
    - lib/Encounter/CmbParser.cs
    - lib/Encounter/Move.cs
    - lib/Encounter/CombatEncounter.cs
  features: [encounter-mechanics, vocabulary, items, factions, combat-encounters]
enforces:
  - text/encounters/**/*.enc
  - text/encounters/**/*.fight
provenance:
  author: migration:M-001
---

# Encounter mechanics quick reference

This is the locked vocabulary the parser and runtime validate against.
Adding an `item_id` not in `ItemDef.cs` will fail validation; adding a verb
not in `ActionVocabulary.cs` will fail validation. Keep this in sync with
those C# definitions.

## Naming & identity

Each .enc file has two distinct names. Don't confuse them.

Filename (without .ext)         Identity / ID — used for all lookups
  "Road Toll.enc"               → ShortId "Road Toll", fully qualified "plains/tier1/Road Toll"

First line of the file          Display title — shown to the player in the UI
  "A Toll on the King's Road"   → Title text, purely cosmetic

Where each one is used:
  +open <target>                Matches against filename (ShortId), not the display title
  Storylet list / selection     Keyed by filename (ShortId), not the display title
  Encounter screen header       Shows the display title (first line of the file)

+open resolution order:
  1. Short-name match within the current category (arc/directory) — case-insensitive
  2. Fall back to fully qualified id (e.g. "arcs/plains/grainway_station/Captain Aldric")
  Targets only need to be unique within the arc. Two arcs can both have "Start".
  Note: `.fight` combat encounters are not opened via `+open` — they are launched
  by the combat mechanic separately.


## Front-matter

Required metadata lines between title and body. Order doesn't matter.

[trigger road|settlement|none]  Where this encounter fires (required)
[tier 1|2|3]                    Which tier this encounter belongs to (default: any)
[vignette <path>]               Override vignette image (path relative to assets/vignettes/, no .png)
[requires <condition>]          Gate the entire encounter (multiple lines AND together)

Conditions in [requires] use the same syntax as @if and choice-level [requires]:
  [requires has <item_id>]
  [requires tag <tag_id>]
  [requires quality <quality_id> <threshold>]
  [requires meets <skill> <tier>]          (tier: untrained|trained|expert)
  Note: check is NOT valid in [requires] — use meets for gate-style skill checks.

Compound conditions with &&, ||, and ! prefix negation:
  [requires tag met_envoy && quality guild 2]
  [requires !tag patrol_alerted]
  [requires tag guild_member || tag kesharat_contact]
  [requires !has torch && tag cave_explored]

Operator precedence: ! (tightest) > && > ||
Restriction: check and meets cannot be negated or used in compound expressions.


## Skills, tiers, time, conditions

SKILLS                          TIERS (for meets/set_skill_tier)
  combat       fighting            untrained  (tier 0)
  negotiation  persuasion/social   trained    (tier 1)
  bushcraft    survival/travel     expert     (tier 2)
  cunning      trickery/awareness

  Gear unlocks by Combat tier:
    untrained  no weapons or armor equipped
    trained    T1 weapons + light/medium armor
    expert     T2 weapons + all armor tiers

APPROACH VERBS PER SKILL (used with check correct:/wrong:)
  negotiation:  charm    reason   threaten
  cunning:      hide     bluff    scheme
  bushcraft:    push     plan     reroute
  combat:       rush     strategize  outlast

LEGACY DIFFICULTY (parseable, emits deprecation warning — not subject to terminal-check rule)
  trivial   easy   medium   hard   very_hard   epic

TIME PERIODS
  morning
  midday
  afternoon
  evening
  night

CONDITIONS
  freezing  thirsty  irradiated  lattice_sickness
  exhausted  lost  injured  poisoned


## Action verbs

Flow control        @if check <skill> correct:<approach> wrong:<approach> { ... } @else { ... }
                      (picker check — terminal branch only, @else required)
                    @if check <skill> <difficulty> { ... } @else { ... }
                      (DEPRECATED legacy DC form — emits deprecation warning; not subject to terminal rule)
                    @if meets <skill> <tier> { ... } @else { ... }
                      (tier: untrained|trained|expert)
                    @if has <item_id> { ... } @elif check <skill> correct:X wrong:Y { ... } @else { ... }
                      (static conditions can precede a terminal picker check)
                    @if tag <tag_id> { ... } @else { ... }
                    @if quality <quality_id> <threshold> { ... } @else { ... }
                    @if tag a && quality guild 2 { ... }
                    @if !tag patrol_alerted || tag bribed_guard { ... }

Choice gating       * Option text [requires has <item_id>]
                    * Option text [requires tag <tag_id>]
                    * Option text [requires quality <quality_id> <threshold>]
                    * Option text [requires meets <skill> <tier>]
                    * Option text [requires tag a && !tag b]
                    * Option text [requires tag a || tag b]

Navigation          +open <encounter_id>

World state         +add_tag <tag_id>
                    +remove_tag <tag_id>
                    +quality <quality_id> <amount>       (signed int, e.g. +quality guild 1, +quality clans -1)

Items               +add_item <item_id>
                    +add_random_items <count> <category>
                    +lose_random_item
                    +discard <item_id>                  (remove a specific item from inventory)

Equipment           +equip <item_id>                    (equip from Pack; sets IsEquipped flag)
                    +unequip <slot>                     (slot: weapon|armor)

Pack                +upgrade_pack <amount>              (permanently increase pack capacity)

Gold                +give_gold <amount>
                    +rem_gold <amount>

Spirits             +damage_spirits <amount>
                    +heal_spirits <amount>

Skills              +increase_skill <skill> <amount>
                    +decrease_skill <skill> <amount>
                    +set_skill_tier <skill> <tier>       (tier: untrained|trained|expert)
                    +add_level                            (grant one pending tableau level-up pick)

Conditions          +add_condition <condition_id>
                    +remove_condition <condition_id>

Time                +skip_time <period> [no_sleep] [no_meal] [no_biome]
                    +advance_time <N> [no_sleep] [no_meal] [no_biome]

Dungeon             +finish_dungeon
                    +flee_dungeon

Return to pool      +repool

Identity            +set_name <name>                     (set player display name; intro only)


## Inventory model

All items live in a single Pack. There is no haversack or separate boots slot.
Equipment (weapon, armor) is tracked via an `IsEquipped` flag on the pack item —
no item leaves the pack when equipped. The only slots are `weapon` and `armor`
for `+unequip`; boots were removed as a gear slot (scarecrow_boots is a Tool).

PassiveImmunities: some Tools (e.g. `scarecrow_boots`, `lattice_ward`) passively
prevent a condition for as long as the item is in the pack. They are never consumed.

Medical kit: `medical_kit` cures any condition without being consumed.

Food cadence: one food item consumed per day. Cadence intervals vary by Bushcraft tier.


## Arc rewards and leveling

+add_level          Grants one pending tableau level-up pick. Arcs award this at
                    completion and at key mid-arc beats. The player picks from a
                    tableau on their next visit to a settlement or chapterhouse.
                    There is no XP bar; advancement is entirely through arc beats.

+set_skill_tier     Used by arc intro encounters to set starting skill tiers based
                    on character background (e.g. a soldier background sets
                    combat to trained). Authors: prefer set_skill_tier over
                    increase_skill for absolute initialization.


## Combat encounters (.fight format)

Combat encounters use the `.fight` extension and drive the RPS combat screen.
They are parsed by `CmbParser` (`lib/Encounter/CmbParser.cs`).

### File structure

    +title Monster Name
    +image monsters/biome_type.webp
    +blood #7a0a0a                    (optional — default mammalian red)
    +stats hp=18
    +repool false                     (optional — default false)

    +move Attack
      narration: It lunges at you, claws raking forward.
      narration: It darts in low and swipes at your legs.

    +move Heavy Slow Attack
      narration: It winds back and throws its full weight into the blow.

    +intro
    Prose shown before combat begins.

    +win
    Prose shown on player victory.
    > gold 12
    > tag killed_name

    +lose
    Prose shown on player defeat.

### Directives

All directives start with `+` at column 0. `#` is a comment; blank lines are ignored.

    +title <text>       Display title
    +image <path>       Image path (relative to assets/)
    +blood <hex>        Blood-splat color; override for non-mammals (golems, lattice, etc.)
    +stats hp=<n>       Monster HP — required, must be > 0
    +repool <bool>      Return monster to pool after defeat (true/yes/1 or false/no/0)
    +move <encoding>    One move in the pool (followed by narration: lines)
    +intro              Block: opening prose
    +win                Block: prose + mechanics on player victory
    +lose               Block: prose on player defeat

### Move encoding

Last token is the base family; preceding tokens are mutators. Tokens are
case-insensitive. Each `+move` block must have at least one `narration:` line.
Multiple narration lines give texture — the runner picks one variant per use.

Base families:

    attack     Deals damage. Cancelled by Defend.
    defend     Reduces incoming damage. Cancelled by Attack.
    recover    Heals the monster. Cancelled by Attack.
    read       Reveals the monster's next-turn commitment to the player.
    skipped    Produced by stun only — never authored.

Attack mutators:

    heavy         +2 damage
    weak          -1 damage
    riposte       Deals damage even when the player defends
    brutal        Chance to inflict Injured on hit
    tainted       Chance to inflict Poisoned on hit
    glowing       Chance to inflict Irradiated on hit
    venomous      Chance to inflict Lattice_sickness on hit
    terrifying    Chance to inflict Fear on hit (restricts player to Defend/Recover next turn)
    provoking     Chance to inflict Berzerk on hit (restricts player to Attack next turn)
    stunning      Chance to stun the player's next slot
    power         Once per turn only
    slow          Once every two turns only
    exhausting    Self-stuns the monster's next slot after use
    telegraphed   Appears in the Tell ("preparing a heavy attack!")

Defend mutators:

    heavy         Blocks +2 additional damage
    perfect       Blocks all damage from an Attack
    shielding     Prevents status riders from landing while this slot is active
    stunning      Chance to stun the attacker's next slot
    power         Once per turn only
    slow          Once every two turns only

Recover mutators:

    heavy         Heals +2 HP
    wary          Converts to Defend if the opponent committed Attack
    shielded      Prevents conditions landing during the recover
    power         Once per turn only
    slow          Once every two turns only
    enraging      Inflicts Berzerk on self

Read mutators:

    wary          Converts to Defend if the opponent committed Attack

Canonical encoded form sorts mutators alphabetically then appends the base
capitalized (e.g. `"Heavy Slow Telegraphed Attack"`). Authoring order does
not matter — the parser normalizes on load.

### Win/lose mechanics

In `+win` and `+lose` blocks, `>` lines are mechanics run through the standard
`Mechanics.Apply` pipeline after combat resolves. They use the same verb
vocabulary as `.enc` action verbs with `>` instead of `+`:

    > gold <n>                Award gold
    > tag <tag_id>            Set a world-state tag
    > add_item <item_id>      Give item
    > damage_spirits <n>      Damage spirits
    (full verb list in the Action verbs section above)

The leading `+` is optional: `> gold 8` and `> +gold 8` both parse.


## Factions

### Continental
- faction.empire
- faction.tradeguild

### Plains
- faction.legion
- faction.scavengers

### Scrub
-  faction.clans
-  faction.kesharat

### Forest
-  faction.exiles

### Mountain
-  faction.miners
-  faction.company
-  faction.scholars
-  faction.renegadescholars

### Swamp
-  faction.revathi
-  faction.revivalists
-  faction.collectors


## Arcs

### Scrub
- arc.tomak

### Plains
- arc.torben

### Mountain
- arc.regula

### Forest
- arc.briarcommons


## Item definitions

Valid item_id values for +add_item, +lose_random_item, @if has, and [requires has].

### Weapons

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

### Armor

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

### Tools

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
  scarecrow_boots     Scarecrow Boots     PassiveImmunity:exhausted  (Tool, not Boots type)
  lattice_ward        Lattice Ward        Lattice_sickness +5
  sakharov_mask       Sakharov's Mask     Irradiated +5
  antivenom_kit       Antivenom Kit       Poison +5
  control_shaft       Control Shaft       (quest item)

### Food

  food_protein        Meat & Fish         (consumable, 3g)
  food_grain          Breadstuffs         (consumable, 3g)
  food_sweets         Sweets              (consumable, 3g)

### Medicines

  bandages            Bandages            Cures injured           3g
  siphon_glass        Siphon Glass        Cures lattice_sickness  scrub T2  40g
  pale_knot_berry     Pale Knot Berry     Cures exhausted         plains T2  15g
  shustov_tonic       Shustov Tonic       Cures irradiated        plains T2  40g
  mudcap_fungus       Mudcap Fungus       Cures poisoned          swamp T2  15g

Capstone arc keys (no stats, unlock arc progression):
  hunters_journal     Hunter's Journal
  grid_cipher         Grid Cipher
  color_lens          Color Lens
  revathi_tile        Revathi Tile

### Haul

  haul                Haul                (generic — identity comes from HaulDef on the instance)
