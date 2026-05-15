# Migration Log — M-001

Migrated 2026-05-12 through 2026-05-15.

Source: `project/` (~118 markdown files + supporting HTML prototypes and assorted scratch).
Destinations: `rules/`, `plans/`, `imp/reference/`, `imp/learnings/`, root `TODO.md`.

Substrate convention reference: `imp/_meta/conventions.md`.

## Approach

Worked in eight chunks, classifying by **current function** rather than original
intent — a doc framed as forward-tense whose work has shipped is `reference` (or a
`shipped` plan), not an active plan. Explore subagents triaged each chunk and
produced classification recommendations; I verified the borderline calls against
code state and wrote the substrate entries (preserving prose verbatim with frontmatter
prepended). The largest chunk (design root, 43 files) was bulk-written by a
general-purpose subagent against a pre-classified table I supplied.

Provenance on every entry: `author: migration:M-001`.

## Totals

| Kind        | Count | Path                |
|-------------|-------|---------------------|
| rule        | 4     | `rules/`            |
| plan        | 22    | `plans/`            |
| reference   | 56    | `imp/reference/`    |
| learning    | 9     | `imp/learnings/`    |
| **Total**   | **91**| —                   |

Plus the root `TODO.md` populated from `project/TODO.md`.

(The repo also has 3 additional plans the human authored during the migration
window — `plans/inventory_consolidation.md`, `plans/inventory_slot_refactor.md`,
`plans/skill_tier_rework.md` — outside the migration's scope. They are not
listed below.)

---

## Created

Grouped by chunk for traceability.

### Chunk 1 — `project/encounter-spec/` (7 sources → 7 entries)

- `rules/encounter_format.md` — .enc grammar, parser-enforced
- `rules/encounter_mechanics.md` — verbs, items, factions; includes .tac format (marked
  deprecated)
- `imp/reference/arc_authoring.md` — multi-scene arc structural patterns
- `imp/reference/encounter_philosophy.md` — design values for encounter authoring
- `imp/reference/encounter_voice_and_tone.md` — prose style + LLM anti-patterns
- `imp/reference/changing_encounter_syntax.md` — how-to-extend guide (reclassified from
  "plan" — describes architecture, no in-flight work)
- `plans/storylet_system.md` (state: exploring) — Step 1 shipped, rest pending

### Chunk 2 — `project/screens/` (10 sources → 10 entries; 2 dropped)

- `rules/ui_styles.md` — typography/color/icon hard rules
- `imp/reference/screen_encounter.md`
- `imp/reference/screen_end_of_day_crisis.md`
- `imp/reference/screen_explore.md`
- `imp/reference/screen_inventory.md`
- `imp/reference/screen_settlement.md`
- `imp/reference/screen_splash.md`
- `imp/reference/screen_trade.md`
- `plans/inventory_screen_rebuild.md` (state: shipped — verified Phases 1-4 landed)
- `plans/market_screen_glow_up.md` (state: shipped — verified marketNaming.ts + WaxSeal live)

### Chunk 3 — `project/architecture/` (14 sources → 14 entries; 1 dropped)

- `imp/reference/cdn_deployment.md` — was "plan"; reclassified, system is live
- `imp/reference/client_server_architecture.md` — API contract + 7 sync pitfalls
- `imp/reference/content_deploy_pipeline.md` — was "plan"; shipped (ReloadBundle + push)
- `imp/reference/deployment_ops.md` — production runbook
- `imp/reference/item_data_model.md` — two-layer item architecture
- `imp/reference/map_tiling.md` — was "plan"; shipped (WebP not PNG in live)
- `imp/reference/web_architecture.md` — was "Web UI Plan"; reclassified, shipped
- `imp/reference/web_tech_stack.md` — React/shadcn/Vite decision record
- `plans/cli_play_harness.md` (state: shipped) — Orchestration lib + ui/cli/ live
- `plans/google_oauth.md` (state: exploring) — no implementation yet
- `plans/poi_map_markers.md` (state: active) — specced, not yet wired
- `plans/ui_component_library.md` (state: shipped) — doc self-reports complete
- `plans/flavor_library.md` (state: exploring) — most generators unbuilt
- `imp/learnings/cli_resurrection_gap_analysis.md` (status: stale) — gaps closed but the
  library-boundaries rubric and rationale for what got retired (water-centric GameState,
  TimeOfDay→TimePeriod) is still useful

