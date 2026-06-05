# TODO

<!-- Migrated from project/TODO.md on 2026-05-15 (M-001). -->

Three buckets toward playable alpha.

--

## 1. Playable Alpha — Code & Mechanics

Features, fixes, and balancing needed for a complete gameplay loop.

### Frontend

- [x] Redesign severe condition end-of-day screen
- [x] UX design: Town — Guild Bank
- [ ] Fix mobile layout — desktop-first design breaks on phones and tablets:
      fixed-width panels (420px inventory mechanics column), 3-column layouts with
      no stacking breakpoint, 20px base font too large for phones, InstrumentCluster
      overlay not adapted for small screens, no touch-friendly target sizing
- [ ] Rewrite any encounters that use single-spacing between paragraphs
- [ ] Combat: cancel monster's later-slot actions when it dies on an early slot
      (currently slots 2/3 still resolve against a corpse, producing nonsense narration
      and zero-damage entries that get stripped client-side)
- [x] Combat: add the standard yellow status banner to the top of the combat screen
      (same as Market/Inventory/etc. — duplicate of in-card vitals so the bar matches
      the rest of the app and gives Flee a permanent home)
- [ ] Combat: cap worksheet panel `max-width: 820px` so cards don't sprawl on wide
      monitors (right column min-width is 420 today, but no upper bound)
- [ ] Combat: rotate the "What's the plan, merchant?" greeting through a small pool
      of one-liners (e.g., "Eyes up.", "Make it count.", "What's it gonna be?"). Pick
      randomly per turn so every fight doesn't open with the same line.

### Map Generation

- [ ] Bisect oversized T1 regions — same technique as T3 bisect (TierAssigner splits by
      distance from city), but inner nodes stay T1 and outer nodes become T2. Prevents a
      mega-T1 region from making an entire biome feel safe.
- [x] Auto-named regions (`MapGenerator.cs` TODO: generated region names for game UI)

### Road Encounter System (Tableau)

- [x] Finish out and test AI authoring tools for tac encounters
- [ ] Sketch out single-paragraph intros for every biome/tier (50 total? 8x5+5x2)
- [ ] ~~Playtest a bunch and tune~~ (ongoing)

### Combat Encounter Triggering

- [x] Wire .fight encounters into the road encounter pipeline — done 2026-06-05, plan at
      `plans/fight_integration.md`. Fights merge into the overworld roll (`[trigger road]`
      opt-in, only the_beast seeded so far), `+combat`/`+chain` link arcs and fights both
      ways, files live under `text/encounters/` (combat/ + arc dirs) and ship via
      `worlds/<name>/combat/`. Remaining: seed fights as they clear balance.
- [ ] Update weapon/armor descriptions in inventory, market, and bank screens to explain
      what they actually do in the RPS combat system — current copy is from the d20 era
      ("+2 to attack rolls" etc.) and doesn't surface the moves the item contributes to
      the player's pool, mutators (heavy/wary/shielding/etc.), or cooldowns. Players can't
      make informed buy/equip decisions without this.

### Rules & Balancing

- [ ] Pad out the Travel Conditions roster — exhausted/freezing/thirsty may be too thin.
      Consider bringing back swamp fever (spirits-draining, Bushcraft-resistible). Review
      whether each biome has at least one natural Travel Condition source. Each new condition
      motivates a new immunity item (mosquito netting, etc.) — important since the skill-bonus
      gear purge will gut the market; immunity gear is the replacement flavor.
- [ ] Nuke equippable boots. Replace with a single non-equippable item (good boots, trail
      boots, whatever) that sits in inventory, consumes a slot, and grants exhaustion immunity.
      Fits the gear-purge ethos and keeps footwear meaningful without a dedicated equip slot.
- [ ] Contemplate nuking Mercantile and folding it into Negotiation, leaving a clean
      two-skill parity: Cunning (encounter checks + resists special attacks) and
      Negotiation (encounter checks + better prices). Frees one tableau slot in the
      arc leveling system. See [[arc_leveling]].
