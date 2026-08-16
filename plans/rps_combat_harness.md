---
kind: plan
title: "RPS combat balance harness"
state: exploring
created: 2026-08-16
updated: 2026-08-16
status: Not started. This document is the findings pass — engine mechanics that determine strategy, corpus measurements, and the strategy archetypes to model. Two design decisions (§7) are still open and block implementation. Prior art deleted in 06f8ad1; no RPS-era harness has ever existed.
touches:
  files:
    - tools/ (new harness project, TBD name)
    - Dreamlands.sln
    - tests/Dreamlands.Combat.Tests/ (baseline regression test)
  features: [combat, balance, tooling]
related:
  - project/combat/landing_plan.md → Phase 5 "Sim and tuning" (unstarted, and still d20-framed — supersede or rewrite it)
  - plans/otel_gameplay_analytics.md (measures what real players do; this measures what is possible)
  - imp/reference/super_rps.md (design reference for moves and mutators)
  - imp/reference/weapon_classes.md:15 ("Numbers still need a sim pass")
---

# RPS combat balance harness

The RPS engine shipped without a balance pass. This plan records what we
learned characterising it, and specs a harness that can grade the 19-fight
corpus against the loadouts players can actually bring.

## 0. Prior art — there is none

`tools/combat-sim/` was written 2026-04-26 against the **d20** engine and
deleted in `06f8ad1`. It had not compiled since the RPS pivot removed
`Resolver.RollAttack` / `RollDamage` / `RollSave`, `DiceRoll` and `IntentClass`;
it was never in `Dreamlands.sln`, which is why nothing caught the rot.

The python sims (`tools/vibe_sim.py`, `powerhouse_sim.py`, `cancel_sim.py`) are
**tactical/card-era** (Mar-Apr 2026) and model the 15-card deck system, not RPS.
`vibe_sim.py` is still worth reading for its per-turn metrics (choice, tension,
juice, weight, triumph) — that framing survives the pivot even though its
subject does not.

**Lesson to carry:** the new harness goes in `Dreamlands.sln`, plus a test that
asserts a few baselines, so drift fails CI instead of waiting for someone to run
a tool.

## 1. Engine facts that determine strategy

These are measured or read out of the engine, not assumed.

**Monster AI is uniform random.** `CombatRunner.cs:239` —
`available[rng.Next(available.Count)]`, filtered only by cooldowns and carry-stun.
There is no adaptation, no targeting, no difficulty scaling. Consequences:

- A monster's difficulty is set by its **pool composition ratio** plus HP.
- Adding a scary move **dilutes every other move**. Measured: removing Tob's
  `Heavy Power Defend` made him *harder*, not softer (68.7% vs 63.3% death).
  This is deeply counterintuitive for authors and the harness should surface
  each monster's effective move-frequency directly.

**Tells encode attack *count*, never *position*.** `Tells.For` is first-match-wins
over a 5-entry table on the whole three-slot commit. Sampled 40k turn-1 commits:

| Tob's tell | fires | attacks in commit (0/1/2/3) |
|---|---|---|
| "pressing the attack" | 37.9% | 0 / 0 / 82.5 / 17.5 |
| "on their back foot" | 29.4% | 42.1 / 57.9 / 0 / 0 |
| "wary and awaits your move" | 21.6% | 0 / **100** / 0 / 0 |
| "is winded" | 11.1% | 56.0 / 44.0 / 0 / 0 |

The tells carry real signal. The *fallback* "wary" tell is the most precise in
the game — it means exactly one attack, 100% of the time. **Slot ordering under
a known attack count is the actual game.**

Caveat: turn-1 only, so no cooldown state. Later turns will differ.

**Suspect tell:** `sentinel_7c` fires "preparing a heavy attack!" **61.7%** of
the time, spanning 1-3 attacks. A warning that is usually on and imprecise when
it is on. Worth a look independent of the harness.

**The player out-sustains almost everything.** Damage ablates spirits 1:1 then
health (`CombatRunner.cs:296-300`), so the effective pool is **24** (20 spirits +
4 health). A basic attack is 4 in both directions, three slots per turn = 12
damage/turn each way. **Only 5 of 19 fights have HP >= 24**, i.e. only 5 can win
a pure damage race against a player who simply attacks.

