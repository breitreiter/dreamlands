# Roadside Encounter System

## Core Concept

Encounters are **visible ledgers** resolved through **tableau manipulation**. No branching narrative. No agent beliefs. No authored prose beyond card and ledger entry names. Variety emerges from combinatorial interaction between biome-specific tableau pools and player equipment, not from scripted scenes.

This system covers roadside/travelling encounters only. Long-form narrative encounters (multiple agents, conflicting desires, information flow) use the separate .enc / Twine-alike system. An encounter is one or the other, never both.

## Encounter Flow

1. **Encounter opens.** Title and summary (hand-written per encounter). Player sees the full ledger: risks and rewards (hand-written per encounter). Fully visible. No hidden gotchas.
2. **Player decides whether to engage.** Disengagement is always an option but carries opportunity cost (lost gains, backtracking, worse route). This is a legitimate tactical decision.
3. **Player works the tableau.** Available **verbs** are listed (assigned per encounter but drawn from a shared pool, e.g. "ignite," "climb," "throw," "distract"). Player picks a verb and slots cards into it. The verb shows a **live preview** of what it will do: risks avoided, rewards lost, new ephemeral card created, roll modifiers by strategy. Player can back out and try different combinations.
4. **Player commits to a strategy.** When satisfied with their board state (or when the situation is exhausted), player selects a **strategy** to resolve the encounter. Strategies are also drawn from a shared pool (e.g. "fight," "evade," "navigate," "pay") and are assigned per encounter. The strategy determines which skill is rolled against and how the final ledger resolves.
5. **Resolution.** Generic success/fail against the chosen strategy. Outcome list: remaining risks and rewards resolve based on the current ledger state.

## Scope Constraint: The Clutch Moment

The tableau phase takes you up to the decisive moment, not through the entire encounter. You don't fight every wolf — you set up the conditions and then roll to see if your setup pays off. The final roll **must still have teeth.** If tableau manipulation could resolve everything, there's no reason to roll and no tension at resolution. The system builds you the best possible position and then asks: is it enough?

## Cards

### Tableau Cards (Environmental)

Dealt from a biome-keyed location pool. Represent physical scene elements. Tagged with **affordance properties**: heavy, loose, elevated, flammable, liquid, narrow, obscuring, sharp, stable, organic, etc.

Biome pools weight toward characteristic vocabularies:
- Mountain: elevated, loose, narrow, rocky
- Swamp: liquid, obscuring, unstable, organic
- Forest: flammable, organic, elevated, obscuring
- etc.

Repeated exposure teaches players to anticipate a biome's tactical vocabulary.

### Equipment Cards (Player)

Represent the player's loadout. Tagged with **capability properties**: fire, light, reach, binding, flexible, leverage, sharp, blunt, digging, etc.

Examples: rope {reach, binding, flexible}, torch {fire, light, fragile}, shovel {digging, leverage, heavy, blunt}.

**Gear contributes properties only, not numeric bonuses.** A +3 sword and a +0 sword with the same property tags perform identically in the tableau — the difference is the +3 sword is better in arc encounters and skill checks outside this system. The roll modifier comes from what you did with the gear (recipe outputs), not from the gear's stats.

This gives gear a second axis: a fancy sword might have great stats but fewer property tags than a humble hatchet. Players who care about road encounters may gear differently than players optimizing raw numbers.

### Ephemeral Cards (Generated)

Spawned by recipe interactions. Represent momentary positional advantages. **Decay on a timer** — the timer is fiction-legible, not a numeric countdown. "Bandit is off-balance" obviously lasts one beat. "Rope is holding" lasts until stressed. "Smoke cover" is visibly dissipating.

Ephemeral cards feed back into subsequent recipes as slottable elements. The scene evolves.

Decay is the system's **natural action limiter**. Earlier plays expire while you set up new ones. You can't stack a perfect position. You must commit while your window is open. Experienced players learn to sequence overlapping windows.

## Recipes

Recipes are **verb + aspect-threshold interactions**, not scripted events. A recipe is a data row: verb + input aspects → output card + ledger modifications + roll modifiers by strategy.

Example: ignite + flammable + near_enemy → spawn "Area Denial (decaying)" + foreclose gains tagged {salvageable} in proximity + modifier to "evade" strategy.