- [ ] Playtest medicine vs. health drain in T3 areas — currently medicine reduces condition
      stacks but you still lose 1 HP if any severe condition remains. Could be too punishing
      in endgame where multi-stack injuries are common and one bandage per night isn't enough.
- [ ] Haul direction balancing — tune destination selection to favor two sweet spots:
      (1) 1-2 steps deeper on the trade graph, pushing exploration forward, and
      (2) way back toward root (Aldgate), rewarding long return trips and encouraging
      players to try new branches and pick up fresh storylets.
- [ ] Quality-gated chapterhouses — scatter additional chapterhouses across the map, each
      unlocked by a faction-standing or arc-completion quality. Solves two problems at once:
      (a) deep-wild settlements currently have no recovery option for severe conditions
      (untreatable poisoned/injured/lattice_sickness stacks force a long retreat), and
      (b) makes faction-friendly outcomes mechanically relevant beyond flavor. Probably plugs
      cleanly into mapgen (roster + placement pass), and into SettlementRunner via a
      `quality >= n`-style gate on the chapterhouse service. Downside: introduces the first
      **canonical qualities** — qualities leaking out of the .enc system into engine code is a
      precedent worth weighing carefully. Worth it, probably.
- [ ] Combat flee balance check. Post-picker, fleeing always succeeds but costs one
      round of monster intent (player slots become Skipped, monster's committed slots
      still resolve). Cunning no longer offers a numeric save. Might be fine — fixed-
      cost flee is honest — but keep an eye on whether T3 monsters punish flee too
      hard or too softly. Cunning-as-flee-modifier (e.g. skip monster carry-stun, or
      skip one slot of monster intent at Trained/Expert) is a potential lever.

### Quality of Life

- [ ] Hide locked choice requirements — currently we show the `requires` condition text to the
      player as a UI hint. This breaks with arc encounters that offer the same choice multiple
      times gated by different mutually-exclusive conditions (e.g. faction standing). Remove or
      rethink the locked-choice display before launch.
- [x] Server "quick start" flag — solved via shady trader encounter (temporary, remove before launch)
- [ ] Biome intro encounters — one-time scripted encounter per biome/tier that fires on
      first entry. `_intro.enc` convention, `SeenBiomeTiers` on PlayerState, `TryPickIntro`
      in selection logic. Design in [[biome_intro_encounters]].
- [ ] More variety in meal labels

### Flavor Text

`lib/Flavor/FlavorText.cs` has real content for region/settlement names and descriptions.
Everything else is a one-liner placeholder.

- [x] Improve settlement and region names

## 2. Deployment, Testing & Hardening

Ship-readiness: hosting, testing, polish, cleanup.

### Deployment & Hosting

- [ ] Set Cosmos DB TTL on `games` container — 30 days (2592000s) to auto-expire idle saves.
      `az cosmosdb sql container update -a <account> -g <rg> -d dreamlands -n games --ttl 2592000`
- [ ] Cloudflare R2 asset CDN — create bucket, attach custom domain, wire up push.sh,
      update web client to use CDN base URL in production. Existing push.sh skeleton works.
      Reference at [[cdn_deployment]].
- [ ] React app hosting — Cloudflare Pages or Azure Static Web Apps, git-connected deploys.
- [ ] GameServer hosting — Azure Functions, App Service, Fly.io, or similar.
      Needs Cosmos DB or equivalent for player state persistence.
- [ ] World build + deploy pipeline — build.sh + push.sh + app deploy as one scripted flow.
      Version-prefix assets for cache busting.
- [ ] Google OAuth login + session reconnect (very late — production only). See [[google_oauth]].

### Testing & Regression

- [ ] CLI integration tests — exercise core loops via CLI against GameServer: encounters,
      movement, inventory, market buy/sell. See [[integration_test_plan]].
- [ ] POI position mismatch — observed a case where the server's in-memory map had a
      settlement at (16,5) but map.json on disk had it at (16,7). Player could enter a
      "ghost" settlement that didn't exist in the data. Server restart fixed it. Root cause
      unclear — map was NOT regenerated between server start and the bug appearing. Need a
      regression test that verifies every node's POI in the server's loaded map matches the
      source map.json.