### Chunk 4 — `project/reference/` (10 sources → 10 entries; 4 dropped, 1 raw-material kept)

- `imp/reference/biome_brief_forest.md`
- `imp/reference/biome_brief_mountains.md`
- `imp/reference/biome_brief_plains.md`
- `imp/reference/biome_brief_scrub.md`
- `imp/reference/biome_brief_swamp.md` — includes Revathi language reference
- `imp/reference/hiking_distances.md` — canonical mapgen spacing numbers
- `imp/reference/mountain_rendering.md` — verified shipped in `mapgen/Rendering/MountainPass.cs`
- `imp/reference/swamp_culture.md` — Revënakh philosophy companion to swamp brief
- `imp/learnings/mapgen_design_foundation.md` (status: current) — graph-not-grid framing,
  rivers-as-edge-position rationale
- `imp/learnings/original_dungeon_plan.md` (status: stale) — predates arc system

### Chunk 5 — `project/combat/` (3 sources → 3 entries)

- `plans/d20_combat_attempt.md` (state: abandoned) — d20 design (stances/edges/AC/HP/.cmb
  format) was partially built then thrown out for RPS
- `plans/tac_removal.md` (state: active) — Track F (fold .tac → inline .enc) still the
  live plan; Track P moot (.cmb never landed)
- `imp/reference/combat_monster_roster.md` — canonical 18-sprite roster with lead-in flavor

### Chunk 6 — `project/design/` root (43 sources → 30 entries; 2 dropped, 1 raw-material kept)

**Rule (1):**
- `rules/icon_usage.md` — definitive SVG→use-case mapping; cited in CLAUDE.md

**References — combat-adjacent (4):**
- `imp/reference/super_rps.md` — three-slot combat core (live)
- `imp/reference/weapon_classes.md` — RPS weapon families (live)
- `imp/reference/armor_classes.md` — RPS armor classes (live)
- `imp/reference/dice_mechanics.md` — d20 skill check framework (still alive for
  non-combat resolutions)

**References — conditions (2, both `status: superseded`):**
- `imp/reference/conditions_design.md` — superseded by condition_rework (source self-reports it)
- `imp/reference/conditions_list.md` — superseded by condition_rework

**References — trade/haul (5):**
- `imp/reference/trade_economy.md`
- `imp/reference/trade_goods.md`
- `imp/reference/trade_rumors.md`
- `imp/reference/haul_economy.md`
- `imp/reference/dynamic_hauls.md`

**References — inventory/items (5):**
- `imp/reference/inventory_design.md`
- `imp/reference/haversack.md`
- `imp/reference/equipment.md`
- `imp/reference/itemization.md`
- `imp/reference/documents.md`

**References — gameplay loop (4):**
- `imp/reference/end_of_day_maintenance.md`
- `imp/reference/foraging.md`
- `imp/reference/pc_status.md`
- `imp/reference/skill_flavor.md`

**References — world/settlement (4):**
- `imp/reference/world_summary.md`
- `imp/reference/settlements_design.md`
- `imp/reference/inn_and_chapterhouse.md`
- `imp/reference/mapgen_tuned.md`

**References — mapgen (1):**
- `imp/reference/river_decals.md`

