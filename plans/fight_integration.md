---
kind: plan
title: "Fight Integration"
state: active
created: 2026-06-05
updated: 2026-06-05
status: current
touches:
  features: [combat, encounters, travel, bundling]
provenance:
  author: claude
---
# Fight Integration

Wire `.fight` encounters into the encounter system proper. Today the RPS engine,
combat screen, and server endpoints all work, but fights are only reachable via the
dev "Pick a fight" overlay in Explore. This plan makes fights first-class in two roles:

1. **Road encounters** — randomly triggered during travel, mixed into the same
   biome/tier pool as `.enc` road encounters.
2. **Arc participants** — launched from a `.enc` choice (`+combat <id>`) and able
   to chain back into an arc on resolution (`+chain <id>` in outros).

Supersedes the "Combat Encounter Triggering" item in `TODO.md` (first bullet;
the weapon/armor description rewrite stays a separate task).

## Design decisions (locked 2026-06-05)

- **Unified pool**: fights join the same category pool as `.enc` road encounters;
  the existing travel roll picks from the merged list. No separate combat-chance knob —
  density self-balances with content count.
- **Spawn location = path**: biome/tier comes from the directory convention
  (`combat/forest/tier3/...`), same as `.enc`. No biome/tier frontmatter override.
- **Chaining both directions**: `+chain <enc_id>` in fight outros, `+combat <fight_id>`
  in `.enc` mechanics.
- **File home**: `text/encounters/combat/<biome>/tier<n>/` for road fights; arc fights
  live in their arc directory (e.g. `text/encounters/arcs/forest/the_lodge/the_beast.fight`).
  The combat loader scans the whole `text/encounters/` tree for `.fight` files.
- **`[persistent]` frontmatter** (locked 2026-06-05, amends the pooling rule): a
  persistent fight is never retired, even by winning — recurrence is a spawn
  property, not an outro script. Validation requires a `[requires]` gate on
  persistent fights. Motivating case: The Beast (the_lodge arc) roams forest T3
  roads until the lair fight sets `the_lodge.beast_defeated`.

## Current state

What exists (all working):

- Parser `lib/Encounter/CmbParser.cs`: frontmatter `[title]`, `[image]` (monster
  sprite), `[blood]`, `[stats hp=N]`, `[repool]`; sections `* move`, `* intro`,
  `* win`, `* lose`; `+verb` mechanics in win/lose blocks flow through
  `Mechanics.Apply()` (gold, tags proven in files).
- `lib/Encounter/CombatBundle.cs`: on-disk loader, id = relative path, category from
  directory, tier inferred from `/tierN/`.
- `lib/Combat/` runner + `CombatOrchestrator`; server endpoints `combat/list`,
  `combat/begin`, `combat/action`, `combat/rescue`; full combat UI.

What's missing:

- No `[trigger]`, `[background]`, or `[requires ...]` frontmatter.
- No `* flee` section — flee mechanics hardcoded empty (`CombatRunner.cs` ~line 172).
- `[repool]` parses but nothing consumes it.
- `EncounterSelection.PickOverworld()` (`lib/Orchestration/EncounterSelection.cs:41`)
  only consults `session.Bundle`; `session.CombatBundle` is loaded but never read
  outside the dev picker.
- No chaining in either direction.
- `.fight` files live at `tools/combat-prototype/Monsters/` and are only found via a
  dev fallback in `GameData.cs`; worlds have no `combat/` content, so deployed builds
  would have zero fights. `worlds/*/update-encounters.sh` doesn't touch fights.
- `EncounterCli check` doesn't validate `.fight` files.

## Phase 1 — Format: frontmatter + flee section [DONE 2026-06-05]

Landed as planned, plus `[persistent]` (see design decisions). One discovery:
fight outro mechanics had been using `+gold`/`+tag`/`+set`, which
`Mechanics.ApplyOne` silently no-ops (unknown verbs return null) — combat wins
never actually granted gold or set tags. All 20 prototype files rewritten to
the canonical `+give_gold`/`+add_tag`. Remaining check failures in the
prototype dir are pre-existing em-dashes in prose; rewrite those during the
balance pass, before the Phase 4 move into `text/encounters/`.

