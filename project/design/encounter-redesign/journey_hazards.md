# Journey Hazard System

## Overview

A system for making long expeditions feel like a series of meaningful decisions
rather than a stat treadmill. Replaces ambient nightly resist rolls with
procedural hazard encounters driven by player knowledge and resource management.

## Part 1: Medical kits (near-term)

### Motivation

Long expeditions (e.g. Rankleburn to The City, 42 tiles / 9 days one-way) require
carrying large quantities of consumable medicine. With 3 severe conditions at 3 stacks
each, a safe loadout needs 15-18 haversack slots just for cures, leaving no room for
food. Medical kits move condition treatment to pack slots, freeing the haversack for
food and tokens.

Sim results (100k trials, DC 8, 9-day return trip, 3 severe conditions @ 3 stacks,
no food, no medicine skill modifier):

| Skill modifier | Pass% | Survival |
|---|---|---|
| -2 | 55% | 23% |
| 0 | 65% | 49% |
| +2 | 75% | 77% |
| +4 | 85% | 95% |

Starting disheartened roughly halves survival across the board, making spirits
management a real factor on long expeditions.

### Changes

**1. Remove consumable medicines**

Delete from ItemDef:
- `bandages` (cures injured)
- `siphon_glass` (cures lattice_sickness)
- `shustov_tonic` (cures irradiated)
- `mudcap_fungus` (cures poisoned)
- `pale_knot_berry` (cures exhausted) — exhausted has no kit, stays inn-only

Remove medicine auto-consume logic from EndOfDay.ResolveMedicines.

**2. New item definitions: medical kits**

Type: `Tool` (goes in Pack, infinite use, no consumable slot pressure).

| Item | Id | Cures | Biome | ShopTier | Cost |
|---|---|---|---|---|---|
| Bandage Roll | `bandage_roll` | injured | any | any | TBD |
| Siphon Lens | `siphon_lens` | lattice_sickness | scrub | 2 | TBD |
| Tonic Kit | `tonic_kit` | irradiated | plains | 2 | TBD |
| Antivenom Pouch | `antivenom_pouch` | poisoned | forest | 2 | TBD |

Bandage Roll is biome-any, tier-any: injury is universal and the cost is pack space,
not availability. The other three are biome-locked to the condition's source region
as a discovery/planning signal for the player.

**3. End-of-day treatment: skill check**

Replace medicine auto-consume with a no-modifier d20 skill check per active severe
condition:

- **DC 8** (65% base pass rate)
- **No skill modifier** — pure d20 roll, no Medicine skill needed
- **Disadvantage if disheartened** — spirits management matters
- **Requires the matching kit in pack** — no kit = no roll = untreated
- One roll per active severe condition per night

**4. Health drain rule** (unchanged logic, new trigger)

- All active severe conditions successfully treated (or already cleared) → no HP loss
- Any severe condition untreated (failed check OR missing kit) → -1 HP (binary)

**5. Stack reduction**

- Successful treatment check → -1 stack of that condition
- Condition cleared when stacks reach 0

**6. Inventory impact**

Endgame pack layout (7 slots max):
- 3 slots: medical kits (injured + 1-2 biome-specific)
- 4 slots: utility tools (+2/+3 skill items)

Pack-upgrade arcs become nearly mandatory for endgame content. Starter 3-slot
pack = barely surviving; maxed 7-slot = well-equipped explorer.

**7. What this doesn't change**

- Exhausted: no kit, cured by inn rest only. Spirits drain is the pressure.
- Freezing/Thirsty: ambient biome conditions, cleared on settlement entry.
- Disheartened: unchanged — triggers below 10 spirits, imposes disadvantage.
- Foraging, food consumption, rest recovery: all unchanged.

### Open questions

- Kit pricing: high enough to be meaningful, cheap enough that availability
  isn't the gate (pack space is the gate)
- Kit naming: above names are placeholders
- Should kits have any secondary effect (e.g. small resist bonus)?

---

## Part 2: Journey hazard encounters (future)

### The problem

The current late-game loop: prepare at settlement, walk for days, roll resists
every night, hope you packed enough. Once you leave town, there are no decisions —
just dice. Preparation is a packing problem, not a planning problem.

The early game works because settlements are close enough that "turn back or push on"
is always live. The late game removes that — once you're 5 days out, you're committed,
and commitment without choices is just watching.

### Design goal: early/late game mirroring

The condition system teaches the same concepts at two severity levels:

**Early game (minor conditions: freezing, thirsty, exhausted):**
- Some areas have unique threats
- Gear mitigates those threats
- Running out of food isn't fatal, but starts a death spiral
- Plan journeys as hops between settlements

**Late game (severe conditions: injured, irradiated, lattice_sickness, poisoned):**
- New unique threats that can end your run
- Preparing for those threats is now essential
- Settlements are still safe havens, but reaching them is the challenge
- Early game risk: "recovery will cost all my money"
- Late game risk: "I might die before I reach safety"