**Plans (10):**
- `plans/condition_rework.md` (state: active) — partial implementation
- `plans/trade_load_system.md` (state: shipped) — shipped as the haul system
- `plans/haversack_refactor.md` (state: active) — coupled to spirits_economy
- `plans/click_to_move.md` (state: active) — tripEstimate exists; full flow not shipped
- `plans/spirits_economy.md` (state: active) — passive regen removed; rest pending
- `plans/lost_encounters.md` (state: active)
- `plans/biome_intro_encounters.md` (state: active)
- `plans/settlement_names.md` (state: shelved) — blocked on Revathi language rework
- `plans/cli_integration_tests.md` (state: exploring) — specced; low priority
- `plans/leaderboard.md` (state: shelved) — out of scope

**Learnings (4):**
- `imp/learnings/spec_gaps.md` (status: stale) — most blockers resolved or pivoted past
- `imp/learnings/locked_design_decisions.md` (status: stale) — early-design snapshot
  patched with migration note
- `imp/learnings/design_seed.md` (status: stale) — original creative brief; patched
- `imp/learnings/gear_gap_analysis.md` (status: current) — gear-bonus reachability audit

### Chunk 7 — `project/design/` subdirs (20 sources → 5 entries; 14 kept as raw-material, 1 dropped)

- `imp/reference/arc_process.md` — current LLM-pipeline authoring methodology with
  scene types + constraint sheet (distinct from `arc_authoring.md`'s scene-structure
  patterns)
- `imp/reference/arc_keys_and_rewards.md` — 20-arc key+reward lookup; note that
  weapon-stat lines may be stale post-RPS
- `plans/rps_combat_pivot.md` (state: shipped) — canonical 10-phase RPS implementation plan
- `plans/card_combat_attempt.md` (state: abandoned) — overview body; references the
  supporting docs still in `project/design/encounter-redesign/` as raw-material
- `imp/learnings/dungeon_seeds.md` (status: stale) — 25 concept seeds; 20 landed as arcs

### Chunk 8 — orphans (3 sources → 2 substrate entries + TODO merge)

- `imp/reference/integration_test_plan.md` — manual CLI integration test procedures
- `imp/learnings/code_reusability.md` — "what would port to another game" assessment
- `TODO.md` (root) — populated from `project/TODO.md`; inline path references converted
  to substrate `[[wikilink]]`s

---

## Left as raw-material in `project/`

The substrate convention: substantive material that's not canonical AND doesn't fit
as a companion dir stays in `project/`. The following dirs/files stayed put.

### `project/design/encounter-redesign/` (14 files)

Supporting design docs for the abandoned card-based encounter system. Referenced from
`plans/card_combat_attempt.md` (the canonical abandoned-plan entry) rather than
duplicated. Includes:

- Root: `combat.md`, `card_archetypes.md`, `card_lists.md`, `deckbuilding.md`,
  `fun_deck.md`, `mma-inspired.md` (the "combat sports" framing the user named as
  a dead-end), `tac_skeleton_gen.md`, `traverse.md`
- `old/`: `aspect-based.md`, `aspects.md`, `journey_alt.md`, `journey_hazards.md`,
  `journey_hazards_blocks.md` — pre-card brainstorming, already retired during the
  card-system phase
- HTML prototypes: `prototype.html`, `traverse-prototype.html`

These are all dead-system documents that captured an entire design effort. Once
`tac_removal` ships and the `lib/Tactical/` + `.tac` files are gone, this whole
directory can be dropped (see Recommend Dropping below). Until then, keeping them
in place preserves the historical trail without bloating the substrate.

### `project/reference/swamp_junk.md` (190 lines)

Tier-by-tier encounter design skeleton for the swamp arc. Substantive but not
substrate-shaped — neither a finished plan nor a frozen learning. Sits beside the
swamp lore docs and informs encounter authoring for the swamp arcs.

### `project/combat/combat_screen.html` + `sprites.txt`

HTML prototype for the (now-RPS) combat screen, plus a bare sprite list superseded by
`combat_monster_roster.md`. Prototype is genuine raw material; sprites.txt is a stub.

### `project/design/condition_redesign/apocalypse_driver.html`

HTML prototype paired with the (dropped) apocalypse_driver.md.

