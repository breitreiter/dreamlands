# Dynamic Haul Generation

When bespoke hauls are exhausted for a given origin/destination biome pair,
the system falls back to a small pool of generic hauls. These are intentionally
bland — the player should recognize that they've worked through the good stuff
and are now running errands.

## Design Goals

1. Trade never dead-ends. There's always something to carry.
2. Players can tell the difference. Generic hauls have a distinct tone —
   bureaucratic, impersonal, slightly tedious. Not mysterious, not flavorful.
3. Small pool, no pretense. These rotate visibly. The player sees the same
   guild reports and sealed crates. That's the point.

## Generic Haul Pool

A fixed set of ~8-10 entries, biome-agnostic. Any origin can generate any of them,
any destination can receive them. No origin/destination biome filtering.

| Name | Origin Flavor |
|------|---------------|
| Guild Reports | Normally a courier handles these, but if you're heading that direction, the work pays a little coin. |
| Sealed Crate | Nondescript wooden crate with a wax seal. Nobody mentions what's inside. |
| Bonded Cargo | A parcel under guild bond. You signed for it, so don't lose it. |
| Unmarked Parcel | Brown paper, twine, no label. Someone at the other end is expecting it. |
| Census Ledger | The guild tallies population counts twice a year. This one's overdue. |
| Tax Remittance | A locked strongbox of collected duties. Heavy for its size. |
| Requisition Forms | A stack of forms requesting supplies. Technically urgent, practically ignored. |
| Merchant's Samples | Fabric swatches and spice vials. A trader wants them moved to the next market. |

Delivery flavor is shared across all generic hauls:
"Singed and accepted without comment."

## Data Model

Generic hauls use the same `HaulDef` shape but are marked with a flag so the
system (and UI) can distinguish them:

- `HaulDef.IsGeneric = true` — new bool field, false for all bespoke hauls
- Generic defs live in `HaulDef.Generic()`, same pattern as the biome partials
- `HaulDef.BuildAll()` concatenates bespoke + generic pools

## Generation Behavior

`HaulGeneration.Generate()` already picks from the haul pool filtered by
origin/destination biome. The fallback logic:

1. Try to match a bespoke haul for the origin/dest biome pair (existing behavior)
2. If no bespoke match, pick from the generic pool instead (no biome filter)
3. Exclude generics the player is already carrying or that are already offered

Generic hauls use the same payout formula (Manhattan distance x 3). No bonus,
no penalty. They're subsistence income — enough to keep moving, not enough to
get rich.

## UI Treatment

The `IsGeneric` flag flows through `ItemInstance` so the client can style
generic hauls differently — muted text, no destination name reveal, or a
"routine work" tag. Exact styling TBD, but the data path should support it.

## Delivery Flavor

All generic hauls share a single delivery line. No per-item delivery flavor.
This reinforces the "you're running errands" feel without requiring authoring
effort per entry.
