# Code Reusability Assessment

*Rough sketch — not exhaustive. Assumes "another game" is broadly similar: turn-based, overworld traversal, text encounters, settlement economy.*

---

## Freely Reusable (take it and go)

**lib/Encounter/** — The .enc parser, bundle loader, and conditional/choice model are fully content-agnostic. The format itself is a good design. You'd keep all of this.

**lib/Game/** — The stateless `(state, args, balance, rng) → (state, results)` architecture is the most transferable asset here. SkillChecks, Conditions, MechanicResult types, Choices resolution — all of it survives. BalanceData.Default would be replaced, but the pattern stays.

**text/encounter-tool/** — The check/bundle/generate tooling works on any .enc content. The LLM generation pipeline (oracle fragments, locale guides, haul-generate) is reusable workflow, not Dreamlands-specific.

**server/GameServer** — If you kept .NET + Azure Functions, the dispatch/session/Cosmos wiring would mostly survive. It's thin glue.

---

## Partially Reusable (rewrite the filling, keep the shell)

**lib/Map/** — Grid types, Node/Region/Direction, serialization: all reusable. POI kinds, dungeon roster, terrain types: replace. ~50% stays.

**mapgen/** — The grid generation, terrain assignment, and tile-slicing pipeline are solid and generic. The content populator pipeline (dungeons, settlements, encounter slots) stays as a pattern but all the specific placers get replaced. SkiaSharp rendering layer stays. ~60% stays, 40% is Dreamlands world-building.

**lib/Rules/** — ActionVocabulary and ArgType registry: reusable. The actual verbs, item IDs, skill names, enum values: replace. ~30% stays.

**ui/web/** — The React + Leaflet map viewer, shadcn setup, and style system: reusable. All the screen layouts (encounter, settlement, market, inn) encode Dreamlands' specific UX flows. You'd keep the component scaffolding and redo the screens. ~40% stays.

---

## Mostly Not Reusable

**lib/Flavor/** — Content by definition. Architecture is fine but there's almost nothing to keep.

**worlds/ build pipeline** — The shell scripts and symlink convention are simple enough to redo in an hour. Not worth counting.

---

## Summary

| Layer | Reuse % | Notes |
|---|---|---|
| Game engine (stateless core) | ~85% | Best asset. Architecture is the win. |
| Encounter system (parser + tooling) | ~90% | Format is generic, tooling is generic |
| Server / session management | ~70% | Thin glue, mostly stays |
| Map library | ~50% | Types yes, world-specific POIs no |
| Mapgen | ~60% | Pipeline yes, content no |
| Rules / vocabulary | ~30% | Pattern yes, Dreamlands nouns no |
| Web UI scaffolding | ~40% | Component setup yes, screens no |
| Flavor / encounters / art | ~0% | Expected |

**Overall**: the architecture and tooling are the reusable assets — probably 4-6 weeks of work saved rather than starting from scratch. The content layer (which is most of the actual effort) is a full rebuild.

The single biggest transferable asset isn't a library — it's the `.enc` format and the LLM-assisted authoring workflow. That pipeline for writing, checking, generating, and bundling encounter content would work for any narrative game.
