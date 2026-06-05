---
kind: rule
title: Encounter Mechanics & Game Commands
created: 2026-02-21
updated: 2026-06-05
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

+open bypasses UsedEncounterIds — the used-encounter pool only gates the random
pickers (road cadence, settlement stocking). Direct navigation always resolves,
so +open works for road and settlement encounters, not just arcs. Self-reference
(+open <same encounter>) is valid and loops back cleanly — useful for hub
encounters that offer multiple choices across visits (see Meilin.enc in the
hermitage arc as the canonical example).

## Pooling & recurrence

The two encounter formats have opposite recurrence defaults. These are locked:

`.enc` — **one-and-done.** Firing through any random picker adds the id to
UsedEncounterIds permanently. The only escape is an explicit `+repool` mechanic,
and it is extremely rare by design: a repooled encounter's text must read
correctly when the player has already lived this exact moment before, which
makes the writing much harder. Don't reach for it casually.

`.fight` — **recurs until you win.** There is no explicit repool for fights.
Lose and flee leave the fight in the pool automatically — the threat wasn't
removed, so it keeps coming (the T1 plains bandit keeps harassing you until you
beat him). Winning is what retires the fight, recorded as `fight:<id>` in
UsedEncounterIds. A fight that must be one-shot regardless of outcome sets a
tag in its outros and gates itself with `[requires tag ...]`.

One declared exception: a `[persistent]` fight is never retired, even by
winning — wins drive the monster off but don't remove the threat. Persistent
fights MUST carry a `[requires]` gate so something else can end them (check
fails otherwise). Canonical example: The Beast roams forest T3 roads, gated on
`!tag the_lodge.beast_defeated`; only the lair fight in the_lodge arc sets
that tag.


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


## Pipeline draft comments

Any line whose first non-whitespace character is `#` is a pipeline draft
comment. The parser strips it everywhere (front-matter, body, choices
block, inside @if blocks, between outcome lines). It produces no role,
no paragraph break, no role-dispatch. `EncounterCli check`'s prose
rules (em-dash detection, banned-phrase detection, FIXME/REVIEW
markers) exempt `#` lines.

Purpose: the arc-writer pipeline (colorize → factual → voice → critic
passes) accumulates draft content alongside the original FIXME beats.
Drafts live in `#`-prefixed blocks so partially-curated files remain
`check`-clean. The human author curates by deleting rejected blocks
and stripping the `# ` prefix from the chosen variant.

Convention (used by the arc-writer passes; not enforced by the parser):

  # --- KIND [attrs...] ---
  # <one or more `# `-prefixed content lines>
  # --- end ---

KIND is one of: COLOR, FACTUAL, VOICED, CRITIC. Drafts attach to the
FIXME beat they sit immediately below — physical proximity is the
binding; no ids or scope keys. Each pass walks FIXME beats and looks
for downstream blocks of its predecessor type.

Attrs disambiguate within a stack only:
  COLOR              (no attrs — one COLOR stack per beat)
  FACTUAL            (no attrs — one FACTUAL block per beat)
  VOICED <author> <scene>   (multiple variants per beat; e.g. VOICED HPL dread)
  CRITIC <target>           (target: factual | voiced-<author>-<scene>)

Re-run a pass on a single beat by deleting its downstream block(s);
the tool picks up any FIXME without the expected next-stage block.

See plans/arc_writer.md for the full pipeline.

A commented-out choice (`# * Some choice`) is also skipped — the `* `
choice-boundary detection does not fire on `#`-prefixed lines.


## Skills, tiers, time, conditions

SKILLS                          TIERS (for meets/set_skill_tier)
  combat       fighting            untrained  (tier 0)
  negotiation  persuasion/social   trained    (tier 1)
  bushcraft    survival/travel     expert     (tier 2)
  cunning      trickery/awareness

  Gear gating by Combat tier (per-item RequiredCombat):
    untrained (0)  daggers, light armor
    trained   (2)  + axes, medium armor
    expert    (4)  + swords, heavy armor
  (RequiredCombat is declared per ItemDef and not yet enforced at equip
  time — see ItemDef.RequiredCombat. Authors should still respect it.)

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
  exhausted  injured  poisoned


