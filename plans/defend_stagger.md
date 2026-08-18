---
kind: plan
title: "Defend staggers the attacker — tempo, not mitigation"
state: ready
created: 2026-08-17
updated: 2026-08-17
status: ready — shape settled and measured. A Defend that stops an Attack costs the attacker their next slot. Inverts berserk vs aggro by 7 points at T0 lower (74.3 vs 81.3) while leaving control at 51.5, i.e. it defangs mindless attacking without rewarding the tedious line. Supersedes the cap+counter approach in defang_aggro, whose counter half was reverted. Not implemented.
touches:
  files:
    - lib/Combat/Resolver.cs
    - lib/Combat/CombatRunner.cs
    - server/GameServer/GameFunctions.cs
    - ui/web/src/screens/Combat.tsx
    - ui/web/src/screens/Reference.tsx
    - text/encounters/combat/**.fight
  features: [combat, rps, balance]
provenance:
  author: claude
  found_via: play report (control is tedious) + harness sweep across five candidate levers
---

# Defend staggers the attacker

A Defend that stops an Attack costs the attacker their next slot. No damage, no
change to the cap, no counter. One rule.

## 1. The problem, stated correctly

Berserk — attack in all three slots, ignore the tell — is the best policy nearly
everywhere. `defang_aggro` §1 has the root cause: **Attack never loses.** It trades
evenly with itself, chips 2 through a guard, and hard-counters Recover.

Two things about that framing turned out to be wrong or incomplete, and both cost
us a shipped-then-reverted change:

**Win% is not the broken metric.** The play complaint that reopened this was not
"control wins too much" — it was that control is *boring*: no risk, mostly
deterministic, a dozen turns long. Control measures at 53.5%. It is not
overpowered, it is **degenerate**. The reverted counter "fixed" the table by
lifting control to 82.0%, which made the tedious line the winning line. It
optimised the metric that wasn't broken and made the experience worse.

**Tempo beats mitigation.** This is the finding that explains why four of the five
levers measured below did nothing to the ordering. Any slot not spent attacking
lengthens the fight, and a longer fight means more total incoming damage. Capping
a hit at 2 never repays the slot it cost. So levers that raise the *stakes*
uniformly — bigger monster damage, riposte — move every policy down together and
leave berserk on top.

## 2. Levers, measured

T0 dagger/light lower, 400 trials/cell, 20 entry spirits, monster AI unchanged
(uniform random). Post-counter-revert engine as baseline.

| variant | berserk | aggro | control | turtle | best |
|---|---|---|---|---|---|
| **current** | **88.7** | 87.5 | 53.5 | 60.2 | berserk |
| monster attack 5 | **85.2** | 83.6 | 53.3 | 58.2 | berserk |
| monster attack 6 | **79.9** | 76.4 | 51.4 | 54.5 | berserk |
| monster attack 7 | **72.2** | 70.9 | 51.3 | 52.8 | berserk |
| monster attack 8 | 62.2 | **63.7** | 49.4 | 49.3 | aggro +1.5 |
| riposte on every monster attack | **69.1** | 67.6 | 51.1 | 50.4 | berserk |
| riposte + monster attack 5 | 61.2 | **62.4** | 51.0 | 48.7 | aggro +1.2 |
| **Defend staggers the attacker** | 74.3 | **81.3** | 51.5 | 64.6 | **aggro +7.0** |
| stagger + riposte | 51.0 | **60.7** | 48.1 | 57.0 | aggro +9.7 |

Consistent across bands for the stagger: T1 lower 74.1 vs 80.9, T2 lower 83.5 vs
87.5. Across all bands at 20 spirits: berserk 93.6 → 84.1, aggro 92.8 → **88.9**,
so the top slot changes hands overall and not just in the starting kit.

### What the failures rule out

**Monster damage is a difficulty knob, not a meta lever.** It has to *double*
before the ordering flips, and then only by 1.5 points. A back-of-envelope said
monster attacks needed to average 6 for pure trading to stop paying; that model
assumed every slot is a mutual attack, and in a mixed pool most are not. Note this
also contradicts `defang_aggro` §3's hope that the cap "buys headroom for bigger
monster attacks" as an anti-aggro move — it buys headroom, but the headroom does
not defang anything.

**Riposte does not invert either.** −19.6 on berserk, the strongest of the
stakes-raising levers, and still berserk on top. It remains worth seeding for
per-monster texture (`defang_aggro` §5) but it is not the fix.

## 3. The change

> A Defend that stops an Attack stuns the attacker's next slot.

Why this one and not the others:

- **It is tempo, so it engages the actual mechanism.** It takes slots away from
  the aggressor rather than raising what a slot costs. That is the only thing
  measured that changed the ordering by more than noise.
- **It does not strengthen the guard.** The cap stays 2/1/0, the leak stays, and
  Defend still deals zero. A blocking player takes exactly as much damage as
  today, so the clock in §4 keeps running.
- **It does not reward the tedious line.** Control 53.5 → 51.5. Compare the
  reverted counter, which took control to 82.0.
- **It gives control the right kind of payoff.** What control gains is not
  survivability but *tempo denial* — a guard that reads correctly takes the
  enemy's turn apart. Per the design call on 2026-08-17: it makes control feel
  more controlly, which is a texture win rather than a power win.
- **It shortens fights rather than lengthening them.** A staggered slot is a slot
  the aggressor does not act in. Cost-of-winning barely moves (berserk 10.1 → 8.3
  spirits left), so it does not read as extra grind — but see §6, there is no
  turn-count metric yet to confirm this.

## 4. Invariants this must not break

**The leak is the clock.** A plain Defend lets 2 through, and that trickle is the
guaranteed loss-per-turn that stops control from stalling forever. Anything that
shrinks it strengthens control directly. Measured: clamping plain to 1 lifts
control 53.5 → 72.0 and turtle 60.2 → 72.3 while leaving berserk untouched
(88.7 → 86.5). **Do not clamp harder.** It looks like a gentle dial and is the
wrong direction.

**`perfect` (cap 0) is the invariant's sharpest edge** — it removes the clock for
that slot outright. It survives today only because it is gated behind `Power`
(once per turn) on two elite armors, so the other two slots still leak. That
gating is load-bearing, not incidental. Adding the stagger makes Perfect Defend
strictly better (zero damage *and* a stolen slot), so re-check it after.

**Defend deals no damage.** A guard that hits back for a full attack is a riposte,
which is a gear rider, not a property of the base move. See `defang_aggro` §9.

## 5. Implementation

1. **`Resolver.Resolve`** — where a Defend meets an Attack, set the stun flag on
   the attacker. The forward-stun plumbing already exists (`StunPlayerNext` /
   `StunMonsterNext`), and `CombatRunner.ApplyForwardStun` already handles the
   slot-3 case carrying into next turn's slot 1. Symmetric: it applies to whichever
   side is guarding.
2. **Decide the mutator interactions before writing tests.** Four questions, none
   of which the measurement answers:
   - Does `shielding` on the *attacker* protect them? It should not — shielding
     blocks statuses aimed at you *while you guard*, and an attacker is not
     guarding. Confirm the resolver reads that way.
   - Do `heavy` and `perfect` Defends also stagger? Presumably yes, but Perfect
     Defend then becomes a zero-damage slot theft (see §4).
   - **`Stunning Defend` becomes redundant** — if every Defend staggers an
     attacker, the rider says nothing. `defang_aggro` §6 concluded it was weak on
     monsters *by design* and should be left alone; this change obsoletes that
     conclusion and the rider needs a new job or removal from the vocabulary.
   - **Berzerk + stagger is a lock-loop, and the only live path into it is the
     player's.** No monster in the corpus inflicts Berzerk (zero `Provoking`, zero
     `Enraging` across all 19 `.fight` files) — berzerk monsters were cut in early
     testing because they favoured racing, which is the thing this whole plan is
     trying to defang. But The Old Tooth (`ItemDef.cs:125`) still carries
     `Heavy Power Provoking Attack`, which berzerks the *monster*: pool-locked to
     Attack, into a player who then guards and staggers every slot. Provoke-then-
     turtle is a real exploit this change would create. `Power` gates the provoke
     to once per turn and Berzerk does not persist unless re-applied, which limits
     it, but it needs a decision before ship.
3. **Tests** — write from the design, not by editing until green, per the pattern
   in `defang_aggro` §9. Cover: plain Defend staggers, both directions, slot-3
   carry, and each mutator decision from step 2.
4. **Narration is not optional.** `defang_aggro` §7 flagged this for the counter
   and it was never done; the counter then shipped and read as damage from
   nowhere, which is a large part of why it was noticed as a bug rather than a
   mechanic. A stolen slot must say so in the log and read on the combat screen
   before this goes out.
5. **Tooltip + reference.** `Combat.tsx`'s Defend line and `Reference.tsx`'s
   triangle matrix both need the stagger. The reference's "every base action beats
   one other and loses to a third" becomes *true again* under this change — Attack
   now genuinely loses to Defend, in tempo rather than damage. That line is
   currently overstated and is tracked in [[reference_omits_defend_counter]].
6. **Re-measure after**, all bands and entry-spirit levels, and re-check the T3
   fights specifically.

### The berzerk ban is undocumented

The no-berzerk-monsters decision lives only in the absence of the rider from the
corpus. `rules/encounter_mechanics.md:381` still describes `provoking` neutrally,
so the vocabulary invites an author to reintroduce it. `enraging` (`:404`) appears
on nothing at all and is dead vocabulary. Writing the ban down is a prerequisite
for this plan, not a tidy-up: the stagger makes a berzerked target strictly worse
off, so the cost of someone re-adding a provoking monster goes up.

`Reference.tsx` no longer mentions Berzerk at all — the 2026-08-17 rewrite dropped
the rider table — so a player who buys The Old Tooth gets a mechanic with no
player-facing explanation anywhere.

## 6. What this does not fix, and measurement caveats

**It does not address the determinism half of the complaint.** Two things make
combat solvable, and neither is touched here:

- **Read is priced at 1 slot for 3.** `CombatRunner.cs:171` — one Read anywhere in
  the turn reveals the monster's entire next commit. The cheapest slot to spend it
  in is one facing a Defend, where nothing was going to happen, so the chain is
  self-sustaining and free. Pricing it per-slot (Read in slot *i* reveals slot *i*)
  makes perfect information cost a whole turn.
- **Monster AI is uniform random** (`CombatRunner.cs:239`, `rps_combat_harness`
  §1). There is no opponent to outguess, so optimal play collapses to a fixed
  policy. Note that adaptive AI is worthless *until* Read is priced — an opponent
  cannot hide a plan the player can read in full.

Both belong in a follow-up. The stagger is orthogonal to them and does not depend
on either.

**Caveats on every number above.** The harness evaluates hand-written fixed
policies, and `rps_combat_harness` §8 records three of them producing contradictory
answers on a single fight. Treat direction as signal and magnitude as provisional.
A human plays better than `AggroPolicy`, and the play report that started this is
evidence the human control line is stronger than the harness's. There is also no
turn-count metric, so the claim that this shortens fights rests on cost-of-winning
as a proxy and should be checked directly.

## 7. Suggested order

1. Decide the four mutator interactions in §5.2 — they are design calls, not
   measurements, and the tests depend on them.
2. Implement in `Resolver`, tests from the design, re-run the harness across all
   bands and entry-spirit levels.
3. Narration + tooltip + reference, together. Do not ship the rule without them.
4. Give `Stunning Defend` a new job or retire it.
5. Re-check `Perfect Power Defend` against §4 now that it also steals a slot.
6. Separately, and independently: price the Read (§6).
