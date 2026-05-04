# Super RPS — Combat Core

The prediction-game layer underneath all combat. Both sides commit three actions per turn to slots 1, 2, 3. Slots resolve simultaneously and in order. Identity comes from the base families plus stacked mutators — no new action rows or columns ever get added.

Working prototype: `experiments/triple-action/`.

## Turn Structure

1. **AI commits.** Each non-stunned slot rolls from the AI's action pool. Stunned slots are locked to `stunned`.
2. **Player sees tell.** One short line, derived from AI's commitment (see *Tell Logic*).
3. **Player sees plan** (conditional). If the player played `read` last turn, the full AI commitment is shown alongside the tell.
4. **Player commits.** Single keypress per slot. Stunned slots skip.
5. **Resolve slots 1 → 3.** For each slot, look up the family matchup, apply HP deltas (clamped to `[0, MaxHP]`), then apply any forward riders (stun next slot, etc.). If the would-be-affected slot is past slot 3, it bleeds into slot 1 of next turn.
6. **End of turn.** Forward stuns and "read active next turn" flags persist; per-turn pacing flags reset.

Player and AI never see each other's commitments before resolution, except through tell, read, or future specialists.

## Tell Logic

Computed from the AI's full three-slot commitment. First match wins:

| Condition | Tell |
|---|---|
| any attack-family with Telegraphed | [enemy] is preparing a heavy attack! |
| 2+ attack-family slots | [enemy] is pressing the attack |
| 2+ defend-family slots | [enemy] is on their back foot |
| 2+ heal-family slots | [enemy] is winded |
| otherwise | [enemy] is wary and awaits your move |

## Preview matrix
- Attack vs Attack: crossed swords - You and the enemy will both deal damage to each other
- Attack vs Defend: shield - You/they will take slightly less damage from the attack
- Attack vs Recover: stun - You/they will be stunned for one action
- Attack vs Read/Stunned: injury - You will take full damage
- All others: no-op - Your actions do not affect each other

We should carry these items over into the combat log, to reinforce them.

## Core Set

### Attack
Base:
- Deal 4 damage
- Against the Recover action, inflicts Stunned (ignore resistance) and cancels the Recover action

Mutators:
- Heavy: +4 damage
- Weak: -2 damage
- Riposte: Against Attack, prevent 2 damage, add +2 damage
- Brutal: Chance to inflict Injured
- Tainted: Chance to inflict Lattice Sickness
- Glowing: Chance to inflict Irradiated
- Venomous: Chance to inflict Poisoned
- Terrifying: Inflicts Fear
- Provoking: Inflicts Berzerk
- Stunning: Chance to inflict Stunned against any action
- Power: Only usable once per turn
- Slow: Only usable once every other turn
- Exhausting: Inflicts Stunned on self
- Telegraphed: Warns opponent on turn before

Examples:
- Heavy Terrifying Slow Telegraphed Attack: Orthos Laval blasts your mind with terrifying psychic horrors!
- Weak Riposte Attack: Thad Butterfield steps under your swing, striking swiftly.
- Stunning Power Attack: You slam the pommel of your sword down with a sharp crack.

### Defend
Base:
- Prevent 2 damage
- Gain +4 resist to core conditions

Mutators:
- Heavy: Prevent +2 damage
- Perfect: Prevent all damage
- Shielding: Prevent all statuses
- Stunning: Chance to inflict Stunned against Attack
- Power: Only usable once per turn
- Slow: Only usable once every other turn

### Recover
Base:
- Heal 4 damage

Mutators:
- Heavy: Heal +2
- Wary: If paired against an Attack, converts to basic Defend before resolution
- Shielded: Prevent 2 damage
- Power: Only usable once per turn
- Slow: Only usable once every other turn
- Enraging: If paired against an Attack, inflicts Berzerk on self

### Read
Not available to monsters
Base:
- On next planning step, view AI plan for next turn

Mutators:
- Wary: If paired against an Attack, converts to basic Defend before resolution

### Skipped
Not selectable, only the result of conditions. No effects.

## Conditions

### Stunned

Converts next action (either pending or carried to first action of next turn) to Skipped

### Berzerk

All actions selected in the next turn must be Attack if possible

### Fear

All actions selected in the next turn must be Defend or Recover if possible

## Player Weapon Philosophy

