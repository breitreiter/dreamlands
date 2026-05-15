---
kind: plan
title: Flavor Text Library
state: exploring
created: 2026-02-21
updated: 2026-02-21
status: current
touches:
  files:
    - lib/Flavor/
    - lib/Flavor/FoodNames.cs
    - lib/Flavor/FlavorText.cs
  features: [flavor, generation]
provenance:
  author: migration:M-001
---

> Migration note (M-001, 2026-05-12): The Flavor library exists at `lib/Flavor/`
> with `FoodNames.cs` (90 biome-specific food names) and `FlavorText.cs` as a
> static entry point. Region/settlement/POI/time/weather generators listed below
> are not yet implemented — most of this spec is still aspirational. Kept as
> `exploring` until the next category gets prioritized.

# Flavor library scope

the flavor library generates text strings for any situation in which it is not useful or appropriate to formally write and store text.

# things the library will need to generate:

## Map regions (inputs: biome, tier)
- name
- per-tile descriptions

## Settlements (inputs: biome, tier)
- name
- description
- guild office description
- market description
- guild office rumors (addl inputs: price sheet)
- temple description
- inn description
- healer description

## Other elements (inputs: biome, tier)
- time of day description
- weather description (addl input: weather state)

## Loose elements
- status condition warning (condition)

The MVP version of the library can ignore inputs and return a static placeholder string. the important thing is that downstream projects are not blocked on flavor text.