## Action verbs

Flow control        @if check <skill> correct:<approach> wrong:<approach> { ... } @else { ... }
                      (picker check — terminal branch only, @else required)
                      Any prose between the choice's `* Option text` line and the
                      `@if check` is the PREAMBLE: it is shown to the player
                      together with the three-approach picker, BEFORE they
                      commit to an approach. The text inside the matching
                      `{ ... }` branch is only shown AFTER the pick resolves.
                      Author the preamble as the framing the player needs to
                      make an informed approach choice; never put outcome
                      reveals or branch-specific consequences there.
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

Skills              +add_level                           (grant one pending tableau level-up pick)

Conditions          +add_condition <condition_id>
                    +remove_condition <condition_id>

Time                +skip_time <period> [no_sleep] [no_meal] [no_biome]
                    +advance_time <N> [no_sleep] [no_meal] [no_biome]

Dungeon             +finish_dungeon
                    +flee_dungeon

Return to pool      +repool                              (.enc only, extremely rare — see Pooling & recurrence)

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
                    There are no direct skill-mutation verbs — skill tiers move
                    only via the tableau level-up flow.


## Combat encounters (.fight format)

Combat encounters use the `.fight` extension and drive the RPS combat screen.
They are parsed by `CmbParser` (`lib/Encounter/CmbParser.cs`). The format shares
sigils with `.enc`: `[key value]` for front-matter, `* name` for sections,
`+verb args` for mechanic verbs in prose blocks.