Recipes are general. "Ignite + flammable" works everywhere fire meets flammable things, across all biomes and encounter types. Authoring scales horizontally.

Every recipe that can fire must pass the **"yeah, that makes sense" test.** Grounded setting means players will immediately identify nonsensical outcomes. No hiding behind arcane mystery. (Exception: tier 3 encounters — see Tier 3 section below.)

## Roll Modifiers

Tableau-generated roll modifiers are **flat bounded bonuses**. Each relevant recipe output contributes a small modifier (+1 or +2). Total modifier from tableau manipulation is **capped** (e.g. +5).

Flat bonuses rather than advantage/disadvantage because multiple tableau plays should stack meaningfully — advantage doesn't compound. The cap gives players a legible target: "I've squeezed out all the bonus I can, time to commit." The cap also reinforces the clutch moment — you can't stack your way past the roll.

**Equipment numeric bonuses do not apply in road encounters.** Gear participates only through property tags. The only roll modifiers come from recipe outputs.

## Ledger Modification

Tableau manipulation modifies the ledger in three ways:

### Adding/Removing Entries
Ephemeral cards can strike losses from the ledger or add new gains. "Blocked escape route" removes "bandits flee and ambush you later." "High ground" adds a ranged bonus to the gain column.

### Verb-Driven Lock/Strike
Some verbs directly lock or remove ledger entries as their primary effect, without creating ephemeral cards. The enemy isn't reacting — you're narrowing the possibility space with each action.

### Foreclosure (Commitment Cost)
Some manipulations **irrevocably** alter the ledger at the moment of action, before resolution. Pushing rocks to block a path also permanently removes "capture bandit alive." Lighting scrub brush for smoke cover also destroys nearby salvageable goods.

Foreclosure is the system's primary source of tension. The **verb preview** shows the player what they'll gain and lose before they commit — but once committed, it's done. The grounded setting reinforces this: everyone knows fire burns nearby things, so the preview confirms intuition rather than revealing surprises.

## Combat

Combat is a **subset of tableau manipulation**, not a separate system. Same create-and-exploit loop.

Core principle (borrowed loosely from HEMA): **you attack openings, not opponents.** An opening is an ephemeral card. "Bandit is off-balance" is an opening. The player slots a weapon into the opening before it decays. Weapon properties interact with opening properties — a dagger gets {lethal, quick}, a polearm gets {knockdown, controlling}.

Weapons differentiate by role in the create/exploit loop:
- Heavy weapons: poor at creating openings, devastating when exploiting
- Quick weapons: good at creating openings, smaller payoffs on exploit
- Reach weapons: can exploit openings at distance that short weapons can't

Environmental and combat interactions are unified. Kicking dirt, shoving a barrel, using terrain — same system, same card types, no mode switch.

## Encounter Archetypes (Agents as Obstacles)

Agents are **mechanistic obstacles**, never social actors. No beliefs, goals, or dialogue. At most, an agent becomes "distracted" as a decorative form of "disabled." No opponent reactivity — enemies do not create ephemeral cards or take actions. Threat is front-loaded in the ledger (the risks column). Escalation pressure comes from ephemeral card decay: the window you created is closing, not because the enemy acted, but because time passed.

- **Bandits**: foes with {payable} — coin cards resolve the encounter directly, bypassing the puzzle
- **Predators**: foes with {territorial, aggressive} — evasion options via tableau
- **Environmental hazards**: rockslide, flood, collapsed bridge — pure tableau puzzle, no hostile agent
- **Blocking obstacles**: toll, broken road, washed-out ford — resource or tableau resolution

## Strategies and Verbs (Shared Pools)

**Verbs** are the tableau interaction vocabulary. Things like "ignite," "climb," "throw," "distract," "block," "cut." Each encounter has a subset of verbs assigned to it. Verbs are not unique to any encounter — they are reusable and their behavior is defined by the recipe table (verb + input aspects → output).

**Verbs can spawn and kill other verbs, and foreclose strategies.** Firing a verb can make new verbs available or remove existing ones. A battle cry might spawn "charge" but kill "sneak." Lighting a fire might spawn "signal" but kill "hide." Verbs can also permanently remove strategies from the resolution menu — if you're screaming and charging, "evade" is no longer on the table. This means tableau manipulation narrows your options as well as improving them. Early choices constrain late choices.

