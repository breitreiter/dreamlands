---
kind: plan
title: D20 Combat Landing Plan (abandoned for RPS pivot)
state: abandoned
created: 2026-04-26
updated: 2026-05-12
status: superseded
touches:
  files:
    - lib/Combat/
    - lib/Encounter/Move.cs
    - project/combat/monster_inventory.md
  features: [combat, abandoned]
supersedes: []
provenance:
  author: migration:M-001
---

> Migration note (M-001, 2026-05-12): **This plan is abandoned.** The d20-style
> design captured below (stances, edge counters, momentum, AC/HP, attack rolls,
> dagger DoT, `.cmb` format) was partially built (engine integration, weapon
> class parity) before being thrown out in commit a184144 "Pivot combat to
> three-slot RPS." The live combat system uses a three-slot RPS prediction game
> (see CLAUDE.md memory: `project_rps_combat.md`); the `lib/Combat/` directory
> was rewritten around that model. `.cmb` files never landed as a content
> format. Retained as a historical record of the design that didn't survive
> contact with playtesting.
>
> **Still valid from this doc:**
> - The 18-sprite monster roster (now in [[combat_monster_roster]])
> - The "no combat in Mountains" decision (still holds)
> - The "combat-in-progress save state on PlayerState" architectural pattern
>   (carried into the RPS engine)
> - Death routes to chapterhouse end-of-day flow (still holds)

# Combat Landing Plan

Tracking doc for taking the combat pivot from prototype to shipped feature.

## Authoritative references

- Design philosophy: `project/design/combat_pivot.md`
- Weapon classes: `project/design/weapon_classes.md`
- Armor classes: `project/design/armor_classes.md`
- Monster roster + flavor intros: `project/combat/monster_inventory.md`
- UI mockup: `project/combat/combat_screen.html`
- Prototype runner: `tools/combat-prototype/`
- Sample `.cmb`: `tools/combat-prototype/gorzog.cmb`

## Locked decisions

- **Combat death** routes to the existing end-of-day death flow ("many hands carry you back to the chapterhouse"). No new death surface.
- **Combat-in-progress save state** lives in the player Cosmos doc as `CombatState? ActiveCombat` on PlayerState. Null when not in combat, cleared on win/lose/flee. No client-side caching. Combat is turn-based and human-paced; one roundtrip per turn is invisible UX-wise and pause-and-resume comes for free.
- **No combat in Mountains.** The 18 sprites cover Plains/Scrub/Swamp/Forest. Mountains stays cold.
- **No PC bleed.** `inflict_condition injured` is the only damage-over-time path against the player; standard resists apply. Daggers may apply a DoT-injured to monsters only (Phase 2).
- **LLM-drafted `.cmb` skeletons are allowed** to reach test-ready faster, then hand-tune.

## Phase 1: Engine integration

Goal: main game loads and runs a `.cmb` encounter end-to-end.

Build:
- `lib/Encounter/`: `.cmb` parser + model, ported from `tools/combat-prototype/Cmb/`
- `lib/Combat/`: stateless resolver matching the existing `(state, args, balance, rng) -> (state, results)` pattern. Turn loop, attack resolution, save resolution, intent preview state, condition application.
- `lib/Game/PlayerState.cs`: add `CombatState? ActiveCombat`. Wire damage through existing Spirits-then-Health ablation. Health at 0 routes to the existing death flow.
- Encounter runner / orchestration: route `.cmb` files to the combat resolver instead of the choice tree.
- `tests/Combat.Tests/`: deterministic resolver tests (seeded RNG).

Notes:
- Rename the prototype's `bleed` mechanic to align the verb with the conceptual model (one mechanic, monster-target only).
- `tests/Tactical.Tests/` is empty: rename or delete.
- Sword stances only at this phase.

Done when: gorzog.cmb runs end-to-end through the main game CLI.

## Phase 2: Weapon class parity

Goal: all three weapon classes implemented in the resolver.

Build:
- Axe momentum: +1 per attack, resets on non-attack. Block action grants +4 AC this turn and clears momentum.
- Dagger Edge: caps at 3. Evade grants +3 AC and +1 Edge. Attack rolls N+1 attacks (N = Edge), each with a per-hit chance to apply DoT-injured to the monster (proc rate set in Phase 5).
- Surprise grants daggers +2 banked Edge instead of initiative.
- Tests for each class.

Done when: all three weapons playable end-to-end against gorzog.

## Phase 3: UI port (parallel with Phase 2)

Goal: combat playable in the React web client.

Build:
- Port `combat_screen.html` to a React component reading server combat state.
- Add the intent preview banner (load-bearing per pivot doc, missing from the mockup).
- Add weapon-class action surfaces: stance toggle, momentum counter, edge counter.
- Wire to a server endpoint exposing the combat state machine.
- Resume UX: a session whose `ActiveCombat != null` drops straight into the combat screen.

Done when: a player can fight gorzog in the browser, close the tab mid-fight, reopen, and finish the fight.

## Phase 4: Content authoring

Goal: 18 `.cmb` files authored, one per monster sprite.

Process:
- Write a one-page `.cmb` authoring guide *after* gorzog runs through the integrated engine, so it captures format issues invisible from the prototype.
- LLM-drafted skeletons grouped by biome batch (Plains, Scrub, Swamp, Forest).
- Hand-tune to tier baselines from `combat_pivot.md` (HP 24/30/40, AC 11/11/12, Atk +4/+5/+5, basic 1d4, heavy 2d8/2d8/2d10).
- T3 bosses (plains_boss, scrub_boss, swamp_boss) get distinct moves and higher numbers.
- T2 (15 monsters) share the tier baseline with flavor variation in moves.

Done when: all 18 sprites have a corresponding `.cmb` that runs through the engine.

## Phase 5: Sim and tuning

Goal: weapon classes balanced, fatality rates match the locked baselines.

Build:
- Evolve `tools/combat-prototype/` into a batch sim: PC profile x monster stat block matrix, output fatality rates and average rounds.
- Primary target: find the dagger DoT-injured proc rate that puts dagger DPS within ~10% of sword/axe across T1/T2/T3.
- Secondary: confirm fatality rates match the pivot-doc baselines (even-match <10%, overmatched 40-70% at T1/T2, overmatched tourist ~91% at T3).
- Adjust monster stat blocks to hit baselines; do not adjust the design.

Done when: sim output matches the locked tuning baseline.

## Still open at landing time

- Crit/fumble effects beyond double damage / miss
- Fled-encounter win/loss verb semantics