### File structure

    [title Monster Name]
    [image monsters/biome_type.webp]
    [blood #7a0a0a]                   (optional — default mammalian red)
    [stats hp=18]
    [trigger road]                    (optional — default none)
    [requires !tag killed_name]       (optional, repeatable)

    * move Attack
      narration: It lunges at you, claws raking forward.
      narration: It darts in low and swipes at your legs.

    * move Heavy Slow Attack
      narration: It winds back and throws its full weight into the blow.

    * intro
    Prose shown before combat begins.

    * win
    Prose shown on player victory.
    +give_gold 12
    +add_tag killed_name

    * lose
    Prose shown on player defeat.

    * flee
    Prose shown when the player flees.

### Front-matter and sections

Front-matter is `[key value]` at column 0. Sections start with `* name` at
column 0. `#` is a comment; blank lines are ignored.

    [title <text>]      Display title
    [image <path>]      Image path (relative to assets/)
    [blood <hex>]       Blood-splat color; override for non-mammals (golems, lattice, etc.)
    [stats hp=<n>]      Monster HP — required, must be > 0
    [trigger <value>]   road = random travel pool; none = arc-launched only (default)
    [background <path>] Combat backdrop override; default is the biome backdrop
    [persistent]        Never retired by winning — requires a [requires] gate
    [requires <cond>]   Spawn gate; same condition syntax as .enc, repeatable (AND)

    * move <encoding>   One move in the pool (followed by narration: lines)
    * intro             Block: opening prose
    * win               Block: prose + mechanics on player victory
    * lose              Block: prose + mechanics on player defeat
    * flee              Block: prose + mechanics when the player flees

### Move encoding

Last token is the base family; preceding tokens are mutators. Tokens are
case-insensitive. Each `* move` block must have at least one `narration:` line.
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
    riposte       Counter-attack: only triggers in mutual Attack vs Attack.
                  When both sides commit Attack, riposte deals +2 outgoing
                  damage AND absorbs 2 incoming damage. Against Defend,
                  Recover, or Read it behaves as a plain Attack.
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

### Outro mechanics (win/lose/flee)

In `* win`, `* lose`, and `* flee` blocks, `+verb` lines are mechanics run
through the standard `Mechanics.Apply` pipeline after combat resolves. They
share the verb vocabulary with `.enc` action verbs — the same canonical names,
no fight-specific aliases (`+gold`/`+tag` silently no-op; use the real verbs):

    +give_gold <n>            Award gold
    +add_tag <tag_id>         Set a world-state tag
    +add_item <item_id>       Give item
    +damage_spirits <n>       Damage spirits
    (full verb list in the Action verbs section above)

`+repool` is NOT valid in fights — recurrence is implicit (see Pooling &
recurrence): lose and flee leave the fight in the pool, winning retires it.


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
These mirror `lib/Rules/ItemDef.cs` — if you add an item there, add it here too.
Weapons and armor contribute RPS combat moves (see ItemDef.RpsMoves); the
description column below summarizes the equip-time identity, not the full
moveset.

### Weapons

Daggers (RequiredCombat 0 — Untrained, cancel/riposte-focused):
  hunting_knife       Hunting Knife        plain Attack                                plains    T1  15g
  kukri               Kukri                Attack + Pommel Stun                        scrub     T2  40g
  seax                Fine Seax            Riposte Attack only                         mountains T2  80g
  the_old_tooth       The Old Tooth        Riposte + Provoke (no plain attack)         arc/reward only

Axes (RequiredCombat 2 — Trained, aggro-focused):
  hatchet             Hatchet              Attack + Wild Chop                          forest    T1  15g
  war_axe             War Axe              Attack + Heavy Chop                         forest    T2  40g
  broadaxe            Broadaxe             Attack + Brutal Stun                        mountains T2  80g
  revathi_labrys      Revathi Labrys       Arcing Chop + Psychic Warp (no plain)       arc/reward only

Swords (RequiredCombat 4 — Expert, hybrid):
  falchion            Falchion             Attack + Wild Lunge                         plains    T1  15g
  short_sword         Short Sword          Riposte + Pommel Stun                       plains    T1  15g
  scimitar            Scimitar             Attack + Whirling Blade (a Defend move)     scrub     T2  80g
  shimmering_blade    Shimmering Blade     Riposte + Lattice Mending (Recover)         arc/reward only

### Armor

Light (RequiredCombat 0 — Untrained):
  tunic               Tunic                plain Defend                                plains    T1  (free)
  silks               Silks                Defend + Cautious Read                      scrub     T1  15g
  cartographers_cloak Cartographer's Cloak Defend + Cartographer's Guile (Recover)     mountains T2  40g
  robe_of_twilight    Robe of Twilight     Shadow Cloak (Shielding Defend) + Shadow Step  arc/reward only

Medium (RequiredCombat 2 — Trained):
  hide_armor              Hide Armor                  plain Defend                  mountains T1  15g
  lamellar                Lamellar                    Defend + Evade                mountains T2  80g
  mountain_regiment_armor 17th Mountain Regiment Armor  Perfect Block + Cautious     arc/reward only

Heavy (RequiredCombat 4 — Expert):
  gambeson            Gambeson             plain Defend                                mountains T1  15g
  scale_armor         Scale Armor          Heavy Defend (Armored) only                 scrub     T2  40g
  brigandine          Brigandine           Defend + Unstoppable                        plains    T2  80g
  golem_armor         Golem Armor          Armored + Perfect Block                     arc/reward only

### Tools

Shopable:
  waterskin           Waterskin             PassiveImmunity:thirsty                    scrub  T2  40g
  cartographers_kit   Cartographer's Kit    gates Lost encounters                      plains T1  80g
  sleeping_kit        Wool Bedroll          PassiveImmunity:freezing                   forest T2  80g
  brass_lantern       Old Brass Lantern     light source                               plains T1  15g
  medical_kit         Medical Kit           Cures injured (not consumed)               (any) 25g

Cure tools (consume nothing; remove the named condition):
  siphon_glass        Siphon Glass          Cures lattice_sickness                     scrub  T2  40g
  shustov_tonic       Shustov Apparatus     Cures irradiated                           plains T2  40g
  mudcap_fungus       Mudcap Spores         Cures poisoned                             swamp  T2  15g

Arc/dungeon-only:
  scarecrow_boots     Scarecrow Boots       PassiveImmunity:exhausted  (Tool, not Boots type)
  control_shaft       Control Shaft         quest item

### Food

  food_ration         Rations               single ration = 1 day of food, 3g
                                            (display name flavored per biome via FlavorText.RationName)

### Haul

  haul                Haul                  generic — identity comes from HaulDefId on the instance