The early systems compound, not get replaced. Spirits matter *more* in the late
game (disheartened = disadvantage). Food matters *more* (no food = no recovery =
stuck disheartened). Early-game habits become survival skills.

### Narrative models

Three vibes:
1. **Heist movies**: preparation pays off visibly. You brought the glass cutter
   because you knew there'd be a window.
2. **Roadside Picnic**: reading the environment, recognizing anomalies, responding
   with expertise. The bolt-throwing ritual.
3. **Wilderness survival / behind enemy lines**: constant micro-decisions with
   limited resources. "Given what I have right now, what do I do?"

What these share:
- Threats are **discrete and readable**, not ambient
- Preparation **matches situation** — the kit is a key, not a stat stick
- Decisions happen **in the field**, not at the shop
- Expertise = **seeing more options**, not rolling better

### Player mastery, not character mastery

The opportunity is not "piloting an expert who shows expertise" but "becoming an
expert yourself within a narrow system."

- Character mastery: "Your Bushcraft is 3, so you see the safe path."
- Player mastery: "YOU know lattice drifts downwind, so you route west."

Requirements:
- **Consistency**: lattice always behaves like lattice. Knowledge accumulates.
- **Legibility**: visible cause and effect, not hidden dice.
- **Depth**: simple rules that interact. Complex in combination.
- **Counterplay**: correct responses the *player* figures out.

### Counterplay bridges gear to decisions

1. **You the player** learn that lattice is a threat in scrub territory
2. **You the player** learn that siphon lenses come from scrub T2 settlements
3. **You the player** acquire one — a mechanical inventory action
4. **You the player** now see new choices you expected and prepared for

The gear system serves the knowledge system, not the other way around.

### The option model

Every hazard presents **options**. Each option has:

```
OPTION
├── Action label              "Cut through the lattice field"
├── Description               "Shorter, but you'll be close to active formations"
├── Unlock cost               to reveal this option exists (multi-currency or free)
└── Outcomes[]
    ├── Label                 "Clean passage" / "Lattice exposure"
    ├── Mechanical impact     none / condition / resource cost / time cost
    ├── Base weight           probability before investment
    └── Visibility            seen (experienced before) / hidden / revealed (paid)
```

Execution cost is a guaranteed outcome (100% weight), not a separate field.

Gear and skills serve three functions:
- **Reveal**: make hidden options visible
- **Enable**: make impossible options possible
- **Narrow**: compress outcomes toward the good end

Options may have a **resource cost**: food, time, spirits. The safe option might
exist and be available, but you can't afford it. Resource pressure turns known-safe
options into tradeoffs.

Two parallel progression tracks:
- **Gear progression**: unlocks options mechanically
- **Knowledge progression**: unlocks options cognitively (free, from experience)

### Outcome visibility and experiential learning

Outcome knowledge comes from character history, not stat thresholds:

- First time: outcome is hidden. You don't know what can happen.
- After experiencing it: permanently visible. "You recall what happened last time."
- Spend points to reveal outcomes you haven't experienced (scouting).
- Player knowledge shortcuts: a veteran recognizes the situation even if this
  character hasn't been here. They skip recon, dump budget into reweighting.

### Presentation: reveal → invest → choose → resolve

1. **Reveal**: core problem with partial information
2. **Invest**: spend budget on recon, preparation, or mitigation
3. **Choose**: pick option with whatever info and steering you've bought
4. **Resolve**: outcome lands within the band you've shaped

Skills as **ephemeral point budget** per encounter. Bushcraft 4 = 4 points to
allocate. Gear adds free points in specific situations. Real resources (food, time)
can be burned for extra points.

Investment buckets:
- **Recon**: reveal hidden options or outcomes
- **Preparation**: improve odds of good outcome
- **Mitigation**: reduce severity of bad outcome

These compete for the same budget. The interesting choice: do I spend my last
2 points finding the safe route, or accept the hard path and pad the landing?

### Oracle structure: core problem + confounds

Each hazard is generated from:
- **One core problem**: the archetypal biome challenge (lattice field, radiation
  pocket, trap line)
- **0-3 confounds**: complications that change the puzzle

Confound count = complexity:
- **0**: routine. Mastery feels rewarding.
- **1**: wrinkle. Think before selecting.
- **2-3**: real puzzle. Multiple axes of pressure.

Maps to tier:
- **T2**: 0-1 confounds. Learning the core problems.
- **T3**: 1-3 confounds. Applying what you know under pressure.

### Journey event taxonomy

Six archetypal event types, each with biome-specific subtypes:

**Negative:**
- **Confrontation** — hostile, sentient. Someone or something blocks your path,
  you have to deal with them.
- **Entrapment** — you're in a bad position and need to get out or escape.
- **Hazard** — dangerous but non-sentient. Something in your path that you
  need to navigate.

**Positive:**
- **Windfall** — an opportunity to find something valuable.
- **Favorable ground** — a potential safe place to pause and recover.

**Neutral:**
- **Stranger** — a neutral party. Could swing positive or negative depending
  on your choices. Social wildcard.