### Map Polish

- [x] Try different map seeds for production — current seed was the first one generated;
      explore a few alternatives and pick the most interesting layout
- [ ] Replace lake sprite
- [ ] Improve settlement sprite variability — avoid placing identical decals near each other;
      may need more decals (recolored variants)
- [ ] Migrate decal directories to tier-aware structure — rendering code (`PoiPass.cs`,
      `SwampPass.cs`, `HillPass.cs`) still references the old flat dirs (`grass_tufts/`,
      `farm_stuff/`, `bogs/`, `trees/`). Migrate code to load from
      `assets/map/decals/{plains,swamp,forest}/` tier-aware structure, then delete old dirs.
- [ ] Mountain settlement decals invisible in deep mountains — a couple of the settlement
      decals get buried by the tallest mountain peaks. Figure out the minimum image height
      they need to render above peak silhouettes, then resize/repaint the offending decals.

### Cleanup

- [ ] DungeonRoster refactor (`DungeonRoster.cs` TODO: per-dungeon `descriptor.yaml` files)
- [ ] Stale reference docs — [[mapgen_design_foundation]] references ocean/coast
      terrain that no longer exists. Several reference docs may have similar staleness.
- [ ] [[spec_gaps]] checkbox audit — many checked items may not reflect current code.

### Unused Code Audit (2026-03-07)

Results from `jb inspectcode`. Most of these are awaiting final system design, not dead.
None are obviously aged-out — keep for now, revisit when their parent systems are built.

**Stubs awaiting systems:**
- `FlavorText.cs` — 9 placeholder methods (temple, inn, market, weather, etc.)
- `ImperialCalendar.cs` — full calendar type, not yet wired into UI
- `ItemDef.Slots`, `ItemDef.CapacityBonus` — inventory system not finalized

**Utility methods with no callers yet:**
- `Direction.Opposite()`
- `ActionVocabulary.IsValidName()`, `.Validate()`
- `Difficulty.GetInfo()`, `Magnitude.GetInfo()/.ScriptName()`, `TimePeriod.GetInfo()/.ScriptName()`
- `ItemDef.IsValidId()`

**Mapgen:**
- `Noise.Octaves()`, `TerrainPass.Draw()`, `SettlementPlacer.GetTraversableNeighbor()`
- `MapGenerator.FindRegions()`, `PoiPass.BiomeToDungeonFolder`

## 3. Writing

Content authoring — encounters, lore, art. ~163 hours remaining.
Pace: ~24 hr/week (Mon–Thu 2hr, Sat–Sun 8hr, Fri off). Target: late May 2026.

Time estimates: simple .enc ~30 min, .tac add-on ~20 min, arc ~5 hr, locale guide ~1 hr.

### World Building

- [ ] Finish mountain bible

### Encounter Pipeline

- [x] Locale guides for all biome/tier combos
- [ ] Batch-generate skeletons via `generate` command
- [ ] Arc reachability lint — `check` command should walk the encounter graph from Start,
      enumerate all paths (branching on choices × check pass/fail), and warn on: unreachable
      encounters, paths that never reach `+finish_dungeon`/`+flee_dungeon`, and cycles with
      no exit. Treats arcs as directed graphs, not individual files.

### Post-picker-pivot rewrites (not urgent)

Sites that survived the Phase 4 sweep but need real authoring attention. None
block Phase 5+; revisit during content polish.

