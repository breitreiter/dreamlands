---
kind: bug
title: "The shipped player reference contradicts the engine on what Defend does"
state: fixed
created: 2026-08-17
fixed: 2026-08-17
severity: medium
status: resolved by reverting the engine, not by editing the doc — the counter is gone, so the
  reference was right all along. See "Resolution".
touches:
  files:
    - ui/web/src/screens/Reference.tsx
    - lib/Combat/Resolver.cs
  features: [ui, combat, docs]
provenance:
  author: claude
  found_via: play — noticed basic Defend dealing 4 back and queried whether it was a regression
---

# The reference does not mention the Defend counter

## What happens

A basic Defend meeting an Attack deals **4 damage back** to the attacker. This
surprised the player mid-fight and read as "absurdly strong".

It is not a regression and not an accident. `Resolver.Counter`
(`lib/Combat/Resolver.cs:153-154`) returns `BaseAttackDamage` (4) whenever a Defend
meets an Attack, shipped deliberately in `f7934ae` with measurements in
[[defang_aggro]]. The design intent is explicit: the counter is what makes Defend
*beat* Attack rather than merely blunt it, closing the RPS triangle. A counter of 2
was measured and rejected as too weak to change behaviour.

## The actual defect

**The player-facing reference does not say so**, and it is live in production
(deployed 2026-08-17). `Reference.tsx`'s triangle matrix reads:

| Action | Vs Attack | Vs Defend | Vs Recover |
|---|---|---|---|
| Attack | Trade 4 damage | Inflict 2 damage | Inflict 4 damage and recovering party skips next action |
| Defend | Caps incoming damage to 2 | Nothing | Nothing |

Two rows are wrong against the engine:

- **Defend vs Attack** is listed as capping only. The 4-damage counter is absent
  entirely, so the single most important fact about the move is missing.
- **Attack vs Defend** says "Inflict 2 damage" with no mention that the attacker
  eats 4 in return, which makes attacking into a guard look like a mild loss
  rather than a bad trade.

The prose immediately below the table — "Every base action beats one other and
loses to a third" — is therefore unsupported by the table it introduces. A player
reading the reference cannot work out that Defend beats Attack, which is the whole
point of the change.

## Why it slipped

The reference was rewritten by hand on 2026-08-17, one day after `f7934ae` landed.
The previous copy *did* describe the counter ("Caps whatever lands on you at 2, and
strikes an attacker back for 4") and it was dropped in the rewrite, along with the
special-moves rider table. So this is a doc regression, not a doc that never caught
up.

## Fix

Correct the two cells and keep the "beats one, loses to one" framing honest:

- Defend vs Attack: caps incoming at 2 **and deals 4 back**.
- Attack vs Defend: inflict 2, **take 4**.

Worth deciding at the same time whether the reference should state the cap ladder
(plain 2 / heavy 1 / perfect 0, `Resolver.DefendCap`), since the rewrite also
dropped the rider table that used to carry it.

## Resolution (2026-08-17)

**Fixed from the other end: the counter was reverted, so the reference is correct
as written.** The doc was never the thing out of step — the engine was. The counter
was not the agreed design (the agreed fix for Defend was the clamp alone), and a
guard that hits back for a full attack is a riposte, which belongs on gear rather
than on the base move. Reverted in `Resolver.cs`; see [[defang_aggro]] §9.

Verified against the current engine, cell by cell: Attack-into-Defend inflicts 2,
Defend caps incoming at 2 and deals nothing back, and the rest of the matrix was
already right. No edit to `Reference.tsx` was needed.

The intro line — "Every base action beats one other and loses to a third" — was
briefly overstated after the revert, since nothing then beat Attack. Resolved
2026-08-17 by [[defend_stagger]]: a guard now costs an attacker their next slot, so
Attack genuinely loses to Defend again, in tempo rather than damage. The triangle
matrix records the stagger in both the Attack and Defend rows.

The `Combat.tsx` tooltip was corrected in the same pass — it had said Defend
"strikes back for 4", which came from the same reverted change. It now reads as a
pure cap. The stale "+2 prevent" text it replaced was a real bug and stays fixed.
