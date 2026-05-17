---
kind: plan
title: Landing plan — skill tiers + inventory consolidation + .enc picker + arc leveling
state: active
created: 2026-05-16
updated: 2026-05-17
related:
  - skill_tier_rework.md
  - inventory_consolidation.md
  - inventory_slot_refactor.md
  - picker_check_chain_semantics.md
  - arc_leveling.md
  - condition_rework.md
touches:
  features: [skills, inventory, encounters, leveling, ui]
---

# Landing Plan: Skills + Inventory + .enc + Arc Leveling

A phased rollout for the bundle of pending design plans listed in `related:`. The
goal is to land all of it without ever leaving the tree red. Each phase ends with
`dotnet test Dreamlands.sln` green, `text/encounter-tool/Encounter.sln` building,
and `ui/web` typechecking. Runtime correctness lags inside phases — broken content
or half-wired flows are acceptable as long as the build is clean.

This is a coordination doc, not a design doc. Per-phase design lives in the
related plans.

## Cross-cutting principles

- **Compile/test green at every phase boundary.** Tests get rewritten in the same
  phase as the engine change that breaks them.
- **Save-format breakage is acceptable** during this work — wipe sessions between
  phases. Migration deferred until live players exist (see
  [[skill_tier_rework]] §8).
- **Content lag is OK.** Parser keeps old DC syntax parseable through Phase 3 so
  the corpus sweep can be incremental.
- **No tactical-lib work here** — its removal is a separate follow-up plan.

---

## Phase 1 — Data-model spine

Change types once so downstream code compiles against the new shape. Behavior
changes are limited to what the type changes force.

