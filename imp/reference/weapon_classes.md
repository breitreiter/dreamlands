---
kind: reference
title: "Weapon Classes (RPS)"
created: 2026-04-27
updated: 2026-04-27
status: current
touches:
  features: [combat, weapons, rps]
provenance:
  author: migration:M-001
---
# Weapon Classes

Status: all three classes locked at the mechanic level (Sword stances, Axe
momentum, Dagger Edge). Numbers still need a sim pass. Companion to
`combat_pivot.md` — locked decisions can collapse back into the pivot doc.

## Foundation: Enemy Intent Preview

Combat assumes a Slay-the-Spire-style intent preview: each turn the enemy
telegraphs what they're about to do (Attack / Big Attack / Defend / Power Up /
etc.). This is load-bearing for every weapon class — sword stances, dagger
exploits, axe Block timing all depend on the player being able to read what's
coming.

Without intent preview, sword stance choice is blind, dagger Exploit has no
trigger, and axe Block is just a coin flip. With it, every weapon has the same
underlying decision shape: *given what the enemy is about to do, what's the
right action this turn?*

This belongs in the pivot doc as a foundational mechanic, not just a UI
affordance.

## Cross-Class Triangulation

Each class differentiates along three axes: which player skill it couples to,
which Spirits relationship it has, and which armor weight it pairs with.

| Weapon | Skill coupling | Per-turn rhythm | Natural armor |
|--------|---------------|-----------------|---------------|
| Sword  | Combat | Moment-to-moment (no banking, stance toggle each turn) | Medium |
| Dagger | Combat | Bursty saw-tooth (silent bank, then explode) | Light |
| Axe    | Combat | Gradient ramp (slow build, catastrophic reset) | Heavy |

All three classes use Combat for to-hit. Differentiation lives in the
**rhythm shape** of their per-turn decisions, not in stat coupling. Each
class has a distinct tempo, all driven by the same intent-preview question
("what's the enemy doing next?").

Spirits is neutral for all three — earlier proposals (axe Fury at 0 Spirits,
dagger Nimble fading with Spirits) all got cut. Combat is fast enough that
overlaying a Spirits relationship adds bookkeeping without payoff.

The armor pairings give Light armor a defensive identity via the Cunning
save (see below) and stop the "wear silk on the road, die" problem from the
pivot doc.

## Cunning Saves Against Damage (foundational)

Cunning is the system's universal stat for **saving against unavoidable
damage** — when something would otherwise hit you and you'd eat it, Cunning is
your save. This generalizes well past combat: trap saves, area-effect dodges,
falling damage, and any future "you should have died but you rolled well"
moment land on Cunning.

The first and most prominent application is **Light armor's defense**. Light
armor doesn't give a high static AC; instead, when a monster's attack roll
meets or beats the wearer's (low) AC, the player makes a Cunning save vs DC =
the attack roll. Success negates the hit. Failure takes full damage. This
delivers the "as long as I keep hitting my Cunning roll, I'm untouchable"
fantasy.

Because Cunning is doing this universal job, **weapon classes should not lean
on Cunning as their identity**. The skill is already pulling its weight
defensively across the whole system; doubling up on it for the dagger class
crowds out other ideas.

## Swords — Stances (locked)

Three persistent stances, switched freely at the start of the player's turn.
Resets to Balanced at encounter start.

| Stance | To-hit | AC |
|--------|--------|----|
| Aggressive | +2 | -2 |
| Balanced   | 0  | 0  |
| Defensive  | -2 | +2 |

Per-turn decision: read the intent preview, pick the stance that fits.
Aggressive when the monster is winding up a big attack but not striking this
turn (free damage). Defensive when a Big Attack is incoming. Balanced as the
default trade.

No additional moves at base tier. Resist bolting on Riposte / Press / etc. —
the stance toggle is the whole identity. Higher tiers might unlock stance
modifiers, but that's a later pass.

Multiple weapons in the class, D&D-mapped:

- Shortsword: 1d6
- Longsword: 1d8
- (Greatsword 2d6, rapier 1d8, scimitar 1d6 if we add them later.)

Tier adds a flat bonus on top, see Tier Scaling below.

## Axes — Momentum (locked)

Press-your-luck stacking damage bonus. No spend-momentum moves; momentum is
purely a passive ramp that you protect.

- Each consecutive **attack action** adds +1 to damage (stacking, cap TBD —
  probably +5).
- **Missing** the attack roll does **not** reset momentum. Only choosing a
  non-attack action resets.
- **Block** action: skip attack, gain a flat AC bonus (+4 working number)
  until your next turn, lose all momentum.
- Item use, flee attempts, and any other non-attack action also reset.

Per-turn decision: read the intent preview, decide whether the incoming threat
is worth the momentum reset. Big Attack telegraphed → Block, eat the reset.
Defend or Power Up telegraphed → keep swinging, ramp continues.

Multiple weapons in the class, D&D-mapped:

- Hand axe: 1d4
- Battle axe: 1d8
- (Greataxe 1d12 if we add a two-hander later.)

Momentum is the axe identity, applied on top of whatever weapon you wield;
tradeoff is that any defensive turn costs everything you've built.

Earlier candidates (Windup cadence, Fury at 0 Spirits) are dropped. Momentum
covers the "barbarian who keeps swinging" fantasy more cleanly without dragging
in a Spirits relationship that fights with the dagger's.

## Daggers — Edge (locked)

Press-your-luck **inverted from the axe**. Where the axe ramps damage by
attacking and resets on defend, the dagger ramps **Edge** by evading and
spends it on attack.