Parser (`CmbParser.cs`) and model (`CombatEncounter.cs`):

- `[trigger road|none]` → `CombatEncounter.Trigger`. **Default `none`** so arc fights
  and untagged prototypes never leak into the road pool; road fights opt in.
- `[background <path>]` → `CombatEncounter.Background`. Optional override for the
  combat backdrop; when absent, keep the current server-side biome mapping
  (`BuildCombatBiomeImage()` in `GameFunctions.cs`).
- `[requires <condition>]` (repeatable) → `CombatEncounter.Requires` (list of raw
  condition strings). Same vocabulary as `.enc` requires — evaluated via
  `Conditions.Evaluate()`, so `tag`, `quality`, `has`, `meets` all work for free.
- New `* flee` section: prose + mechanics, parsed identically to `* win`/`* lose` →
  `FleeText` / `FleeMechanics`.
- No `+repool` in fights — recurrence is implicit (see Phase 2). Reject the verb
  in `.fight` validation, and delete the orphaned `[repool]` frontmatter from the
  parser and the one or two files that use it. (`+repool` remains a `.enc`-only
  verb. Pooling rules are enshrined in `rules/encounter_mechanics.md`
  §Pooling & recurrence.)

Validation: extend `EncounterCli check` to parse `.fight` files and verify
mechanics verbs, requires conditions, and `+chain`/`+combat` target syntax.

Tests in `tests/Encounter.Tests` for each new token.

## Phase 2 — Selection: fights in the road pool [DONE 2026-06-05]

Landed as planned. Notes: `PickOverworld` returns `RoadPick`; fight categories
match both `<biome>/tier<n>` (prototype dir) and `combat/<biome>/tier<n>`
(post-relocation) during the transition; the travel response carries
`stopReason: "combat"`. The only seeded road fight so far is the_beast
(forest/tier3, persistent) — everything else stays `[trigger none]` until it
clears balance.

- `CombatBundle`: add `GetRoadPool(category)` (or reuse `GetByCategory` + trigger
  filter at call site) mirroring the `.enc` filter chain.
- `EncounterSelection.PickOverworld()` becomes a merged pick. Introduce a small
  result type since the two encounter types share no base:

  ```csharp
  public sealed record RoadPick(Encounter.Encounter? Enc, CombatEncounter? Fight);
  ```

  Build both eligible lists (category match, `Trigger == "road"`, not used,
  requires pass), concatenate, roll once over the combined count. Road fight
  category is `combat/{biome}/tier{n}` — derive from `GetCategory(node)` with the
  `combat/` prefix.
- **Used-marking — only winning consumes a fight**: on a win, record the fight in
  `PlayerState.UsedEncounterIds` with a `fight:` prefix
  (e.g. `fight:combat/plains/tier1/bandit`) — unless the fight is `[persistent]`,
  which is never marked. Lose and flee never mark — the threat wasn't removed, so it stays in the pool
  (the T1 plains bandit keeps harassing you until you beat him; defeating him is
  the small triumph that retires the fight). A fight that must be one-shot
  regardless of outcome sets a tag in all three outros and gates itself with
  `[requires tag ...]` — no dedicated verb until content wants one.
- Travel handler in `GameFunctions.cs` (the existing 50% roll site): when the pick
  is a fight, start it through the same path `combat/begin` uses
  (`CombatOrchestrator`), so the response is the normal combat mode payload. No new
  client states — the web UI already renders `mode == "combat"`.

## Phase 3 — Outro scripting: flee + chaining [DONE 2026-06-05]