- `lib/Rules/CharacterBalance.cs`: `StartingPackSlots = 5 → 8` (skip the
  intermediate value from [[inventory_slot_refactor]] — we know we're going to 8).
- `lib/Rules/ItemInstance.cs`: add `bool IsEquipped`.
- `lib/Rules/ItemDef.cs`: remove `ItemType.Token`; delete 8 token defs; add
  `medical_kit` (Tool); remove `bandage` def.
- `lib/Rules/SkillTier.cs` (new): `Untrained | Trained | Expert`.
- `lib/Game/PlayerState.cs`:
  - `Skills` becomes `Dictionary<SkillId, SkillTier>`.
  - Remove `Haversack`, `HaversackCapacity`.
  - Remove `Equipment` field; add computed `EquippedWeapon/Armor/Boots`
    helpers scanning `Pack` for `IsEquipped == true` by type.
- Delete `lib/Game/EquippedGear.cs`.

**Test impact**: any `Game.Tests` site constructing `PlayerState`, asserting
`Skills[..] == 3`, or touching `Haversack` breaks. Translate in-phase to tiers
and pack-only.

**Phase exit**: build + tests green; many engine paths still reference deleted
APIs through type-shaped shims — those land in Phase 2.

## Phase 2 — Engine mechanics under new schema

- `lib/Game/Mechanics.cs`:
  - `ApplyEquip` / `ApplyUnequip` become flag flips with mutual-exclusion sweep.
  - Token-related verbs removed.
  - Add `meets <skill> <tier>` evaluator stub (wired by parser in Phase 3).
- `lib/Game/SkillChecks.cs` → rename to `SkillResolution.cs`:
  - Delete `Roll()`, `GetTokenBonus`, `GetTokenResist`.
  - Add `ResolvePicker(tier, pick, correct, wrong) → (Outcome, ConnectorKind)`.
  - Add `RollPassiveResist(tier)` (Cunning serious-condition resist, Bushcraft
    travel-condition resist) — 0/40/80% by tier.
- `lib/Game/EndOfDay.cs`:
  - Drop d20 foraging path.
  - Food cadence by Bushcraft tier (Untrained nightly; Trained/Expert
    every-other-night via a `DaysSinceAte` counter).
  - Medical kits cure-without-consume; bandage consume path deleted.
  - Spirits economy per [[condition_rework]] and [[skill_tier_rework]].
- `lib/Game/Market.cs`, `lib/Game/Bank.cs`:
  - Pack-only; haversack tabs and capacity logic gone.
  - Auto-equip on buy (if no equipped of that type); clear flag on sell/deposit.
- `lib/Game/Conditions.cs`:
  - Add `meets <skill> <tier>` predicate evaluator.
  - Old `check <skill> <DC>` evaluator deleted; runner owns picker dispatch
    starting Phase 5.

Tests get rewritten alongside (Equip/EndOfDay/Market/Bank cohorts). Picker
resolution gets a tight new unit-test file.

## Phase 3 — .enc parser & bundle shape

- `lib/Encounter/` parser:
  - Recognize `check <skill> correct:X wrong:Y` attribute shape.
  - Keep `check <skill> <difficulty>` parseable with a deprecation diagnostic
    (so Phase 4 can land incrementally).
  - Enforce terminal-check rule per [[picker_check_chain_semantics]]; parser
    test for the four illegal shapes.
  - Add `meets` to condition vocabulary (`ArgType.SkillTier`); allow in
    `[requires]`.
- Bundle JSON: tag terminal picker-check branches (`branchKind: "picker"` or
  similar) so the runner knows to render UI.
- `text/encounter-tool/EncounterCli/`: `check` command surfaces the new
  diagnostics; `bundle` emits the new shape.
- Spec docs (`project/encounter-spec/format.md`,
  `project/encounter-spec/mechanics_reference.md`) updated in this phase to
  keep source-of-truth current.

## Phase 4 — Content sweep

Mechanical pass across `text/encounters/`. Build stays green because Phase 3
left old DC syntax parseable.

- `check <skill> <difficulty>` → either picker form (`correct:X wrong:Y`) or
  gate (`[requires meets <skill> <tier>]`). Per-site authoring judgment.
- Strip token rewards (`+item ivory_comb`, etc.).
- Background/intro encounter: drop the 8-point skill allocation; pivot to
  identity-flavored starter state per [[skill_tier_rework]] §B.
- Verify the two `@elif`-chain sites in `arcs/forest/the_fugitive/` already
  conform per [[picker_check_chain_semantics]] §Mareen.enc impact.

**Phase exit**: drop the deprecated DC parse path; `bundle` hard-fails on
stragglers.

## Phase 5 — Orchestration & server

- `lib/Orchestration/EncounterRunner.cs`:
  - Terminal-check branch suspends on a new `EncounterRequest.PickApproach`;
    resume with player's pick.
  - Emit connector line per [[skill_tier_rework]] connector table.
  - Fire success body or `@else` fail body.
  - **Risk**: assumes RPS combat already paved a suspend/resume pattern. If
    not, this phase grows.
- `lib/Orchestration/SettlementRunner.cs`, `lib/Orchestration/Rations.cs`:
  drop haversack.
- `server/GameServer/GameResponse.cs`:
  - `InventoryInfo.Haversack` removed.
  - `EquipmentInfo` becomes a derived view computed from pack `IsEquipped`.
  - `ItemInfo.IsEquipped` added.
  - New payload for picker prompts (skill, three verbs, icons).

## Phase 6 — Frontend

- `ui/web/src/api/types.ts`: mirror server; add picker-prompt type.
- `Inventory.tsx`: flat sorted list with group headers per
  [[inventory_consolidation]] §5; equipped badge; in-place equip/unequip
  buttons.
- `Market.tsx`: redo `projectedEquipment` against the `IsEquipped` flag.
- `Bank.tsx`: derive equipped section from `pack.filter(i => i.isEquipped)`.
- New `<ApproachPicker />` component shown by the encounter screen when the
  runner suspends on a picker prompt. Approach roster as a TS const mirroring
  the C# table.

## Phase 7 — Arc-completion leveling

Greenfield; smallest surface of the bundle. Design lives in [[arc_leveling]].

- `lib/Game/PlayerState.cs`: `ArcCompletions` set; `PendingArcRewards` queue.
- `lib/Rules/ArcRewards.cs`: tableau definition and caps (skill ≤ 2 picks
  maps onto tiers; health/inv ≤ 2).
- `lib/Orchestration/ArcCompletionFlow.cs`: handler for `+arc_complete <id>`
  queues a pending tableau pick.
- `lib/Game/Mechanics.cs`: handle `+arc_complete` and
  `+grant_arc_reward <kind>` verbs.
- Server: surface pending rewards in `GameResponse`.
- `ui/web/src/screens/Tableau.tsx` (new): entry via home-screen badge +
  auto-prompt on arc resolution.
- Equipment-arc rewards (3 weapons / 3 armors / 1 boots / 2 fixed inv) wire
  directly via `+item` and `+upgrade_pack` — no tableau pick.

Independent enough to slot between Phases 5/6/8.

## Phase 8 — Cleanup

- Delete Luck items, Mercantile skill, `SkillUses` field, lingering
  `EquippedGear` refs.
- Drop the deprecated DC parse path (already gated at Phase 4 exit; this is
  the final removal).
- Update `MEMORY.md` (skill model, no haversack, no tokens, arc leveling).
- Flip `state:` on the related plans to `shipped` as their pieces land.

---

## Sequencing

```
1 → 2 → 3 → 4
            └→ 5 → 6 → 8
               └→ 7 ──┘
```

Phases 1–3 are a hard chain (types → engine → format). Phase 4 only needs 3.
Phase 5 needs 2 + 3. Phase 6 needs 5. Phase 7 is mostly independent.

## Risks

1. **Runner suspend/resume in Phase 5**: assumed paved by RPS combat. If not,
   Phase 5 grows.
2. **Phase 4 is judgment-heavy** (picker vs gate per site). Probably wants a
   review pass with the user before bulk edits.
3. **Background encounter rewrite** in Phase 4 hides real design work
   ("identity-flavored starter state" is not yet specified).
4. **Connector phrasings**: a thin set will get old fast. Authoring + playtest
   pass before content lock.
5. **Tactical lib** still compiles but is dead weight; deferred to its own
   plan.
