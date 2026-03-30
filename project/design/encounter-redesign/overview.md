# Encounter System Overview

Encounters are short (3-7 turn) resource-management puzzles. The player manages **Momentum** (self-replenishing), **Spirits** (non-renewable), and progress against **Resistance** (the encounter's "health") while **timers** create recurring pressure.

Two variants share the same core economy:

- [**Combat**](combat.md) — reactive. One opening per turn, decided blind. Approach selection (Scout/Direct/Wild) sets starting conditions.
- [**Traverse**](traverse.md) — proactive. A visible queue of upcoming openings lets the player plan a route. No approach selection; the encounter sets the parameters.

## Shared Mechanics

**Openings** are the atomic unit of player action. Each has a cost (Momentum, Spirits, tick a timer, or free) and an effect (damage Resistance, stop a timer, gain Momentum). Some openings require specific gear.

**Timers** (threats/hazards) count down each turn. When they fire, they drain Spirits or restore Resistance, then reset. Stopping a timer permanently removes it.

**Press the Advantage** (-2 Momentum) and **Force an Opening** (-2 Spirits) both sacrifice a turn to receive 3 openings next turn.

**Endings**: Resistance = 0 (attrition win), all timers stopped (control win), Spirits = 0 (loss/flee).

## Encounter Bundles

Some situations present an initial choice that branches into separate encounters:

> *There are bandits on the road ahead.*
> - Challenge them to a fight *(violence / combat)*
> - Sneak around them *(stealth / traverse)*
> - Parley with them *(negotiation / combat)*

Each branch is a full encounter with its own timers, openings, and difficulty. The bundle groups them under shared introductory text and an initial choice.

A bundle can contain a single encounter (no choice — you're in it) or several. The intent tags on each branch (violence, stealth, negotiation, etc.) are cosmetic/narrative; the variant (combat or traverse) determines the mechanics.

## Data Structure

### Encounter file

```
encounter:
  name: "Bandit Roadblock"
  variant: combat           # combat | traverse
  intent: violence          # narrative tag: violence, stealth, negotiation, endurance, etc.

  intro: |
    Three figures step out from behind the overturned cart,
    blades already drawn. They've done this before.

  # Starting conditions
  resistance: 8
  spirits: 10
  # Momentum (combat): set by approach selection, not authored in stats
  # Queue depth (traverse): determined by player's base governing skill (0–4)

  # Timer pool — encounter randomly selects N from this list
  timer_count: 3
  timers:
    - name: "Flanking maneuver"
      effect: spirits
      amount: 2
      countdown: 4
    - name: "Rallying cry"
      effect: resistance
      amount: 2
      countdown: 5
    - name: "Closing trap"
      effect: spirits
      amount: 1
      countdown: 3
    - name: "Reinforcements"
      effect: resistance
      amount: 1
      countdown: 4

  # Opening pool — encounter randomly selects from this list each turn
  openings:
    - name: "Exposed flank"
      cost: { type: momentum, amount: 2 }
      effect: { type: resistance, amount: 3 }
    - name: "Moment of doubt"
      cost: { type: free }
      effect: { type: momentum, amount: 2 }
    - name: "Read their formation"
      cost: { type: spirits, amount: 1 }
      effect: { type: stop_timer }
      requires: [perception]    # gear tag — excluded if player lacks this
    - name: "Disrupt their signals"
      cost: { type: tick }
      effect: { type: resistance, amount: 2 }
      requires: [jammer]

  # Approaches — combat only
  approaches:
    scout:
      momentum: 0
      timer_count: 3
      bonus_openings: 3       # openings on turn 1
    direct:
      momentum: 3
      timer_count: 3
    wild:
      momentum: 6
      timer_count: 4

  # What happens on Spirits = 0
  failure:
    text: |
      You stagger back, bloodied. They take what they want
      from your pack and leave you in the dirt.
    mechanics:
      - lose_item: random
      - spirits_recovery: 3   # you wake up with 3 Spirits, not 0
```

### Bundle file

```
bundle:
  name: "Bandits on the Road"
  intro: |
    The road ahead narrows between two rocky outcrops.
    You spot movement — three figures, armed, waiting.

  branches:
    - label: "Challenge them"
      intent: violence
      encounter: bandit_roadblock_fight

    - label: "Sneak around through the rocks"
      intent: stealth
      encounter: bandit_roadblock_stealth
      requires: [light_armor]   # branch-level gating

    - label: "Call out to parley"
      intent: negotiation
      encounter: bandit_roadblock_parley
```

### Key decisions in the data model

**Pools, not sequences.** Timers and openings are pools that the engine draws from randomly. The authored list is the possibility space; the engine selects a subset each encounter. This means the same encounter plays differently each time without requiring multiple authored versions.

**Gear-gated openings.** The `requires` field on an opening means it's excluded from the pool at runtime if the player doesn't have matching gear tags. This keeps the pool honest — you never see an opening you can't conceptually attempt. The encounter still works without gated openings; they're bonuses for prepared players.

**Gear-gated branches.** Bundle branches can also have `requires` — you can't sneak past the bandits in plate armor. Gated branches are hidden entirely, not shown-and-disabled.

**Failure is authored.** Each encounter defines what happens when Spirits hits 0. This is narrative (what the player sees) plus mechanics (item loss, condition, spirits recovery, etc.). Fleeing mid-encounter could use the same failure block or a softer variant.

**Approaches are per-encounter (combat only).** The three approaches (Scout/Direct/Wild) exist on every combat encounter, but their parameters can vary. A tough combat might give Scout only 2 bonus openings instead of 3. A chaotic combat might give Wild 5 timers instead of 4.

**Intent is flavor, variant is mechanics.** A "negotiation" encounter might be a combat (back-and-forth, reactive, reading your opponent) or a traverse (you can see the argument ahead and plan your approach). The intent tag drives narrative; the variant drives the game loop.