### Combat skill
- 0: daggers
- 2: axes
- 4: swords

### T-0 - no weapon equipped
Disable attack action

### T-1 - starter
- Basic attack

### T-2 - competent
- Uncomplicated upgraded attack

### T-3 - specialist
- Basic attack
- Complicated upgraded attack OR
- Uncomplicated upgraded non-attack

### T-4 - legendary
- Uncomplicated upgraded attack
- Complicated upgraded attack OR
- Uncomplicated upgraded non-attack

## Player Armor Philosophy

### T-0 - no armor equipped
- Basic defense
- No resists against conditions

### T-1 - starter
- Basic defense
- Basic resists against conditions

### T-2 - competent
- Uncomplicated upgraded defense
- Basic resists against conditions

### T-3 - specialist
- Basic defense
- Complicated upgraded defense OR
- Uncomplicated upgraded non-attack
- Varied resists against conditions

### T-4 - legendary
- Uncomplicated upgraded defense
- Complicated upgraded attack OR
- Uncomplicated upgraded non-attack
- Good resists against conditions

## Monster Move Philosophy

### Basic striker
- 18 HP
- Basic moves
- Limited deadly attack

### Basic duelist
- 18 HP
- Basic moves
- Limited powerful attack
- Limited powerful defense

### Basic tank
- 24 HP
- Basic moves
- Limited powerful defense
- Limited powerful recover

### Legendary striker
- 24 HP
- Basic defense
- Strong attack
- Limited deadly attack
- Limited powerful recover

### Legendary duelist
- 24 HP
- Convoluted defense
- Convoluted attack
- Limited powerful recover

### Legendary tank
- 28 HP
- Strong defense
- Strong attack
- Limited strong recover

## Item Movesets (proposal)

### Daggers — cancel/stun-focused, no Heavy

#### Hunting Knife (T-1)
- Attack

#### Jambiya (T-2)
- drop item

#### Kukri (T-3)
- Attack
- Stunning Power Attack

#### Fine Seax (T-3 high)
- Attack
- Riposte Attack — the parrying-knife identity

#### The Old Tooth (T-4)
- Riposte Attack
- Provoking Heavy Attack — the killing strike (huge damage, self-stuns next turn)

### Axes — brutal/heavy-focused, no fine work

#### Hatchet (T-1)
- Attack

#### Tomahawk (T-2)
- drop item

#### War Axe (T-3)
- Attack
- Heavy Power Attack — the committal heavy swing

#### Broadaxe (T-3 high)
- Attack
- Heavy Brutal Power Attack

#### Revathi Labrys (T-4)
- Heavy Attack
- Heavy Terrifying Slow Attack

### Swords — riposte/balanced

#### Falchion (T-1)
- Attack

#### Short Sword (T-2)
- Riposte Attack

#### Tulwar (T-3)
- drop item

#### Scimitar (T-3 high)
- Attack
- Heavy Power Defend

#### Shimmering Blade (T-4)
- Riposte Attack
- Heavy Wary Recovery

### Light Armor — evasion, not absorption

#### Tunic (T-0)
- Defend

#### Silks (T-1)
- Defend
- Wary Read

#### Hunter's Gear (T-1 high)
- drop item

#### Cartographer's Cloak (T-2)
- Deep Wary Read
- Power Wary Recover

#### Desert Scout Gear (T-3)
- drop item

#### Robe of Twilight (T-4)
- Power Heavy Wary Recover
- Perfect Slow Defend

### Medium Armor — balanced

#### Leather (T-1)
- drop item

#### Hide Armor (T-1)
- Defend
- Resist freezing
- Resist injury

#### Buff Coat (T-2)
- drop item

#### Lamellar (T-3)
- Defend
- Power Heavy Defend
- Resist freezing
- Resist injury

#### 17th Mountain Regiment Armor (T-4)
- Power Heavy Defend
- Power Wary Recover
- Resist freezing
- Resist injury

### Heavy Armor — absorption, no movement riders

#### Gambeson (T-1)
- Defend
- Resist injury

#### Chainmail (T-1 high)
- drop item

#### Scale Armor (T-2)
- Heavy Defend
- Resist injury

#### Brigandine (T-3)
- Defend
- Power Heavy Shielding Defend
- Resist injury

#### Golem Armor (T-4)
- Heavy Defend
- Perfect Power Defend
- Resist injury

