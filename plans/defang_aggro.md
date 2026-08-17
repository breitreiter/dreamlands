---
kind: plan
title: "Defang aggro — closing the RPS triangle"
state: active
created: 2026-08-16
updated: 2026-08-17
status: SHIPPED to lib in f7934ae (cap + counter 4), with tests rewritten from the design and the UI updated. Re-measured across all 6 gear bands and 4 entry-spirit levels: the inversion holds everywhere and the best-policy column now varies instead of being aggro in 57/57 cells. First play signal 2026-08-17: the counter reads as too strong to a player who was never told it exists (§9) — fix the reference and the missing narration before touching the numbers. Remaining: imp/reference/super_rps.md is stale (gnome territory, needs regeneration not hand-editing), and the riposte content seeding in section 5 is still untouched.
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
| Defend counter-damage 4 | 67.5 | **70.6** | 61.7 | 62.4 | ~9 |
| Defend **caps** damage at 2 | 88.7 | 87.5 | 53.5 | 60.2 | ~35 |
| cap + heavy 8 → 12 | 74.5 | **77.3** | 52.3 | 57.5 | ~25 |
| **cap + counter-damage 4** | **68.6** (last) | 78.2 | **82.0** | 80.8 | ~13 |

Cap semantics tested: a Defend caps what gets through rather than shaving a flat
amount — plain 2, heavy 1, perfect 0 — so a guard holds against a haymaker as
well as against a jab.

Read the cap row carefully: **it does not touch aggro at all** (88.7 vs a
baseline 88.2) while lifting control by 22 points and turtle by 19. It raises the
floor without lowering the ceiling. That is a different, and gentler, instrument
than the counter — and the two compose.

### Content levers (single monster, old_bram, berserk win%)

| variant | berserk | note |
|---|---|---|
| baseline | 93.4 | |
| + `Riposte Attack` | **79.6** | works, -13.8; mutual kills 3.4% → 6.8% |
| + `Stunning Defend` | 94.6 | no effect — but see §6, this is by design |
| + both | 79.4 | riposte is doing all the work |
| hp 18 → 28 | 74.2 | **backfires — see below** |

## 3. Recommendation: cap + counter, together

Two changes in `Resolver.cs`, which do different jobs and compose:

**Cap** — a Defend caps incoming damage (plain 2, heavy 1, perfect 0) instead of
subtracting a flat 2. Makes defending *survivable* against big hits.

**Counter** — a plain Defend meeting an Attack deals 4 back. Makes defending
*worth a slot*.

Together they invert the problem: berserk becomes the **worst** strategy at
68.6%, while aggro (78.2), control (82.0) and turtle (80.8) land within four
points of each other. Mindless attacking is punished and all three archetypes
are live.

### Why both

Neither alone is sufficient, and they fail in opposite directions:

- **Cap alone** leaves berserk untouched (88.7). Defending survives heavies but
  still deals nothing, so racing is still correct. It lifts the floor only.
- **Counter alone at 4** works (berserk 67.5) but does nothing about the heavies
  that make defending feel obligatory rather than chosen.

Cap is also the prerequisite for the thing you actually want out of this: **it
buys headroom to give monsters bigger attacks.** A player who guards is insulated
from the buff; a player who does not, eats it. Tested by raising heavy from +4 to
+8 on top of the cap — berserk falls to 74.5% and tell-reading aggro overtakes it
at 77.3%.

Caveat on that test: `heavy` is symmetric, so raising it buffs player heavy
weapons too, which is why the upper bands barely move. An asymmetric version —
authoring more or bigger heavies onto monsters — is a content change and would
bite harder.

### What the counter buys

**It closes the triangle.** Attack beats Recover (cancel + stun) → Recover beats
Defend (free heal against a guard with nothing to block) → Defend beats Attack
(counter). Each move beats one and loses to one, which is the game the format
promises and does not currently deliver.

It has to be a real trade: counter-damage **2** is not enough (berserk still
82.0). At 4 the attacker loses the exchange outright.

**Do not go further and make Defend block everything.** That was tried during the
pivot and made combat feel extremely tedious — full immunity turns every exchange
into a stall. The cap keeps a trickle coming through, which is what stops a guard
from being an off-switch. The 2/1/0 cap ladder is a tuning knob if the trickle
feels wrong.

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

## 6. `Stunning Defend` is weak on monsters BY DESIGN — leave it alone

Adding it to a monster moved berserk by +1.2 points, i.e. nothing. That is not a
bug and it is not a reason to buff it.

It is balanced for **player** use, where the constraint is the opposite one: a
guaranteed skip-on-enemy-attack would make control far too strong, since a
control player already knows the plan and could place the stun exactly where it
hurts. The 50% proc is what keeps that in check.

The lesson is about the harness, not the move: measuring a player-side move by
bolting it onto a monster answers the wrong question. Player-side and monster-side
balance are separate problems for any move whose value depends on knowing the
opponent's commit.

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

1. Prototype cap + counter-damage 4 in `Resolver.cs`, re-run the harness, confirm
   the inversion holds across all bands and entry-spirit levels.
2. Tune the cap ladder (2/1/0) against feel — it is the dial that decides whether
   guarding reads as attrition or as a stall (§3).
3. Narration for countered attacks, and for a capped hit, so a blocked haymaker
   reads differently from a blocked jab.
4. Re-check the T3 fights and fight lengths against the new resolver.
5. Then, with the cap in place, revisit monster attack sizes — the cap is what
   makes bigger heavies survivable for a player who guards (§3).
6. Independently, seed `riposte` through the monster corpus (§5) — useful with or
   without the engine change.

## 9. Play signal, 2026-08-17 — the counter reads as too strong

First report from actual play (not the harness): a basic Defend countering an
Attack for 4 "makes the defend action absurdly strong". The player did not know
the counter was intentional, which is itself a finding — see
[[reference_omits_defend_counter]], the shipped reference never mentions it.

**Do not re-tune on this alone.** The 4 was measured, and 2 was measured and
rejected: berserk stayed at 82.0% with a counter of 2, versus 68.6% at 4. Dropping
it back re-opens exactly the aggro dominance this plan closed. One fight's
impression is not the harness.

What the report is genuinely evidence for, in order of likelihood:

1. **A discoverability failure, not a balance one.** A move whose headline effect
   is undocumented reads as a bug when it fires. Fixing the reference may resolve
   the complaint entirely. Do this first, then re-ask.
2. **Narration is still missing** (§7, item 3 of the order above, never done). A
   countered attack currently produces damage with no explanation in the log, so
   the counter arrives as an unexplained number — the worst possible framing for a
   mechanic that is supposed to feel like a read paying off.
3. **The cap ladder, not the counter, may be the wrong dial.** If guarding feels
   dominant, the trickle (plain 2) is the tuning knob §3 already nominates; it
   changes how much a guard concedes without un-closing the triangle.

If a re-tune does turn out to be wanted, re-run the harness across all 6 gear bands
and 4 entry-spirit levels rather than adjusting to taste — the whole point of the
existing numbers is that aggro dominance was invisible without them.