- [ ] `arcs/plains/grainway_station/Dalla.enc` — rewrite
- [ ] `arcs/plains/metal_beast/Belly of the Beast.enc` — rewrite
- [ ] `arcs/plains/metal_beast/The Gauntlet.tac` — summarize into the arc's
      Start.enc as an .enc check upstream (the .tac itself isn't worth keeping)
- [ ] `forest/tier1/Lost.enc` — rewrite
- [ ] `plains/tier1/Lost.enc` — rewrite
- [ ] **Drop `Road Toll`**; fold the best beats into Tob's `.fight` encounter
      and move Tob to tier 1 as the noob-zone menace
- [ ] **Drop `Collapsed Earthworks`** — not worth rewriting into .enc
- [ ] Review `The Conscripts`, `The Courier`, `The Requisition Line`, `The Scavengers`, `The Gentle Giant`, `The Passage` — tac choices filled; review prose + mechanic balance
- [ ] `Unusual Cargo` — design note, not a rewrite: this encounter implies
      the player should pick an approach **before** the preamble text renders.
      Not a blocker; capture for the picker UX pass.
- [ ] `swamp/tier2/The Hermit of Sallow Fen.enc` — rewrite

### Plains — ~13 hr (Week 1: Apr 5–11)

- [x] Tier 1 — encounters
- [x] Tier 1 — locale guide
- [x] Tier 2 — encounters
- [x] Tier 2 — locale guide
- [x] Tier 3 — encounters
- [x] Tier 3 — locale guide
- [ ] Review Lost.enc files (plains tiers 1–3) — now pool encounters; check prose + mechanic balance
- [ ] Settlement flavor encounters
- [x] Arc — Bride's Cave
- [x] Arc — Metal Beast
- [x] Arc — Grainway Station
- [ ] Arc — Wrenbury (~5 hr)
- [ ] Arc — The City (~6 hr)

### Scrub — ~28 hr (Weeks 2–3: Apr 12–25)

- [x] Tier 1 — locale guide
- [ ] Tier 1 — encounters (~3 more needed)
- [x] Tier 2 — locale guide
- [ ] Tier 2 — encounters (nearly done, 1–2 more)
- [x] Tier 3 — locale guide
- [ ] Tier 3 — encounters (~4 needed)
- [ ] Review Lost.enc files (scrub tiers 1–3) — now pool encounters; check prose + mechanic balance
- [ ] Arc — The Census House (~5 hr)
- [ ] Arc — The Foundry (~5 hr)
- [ ] Arc — The Relay Post (~5 hr)
- [ ] Arc — The Wellhead Station (~5 hr)

### Swamp — ~28 hr (Weeks 4–5: Apr 26–May 9)

- [x] Tier 1 — locale guide
- [ ] Tier 1 — encounters (1–2 more needed)
- [x] Tier 2 — locale guide
- [ ] Tier 2 — encounters (~4 needed)
- [x] Tier 3 — locale guide
- [ ] Tier 3 — encounters (~5 needed)
- [ ] Review Lost.enc files (swamp tiers 1–3) — now pool encounters; check prose + mechanic balance
- [ ] Arc — The Listening Blind (~5 hr)
- [ ] Arc — The Revenakh (~5 hr)
- [ ] Arc — The Tile House (~5 hr)

### Mountains — ~28 hr (Weeks 5–7: May 10–23)

- [x] Tier 1 — locale guide
- [ ] Tier 1 — encounters (1–2 more needed)
- [x] Tier 2 — locale guide
- [ ] Tier 2 — encounters (~4 needed)
- [x] Tier 3 — locale guide
- [ ] Tier 3 — encounters (~5 needed)
- [ ] Review Lost.enc files (mountains tiers 1–3) — now pool encounters; check prose + mechanic balance
- [ ] Arc — The Halfway House (~5 hr)
- [ ] Arc — The Ledgerhaus (~5 hr)
- [ ] Arc — The Stift (~5 hr)
- [ ] Arc — The Zahlenhaus (~5 hr)

### Forest — ~27 hr (Weeks 7–9: May 24–Jun 6)

- [x] Tier 1 — locale guide
- [ ] Tier 1 — encounters (~5 needed)
- [x] Tier 2 — locale guide
- [ ] Tier 2 — encounters (~5 needed)
- [x] Tier 3 — locale guide
- [ ] Tier 3 — encounters (~5 needed)
- [ ] Review Lost.enc files (forest tiers 1–3) — now pool encounters; check prose + mechanic balance
- [x] Arc — The Fugitive
- [x] Arc — The Hermitage
- [ ] Arc — The Lodge (~5 hr)

### Art Assets

- [ ] Vignettes for all encounter types
- [ ] Vignettes for key encounters
