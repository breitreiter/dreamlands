# What makes a fun deck?

## Variety & Decisions

A low-power deck should not just be a completely bland press-button-15-times slog. It should be "low-power" because:
- The range of impact of cards is limited, there are very few haymakers
- Resource generation is modest, so setting up haymakers can be challenging
- There are few or no cancel cards, so time pressure is very real

However, it should have a few high-impact cards that are exciting to see and which should turn the tides of a fight. This could mean:
- Cards that have big numbers but are inefficient
- Cards that are strong but rare

Low-power decisions:
- Do I try to save up momentum in case i get a haymaker?
- Do I burn momentum to try to find one of the strong cards?

Conversely, a high-power deck should have:
- Many options
- Good cadence of risk vs reward decisions
- Probably 1 cancel card, at most 3

High-power decisions:
- Do I waste a turn on a momentum-builder?
- How many spirits do i burn fishing for silver bullets?
- Do I risk trying to race the clock or just plan to take a hit and get 3-4 more turns?

## Fantasy Archtectypes

Players should feel like their deck models their view of their character.

### The Expert
This is the control-kill fantasy.
- Combat - The expert duelist
- Negotiation - The intricate debater
- Bushcraft - The wise stalker
- Cunning - The master assassin

Playstyle: The expert relies on card selection to find magic bullets. Satisfying play for the expert is finding answers just in time to stop a counter, and deciding whether to take a hit in exchange for a timer reset (and more turns) or risk digging.

### The Powerhouse
This is the damage-kill fantasy
- Combat - The berzerker
- Negotiation - The fast-talker
- Bushcraft - The wraith
- Cunning - The acrobat

Playstyle: The powerhouse relies either on efficient momentum-to-damage conversion or on powerful momentum ramp which dumps into big swingy attacks.

There are a few archetypes we do not and will not have:
- The tank - slow but safe is just not gameplay we want
- The healer - again, you're solo, so no one to heal or protect

## Timer Design

Timers with identical countdowns create synchronized burst damage — all firing on the same turn for a massive spike. This is almost never what you want; it turns spirit drain into a binary "did you cancel enough before the nuke" check rather than a sustained pressure curve.

**Stagger timer countdowns.** A 3-timer encounter should use countdowns like 3/4/5, not 4/4/4. This spreads pressure across turns and gives the player distinct "most urgent" choices when they draw a cancel.

The exception is a deliberate doomsday timer: a single long countdown (e.g. 8 turns) that kills you if you haven't won by then. That's a race clock, not a drain — it creates urgency without the degenerate burst pattern.

### Condition timers are the real difficulty knob

T2/T3 encounters should include a condition timer — a single-fire timer that inflicts a serious condition (poisoned, irradiated, cursed) rather than draining spirits. This is the primary source of tension for powerhouse builds, who otherwise steamroll resistance at realistic values (R=6-8) in 4-5 turns with spirits to spare.

The condition timer countdown is extremely sensitive. Simulation with a berzerker (Wild, R=6, 1 drain):
- cond@4: 57% get conditioned
- cond@5: 20% get conditioned
- cond@6: 3% get conditioned
- cond@7: <1% get conditioned

A single turn shifts the outcome from "trivial" to "coin flip." This is the lever for difficulty tuning across tiers — not resistance or drain count.

Key implications:
- Spirits are a session budget (start 20, recover at inns). Drain timers are a tax, not a threat. Condition timers are the threat.
- Cancel builds are strong precisely because they can blank the condition timer entirely, dropping the stakes dramatically.
- Slow decks (grinders) get punished hard: at medium difficulty (R=8, cond@5), grinders eat the condition 98% of the time. Speed matters.
- The powerhouse decision isn't "can I win?" — it's "can I close before the condition fires?" Every turn spent digging or building momentum is a turn closer to the deadline.

## The No-Skill Baseline

A character with no relevant skill or gear gets a deck of 15 filler cards. This is the grinder — slow, inevitable progress. The filler pool should favor cheap/free incremental damage with a few bigger momentum-costed cards for drama. The no-skill player is always doing *something*, never stuck, but they're slow. Timers will tick.

This is fine in T1. Drain timers ding you for 1-2 spirits, no condition timers. You grind through R=6 in 5-6 turns, spend 2 spirits, move on. The filler pool *is* the game and it needs to follow the low-power deck philosophy above — mostly chip, one or two haymakers to get excited about.

Skill progression doesn't make encounters *possible* — it makes them *safe*. A Combat 3 berzerker with Wild closes R=6 in 3-4 turns and dodges the condition timer that would have hit the no-skill grinder on turn 5. The encounter was always winnable; the skill bought you turns you didn't have to spend.

### Tier difficulty curve

- **T1**: No condition timers. Drain-only (spirits -1, maybe -2). The no-skill grinder can handle this. Spirits cost is a tax, not a threat. Encounters teach the system without punishing.
- **T2**: Condition timers appear with generous countdowns (cond@6-7). A skilled character dodges them easily. A no-skill character has a 3-20% chance of eating the condition — a real risk that makes the player want better gear, but not a wall.
- **T3**: Condition timers are tight (cond@4-5) and the conditions are brutal. No-skill characters basically cannot avoid them. Even skilled characters need to play well. This is the "scary and overmatched" feel.

The key insight: the same R=6 encounter can serve all three tiers by changing only the timer configuration. The filler cards stay the same. The resistance stays the same. The condition timer countdown and condition severity are what make T3 terrifying and T1 gentle.

## Advancement

When returning to earlier challenges with more capable equipment, the player should feel like they are able to overpower challenges with minimal risk. Conversely, when the player pushes into harder content (especially t3) it should feel very scary and they should feel overmatched. However, that same content with good gear should feel challenging but possible.

## New Card Mechanics

  - Quick - playing this card doesn't advance the turn
  
## Archetypes

### Time Bomb
Severe clock, long horizon
- 7-8 turns on each timer
- no cancels in the seed
- Each timer is maximally awful for the current tier

### Minefield
Normal clock, but some cards are bad
- 5 cards have negative effects
- 5 momentum cards
- 4 mixed damage cards
- 1 haymaker

### Bazooka
Normal clock
- 10 momentum builders
- 3 big hits
- 2 haymakers

## Timers

### Spirits
- These are busy work threats, they add character
- Max 1

### Resistance
- Boring, interesting as a spice
- Max 1

### Conditions
- The workhorse
- t1-t2 common: exhausted, freezing
- t1 rare, t2 common: injured
- t2 rare, long timer: irradiated, lattice_sickness, poison
- t3 common: injured, irradiated, lattice_sickness, poison