Landed as planned. Notes: `+chain` resolves at combat-resolution time (fail
fast) and stores the qualified id in `PlayerState.PendingEncounterChain`; the
win/flee coda's Continue triggers a state refetch and GetGame launches the
chained .enc (closed-tab resilient). `+combat` ends the encounter and begins
the fight in the same request — the choice's outcome prose is NOT shown, so
put transition prose in the fight's intro. Check enforces context: `+chain`
fight-side only (win/flee, not lose), `+open`/`+combat` .enc-side only;
unqualified targets must have a sibling file. the_beast_cornered now sets
`the_lodge.beast_defeated` (was the_beast_slain, which would never have
closed the_beast's gate).

- `CombatRunner`/`CombatOrchestrator`: on flee, apply `FleeMechanics` and show
  `FleeText` in the coda (today flee applies nothing).
- **`+chain <enc_id>`** (win/flee blocks; lose means rescue, no chain): after the
  outcome coda is acknowledged, launch the named `.enc` instead of returning to
  Exploring. Resolution rules mirror `EncounterSelection.ResolveNavigation`:
  short id relative to the fight's own directory first, then qualified lookup.
  Store the pending chain target on `PlayerState` (the coda is a separate
  request/ack round-trip).
- **`+combat <fight_id>`** in `.enc` mechanics: new verb in `Mechanics.Apply()`
  vocabulary that suspends the current encounter flow and starts the named fight.
  Same short-id-then-qualified resolution, relative to the encounter's category —
  so `the_lodge/The Lair.enc` can say `+combat the_beast`. This is the arc
  integration point: enc → fight → (`+chain`) → enc.
- Loot: no new verb needed — `+item <id>` already exists in the mechanics
  vocabulary; start actually using it in win blocks.

## Phase 4 — Relocation + build pipeline [DONE 2026-06-05]

Landed as planned, with details: 19 road fights moved to
`text/encounters/combat/<biome>/tier<n>/`, the_beast_cornered into the
the_lodge arc dir (and gained `[background forest/forest_tier_3_1]` since
arc fights have no tier for the biome-backdrop default).
`update-encounters.sh` mirrors all `.fight` files into `worlds/<name>/combat/`
preserving text/encounters-relative paths, so ids are identical in dev
(fallback root = `text/encounters/`) and deployed worlds; the dir is
gitignored like the other world artifacts. `ops/reload-bundle` now reloads
the combat bundle too. `BuildCombatBiomeImage` handles the `combat/` and
`arcs/` category prefixes and honors `[background]`.
`tools/combat-prototype/` is gone.

- Move `tools/combat-prototype/Monsters/<biome>/tier<n>/*.fight` →
  `text/encounters/combat/<biome>/tier<n>/`. Add `[trigger road]` to each fight
  that should spawn on roads (per `project/combat/monster_inventory.md` roster).
- `CombatBundle.LoadDirectory` root becomes `text/encounters/` (it already scans
  recursively, so arc-dir fights are picked up; ids become `combat/...` and
  `arcs/...` naturally).
- World build: `update-encounters.sh` copies the `.fight` tree into
  `worlds/<name>/combat/` (preserving relative paths) — or, if we'd rather ship one
  artifact, extend `bundle` to emit `combat.bundle.json` and add a JSON load path
  to `CombatBundle`. Start with the file copy; bundle later if it annoys.
- `GameData.cs`: point at `worlds/production/combat/`, keep the prototype fallback
  only until the move lands, then delete it. Wire fights into the
  `ops/reload-bundle` hot-reload.
- Combat sprite/background assets: confirm `[image monsters/...]` and
  `[background ...]` paths resolve under `worlds/<name>/assets/` like vignettes do;
  add to the webp conversion step if sources are png.

## Phase 5 — Polish

- Keep the dev "Pick a fight" overlay (it's useful for tuning) but have it read the
  relocated bundle.
- `the_lodge` arc as the proving ground: `The Lair.enc` hands off via `+combat`,
  the beast fight chains back via `+chain` on win; flee leaves the beast in play
  (implicit) and sets a tag the arc can react to.
- Update `project/encounter-spec/format.md` (or a sibling `fight_format.md`) with
  the final `.fight` spec, and check off the TODO item.

## Open questions

- Should a fight loss (rescue path) be able to script anything beyond the existing
  lose mechanics — e.g. chain into a "you wake up robbed" enc? Deferred; lose
  already supports mechanics, chaining from lose can come later if an arc wants it.
- Per-fight spawn weighting was considered and rejected for now (unified pool);
  revisit only if playtests show fight density is wrong.
