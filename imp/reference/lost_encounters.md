---
kind: reference
title: Lost Encounters — Getting Un-Lost as an Encounter
created: 2026-05-17
updated: 2026-05-17
status: current
touches:
  files:
    - text/encounters/plains/tier1/Lost.enc
    - text/encounters/mountains/tier1/Lost.enc
    - text/encounters/forest/tier1/Lost.enc
    - text/encounters/swamp/tier1/Lost.enc
    - text/encounters/scrub/tier1/Lost.enc
    - lib/Orchestration/EncounterSelection.cs
    - lib/Game/PlayerState.cs
  features: [encounters, lost-condition, encounter-selection]
provenance:
  author: migration:M-002
subject: Design spec for 15 bespoke "lost" encounters (one per biome/tier) that trigger when the player has the lost condition and fire before the normal encounter pool.
---

> Migration note (M-002, 2026-05-17): Migrated from `project/design/lost_encounters.md`.
> Spec is complete; encounter .enc files and engine changes are not yet authored.

## Overview

When a player has the `lost` condition the explore screen currently soft-locks until
the condition clears. The fix: 15 bespoke "lost" encounters (one per biome/tier)
triggered automatically. All outcomes clear `lost`. The difference between choices
is cost, not reachability.

## Trigger

Detect the `lost` condition and force the lost encounter for the current biome/tier
instead of normal movement. No map interaction while lost.

Trigger type: `[trigger none]` — not pooled road/settlement encounters. The engine
selects them directly by condition + location.

## Structure (shared across all 15)

```
Title
[trigger none]

Body: 2-3 paragraphs. You're lost. Terrain, what's confusing,
what you can see/hear. Locale-specific.

choices:

* Option A (best outcome)
  @if check <skill> correct:X wrong:Y {
    correct: find your way quickly. Minimal time cost.
    +remove_condition lost
    +advance_time 2
  } @else {
    wrong: wander but eventually orient. More time, maybe spirits.
    +remove_condition lost
    +advance_time 4
    +damage_spirits 1
  }

* Option B (moderate)
  @if check <skill> correct:X wrong:Y {
    correct: costs time but works.
    +remove_condition lost
    +advance_time 3
  } @else {
    wrong: significant time, spirits, maybe condition in T2/T3.
    +remove_condition lost
    +advance_time 5
    +damage_spirits 2
    (+add_condition <something> in dangerous areas)
  }

* Option C (safe fallback, no check)
  Always works. Costs the most time.
  +remove_condition lost
  +advance_time 6
```

## Key rules

- **Every branch clears `lost`.** No outcome leaves you stuck.
- **Bad outcomes cost time, not health.** `+advance_time` with higher values.
  Time costs trigger end-of-day cycles (food, conditions, drain) — time IS damage.
- **T2/T3 can add conditions on failure** — injured from a fall, exhausted.
  T1 is forgiving.
- **No `+repool`.** One-visit. Once lost here, you've learned the terrain.
- **Choices are locale-themed.** Plains: climb a watchtower, follow a road.
  Mountains: follow a ridgeline, descend to a valley. Swamp: find dry ground,
  follow water flow.
- **Different skills matter in different places.** Bushcraft is obvious, but
  Negotiation (ask locals), Cunning (read trail signs), and Combat
  (push through dangerous territory) should appear where they fit.

## Flavor by tier

- **T1**: Getting lost is an inconvenience. Friendly terrain, people nearby.
  Options like "ask a farmer" or "follow the road markers." Low DCs.
  Worst outcome is losing half a day.
- **T2**: Getting lost is costly. Rougher terrain, fewer people. Real skill
  checks. Failure can mean conditions (exhausted, injured).
- **T3**: Getting lost is dangerous. Hostile or alien terrain. The "safe"
  fallback is expensive. Failure on skill checks adds serious conditions.
  The locale itself is the threat (Grid ruins, deep swamp, high peaks).

## File locations

```
text/encounters/plains/tier1/Lost.enc
text/encounters/plains/tier2/Lost.enc
text/encounters/plains/tier3/Lost.enc
text/encounters/mountains/tier1/Lost.enc
...etc (15 total)
```

## Engine changes needed

- `EncounterSelection`: add `TryPickLost(session, node)` that checks for `lost`
  condition, selects the `Lost.enc` for current biome/tier, returns it before
  normal `PickOverworld`.
- Explore screen: instead of disabling all interaction, show a "You are lost"
  state leading into the encounter.

## Generation plan

Good candidate for single-pass LLM generation. The skeleton is mechanical
(same choice structure, same mechanics per tier); prose just needs locale grounding.

For each of the 15 biome/tier combos:
1. Feed the LLM the locale_guide.txt + this spec as context
2. Ask for title, body text, 3-4 choices with skill checks and outcomes
3. Mechanics are fixed per tier (DCs, time costs, conditions) — only prose varies
