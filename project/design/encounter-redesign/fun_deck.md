# Fun Deck Design

This is the core design document for tactical encounter feel and balance. It covers the encounter structure, approach system, turn flow, deck composition, itemization, and tier difficulty curve.

## Encounter Structure

An encounter is a **sequence of timers**. There is no separate resistance track. Each timer has:
- **Resistance**: progress needed to clear it
- **Countdown**: turns until it fires
- **Effect**: what happens when it fires (spirits drain or condition)

Timers are sequential — only one is active at a time. Clearing a timer (by filling its resistance or playing a cancel card) advances to the next timer. When the last timer is cleared, the encounter is won.

If a timer fires, its effect hits the player and it **resets its countdown**. The timer keeps firing until the player clears it or flees. This means an undergeared player stuck on a tough timer bleeds spirits/conditions every N turns until they punch through.

**Flee**: the player can flee at any time. This skips to the encounter's scripted failure outcome. Fleeing is better than death-spiraling through multiple timer resets — it's the "cut your losses" option.

### Timer design

Timers run one at a time, creating **encounter sections** with distinct character:
- Section 1: spirits drain every 5 (a tax, teaches the system)
- Section 2: condition timer every 6 (real threat, but generous)
- Section 3: condition timer every 4 (desperation, the crunch)

This provides natural narrative arc (escalation) and solves the synchronized-burst problem — timers can't stack because only one is active.

**Cancel cards one-shot the current timer**, skipping its resistance entirely and advancing to the next section. Cancels are the control-kill path: a deck with 2 cancels turns a 4-timer encounter into a 2-timer encounter.

## Approaches

Two approaches, representing a core strategic choice:

### Aggressive
- **+2 momentum per turn** (instead of +1)
- Starts with high momentum
- Wins by **burning through timer resistance fast**
- The berserker/powerhouse fantasy

The aggressive player is *intensely invested in the encounter deck*. Every draw matters — they're spending momentum on big cards and racing the clock. Having 12 momentum and drawing "+1 momentum" feels awful. Good gear means good draws which means efficient kills.

### Cautious
- **Draws 2 cards per turn** (instead of 1)
- Starts with low momentum
- Wins by **finding cancel cards** to skip timers entirely
- The expert/planner fantasy

The cautious player **barely cares about the encounter deck**. They're fishing for cancels and dumping spare momentum (and sometimes spirits) into Press to see even more cards — up to 4 per turn. The encounter openings are flavor text scrolling past while they hunt for answers.

### Why this works

The two approaches use completely orthogonal resources:
- Cancel builds don't need momentum (cancels are free or spirit-costed)
- Damage builds don't need extra card draw (most of their deck is playable)

The approach choice becomes a character-knowledge check: "I know my deck, I know which mode it wants." Axe berserker with all damage cards → aggressive. Sword duelist with cancel cards → cautious, dig for answers.

## Turn Flow

1. Active timer ticks down (fires if it hits 0: effect applied, countdown resets)
2. Momentum +1 (or +2 if aggressive)
3. Draw 1 opening (or 2 if cautious)
4. **Either**: play one opening, **or** Press/Force to draw 2 more cards immediately, then pick one of your cards to play
5. Progress applied to current timer's resistance
6. Timer resistance cleared → next timer activates
7. Last timer cleared → victory

### Press and Force

Press (costs 2 momentum) and Force (costs 2 spirits) each draw 2 additional cards immediately. You see your initial draw, decide it's not good enough, and dig for something better. You pick one card from your full hand (3 if normal draw, 4 if cautious draw) to play.

**Press and Force are mutually exclusive per turn.** You activate one draw-2. Can't do it again until next turn. This prevents degenerate turbo-dump cycles where a player burns 20 spirits cycling into their 2 cancel cards.

**Force is the emergency brake nobody wants to pull.** Spirits are ablative character health — the encounter is trying to take them from you via timer drain and Force costs. A good deck means you rarely Force. A bad deck means you're bleeding spirits every few turns just to find playable cards.

**You must always play a card.** There is no "pass" or "hang back" option. If you draw something you can't play (momentum gate too high) and can't afford to Press, you're forced to Force. Every turn costs *something*.

### Cautious digging