The vibe is knife-fighting across martial traditions: circling, watching,
slipping the strike, then the burst when the moment comes. Both fighters
bleed if you stand toe-to-toe; the win condition is reading the opening,
not measuring the exchange.

### Mechanics

- **Evade action:** +3 AC this turn, +1 Edge. Always succeeds (no roll). Eats
  the turn — no attack.
- **Edge cap:** 3. You can't bank past full.
- **Attack action:** roll **N+1 attacks** at 1d4 + tier (where N = current
  Edge). Each attack rolls to hit, crits, and rolls bleed independently.
  Edge resets to 0 after.
- **Bleed (per hit):** 25% chance to apply a bleed. Bleed deals 1d4 at the
  start of the monster's turn for 2 turns. Multiple bleeds stack additively.
- **Surprise integration:** winning the Bushcraft surprise check at encounter
  start gives the dagger user **2 banked Edge** instead of just first-strike
  initiative. Daggers reward ambush more than other weapons.

### Per-turn decision

The intent preview drives an "evade or strike?" question every turn:

- Big Attack telegraphed → Evade. AC bump softens the hit, Edge ramps for
  later.
- Defend / Power Up / non-attack telegraphed → Strike. Free turn, no AC
  loss.
- Normal Attack telegraphed → judgment call based on current Edge, your
  HP, monster HP.

The deeper decision is *when to commit*. Too early and you waste banked
Edge on overkill or a missed flurry; too late and the monster grinds you
down before you swing. Bleed adds a third lever: striking early can plant
DOT damage that ticks while you re-bank, opening up "early DOT then big
finisher" plays alongside "max bank, single burst."

### Damage profile

A tier-3 dagger over a 4-turn cycle (3× Evade, 1× Attack with 3 Edge):

- 4× 1d4+3 = 16–28 damage burst, avg ~22
- 1−(0.75)⁴ = ~68% chance of at least one bleed (~3 avg follow-up)
- 1−(0.95)⁴ = ~18% chance of at least one crit
- Cycle DPS ≈ 6/turn averaged, comparable to a sword's 1d8+3 turn-by-turn
  but concentrated into spikes

This makes daggers feel mechanically distinct without being numerically
imbalanced. Variance is high; consistency is low; rhythm is unique.

### Why this captures the knife vibe

- **Flurry, not stroke:** N+1 attack rolls per turn maps to the multiple-cuts
  reality of knife combat across traditions.
- **Opportunism:** the bank/spend rhythm models circling and waiting for
  the opening. Strike at the wrong moment and you've wasted your setup.
- **Ambush primacy:** surprise gives banked Edge, so winning Bushcraft is
  way more valuable for daggers than for swords/axes. Sicarii built in.
- **Anatomical wounds:** bleed-on-hit captures the "wound that keeps
  cutting" theme. Knives leave damage that lingers.
- **The committed weapon:** strike turns are 0 AC contribution — you're
  inside, you can't dodge, you're committed. Mutual destruction is the
  default if the strike doesn't land.

### Cut from earlier passes

- **Cunning scaling (Nimble passive, Cunning gambit):** Cunning was
  generalized to the universal damage-save stat, so dagger identity moved
  off Cunning entirely.
- **Exploit (intent-triggered bonus damage):** subsumed by the broader
  Edge mechanic — every turn already involves reading the enemy and
  deciding whether to bank or strike.
- **Improved crit range / Fresh-Spirits decay:** unnecessary once Edge
  carries the class. Don't pile on flavor.
- Multi-attack: do daggers fire more times per turn, or once with a bigger
  effect? Both have rogue precedent; they pull the design in different
  directions.

## Tier Scaling

OGL-style: **base damage die is fixed per specific weapon**, tier adds a flat
to-hit and damage bonus on top. We do not scale die sizes with tier — that's
the whole point of being OGL-shaped.

Each class has multiple weapons with D&D-mapped dice:

| Class  | Weapons (base die)                                  |
|--------|-----------------------------------------------------|
| Sword  | shortsword (1d6), longsword (1d8)                   |
| Dagger | dagger (1d4)                                        |
| Axe    | hand axe (1d4), battle axe (1d8)                    |

Tier ladder (current +1 to +5) applies as flat bonuses to attack rolls and
damage, exactly like a +1/+2/+3 magic weapon in D&D. A tier-3 longsword does
1d8+3 damage; a tier-5 hand axe does 1d4+5.

**Within-class progression is curated.** The ItemDef table is hand-authored
(roughly five entries per class). Smaller-die weapons live at lower tiers,
bigger-die weapons at higher tiers — players upgrade through the slots, they
don't pick between sidegrades. We don't need a finesse-style mechanic to make
hand axes or shortswords "interesting" because we just don't put a tier-5
hand axe in the table.

Sketch of how an axe progression might look (illustrative, not committed):

| Slot | Weapon      | Damage  |
|------|-------------|---------|
| 1    | hand axe    | 1d4+1   |
| 2    | hand axe    | 1d4+2   |
| 3    | battle axe  | 1d8+1   |
| 4    | battle axe  | 1d8+3   |
| 5    | battle axe  | 1d8+5   |

The "rebase" from hand axe to battle axe at slot 3 is a meaningful upgrade
(d4 → d8 jump dwarfs a +1) without violating OGL conventions. Each weapon
class can pick its own pacing.

## References

- `combat_pivot.md` — parent design doc, locked-in decisions
- `armor_classes.md` — armor identity (Light/Medium/Heavy + biome affinity)
- `dice_mechanics.md` — d20 + skill vs DC foundation
- `project_spirits_mechanics.md` — Spirits-as-buffer model