**Strategies** are resolution approaches. There are exactly four, each mapping 1:1 to a player stat:

| Strategy | Stat | Fantasy |
|---|---|---|
| **Fight** | Combat | Overpower the obstacle |
| **Parley** | Negotiation | Talk, bribe, de-escalate |
| **Navigate** | Bushcraft | Survive it, find a way through |
| **Outmaneuver** | Cunning | Exploit the angle, slip past |

Luck and Mercantile have no strategy mapping — they operate on different axes (Luck is the "no plan" stat; Mercantile shapes the economic game between encounters). The fixed mapping means players always know what they're rolling when they pick a strategy, and verb foreclosure has real teeth: killing "outmaneuver" removes your best option if you built around Cunning.

Each encounter has a subset of strategies assigned to it. The player's choice of strategy determines which skill is rolled and how the final ledger resolves. Strategy selection happens *after* tableau manipulation — the player shapes the ledger first, then picks how to cash out. But the strategies available at resolution may be a subset of what was originally offered, depending on what verbs were fired.

Player stance is expressed through the combination of which verbs they use during tableau work and which strategy they select at resolution. No explicit stance menu.

## Tier 3: Broken Rules

Tier 1 and 2 encounters operate under full transparency. All recipes are visible. The "yeah, that makes sense" test applies. Preview tells all.

Tier 3 encounters break this contract. The setting's normal logic doesn't apply — eldritch territory, angry robots, things that shouldn't exist. Tier 3 draws from a **separate recipe table** where mappings are unintuitive. "Ignite + liquid" does something different in the Dreamlands.

**Unknown recipes show `?` in the verb preview** until the player has fired them once. Discovery is permanent and tied to the player's profile, not the encounter instance. First exposure is a surprise; repeat encounters teach you the new logic.

This creates a progression arc: tier 1-2 teaches you the system with full information, tier 3 asks you to apply that understanding in a space where your assumptions are wrong. Getting clapped the first time is the lesson.

## System Boundary

This system and the .enc / Twine-alike system are **completely separate**. They serve different purposes:

| | Tableau (this system) | .enc / Twine-alike |
|---|---|---|
| **Scope** | Road encounters | Arc encounters, narrative events |
| **Agents** | Mechanistic obstacles | Social actors with goals and desires |
| **Tension source** | Tactical trade-offs, foreclosure, the final roll | Information asymmetry, conflicting desires, consequences |
| **Authoring** | Card names, ledger entries, verb/strategy assignments | Authored prose, branching dialogue, conditional flow |
| **Variety** | Combinatorial (biome pools × equipment × recipes) | Hand-written per encounter |

An encounter slot in the world is one or the other. The tier/biome directory structure already supports this — a road encounter slot pulls from the tableau pool, an arc encounter fires its .enc file.

## Authoring Requirements

Per-encounter (hand-written):
- **Title and summary**
- **Risks and rewards** (the ledger)
- **Verb assignments** (which verbs from the shared pool apply here)
- **Strategy assignments** (which strategies from the shared pool apply here)

Shared/systemic (authored once, reused everywhere):
- A **property taxonomy** (affordance + capability aspects)
- A **recipe table** (verb + aspect thresholds → outputs + ledger effects + roll modifiers by strategy)
- A **tier 3 recipe table** (separate, unintuitive mappings)
- A **verb pool** with recipe bindings
- A **strategy pool** with skill mappings and resolution rules
- A **biome card pool** per location type
- **Good names** for cards and ledger entries (reusable across encounters)

No branching narrative. No bespoke flavor text per encounter beyond title/summary/ledger.

## Open Questions

- **Roll modifier cap value**: +5 feels right as a starting point, but needs playtesting against the existing DC scale.
- **Ephemeral card decay model**: fiction-legible timers are the design intent, but what's the mechanical implementation? Beat counter? Verb-count since creation? Needs prototyping.
- **Tier 3 recipe discovery persistence**: profile-level (permanent unlock) or per-run? Permanent fits the "learning" framing but reduces replayability of the surprise. Per-run is punishing.
- **Biome card pool sizing**: how many tableau cards per biome pool? Too few and encounters feel samey within a biome. Too many and the biome loses its characteristic vocabulary.
