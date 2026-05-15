---
kind: reference
title: "Armor Classes"
created: 2026-03-01
updated: 2026-04-27
status: current
touches:
  features: [combat, armor, rps]
provenance:
  author: migration:M-001
---
# Armor Classes

Three armor classes, each with a primary stat and different freezing tolerance.

> **AC under the combat pivot.** Light AC 11–12, Medium AC 13–15, Heavy
> AC 16–18 (matches D&D's plate ceiling). Tier-3 frontline combat
> assumes plate; light-armor builds are deliberately worse in a
> straight fight and earn their keep on out-of-combat checks. See
> `combat_pivot.md` "Encounter Tuning Baseline" for the locked numbers.

## Stats

Every armor piece has +0 to +5 in three areas:

- **Injury resistance** — resists the most common and dangerous condition (stacks to 3, requires medicine)
- **Cunning bonus** — flat skill modifier for Cunning checks (stealth, traps, trickery)
- **Freezing resistance** — resists a mountains-only ambient condition (1 stack, clears on settlement)

Freezing is the weakest of the three: it's biome-specific, single-stack, and auto-cures.
But high freezing lets you camp freely in the mountains without worrying about overnight cold.

## Light Armor

Robes, cloaks, silks. Primary stat: **Cunning** (+0 to +5).

- Injury: +0 (cap). Light armor doesn't stop blades.
- Freezing: +0 to +3. Cloaks and layers help with cold.
- Biome affinities: scrub, swamp, mountains.

## Medium Armor

Leather, hide, layered coats. Straddles all three stats.

- Cunning: +1 to +2. Flexible enough for stealth.
- Injury: +1 to +3. Decent protection.
- Freezing: +1 to +5. Rugged outdoor gear insulates best. The medium capstone hits +5 freezing — if you're building for mountain survival, this is the payoff.
- Biome affinities: forest, mountains.

## Heavy Armor

Mail, scale, plate. Primary stat: **Injury** (+1 to +5).

- Cunning: +0 (cap). Metal armor is loud and stiff.
- Freezing: +0 to +2. Steel conducts cold; some padding helps.
- Biome affinities: plains, scrub.

## Tier & Shop Rules

Same as weapons:

- **T1 shops**: starter and poor gear
- **T2 shops**: most purchasable gear
- **Capstones**: dungeon-only (no ShopTier, no Cost)