A cautious player's typical turn: draw 2, don't love either, Press for 2 more (spending momentum they don't need for gates anyway), pick the best of 4. On turns where Press is on cooldown or momentum is low, they see 2. This means cautious cycles through the deck extremely fast, finding every cancel reliably.

## Momentum Gates

Some openings require momentum ≥ N to play (without necessarily spending it). This makes momentum a **continuous throttle**, not just a currency:

- Low momentum = stuck with weak/free options
- High momentum = powerful cards unlock
- Spending momentum on a card can drop you below the gate for your next draw

The tension: do I spend momentum now for progress, or hold it to keep strong options available? This decision exists every single turn, making momentum management the core skill expression for aggressive players.

For cautious players, momentum is mostly fuel for Press. They'll take a free +momentum card if spirits are low and they have wiggle room on the timer, but they're not building toward gates.

## Deck Composition

### Always 15 cards. True deck draw.

Shuffle once at start. Draw without replacement. See every card once per cycle, then reshuffle. This rewards card counting and makes the system a hand management puzzle.

### Card pools by source

A character's deck is assembled from **separate pools**, each gated by a different piece of gear or skill:

**Skill pool** (0–4 cards): Your training. Governed by your skill level in the encounter's stat. Full access to all cards at or below your rank. Combat 4 means you know advanced techniques regardless of what you're holding.

**Weapon/gear pool** (1–5 cards): Your equipment. A +1 axe gives 1 card from the level 1 axe pool. A +4 scimitar gives 4 cards from the sword pool (levels 1–4). The weapon's level determines both how many cards it contributes AND which tiers of the weapon pool are available.

**Trinket pool** (0–1 cards): Rare findables with their own small card pool.

**Filler** (remaining slots to 15): Encounter-authored openings. Gated filler (requires rope, torch, etc.) is prioritized over generic scenery filler, which is prioritized over global chaff.

Example: Combat 4, +4 broadaxe, lucky buckle
- 4 cards from combat skill pool (ranks 1–4)
- 4 cards from axe pool (levels 1–4)
- 1 card from lucky buckle pool
- 6 filler cards

### Weapon type = tactical profile

The weapon pool determines the *kind* of cards you get, not just the power level. This is where gear identity lives:

- **Daggers**: cancel/control. The expert's weapon. Find openings, neutralize threats. Pairs naturally with cautious approach (dig for cancels). The assassin, the duelist, the precision fighter.
- **Axes**: aggro/damage, zero cancels. The berserker's weapon. Big momentum dumps, overwhelm before timers fire. Pairs naturally with aggressive approach (race the clock). The powerhouse, the brute.
- **Swords**: hybrid. Some progress, some cancel, best at neither. Works with either approach. The soldier, the adventurer.

All three weapon types scale from +1 to +5. A +4 axe and a +4 dagger both give 4 collection cards, but the axe deck is pure damage and the dagger deck is cancel-heavy.

Weapon cards are **additive**: a +3 weapon has the same cards as the +2, plus one new card at level 3. All weapons of the same type share the same card pool. Cancel card names are never shown (overwritten by the encounter's timer counterName).

**Dagger pool (cancel-focused):**
1. "Lunge forward and stab at their guard" = momentum_to_progress
2. (cancel) = momentum_to_cancel
3. "Circle your opponent, looking for a gap" = free_momentum
4. (cancel) = spirits_to_cancel
5. (cancel) = free_cancel

**Axe pool (aggro, zero cancels):**
1. "Swing the axe into their defense" = momentum_to_progress
2. "Shift your grip and ready a heavy swing" = free_momentum
3. "Put your weight behind a brutal chop" = momentum_to_progress_large
4. "Charge forward swinging wildly" = threat_to_progress_large
5. "Bring the axe down with everything you have" = momentum_to_progress_huge

**Sword pool (hybrid):**
1. "Test their guard with a quick cut" = momentum_to_progress
2. "Feint high and step back to recover" = free_momentum
3. (cancel) = momentum_to_cancel
4. "Commit to a powerful driving thrust" = momentum_to_progress_large
5. (cancel) = free_cancel

**Cancel sources**: only weapons (daggers, swords) and tokens provide cancel cards. Combat skill cards have no cancels.

### The +N = N cards model

A "+4 combat" item means "4 card slots from that weapon type's pool (levels 1–4)." This means:
- All three weapon types have full +1 to +5 progression (5 weapons each)
- Weapon type determines which pool you draw from
- Cards are additive — higher-level weapons strictly contain all lower-level cards plus a new one
- The shimmering blade (+5 sword) and the old tooth (+5 dagger) are the only items accessing the level 5 pool, so those cards are premium (free_cancel)
- Weapons are purely combat tools — no foraging bonus. Foraging uses the Bushcraft skill bonus stack.

### Weapon roster

| Level | Dagger | Axe | Sword |
|-------|--------|-----|-------|
| +1 | Bodkin (plains, 15gp) | Hatchet (forest, 15gp) | Falchion (plains, 15gp) |
| +2 | Jambiya (scrub, 15gp) | Tomahawk (forest, 15gp) | Short Sword (plains, 15gp) |
| +3 | Kukri (scrub, 40gp) | War Axe (forest, 40gp) | Tulwar (scrub, 40gp) |
| +4 | Hunting Knife (mountains, 80gp) | Broadaxe (mountains, 80gp) | Scimitar (scrub, 80gp) |
| +5 | The Old Tooth (unique) | Revathi Labrys (unique) | Shimmering Blade (unique) |

## Tier Difficulty Curve

The encounter always completes — the question is how much it **costs** you. Spirits are a session budget (start 20, recover at inns). A good encounter costs you 1-2 spirits. A bad one costs 5-6 and maybe a condition.

### T1: Tutorial
- 1-2 timers, spirits drain only (no conditions)
- Generous countdowns (5-6)
- Low resistance per timer
- No-skill grinder handles this. Costs some spirits, not dangerous.
- Some autopilot turns are fine — teaching the system.

### T2: Real stakes
- 2-3 timers, condition timers appear with generous countdowns (6-7)
- Skilled characters dodge conditions easily
- No-skill has 3-20% chance of eating a condition — a real risk that motivates better gear
- Common conditions: exhausted, freezing, injured

### T3: Scary and overmatched
- 3-4 timers, tight condition countdowns (4-5), brutal conditions
- No-skill characters basically cannot avoid conditions
- Even skilled characters need to play well
- Conditions: injured, irradiated, lattice_sickness, poison
- This is where good gear and the right approach choice matter most

### The key insight

The same encounter bones can serve all three tiers by changing **only the timer configuration**. Filler cards stay the same. The timer countdown and condition severity are what make T3 terrifying and T1 gentle.

### Condition timer sensitivity

The condition timer countdown is extremely sensitive. Simulation with a berserker (aggressive, R=6, 1 drain):
- cond@4: 57% get conditioned
- cond@5: 20% get conditioned
- cond@6: 3% get conditioned
- cond@7: <1% get conditioned

A single turn shifts the outcome from "trivial" to "coin flip." This is THE lever for difficulty tuning.

## Encounter Authoring

Encounters are primarily designed for the **aggressive player**. The 15 openings need to serve the damage-racing playstyle — things that feel good to smash with momentum. Cautious players are barely reading the card text; they're digging for cancels.

This thins out encounter design complexity considerably. The filler pool is mostly momentum→progress and free→small progress at various costs, with scene-appropriate flavor text. The author doesn't need to design trick decisions or complex multi-path card interactions.

### Authoring budget per encounter
- **Timer sequence**: the core creative work. How many timers, what effects, what countdowns.
- **~7-8 filler openings**: scene-grounded flavor text mapped to standard archetypes. These should reference things in the environment ("Use the fallen log as cover", "Shout over the howling wind").
- **Gated filler**: optional openings requiring specific items (rope, torch). Rewards smart packing.

The player's half of the deck (skill + weapon + trinket cards) comes from global pools authored once, not per-encounter.

### Target encounter length

Under 10 turns. A well-geared aggressive berserker should blow through 3 timers more often than not before any timer ticks. A grinder takes longer and eats some hits but still finishes.

## Encounter Archetypes

### Time Bomb
Severe clock, long horizon.
- Single long-countdown timer with brutal effect
- High resistance
- No cancels in filler — bring your own or race it

### Minefield
Normal clock, but some filler cards are bad (threat-cost or momentum-draining).
- 3-4 timers at moderate countdowns
- Filler pool mixes useful cards with risky ones
- Aggressive players need to manage bad draws; cautious players dig past them

### Gauntlet
Lots of short timers in sequence.
- 4-5 timers with low resistance each
- Fast transitions — each section is 2-3 turns
- Rewards consistent output over haymakers

## Advancement Feel

When returning to earlier challenges with better gear, the player should feel like they overpower it with minimal risk. When pushing into harder content (especially T3), it should feel scary and overmatched. That same content with good gear should feel challenging but possible.

Skill progression doesn't make encounters *possible* — it makes them *safe*. A Combat 3 berserker closes T1 in 3-4 turns per timer and never gets hit. A no-skill grinder gets through T1 too, just bruised. The difference is spirits left on the road.

## Fantasy Archetypes

### The Expert (Cautious)
The control-kill fantasy. The duelist, the stalker, the master assassin, the intricate debater.

Playstyle: goes cautious, digs through the deck, finds cancels, skips timers entirely. Satisfying play is dismantling an encounter piece by piece without ever being in real danger. Gear: swords (cancel cards), light armor, spyglass.

### The Powerhouse (Aggressive)
The damage-kill fantasy. The berserker, the fast-talker, the wraith, the acrobat.

Playstyle: goes aggressive, powers through timer resistance before it fires. Satisfying play is the momentum ramp into a huge hit that clears a timer in one card. Gear: axes (big momentum→damage).

Archetypes we don't have:
- The tank — slow but safe is just not gameplay we want
- The healer — solo, no one to protect

## Traverse Cancel Meta

Traverse encounters get their cancel cards from **gear-gated filler openings** in the encounter definition, not from weapons. The `.tac` format already supports `[requires has item_id]` on openings. This means traverse is tool-dependent rather than equipment-dependent — the right tool for the right situation.

Example: an encounter involving ancient grid defenses might have a cancel opening gated behind a specific scanning tool. That tool is useless everywhere else, but it's the key to trivializing this particular traverse. This creates a "right tool for the job" dynamic where smart packing matters more than raw combat stats.

This is the natural complement to the combat system where cancel comes from weapon type (daggers, swords). In traverse, cancel comes from the encounter author's filler pool.

## Open Questions

- Does the player choose collection cards when they have more available than their stat allows, or is it automatic (best N)?
- Exact momentum gate values per card archetype — determines whether cautious/aggressive lanes actually diverge in practice
- Per-tier filler composition — how many free/cheap cards in T1 vs T2 vs T3 filler pools
- Armor's contribution: active cards (collection), passive timer reduction, or both?
- Traverse variant: how does the cautious/aggressive split apply to the authored-path model?
- Token items (+1 stat, 1 card from their own pool) — keep or drop? "Weird" feel.
