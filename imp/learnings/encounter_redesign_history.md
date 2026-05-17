---
kind: learning
title: "Encounter Redesign History — What We Tried Before .enc + RPS"
created: 2026-05-17
updated: 2026-05-17
status: stale
touches:
  features: [encounter-design, combat, tactical-encounters]
topics: [design-history, encounters, combat, card-game, deckbuilding, rejected-approaches]
provenance:
  author: migration:M-002
---

> Migration note (M-002, 2026-05-17): Synthesized from `project/design/encounter-redesign/`
> (10 files + old/ subdirectory). That directory has been deleted. This doc captures
> what was explored and why it was abandoned in favour of the current .enc format +
> RPS combat pivot.

Three generations of encounter design were explored before the current system.
All were rejected as too mechanically heavy for a narrative-forward game.

## Generation 1 — Aspect-based (old/)

`aspect-based.md`, `aspects.md`, `journey_alt.md`, `journey_hazards.md`, `journey_hazards_blocks.md`

Players built a "kit" of Aspects — narrative stance tokens (Aggressive, Cautious, Clever,
Enduring) that modified how encounter options resolved. Journey hazards were block-structured
sequences of threats players navigated with their kit.

**Why abandoned**: Aspect management was a separate bookkeeping layer on top of the
narrative. Players had to track meta-stance across encounters with no direct fictional
grounding. The block structure made authoring laborious.

## Generation 2 — Combat/Traverse resource puzzle (overview.md, traverse.md, mma-inspired.md)

A unified 3–7 turn resource-management loop with two variants:

- **Combat** — reactive, blind draws. Player manages Momentum, Spirits, and Resistance
  (encounter health) while **timers** create pressure each turn. Initial approach selection
  (Scout / Direct / Wild) sets starting Momentum and timer count.
- **Traverse** — proactive, visible queue of upcoming openings. No approach selection;
  the encounter sets the parameters.

Core economy: Momentum self-replenishes; Spirits are non-renewable. **Openings** (the
atomic action unit) have a cost (Momentum, Spirits, tick a timer, or free) and an effect
(damage Resistance, stop a timer, gain Momentum). Gear-gated openings: equipment with
matching tags unlocks additional openings from the pool.

Win conditions: Resistance → 0 (attrition win) or all timers stopped (control win).
Failure: Spirits → 0.

**Why abandoned**: The system was internally coherent but required players to track 4
numeric resources simultaneously (Momentum, Spirits, Resistance, N timers). Too much
cognitive overhead for a game whose primary texture is narrative. The traverse variant
was genuinely interesting (visible queue + planning) and some ideas were recycled into
the final RPS combat screen.

## Generation 3 — Card/deckbuilding (card_archetypes.md, card_lists.md, deckbuilding.md, fun_deck.md)

Extended generation 2 with a deck of 15 cards assembled at encounter start from:
- Base skill level (0–4 cards, typed to the encounter's governing skill)
- Equipped weapon/armor (1–5 cards per item, additive as item tier increases)
- Lucky charm (0–1 card)
- Remaining slots filled with scenery filler + global chaff

Cards were named archetype-reskins: `momentum_to_progress`, `free_cancel`, `spirits_to_momentum`
etc., with flavor names driven by encounter type (Fight: "Slash", Debate: "Sharp Rebuke",
Sneak: "Slip Past"). Weapon types had distinct tactical identities:
- **Daggers**: control-heavy, cancel cards at tier 2; relies on openings not brute force
- **Axes**: pure aggro, no cancels, overwhelm before timers fire
- **Swords**: hybrid, jack-of-all-trades

`fun_deck.md` explored a "fun deck" design that prioritized moment-to-moment interest
over balance optimisation.

**Why abandoned**: The deckbuilding layer on top of an already resource-heavy encounter
loop was a full minigame embedded inside an encounter. It required authoring card sets
per weapon per skill combination (~60+ unique named cards), and the strategic depth it
offered wasn't commensurate with the implementation cost or the game's genre expectations.

## What landed

The final system discarded all of the above and pivoted to:
- **.enc format** — plain-text encounter authoring with `@if`/`@elif` branching and
  `check`/`has`/`meets` conditions. No authored resource pools or timer systems.
- **RPS combat** — three-slot prediction game at `lib/Combat/`. Replaces d20 + tactical
  encounter for creature fights. See [[rps_combat_pivot]].

The traverse encounter's "visible queue + planning" intuition is the one element worth
revisiting if the encounter system ever gains a multi-step structure.
