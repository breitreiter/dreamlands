# Card Archetypes

Canonical opening templates. Every card in the game should be a reskin of one of these. If you need a new archetype, add it here first.

Notation: `cost → effect`. Free has no cost listed.

## Core concepts

### Encounter types

#### Conflict
In these encounters, the PC is facing an active, agentic threat which wants to defeat them. This may be an actual fight, a tense debate.

Variants:
- Fight: Conflict + Combat
- Debate: Conflict + Negotiation

#### Traverse
In these encounters, the PC must traverse a challenging or dangerous path. This could be sneaking through an enemy camp, climbing a dangerous cliff, or navigating a haunted ruin.

Variants:
- Sneak: Traverse + Cunning
- Navigate: Traverse + Bushcraft

### Momentum 
Represents the PC committing to powering through the encounter. It represents their ability to snowball and power through obstacles. 

Things in the fiction which generate momentum:
- Opening with an all-out assault
- Striking with a flurry of blows to overwhelm the opponent
- Striking an opponent's guard *hard* to stagger them
- Picking up your pace to move through a hazard
- Distracting your opponent
- Setting up a logical trap for your debate opponent

Things in the fiction which sap momentum:
- Striking a staggered opponent to wound them
- Hitting a surprised or overwhelmed opponent
- Pushing hard to reach a safe point
- Hiding and waiting for a patrol to pass
- Revealing the inevitable conclusion of your argument

### Progress
Represents the PC's progress through the encounter. How close they are to resolving the situation in their favor.

Things in the fiction which generate progress:
- Killing or wounding an enemy
- Exhausting or frightening an enemy
- Reaching a key milestone on a dangerous traverse
- Completing a powerful argument in your favor

Things in the fiction that sap/reverse progress:
- Enemies having a moment to recover
- Having to backtrack
- The next step being unexpectedly hard (last 20% turns out to be last 80%)
- Your opponent making an excellent point
- Your audience losing interest

### Threats
Timers which, when they fill, result in a bad effect for the PC. The PC may decide to try to disable timers, race them, or most commonly a mix of both.

Things in the fiction which disable timers:
- Disarming an opponent
- Killing a named opponent
- Breaking a formation
- Sneaking past a sentry
- Navigating through a danger
- Convincing someone to stand down

Things in the fiction which accellerate timers:
- Ignoring an opponent
- Pausing for too long
- Making a clumsy argument or faux pas

## Progress cards (damage resistance)

These are the workhorses. Most decks are mostly progress cards.

### free_progress_small
- Cost: free
- Effect: +1 progress
- Role: Chaff

### momentum_to_progress
- Cost: -1 momentum
- Effect: +2 progress
- Role: Bread and butter

### momentum_to_progress_large
- Cost: -2 momentum
- Effect: +3 progress
- Role: Power card

### momentum_to_progress_huge
- Cost: -3 momentum
- Effect: +5 progress
- Role: Finisher

### spirits_to_progress
- Cost: -1 spirits
- Effect: +3 progress
- Role: High-risk progress

### spirits_to_progress_large
- Cost: -2 spirits
- Effect: +5 progress
- Role: Emergency finisher

### threat_to_progress
- Cost: 🠻 threat
- Effect: +2 progress
- Role: Tempo play
- Progress at the cost of timer pressure.

### threat_to_progress_large
- Cost: 🠻 threat
- Effect: +3 progress
- Role: Aggressive tempo
- Bigger payoff, bigger gamble.

## Momentum cards (fuel generation)

These set up future turns. The "ramp" cards.

### free_momentum_small
- Cost: free
- Effect: +1 momentum
- Role: Chaff
- Testing the opponent's guard, feeling out the terrain. Small setup.

### free_momentum
- Cost: free
- Effect: +2 momentum
- Role: Solid utility
- Good card in any deck. Enables momentum_to_progress next turn.

### threat_to_momentum
- Cost: 🠻 threat
- Effect: +2 momentum
- Role: Risky ramp
- Trading timer pressure for future power.

### spirits_to_momentum
- Cost: -1 spirits
- Effect: +3 momentum
- Role: Spirits-to-momentum conversion
- Converts non-renewable to renewable. Painful but sometimes necessary.

## Stop-threat cards (counter timers)

These are the control cards. Must be brought via collection — not free.

### momentum_to_cancel
- Cost: -2 momentum
- Effect: ×threat
- Role: Core control
- Neutralize a threat by spending your built-up advantage. Costs a full momentum_to_progress's worth of momentum.

### spirits_to_cancel
- Cost: -1 spirits
- Effect: ×threat
- Role: Emergency control
- When you can't afford the momentum.

### free_cancel
- Cost: free
- Effect: ×threat
- Role: Premium control
- Best card in the game. Very rare — only on top-tier gear.

## Flavor names by encounter type

Same 15 archetypes, reskinned per encounter type. Items and skills contribute archetype cards; the engine picks the flavor name matching the encounter type.

