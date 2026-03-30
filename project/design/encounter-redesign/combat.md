# Combat Encounter Model

## State

Three player resources, one antagonist resource:

- **Momentum** — momentum. Starts based on approach, +1 per turn passively. Spent to take openings or Press the Advantage.
- **Spirits** — mental/physical reserve. Does not regenerate during the encounter. Spent to take openings or Force an Opening. Drained by timers.
- **Resistance** — the antagonist's remaining capability. Damaged by openings. Healed by some timers. Reaching 0 = victory.

## Timers (Threats)

Each combat has a set of named timers with countdown pips. Every turn, all active timers tick down by 1. When a timer hits 0 it fires, applies its effect, and resets to full.

Timer effects:
- **Spirits drain**: -N Spirits (most common)
- **Resistance recovery**: +N Resistance (antagonist rallies)

Timers are the encounter's offensive pressure. They create urgency — you can't grind forever.

A timer can be **stopped permanently** by certain openings. Stopping all timers wins the encounter (control kill).

### Timer design levers

| Parameter | Role |
|---|---|
| Count | How many simultaneous threats (difficulty) |
| Countdown | Turns until it fires (tempo) |
| Effect type | Spirits drain vs resistance recovery (pressure shape) |
| Amount | Severity per firing |

Prototype ranges: countdown 3-6, amount 1-2, count 3-4 per encounter.

## Openings

An opening is a fleeting opportunity with a **cost** and an **effect**. You get 1 free opening per turn. Taking an opening ends your turn.

### Costs

| Cost | Description |
|---|---|
| Free | No resource cost (rare) |
| Momentum | Spend N Momentum |
| Spirits | Spend N Spirits |
| Tick | Advance a random active timer by 1 pip |

### Effects

| Effect | Description |
|---|---|
| Damage Resistance | -N Resistance (the direct path to victory) |
| Stop Timer | Permanently neutralize the most urgent active timer |
| Gain Momentum | +N Momentum (fuel for future turns) |

The cost/effect pairing is what makes openings interesting. A free opening that stops a timer is a gift. A tick-cost opening that damages resistance is a gamble — you're trading timer pressure for progress. A momentum-cost opening that gains momentum is a wash unless you're setting up for an expensive opening next turn.

### Opening design levers

In the prototype, openings are randomly generated. In actual encounters, openings would be authored per-encounter or drawn from themed pools. The authored version gives us control over:

- Which cost/effect pairings appear (encounter personality)
- How generous the free openings are (difficulty tuning)
- Whether stop-timer openings exist at all (can this encounter be control-killed?)
- Narrative flavor per opening (what the player is actually doing)

## Turn Structure

Each turn:

1. All active timers tick down (firing any that reach 0)
2. Momentum +1
3. Player receives opening(s) — normally 1, but 3 after a Press the Advantage/Force an Opening
4. Player chooses one action:
   - **Take an opening** — pay its cost, gain its effect, turn ends
   - **Press the Advantage** — spend 2 Momentum, skip this turn, receive 3 openings next turn
   - **Force an Opening** — spend 2 Spirits, skip this turn, receive 3 openings next turn

One action per turn. No shopping, no stacking. The decision is always: use what you have, or invest for better options.

## Approach (Initial Stance)

Chosen at the start of combat. Sets starting resources and threat level.

| Approach | Momentum | Timers | Special |
|---|---|---|---|
| Scout | 0 | 3 | Start with 3 openings on turn 1 |
| Direct | 3 | 3 | — |
| Wild | 6 | 4 | Extra timer |

- **Scout**: no momentum, but you've studied the situation and see multiple paths. The 3 opening spread on turn 1 often includes something valuable. Low-resource, high-optionality.
- **Direct**: balanced start. Enough Momentum to Press the Advantage on turn 1 if the free opening is bad.
- **Wild**: lots of momentum but an extra threat ticking. You can afford expensive openings early, but the timer pressure is relentless.

## Endings

| Ending | Condition | Narrative |
|---|---|---|
| Resistance kill | Resistance = 0 | You've worn them down. Attrition victory. |
| Control kill | All timers stopped | Total dominance. Every threat neutralized. |
| Spirits loss | Spirits = 0 | Overwhelmed. You suffer consequences. |

The two victory paths create a real strategic fork:
- **Resistance kill** — race the timers, eat the spirit damage, hammer resistance with every opening
- **Control kill** — invest in stop-timer openings, endure until every threat is neutralized

Most encounters will naturally lean toward one or the other based on which openings are available and how many timers there are.

## Key Tensions

**Momentum is self-replenishing, Spirits are not.** Momentum grows by +1/turn, so Press the Advantage gets cheaper over time. Force an Opening costs a non-renewable resource. This creates a natural arc: early game you might Force an Opening because you can't afford to Press the Advantage; late game Momentum is abundant but Spirits are precious.

**Timers are recurring, openings are fleeting.** A timer you don't stop will fire again and again. An opening you don't take is gone. This pressures you toward action — skipping a turn (Press the Advantage/Force an Opening) has a real cost because those timers just ticked.

**Tick-cost openings are a devil's bargain.** They're "free" in terms of your resources, but they accelerate the clock. Taking one when a timer is at 1 pip could trigger a firing.

**Press the Advantage/Force an Opening is an investment, not a guarantee.** You get 3 openings instead of 1, but they're random. You might get three bad options. The question is whether your current single opening is bad enough to justify a lost turn.

## Balance Numbers (from prototype)

### Starting State

| | Spirits | Resistance |
|---|---|---|
| All approaches | 10 | 8 |

### Per-Turn

- Momentum: +1 passive per turn
- Press the Advantage cost: 2 Momentum
- Force an Opening cost: 2 Spirits
- Press the Advantage/Force an Opening bonus: 3 openings next turn
- Scout turn-1 bonus: 3 openings

### Opening Costs (weighted random)

| Cost | Weight | Amount |
|---|---|---|
| Momentum | 30% | 1, 2, 2, or 3 (median 2) |
| Spirits | 25% | 1, 1, or 2 (median 1) |
| Tick a timer | 25% | 1 pip |
| Free | 20% | — |

### Opening Effects (weighted random)

| Effect | Weight | Amount |
|---|---|---|
| Gain Momentum | 30% | 2 or 3 |
| Stop Timer | 30% (only if active timers exist, else falls through to Resistance) | 1 timer (most urgent) |
| Damage Resistance | 40% (base) | 2, 2, or 3 (median 2) |

### Timers

| Name | Effect | Amount | Countdown |
|---|---|---|---|
| Flanking maneuver | Spirits | 2 | 4 |
| Rallying cry | Resistance | 2 | 5 |
| Closing trap | Spirits | 1 | 3 |
| Waning resolve | Spirits | 1 | 4 |
| Regrouping | Resistance | 1 | 4 |
| Encirclement | Spirits | 2 | 5 |
| Rising flood | Spirits | 2 | 4 |
| Crumbling path | Spirits | 1 | 3 |
| Gathering storm | Spirits | 1 | 5 |
| Dwindling light | Spirits | 2 | 6 |

Encounters draw 3-4 timers from this pool (3 for Scout/Direct, 4 for Wild). Timers reset to full countdown after firing.

## Prototype

Playable prototype at `prototype.html` in this directory.