**Attack into Recover is the big swing.** Heal is cancelled (`Resolver.cs:141`)
and the recoverer is *always* stunned, explicitly overriding resistance
(`Resolver.cs:157`); the stun eats their next slot. Net swing ≈ 12 (4 dealt +
4 heal denied + ~4 slot denied).

**Read economics.** Spending 1 of 3 slots on Read costs **4 damage of race
speed**. A basic Defend only prevents **2**. So reading in order to defend
better is net negative; reading pays when it finds a **Recover**.

**`Wary Read` is a free read.** The Wary conversion happens on a *local copy*
inside `Resolve` (`Resolver.cs:47`), while the reveal check reads the original
`playerSlots` array (`CombatRunner.cs:171`). So a Wary Read **reveals and
defends**. `silks` grants it at ShopTier 1, cost 15 — the cheapest armor in the
game. Flagged as a possible balance problem, not yet judged.

## 2. Corpus facts

19 road-pool fights (plus `the_beast_cornered`, arc-launched).

| HP | count |
|---|---|
| 12 | 1 (tob_ashford) |
| 18 | 13 |
| 24 | 4 |
| 28 | 1 (sothekavath) |

**Tier does not track difficulty.** HP is near-uniform across tiers, and pool
composition dominates. Measured: Bog Stalker (T2, hp 24) is far *easier* than
Tob (T1, hp 12) because its pool is almost all defense — 1 attack move against
2 recovers and 2 defends.

**Every monster can recover.** 0 of 19 fights lack a Recover move; recover-share
runs 20-40% throughout. See §3 — this matters a lot.

## 3. Strategy archetypes to model

Three overarching strategies (user's framing, and it matches what the mechanics
imply):

1. **Aggro / race.** Attack relentlessly; pause only when losing the race to a
   heavy attack or a condition-granting attack. Strong because of the effective-HP
   asymmetry in §1 — the player wins pure trades against 14 of 19 fights.
2. **Reader.** Always Read, play optimal counters. Pays for itself via the
   attack-into-Recover swing.
3. **Turtle.** Defend by default, attack into Recovers and Defends.

**The reader's hard counter is a non-recovering monster** (user-tested): with no
Recover to punish, reads only ever convert into Defends, which is net -2 per
turn, and the reader bleeds tempo until it loses the race.

**That monster does not exist — and its absence is deliberate.** All 19 fights
have a Recover. It was removed on purpose: the read-strategy felt like the
*correct* strategy, and a monster that suddenly invalidates it reads as
"surprise, you suck now" — a punish the player cannot see coming and cannot
learn from. That is a sound instinct and the reason this is not simply a gap to
fill.

Worth reconsidering, though, **if the counter is telegraphed.** The objection is
to the ambush, not to the counter existing. A no-recover monster that announces
itself lets the player *switch* strategies, which is the interesting version of
the same idea. Telegraphing options, roughly in order of strength:

- **Intro text** — the `.fight` intro is read before the first commit, so it is
  the one channel guaranteed to land before any decision. Cheapest and clearest.
- **A dedicated tell.** `Tells.For` is a 5-entry first-match table
  (`lib/Combat/Tells.cs`); a "never rests" style entry would need engine work but
  would sit inside the existing vocabulary.
- **Absence of "is winded"** is technically a signal, but a weak one — it
  requires the player to notice something that never happens, over several turns,
  while losing. Do not rely on it alone.

If we do add one, the harness should confirm it does what it is meant to: the
reader's win rate against it should drop sharply *and* the aggro/turtle lines
should stay healthy, so the answer is "switch strategy", not "this fight is
unwinnable".

**Untested hypothesis worth checking first:** these three may form their own
RPS triangle at the strategy layer (turtle absorbs aggro; aggro out-tempos
reader; reader dismantles turtle). If they do, that is the design working as
intended and the harness should confirm it. If one strategy dominates across the
whole matrix, that is the balance problem to fix. **Do not assume the triangle —
measure it.**

## 4. Loadout combinatorics

- **156 loadouts** (12 weapons + none × 11 armors + none)
- collapse to **117 distinct move pools** — only a 25% reduction, so item
  identity is very nearly strategic identity
- **99 of 117 contain a rule-breaker** (Perfect / Shielding / Riposte / Wary).
  The exotic case is the common case.