### Three orphan files reabsorbed (not "kept" — merged):

- `project/TODO.md` → merged into root `TODO.md`
- `project/integration-test-plan.md` → `imp/reference/integration_test_plan.md`
- `project/reusability.md` → `imp/learnings/code_reusability.md`

---

## Recommend dropping

Files I think should be deleted but the user should pull the trigger on.

### Outright superseded / fragments

- **`project/reference/economy.md`** — incomplete; ends mid-sentence at line 39.
  Superseded by `plans/trade_load_system.md` (the haul system) and
  `imp/reference/trade_economy.md`.
- **`project/reference/encounter_concept.md`** (221 lines) — pre-spec template
  brainstorming about danger/help_request/contemplate categories. Wholly superseded
  by `rules/encounter_format.md` + `rules/encounter_mechanics.md` +
  `imp/reference/encounter_philosophy.md`.
- **`project/reference/encounters.md`** (73 lines) — stale LLM prompt template with
  superseded vocabulary (`give_gear_key`, `get_lost`, `give_water`). The current
  mechanic verbs are `+add_item`, `+damage_health`, `+add_tag`, etc.
- **`project/reference/png_renderer_plan.md`** — 19-line kanban-style checklist.
  Mapgen has the actual rendering code; this never carried real content.

### Empty or stub files

- **`project/screens/trip_planner.md`** — zero-length file. `tripEstimate()` is now
  inline in `Explore.tsx`.
- **`project/screens/typography.md`** — URL collection + partial icon stub; all
  useful content is in `rules/ui_styles.md`.
- **`project/combat/sprites.txt`** — bare sprite list, fully superseded by
  `imp/reference/combat_monster_roster.md`.
- **`project/combat/.sprites.txt.kate-swp`** — kate editor swap file.

### Misfiled / out of scope

- **`project/design/condition_redesign/apocalypse_driver.md`** (+ html) — describes
  a wholly different game ("Apocalypse Driver", post-apocalyptic driving sim). Not
  Dreamlands content. Either delete or move out of `project/` if kept for inspiration.
- **`project/design/encounter-redesign/combat.md`** — d20-era combat design,
  superseded by `plans/rps_combat_pivot.md` + `imp/reference/super_rps.md`.
- **`project/design/courier_system.md`** — pre-haul courier dispatch design. Wholly
  superseded by `plans/trade_load_system.md` (which shipped as the haul system).
- **`project/design/combat.md`** — node-graph "posture combat" dead-end (Locked/
  Pressed/Exposed nodes + intent verbs). The "combat sports" framing the user named.
- **`project/architecture/web_backend.md`** — conversational LLM-chat exploration
  about Azure/Cosmos/Table Storage trade-offs. The live system is fully covered by
  `imp/reference/deployment_ops.md` and `imp/reference/client_server_architecture.md`.
  Nothing canonical was lost.
- **`project/design/dungeons/generative_narrative.md`** — academic procedural-narrative
  research; never converted to actionable substrate.

### After `plans/tac_removal.md` ships

