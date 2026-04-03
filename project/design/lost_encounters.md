# Lost Encounters

## Overview

When a player gains the `lost` condition, the explore screen disables map
interaction (no click-to-move, no player marker). Currently this soft-locks the
game until the condition clears at a settlement the player can't navigate to.

The fix: 15 bespoke "lost" encounters (one per biome/tier), triggered
automatically when the player is lost. Each encounter presents 3-4 ways to
reorient. All outcomes clear `lost`. The difference is cost.

## Design

### Trigger

The game detects the `lost` condition and forces the lost encounter for the
current biome/tier instead of normal movement. No map interaction while lost.

Trigger type: `[trigger none]` — these are not pooled road/settlement
encounters. The engine selects them directly by condition + location.

### Structure (shared across all 15)

Every lost encounter follows the same skeleton:

```
Title
[trigger none]

Body: 2-3 paragraphs. You're lost. Describe the terrain, what's confusing,
what you can see/hear. Locale-specific.

choices:

* Option A (best outcome) = preview
  @if check <skill> <hard DC> {
    Success: you find your way quickly. Minimal time cost.
    +remove_condition lost
    +advance_time 2
  } @else {
    Failure: you wander but eventually orient yourself. More time, maybe spirits.
    +remove_condition lost
    +advance_time 4
    +damage_spirits 1
  }

* Option B (moderate) = preview
  @if check <skill> <medium DC> {
    Success: costs some time but works.
    +remove_condition lost
    +advance_time 3
  } @else {
    Failure: costs significant time, spirits, maybe condition in T2/T3.
    +remove_condition lost
    +advance_time 5
    +damage_spirits 2
    (+add_condition <something> in dangerous areas)
  }

* Option C (safe fallback, no check) = preview
  Always works. Costs the most time.
  +remove_condition lost
  +advance_time 6
```

### Key rules

- **Every branch clears lost.** No outcome leaves you stuck.
- **Bad outcomes cost time**, not health. `+advance_time` with higher values.
  Time costs trigger end-of-day cycles (food, conditions, drain), so time IS
  damage — it just works through existing systems.
- **Dangerous areas (T2/T3) can add conditions** on failure — injured from a
  fall, exhausted from wandering, etc. T1 is forgiving.
- **No +repool.** These are one-visit. Once you've been lost here, you've
  learned the terrain. If you get lost again in the same region, you'll know
  what works.
- **Choices are locale-themed.** The options reflect what's actually available
  in the terrain. Plains: climb a watchtower, follow a road. Mountains: follow
  a ridgeline, descend to a valley. Swamp: find dry ground, follow water flow.
- **Different skills matter in different places.** Bushcraft is obvious, but
  negotiation (ask locals), cunning (read trail signs, navigate by stars), and
  even combat (push through dangerous territory) should appear where they fit.
- **Replayability through mastery.** Over time the player learns "in the swamp,
  always follow the water" or "in the mountains, the ridgeline check is easy."
  This rewards familiarity with regions.

### Flavor by tier

- **T1**: Getting lost is an inconvenience. Friendly terrain, people nearby.
  Options like "ask a farmer" or "follow the road markers." Low DCs. Worst
  outcome is losing half a day.
- **T2**: Getting lost is costly. Rougher terrain, fewer people. Options
  require real skill checks. Failure can mean conditions (exhausted, injured).
- **T3**: Getting lost is dangerous. Hostile or alien terrain. The "safe"
  fallback is expensive. Failure on skill checks adds serious conditions.
  The locale itself is the threat (Grid ruins, deep swamp, high peaks).

### File locations

One .enc per biome/tier, named `Lost.enc`:

```
text/encounters/plains/tier1/Lost.enc
text/encounters/plains/tier2/Lost.enc
text/encounters/plains/tier3/Lost.enc
text/encounters/mountains/tier1/Lost.enc
...etc (15 total)
```

### Engine changes needed

- Orchestration layer: when player has `lost` condition and tries to act,
  force-select `Lost` encounter for current biome/tier instead of normal
  movement.
- Explore screen: instead of disabling all interaction, show a "You are lost"
  state that leads into the encounter.

## Generation plan

These are a good candidate for single-pass LLM generation. The skeleton is
mechanical (same choice structure, same mechanics per tier), and the prose
just needs to be grounded in the locale guide.

For each of the 15 biome/tier combos:
1. Feed the LLM the locale_guide.txt + this design doc as context
2. Ask for title, body text, 3-4 choices with skill checks and outcomes
3. Mechanics are fixed per tier (DCs, time costs, conditions) — only prose varies

This can be a batch Sonnet task: one prompt template, 15 locale guides, 15
output files.