The rule-breakers do not shade the math, they **invert** it. The §1 finding that
"defence is net-negative" holds only for *basic* Defend. `Perfect Power Defend`
(`mountain_regiment_armor`, `golem_armor`) prevents **999** — total immunity,
once per turn. In that pool one slot per turn is free, turtling costs nothing,
and the race logic that makes aggro dominant simply does not apply.

**So per-cell best strategy genuinely varies.** That is a design feature. It is
also why a single hand-authored policy cannot grade this corpus.

Honest matrix size: **117 pools × 19 fights = 2,223 cells.** Compute is not the
constraint — cheap sims, embarrassingly parallel, minutes. **The constraint is
that nobody can read 2,223 numbers.** The design work is in the report.

## 5. What the harness measures

Not a win-rate table. Outlier detection over the matrix:

- **unwinnable cells** — best policy still loses (monster hard-counters a legal loadout)
- **trivial cells** — worst policy still wins (no decision content)
- **dominant loadout** — a pool top-quartile against all 19 (gear balance failure)
- **flat monster** — all policies score the same (slot machine, not a fight)
- **strategy diversity** — does the argmax policy *change* across cells? If one
  policy wins everywhere, the rule-breakers are decorative. This is the metric
  that says the §4 feature is working.
- **naive-vs-optimal gap** — the learning-curve metric (§6)
- **effective move frequency** per monster, so authors can see the dilution
  effect from §1

Secondary, per fight: fatality rate, rounds distribution (separates blowout from
grind from spongy-win), and whether Read is ever correct.

## 6. The naive-vs-optimal gap is a design target

Tob's brief, in the user's words: a new player may not know the game *has*
combat until they get jumped, then must learn the system, then must find sound
strategy — while a returning player should crush him.

That is a **large naive-vs-optimal gap**, and it is a per-monster target rather
than a side effect. Early-tier fights want a big gap; late-tier fights probably
want a high floor instead. The harness should report both numbers per cell so
the gap is gradeable.

Tob is currently **accepted as-is at hp 12** on this basis and is deployed. Note
the caveat in §8 about how weakly that number is grounded.

## 7. Open decisions — these block implementation

**1. How wide a policy set?** True per-cell best-response is expensive and
probably overkill. Proposal: 6-8 archetypes (the three from §3, plus
riposte-trade, perfect-block-abuse, naive-random, read-conditional) and report
the best of them — labelled explicitly in the output as a **lower bound on
optimal play**, not optimal. Given §8, honesty about that bound matters more
than the bound being tight.

**2. Naive baseline: uniform-random, or plausible-beginner?** Random is
reproducible and assumption-free but harsher than a real novice, who at least
defends when told a heavy attack is coming. Plausible-beginner is more realistic
but bakes in an assumption about what beginners notice. Leaning
plausible-beginner since §6 makes this a graded target — but it sets the number
Tob is judged against, so it is a design call.

## 8. Methodological warning — read before trusting any number

Hand-authored policies produced **three different answers** for Tob at hp 12:

| policy | win% |
|---|---|
| oracle dial @33% "read accuracy" | 60.3% |
| read-chain every turn | 53.0% |
| tell-based aggression, never reads | **94.7%** |

An earlier no-read baseline scored 0% because it turtled blind instead of
playing the tell; fixing that bug **inverted the conclusion** about whether
read-chaining is good.

The spread is not noise — it is the author guessing wrong about good play, three
times, in three directions. **Any harness that evaluates one hand-written policy
will confidently report an artifact.** This is the core argument for §7.1's
policy set and for reporting the best-of-N rather than a single number.

It also means **the hp 18 → 12 decision for Tob rests on the weakest of the
three models.** Under tell-based aggression, Tob at hp 12 is a 94.7% win in 2.4
turns. Accepted for now per §6; revisit once the harness can grade him properly.

## 9. Fixed along the way

`kukri` and `short_sword` encoded `"Stun Power Attack"`; the valid mutator is
`stunning`. `Move.Parse` threw inside `CombatPlayerProfile.From`, so equipping
either **crashed combat**. Both are purchasable. Fixed in `819a314`.

**Follow-up worth doing:** a test that walks every `ItemDef.RpsMoves` encoding
through `Move.Parse`. The bug was invisible until something tried to build every
loadout — exactly what this harness does.