When the 14 `.tac` files are folded into inline `.enc` checks and `lib/Tactical/` is
deleted, the entire `project/design/encounter-redesign/` directory can be dropped
(it's all dead-card-system design with no remaining downstream value).

### `project/` itself, post-cleanup

After applying the deletions above, what's left in `project/` should be:

- `project/design/encounter-redesign/` (until tac removal completes)
- `project/reference/swamp_junk.md`
- A couple of HTML prototypes

That's a thin enough residue that `project/` could either be retained as the
raw-material namespace or, after the encounter-redesign dir is cleaned out,
collapsed entirely.

---

## Uncertain

Items I couldn't classify confidently. None blocked the migration; flagging for
human decision.

- **`imp/learnings/locked_design_decisions.md`** and **`design_seed.md`** were both
  early-era snapshots; I patched them to `status: stale` with migration notes
  explaining the partial obsolescence (combat pivots, skill advancement decided
  "none"). If you'd rather drop them entirely, both are safe to delete — the
  rationale isn't load-bearing.
- **`imp/reference/conditions_design.md`** and **`conditions_list.md`** — marked
  `status: superseded` because the source docs self-report supersession by
  `condition_rework.md`. Worth deciding: do you want the historical record kept,
  or should they collapse into `plans/condition_rework.md` once that plan ships?
- **`plans/cli_integration_tests.md`** state — left as `exploring`. The matching
  reference (`integration_test_plan.md`) exists; only the automation harness is
  pending. Could equally be `active` if you intend to ship the harness soon.
- **`plans/condition_rework.md`** state — left as `active` per the chunk-6 table.
  The two-tier severity model is in code; the UX program (toast vs crisis screen)
  isn't fully shipped. Defensible to flip to `shipped` and file the remaining UX
  work as a separate plan.

---

## Convention decisions

Decisions I made about how dreamlands uses the substrate. Worth surfacing because
they shape future migrations and the gnome's behavior.

1. **`project/` is the raw-material namespace.** Substantive but not-canonical
   docs stay here. The substrate references them by path. This avoided
   duplicating ~14 dead-system docs into companion dirs.

2. **Provenance string for migration:** `author: migration:M-001`. Per the
   conventions doc, this is one of the allowed authors. Future bulk-imports
   should bump the migration number (`M-002`, etc.).

3. **Migration notes for state-changed plans.** Plans that shipped, abandoned, or
   went stale get a `> Migration note (M-001, YYYY-MM-DD): ...` block immediately
   after the frontmatter, before the original body. This makes the state change
   readable without forcing edits to the source prose. Pattern:

   ```
   ---
   <frontmatter, including state and updated date>
   ---

   > Migration note (M-001, 2026-05-15): <what changed and why>.

   <original body verbatim>
   ```

4. **Wikilinks `[[name]]` reference substrate entries by their filename slug**
   (without extension). Used in:
   - Plan/learning frontmatter via implicit `topics` and `supersedes`
   - Migration notes that cross-reference other entries
   - The root `TODO.md` (converted from inline `project/.../foo.md` paths)

5. **References vs. learnings vs. plans:** stuck close to the conventions doc.
   When in doubt about reference vs. learning, leaned on the "is the system
   live now?" code-cross-check. When in doubt about reference vs. rule, leaned
   on "would breaking this alarm the project?" — only 4 things made the rule cut
   (encounter format, encounter mechanics, UI styles, icon usage).

6. **Companion dirs (`plans/<slug>/`) not used.** Considered for the card-combat
   attempt and the dungeon-pre-arc set, but the substantive supporting docs were
   already in `project/`. Duplicating into companion dirs would have doubled
   disk usage for material the user marked as recommend-drop anyway. Pointed at
   `project/` from the canonical entry instead.

7. **Frontmatter `touches.files` lists code paths** when they can be verified live
   in HEAD. When the doc is purely about design intent with no code anchor, the
   field is omitted (or only `features:` is populated). Goal: drift-tracking only
   makes sense if the cited paths exist.

8. **Status semantics applied:**
   - `current` — doc reflects how the system works/is intended now
   - `stale` — doc is historical; either the system moved past it (combat pivots),
     or many specific claims are outdated, but the broader framing is still useful
   - `superseded` — explicitly replaced by another doc; the source said so or
     the replacement is unambiguous

---

## Next actions (for the human)

1. Review the **Recommend dropping** list above. Most are safe deletes; a few
   ("apocalypse_driver" and "web_backend" come to mind) might be worth keeping
   as inspiration archives even though they're not substrate-bound.
2. After `plans/tac_removal.md` ships, drop `project/design/encounter-redesign/`
   en masse.
3. Decide what to do with the three "Uncertain" items in the previous section.
4. The gnome's drift-tracking will run against these `touches:` entries the next
   time `imp tidy` fires. Expect some false positives on the partial-implementation
   plans (spirits_economy, haversack_refactor, condition_rework).