### free_progress_small
- Fight: Jab
- Debate: Pointed Remark
- Sneak: Inch Forward
- Navigate: Careful Step

### momentum_to_progress
- Fight: Slash
- Debate: Sharp Rebuke
- Sneak: Slip Past
- Navigate: Scramble Across

### momentum_to_progress_large
- Fight: Cleave
- Debate: Damning Evidence
- Sneak: Sprint Between Cover
- Navigate: Power Through

### momentum_to_progress_huge
- Fight: Haymaker
- Debate: Closing Argument
- Sneak: Ghost Through
- Navigate: Leap of Faith

### spirits_to_progress
- Fight: Reckless Lunge
- Debate: Bold Claim
- Sneak: Brazen Dash
- Navigate: Force the Crossing

### spirits_to_progress_large
- Fight: Death Blow
- Debate: Bare Your Soul
- Sneak: Now or Never
- Navigate: Last Reserves

### threat_to_progress
- Fight: Press Attack
- Debate: Talk Past Them
- Sneak: Ignore the Noise
- Navigate: Ignore the Signs

### threat_to_progress_large
- Fight: All-Out Assault
- Debate: Inflammatory Accusation
- Sneak: Run For It
- Navigate: Charge Ahead

### free_momentum_small
- Fight: Test Guard
- Debate: Feel Them Out
- Sneak: Watch the Pattern
- Navigate: Read the Terrain

### free_momentum
- Fight: Feint
- Debate: Build Your Case
- Sneak: Time Their Rounds
- Navigate: Find Your Footing

### threat_to_momentum
- Fight: Overextend
- Debate: Give Them Rope
- Sneak: Tune It Out
- Navigate: Press On Regardless

### spirits_to_momentum
- Fight: Second Wind
- Debate: Swallow Your Pride
- Sneak: Steady Your Nerves
- Navigate: Grit Your Teeth

## Chaff pool (global, used to fill decks)

These are the 6-8 generic cards the engine draws from when the deck needs padding. They're all playable but none are exciting.

These need to be carefully skinned to the encounter so they feel natural in the fiction. We should avoid generic "lunging strike" type names, instead favoring "Lunge at the bandit" or "Cite imperial law" or "leap to the next stone."

## Card sources and deckbuilding

A character's collection comes from three additive sources. The deck is collection + chaff padding.

Card sources:
- Base skill: 0–4 cards (one per skill level)
- Weapon / gear: 1–5 cards (depends on item quality)
- Lucky charm: 0–1 card (rare findable)

Maximum collection size: 10 cards (4 skill + 5 weapon + 1 charm). Deck is always 15; remainder is chaff.

### Combat skill (4 cards)

In general, core skills should offer a range of cards to fill utility slots and patch over gear-based weakness.

Each point in Combat adds one card. Higher levels unlock stronger archetypes. Cumulative.

- Combat 1: "Zornhau - wrathful strike" = momentum_to_progress
- Combat 2: "Nachdrängen - take initiative" = spirits_to_momentum
- Combat 3: "Überlaufen - fearless strike" = threat_to_progress
- Combat 4: "Scheitelhau - inevitable end" = momentum_to_progress_large

### Weapon: shimmering blade (5 cards)

In general:
- Axes should be big, inefficient blows. Slow to wind up but devastating
- Daggers should be quick and efficient, but struggle to convert momentum into big payouts
- Swords should balance, taking the best of both. They are the optimial martial weapon.
- Starter weapons should just offer 1-2 predictable mechanics
- Mid-tier weapons should have some distinctive character, mixing core utility with interesting options
- End-game weapons should be generally useful but also expose powerful options
- Cards should have muscular but legible names that communicate their function

An endgame weapon. Each card represents a distinct technique the weapon enables.

- "Quick Cut" = free_progress_small
- "Radiant Strike" = momentum_to_progress
- "Disrupting Slash" = momentum_to_cancel
- "Searing Arc" = momentum_to_progress_huge
- "Banishing Edge" = free_cancel

### Lucky charm: rabbit's foot (1 card)

In general, charms should be rare pulls that feel delightful but not overwhelming

- "Lucky Break" = spirits_to_progress

### Full collection (10 cards)

Sorted by source:

From Combat skill:
- "Zornhau" = momentum_to_progress
- "Nachdrängen" = spirits_to_momentum
- "Überlaufen" = threat_to_progress
- "Scheitelhau" = momentum_to_progress_huge

From shimmering blade:
- "Quick Cut" = free_progress_small
- "Radiant Strike" = momentum_to_progress
- "Disrupting Slash" = momentum_to_cancel
- "Searing Arc" = momentum_to_progress_huge
- "Banishing Edge" = free_cancel

From rabbit's foot:
- "Lucky Break" = spirits_to_progress

Deck: 10 collection + 5 chaff. Strong draws most turns. Two momentum_to_progress copies, a huge finisher, and both cancel types available. Chaff is rare enough that a bad draw just means a slower turn, not a dead one.
