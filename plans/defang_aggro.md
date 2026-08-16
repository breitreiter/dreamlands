---
kind: plan
title: "Defang aggro — closing the RPS triangle"
state: exploring
created: 2026-08-16
updated: 2026-08-16
status: Findings + measured lever comparison. Recommends one engine change (Defend counter-damages Attack) validated by experiment, plus one content lever that ships without engine work. Nothing implemented; the engine experiments were run locally and reverted.
touches:
  files:
    - lib/Combat/Resolver.cs (the recommended change)
    - text/encounters/combat/**.fight (the content lever)
    - ui/web/src/screens/Combat.tsx (narration for a countered attack)
  features: [combat, balance]
related:
  - plans/rps_combat_harness.md (the harness these numbers come from, and its §8 caveat)
  - imp/reference/super_rps.md (design reference for moves and mutators)
---

# Defang aggro

Attack-spam is the dominant strategy against every fight in the corpus. Measured
with `tools/combat-harness`: a policy that attacks every slot and never defends
wins **88.2%** of fights in starting gear, against **31.4%** for control and
**41.5%** for turtle.

Worse, sophistication is *punished*: an aggro policy that reads the tell and
defends against a telegraphed heavy does **worse** than one that ignores the tell
entirely (79.4% vs 88.2%). Whatever the player is being asked to learn, the game
is not currently rewarding it.

## 1. Root cause: Attack never loses

Walk the pairings in `Resolver.cs` as they stand:

| player \ monster | outcome |
|---|---|
| Attack vs **Recover** | attack deals 4, cancels the heal, and **always** stuns (overrides resistance, `Resolver.cs:157`) — a swing of ~12 |
| Attack vs **Defend** | attacker still deals 2 (4 damage less 2 prevention). Chip, but no punishment |
| Attack vs **Attack** | even trade — and the player's 24-point pool beats every monster's HP but one |
| Defend vs **Attack** | prevents 2, deals 0. Costs a slot worth 4 damage to save 2 |

**Nothing punishes Attack.** It beats Recover outright, chips through Defend, and
trades evenly with itself — and the player wins even trades because a 24-point
pool (20 spirits + 4 health, ablating 1:1) exceeds the HP of 18 of 19 fights.
Defending forgoes 4 damage to prevent 2, so it is never correct.

This is not an RPS triangle. It is one move that is never wrong.

## 2. Levers, measured

All numbers are mean win% across all 19 fights at **T0 dagger/light lower**
(starting kit — the case that matters most for a new player), 400-500 trials/cell.

### Engine levers

| variant | berserk | aggro | control | turtle | spread |
|---|---|---|---|---|---|
| **baseline** | **88.2** | 79.4 | 31.4 | 41.5 | ~57 |
| Defend prevention 2 → 4 | 83.1 | 77.3 | 42.3 | 50.9 | ~41 |
| Defend counter-damage 2 | 82.0 | 77.2 | 46.2 | 51.6 | ~36 |
| **Defend counter-damage 4** | 67.5 | **70.6** | 61.7 | 62.4 | **~9** |

### Content levers (single monster, old_bram, berserk win%)

| variant | berserk | note |
|---|---|---|
| baseline | 93.4 | |
| + `Riposte Attack` | **79.6** | works, -13.8; mutual kills 3.4% → 6.8% |
| + `Stunning Defend` | 94.6 | **inert** |
| + both | 79.4 | riposte is doing all the work |
| hp 18 → 28 | 74.2 | **backfires — see below** |

## 3. Recommendation: Defend counter-damages Attack, at 4

One change in `Resolver.cs`: a plain Defend meeting an Attack deals damage back
equal to a base attack.

This is the only lever tested that actually fixes the problem rather than
scaling it. At counter-damage 4:

- **Attack-spam stops being best.** Tell-reading aggro (70.6%) overtakes mindless
  berserk (67.5%) — the game starts rewarding attention.
- **The spread collapses from ~57 points to ~9.** All three archetypes land
  within a few points of each other, which is the "different strategies are
  viable" property the design wants.
- **The triangle actually closes:**
  Attack beats Recover (cancel + stun) → Recover beats Defend (free heal against
  a guard that has nothing to block) → Defend beats Attack (counter). Each move
  beats one and loses to one. That is the game the format promises.

Counter-damage 2 is not enough (berserk still 82.0). The counter has to be a
real trade, not a scratch.

**Open sub-decision:** whether to also raise prevention 2 → 4 so a defender takes
nothing at all from a basic attack, or leave them taking 2 so defending is still
a slow bleed. Prevention 4 alone was tested and is weaker than counter 4; the two
combined were not tested.

## 4. Do NOT use monster HP

The obvious lever is the wrong one. At hp 18 → 28, berserk falls to 74.2% — but
control collapses from 16.7% to **3.3%** and turtle from 25.8% to 9.9%.

HP bloat punishes low-damage strategies hardest, so it makes aggro *more*
dominant in relative terms while making every fight longer. It is the lever most
likely to be reached for and it moves the design backwards.

## 5. Ships without engine work: put riposte on monsters

`Riposte Attack` on a monster drops berserk by ~14 points and roughly doubles the
mutual-kill rate — which is the "I traded and got wrecked" texture that pure
aggression should have.

The anti-aggro tools already exist in the engine and are almost entirely
unauthored. Across the whole 19-fight corpus the monster-side mutator counts are:

    heavy 33, slow 17, power 13, telegraphed 8, tainted 4, brutal 4,
    glowing 3, stunning 2, riposte 1, shielding 1, venomous 1, terrifying 1

**`riposte` appears once. `shielding` appears once.** The counter-punch tools are
sitting unused while every monster leans on `heavy`. Even without the engine
change, seeding riposte through the corpus would blunt the worst of it.

Caveat: riposte also drove control down (16.7% → 4.1%), so on its own it makes
fights harder without making them more interesting. It is a mitigation, not the
fix.

## 6. `Stunning Defend` is inert — worth a look

Adding it moved berserk by **+1.2 points**, i.e. nothing. It procs at 50%
(`ConditionProcChance`) and only converts one enemy slot to Skipped, which
against a 3-slot commit is worth ~4 damage half the time. Either the proc rate or
the effect is too small to matter. Worth deciding whether it should exist at all.

## 7. Caveats

**These numbers are a lower bound on optimal play.** They come from four
hand-written policies (see `plans/rps_combat_harness.md` §8, where three
different hand-written policies gave three different answers for one fight). A
sharper policy might find a way to break counter-damage 4 that these do not. The
change should be re-measured, not assumed, after any policy work.

**This is a large balance change.** Every authored fight was tuned — insofar as
anything was tuned — against a resolver where defending was worthless. Expect
fight lengths to move and the T3 fights to need re-checking.

**It needs narration.** A countered attack has to read as something in the log
and in `Combat.tsx`, or players will see damage appear with no explanation.

**It does not address the reader's missing counter.** A monster that never
recovers still punishes control, and none ships (see
`plans/rps_combat_harness.md` §3). That is a separate, deliberate design question.

## 8. Suggested order

1. Prototype counter-damage 4 in `Resolver.cs`, re-run the harness, confirm the
   spread collapse holds across all bands and entry-spirit levels.
2. Decide the prevention sub-question (§3).
3. Narration for countered attacks.
4. Re-check the T3 fights and fight lengths against the new resolver.
5. Independently, seed `riposte` through the monster corpus (§5) — useful with or
   without the engine change.