The positive/neutral types provide pacing — breathing room that makes the
negative events tense by contrast. Stranger is the most encounter-like,
as it can go either way.

### Mechanical levers

| Lever | What it is | Role |
|---|---|---|
| **Currency** | Skill points, gear bonuses | Per-encounter budget. Spend to reveal options, reveal outcomes, tilt weights. "What currency does this gear provide?" is a separate question. |
| **Ablative stats** | Spirits, gold, food | Direct gain/loss as outcomes. Spirits and food cascade into other systems (disheartened, starvation). |
| **Conditions** | Acquire or cure | Outcome of bad choices. Feeds into end-of-day/HP timer. |
| **Inventory** | Gain items | Positive outcomes only. Windfall payoffs. |
| **Time** | Days | Outcome or cost. Cascades: more days = more food, more end-of-day cycles, more condition exposure. |
| **Qualities/tags** | Flags, standings | Sparingly. Could gate future options. |

### Currency: broad vs narrow

Currency comes in two flavors:
- **Broad** (skills): Cunning coins work on any option where cleverness applies.
  Versatile, but spread thin across many situations.
- **Narrow** (gear): Lockpick coins only work on the specific option they match.
  Powerful in that situation, dead weight otherwise.

They **stack** at a **1:1 exchange rate**. All coins are equal in power. For a
given option, you might be able to spend Cunning OR Lockpick coins — multiple
buttons, same target. If you have both, you have more total budget for that
specific problem.

The specialist advantage is **more coins** in matching situations. The specialist
tax is **pack space** — carrying a lockpick, siphon lens, trap kit, and three
medical kits is 6 of 7 pack slots. Batman's utility belt is expensive. A
high-Cunning character does the same work with zero gear and flexible coins.

Player mastery angle: veterans know which narrow tools a specific route needs,
so they pack precisely. New players are better off with broad skills because
they don't know what's coming.

Each piece of gear declares: "I provide N coins of type X for situations tagged Y."

**HP is never a direct lever.** HP is always downstream of conditions — you don't
lose HP because you fell, you lose HP because you got injured and couldn't treat
it that night. This preserves the medical kit loop as the single choke point for
survival pressure.

### Biome threat inventory

| Biome | Threat source | Character | Condition |
|---|---|---|---|
| Plains | Imperial war ruins / The Grid | Ambient, gradient — intensity by proximity | irradiated |
| Scrub | Kesharat / The Lattice | Structural, geometric — patterns and growth | lattice_sickness |
| Forest | The Hunter | Deliberate traps — placed by an intelligence | poisoned |
| Mountains | (different axis — not a survival zone) | — | — |
| Swamp | (TBD) | — | — |

Through-line: all three are "read the environment, then navigate." Same core
loop, different rules for how the threat behaves.

### Constraints

**Text variety is not the source of replayability.** LLM-generated text hits
visible repetition fast (see: haul generation — 200 micro-stories, clear patterns
despite explicit negative constraints). Replayability comes from system state:
loadout, resources, threat configuration, route options. Text is a thin frame —
small hand-authored pool (10-15 per biome), not procedural prose.

### Composable blocks and fiction skins

Journey hazard encounters are composed from **lego blocks** — self-contained
mechanical elements that combine freely.

**Block design principle:** blocks must be abstract enough to compose without
dependency graphs, but concrete enough to carry mechanical meaning.

Good blocks (self-contained, freely composable):
- **Physical barrier** — door, gate, rockfall. Accepts lockpick/bushcraft coins.
- **Gatekeeper** — guard, bureaucrat, cultist. Accepts cunning/negotiation coins.
- **Alternate route** — side passage, over the wall, streambed. Accepts bushcraft,
  may cost time.
- **Environmental hazard** — radiation, lattice, poison. Accepts matching kit.
- etc.

An encounter pulls 2-3 blocks, which define the mechanical option space. The
fiction comes from **skins** — a small set of locale-specific one-liners per block:

```
Gatekeeper:
  stift:   "A clerk at the transit office insists your papers are out of order."
  forest:  "A trapper has strung rope across the path and won't move."

Alternate Route:
  stift:   "You notice a window onto an interior courtyard one floor down."
  forest:  "An animal track leads through the undergrowth."
```

Concatenated: *"A clerk at the transit office insists your papers are out of
order. You notice a window onto an interior courtyard one floor down."*

Authoring load: ~2-3 skins per block per locale. Maybe 8 blocks, 5 locales,
3 skins each = ~120 hand-authored one-liners. Tractable. Seeing the same clerk
twice is fine because the mechanical puzzle around him is different each time
(different confounds, different budget, different player state).

**LLM assist note:** given the world bibles / locale guides, Sonnet can probably
generate single-sentence skins with 60-80% accuracy. Not good enough for
production, but good enough to reduce writing toil by 60-80% (generate, then
hand-edit the bad ones).

**Transparency vs. diegesis.** The option model must be legible (player understands
what's happening) AND diegetic (doesn't feel like a spreadsheet). The challenge is
communicating option/gate/risk through the fiction. This is the central unsolved
presentation problem.
