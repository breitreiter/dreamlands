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
| Dusty Ledger | Looks like someone's to be audited. Or was audited. Anyway, the book needs to move. |
| Tax Remittance | A locked strongbox of collected duties. Heavy for its size. |
| Requisition Forms | A stack of forms requesting supplies. Technically urgent, practically ignored. |
| Merchant's Samples | Fabric swatches and spice vials. A trader wants them moved to the next market. |
| Mail Bundle | A stack of letters tied with twine. Courier work but coin is coin. |
| Blank Guild Ledger | The factor ended up with an extra and needs it moved to the next office in line. |
| Box of ink jars | They say the guild runs on ink and blood, but we just need you to deliver the former |

Delivery flavor is shared across all generic hauls:
- Signed and accepted without comment.
- The factor nods to a stack of packages and returns to his work. You add your delivery to the pile.
- The factor's young assistant accepts the package with genuine enthusiasm.
- The factor looks the package over with an appraising eye, and, presumably satisfied, signs the ledger.
- The factor processes the package with remarkable efficiency, then dispatches you on your way.
- You wait an eternity while the trader in front of you argues about a pricing issue, then finally deliver the package without ceremony.
- The factor is nowhere to be found. With some trepidation, you stash the package below their desk and sign the ledger yourself.
- The factor is eating a hearty meal and invites you to join. You decline, but leave the package in his greasy hands.
- The factor has fallen ill, but insists on performing her duties. She pauses occasionally to cough violently before returning to the work. You're glad to be rid of the package.
- The factor insists the package was due two days ago. You explain you delivered it as quickly as the roads allowed. He says he'll file a complaint.
- The factor is asleep at his desk. You nudge him awake and, after a moment's disorientation, he starts to explain that he was entirely awake. You don't argue the point and deliver the package.
- The factor seems fidgety and furtive, but she has a guild ring. You drop off the package and hope for the best.
- The factor is having a loud conversation with a local. You are not invited to join. You're directed where to drop the package off with hand signals. You do so and move on.

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
