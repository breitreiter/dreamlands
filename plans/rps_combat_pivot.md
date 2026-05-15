---
kind: plan
title: RPS Combat Pivot — Implementation Plan
state: shipped
created: 2026-05-02
updated: 2026-05-15
status: current
touches:
  files:
    - lib/Combat/
    - lib/Encounter/Move.cs
    - lib/Encounter/CombatEncounter.cs
    - lib/Encounter/CmbParser.cs
    - lib/Game/CombatState.cs
    - lib/Game/CombatPlayerProfile.cs
    - lib/Orchestration/CombatOrchestrator.cs
    - server/GameServer/GameFunctions.cs
    - ui/web/src/screens/Combat.tsx
    - ui/cli/Program.cs
    - lib/Rules/ItemDef.cs
  features: [combat, rps, pivot]
supersedes: [d20_combat_attempt]
provenance:
  author: migration:M-001
---

> Migration note (M-001, 2026-05-15): The d20 combat system was hard-cut and
> replaced with the three-slot RPS prediction game described here. All ten
> phases shipped — engine, format, orchestration, server, UI, CLI, gear
> rebalance, .fight rewrites, .enc DC sweep, and cleanup. Lives in
> `lib/Combat/` and `lib/Encounter/Move.cs`. The mechanical spec referenced
> as `super_rps.md` is now at [[super_rps]]. The d20 attempt this superseded
> is preserved at [[d20_combat_attempt]] for historical context. Bugs list
> at the bottom may or may not still apply — verify against current state.

This document contains the plan to implement the new RPS combat system.

The mechanical spec for the system itself lives in `project/design/super_rps.md`. The working prototype is `experiments/triple-action/`; the implementation strategy below is "promote the prototype, then rip out d20."

# Key decisions

## The Combat skill
Combat flattens to a gear unlock.
- 0: daggers, light armor
- 2: axes, medium armor
- 4: swords, heavy armor

This returns the sword to its prior role as premier martial weapon. We balance by quality of gear moves. This also retains advancement within a gear type.

Weapons no longer contribute a bonus to the Combat skill.

We will adjust all existing enc files to have an appropriate DC, given that the effective cap is now +6.

We will adjust the Lucky Buckle token to grant +2 Combat. This will allow non-combatants to unlock axes/medium armor and non-specialists to unlock swords/heavy armor. This will also allow anyone to hit a +2 skill level gate. This means the max skill level gate for core content (like arcs) is now +2.

This means that if someone's Combat drops below the threshold for an equipped weapon, we may need to handle that. I think we can add this to the TODO backlog, and just let people cheat equipment for now.

## Itemdefs
We will be thinning out equipment (weapons and armor) which no longer have a mechanically interesting identity. This is fine.

We'll also be removing irrelevant features (damage rating, +combat bonus, AC). We will retain +Cunning and various condition resists.

We should add a to-do for checking in with light armor.
We should add a to-do for reviewing whether we need to tackle armor resisting RPS conditions (stunned, berzerk, fear)

## Combat screen
For now, we'll just hack in whatever UX make sense. In parallel, we'll be refining the design.

## .fight encounters
Should remains largely intact, with the obvious caveats:
- Timers are gone
- AC/damage/attacks now gone
- Monsters now have descriptive move pools

All current .fight encounters will need to be minimally rewritten, though they should now be quite compact

Move pools will need appropriate text for the monster. We may need multiple text for each attack.

We will want to retain the current debugging fight launcher. It is tremendously useful.

# Strategy

The d20 system is fully built and shipped (engine, parser, server, UI, CLI, tests, 19 `.fight` files). It has no users in production except us. We are doing a hard cut, not a phased coexistence — there's no gain to running both engines simultaneously, and the surface area to keep typed-through is large.

The shape of the cut:

1. **Promote the prototype** into `lib/Combat` as the new engine, replacing the d20 resolver outright. The prototype's data model (Move = base + mutators, three slots per turn, slot-by-slot resolution) becomes the canonical engine.
2. **Replace the `.fight` format** with an RPS-shaped one. Old format dies; new parser reads new format only. No back-compat.
3. **Rewire orchestration, server, web, CLI** to the new engine in lockstep. There is no "both engines selectable" state.
4. **Rewrite the 19 existing `.fight` files** to the new format and the tier shapes in `super_rps.md`.
5. **Cut player gear** down to the proposed movesets; gate weapons/armor on Combat skill thresholds.
6. **Recalibrate `.enc` DCs** for the new effective Combat cap (+6 → +2 for any reachable gating check after the bonus removal).
7. **Cleanup**: delete `SwordStance`, `TimingBand`, `Bushcraft` surprise, `Cunning` save-vs-pierce, dagger-reflex docs, and adjacent dead code.

Tactical encounters (`.tac`, `lib/Tactical`, `Dreamlands.Tactical.Tests`) are out of scope here. They were already slated for removal per `project_combat_pivot.md` memory; we'll close them out in a separate pass so this work stays focused.

# Files affected

A complete inventory of what changes, gets replaced, or gets deleted:

**Engine — replaced**
- `lib/Combat/Resolver.cs` — d20 attack/save/damage rolls. Replace with slot resolver from `experiments/triple-action/Combat.cs` (`Resolver.Resolve`, `Tells.For`).
- `lib/Combat/CombatRunner.cs` — Begin/Step around d20. Replace with Begin/Step around three-slot commitments.
- `lib/Combat/CombatEvent.cs` — events shaped around `AttackOutcome`/`SaveOutcome`/`DamageResult`. Replace with events shaped around per-slot resolutions, tells, plan reveals.
- `lib/Combat/PlayerCombatAction.cs` — `Attack`, `DaggerAttack`, `SetStance`, `Flee`. Replace with `Commit(slot1, slot2, slot3)` + `Flee` (still TBD: see open questions).
- `lib/Combat/StanceModifiers.cs` — delete (sword stance is gone).
- `lib/Game/CombatState.cs` — d20 state (`MonsterAc`, `Cooldowns` keyed by move id, `MonsterAcBonusThisTurn`, `SkipNextMonsterTurn`, `Stance`). Replace with RPS state (per-side carry-stun arrays, last-used turn per move, `ReadActiveNextTurn`, pending Berzerk/Fear flags).
- `lib/Game/CombatPlayerProfile.cs` — `BaseAc`, `AttackBonus`, `DamageBonus`, `DamageDieCount`, `DamageDieSize`. Replace with a derived player move pool + max HP.
- `lib/Game/SwordStance.cs` — delete.
- `lib/Orchestration/CombatOrchestrator.cs` — keep the orchestration shape (Begin/Step, Finalize-applies-mechanics, session mode transitions); rewrite the body to call the new resolver.

**Format — replaced**
- `lib/Encounter/CombatEncounter.cs` — `MonsterStats(hp, ac, to_hit, damage)`, `Hitbox`, `IntentClass` enum, `MonsterMove` with `Timer`/`Mechanics`. Replace with `MonsterStats(hp)`, `MonsterMove` carrying base + mutators + narration variants. Drop `Hitbox` (not used by the RPS UI; revisit if hit-region targeting comes back).
- `lib/Encounter/CmbParser.cs` — token parser for the d20 format. Rewrite for the new format.
- `lib/Encounter/CombatBundle.cs` — bundle stays; it just loads new-format files.

**Server — touched**
- `server/GameServer/GameFunctions.cs` (lines ~2060–2300) — `CombatBegin`, `CombatAction`, `BuildCombatResponse`, `BuildCombatInfo`, `BuildCombatEventLog`, `BuildMonsterAttackNarrative`, `BuildCombatLogEntry`, `BuildCombatBiomeImage`. Rewire DTOs, drop `stance`/`band` action paths, add `slots` action.
- `server/GameServer/GameData.cs` — references to `CombatBundle`, `ActiveCombat` carry through unchanged shape but the inner types change.
- DTOs: `CombatInfo`, `CombatIntentInfo`, `CombatLogEntry`, `CombatNarrationInfo`, `CombatBeginRequest`, `CombatActionRequest`, `CombatEncounterSummary`, `CombatListResponse`. Rewrite to carry: monster move pool (visible to player), tell, optional plan, player available moves with cooldown state, per-slot resolution log, current-turn carry-stun flags.

**Web — touched**
- `ui/web/src/screens/Combat.tsx` — full rewrite. Three-slot picker (one keypress per slot), tell banner, optional plan banner (when Read was active last turn), per-slot resolution timeline, monster + player move pool with cooldown state, hit/miss animations per slot. Drop `SwordControls`, `DaggerControls`, `DefaultControls`, `intentVerb`.
- `ui/web/src/components/CombatPicker.tsx` — keep as-is, just update the summary fields it shows (hp only; no AC).
- `ui/web/src/components/HitLens.tsx`, `HitSplat.tsx`, `MissMoon.tsx`, `DieRoll.tsx` — keep. Hit/miss anim still applies per attack-vs-attack/defend slot. `DieRoll` may go unused in combat (no rolls); leave it for skill checks elsewhere.
- `ui/web/src/api/types.ts` — DTOs above.

**CLI — touched**
- `ui/cli/Program.cs` — replace `combat attack` / `combat dagger` / `combat stance` with `combat commit <slot1> <slot2> <slot3>`. Keep `combat begin <id>`, `combat flee`, `combat list`.
- `ui/cli/GameClient.cs`, `Session.cs` — DTO updates only.

**Content — rewrite**
- `tools/combat-prototype/Monsters/**/*.fight` (19 files) — rewrite to the new format using the tier shapes from `super_rps.md` § Monster Move Philosophy. Stay wherever the testing loop is fastest for now; canonical relocation (to `text/encounters/combat/<biome>/tier<n>/` or `worlds/<name>/combat/`) is deferred until encounters are tuned.
- `lib/Rules/ItemDef.cs` — drop `WeaponClass`+`SkillModifiers[Combat]` from weapons; replace `TacticalCards` with `RpsMoves` (or equivalent); thin the catalogue to match `super_rps.md` § Item Movesets; retain `+Cunning` and resists; add `RequiredCombat` gate (0/2/4).
- `text/encounters/**/*.enc` — sweep DCs that gate on Combat. Cap is now +6 (skill 4 + Lucky Buckle 2), no weapon bonus. Most checks will need DC reduction. Use `text/encounter-tool/EncounterCli check` to verify after.
- Lucky Buckle token in `ItemDef.cs` — change to `+2 Combat`.

**Tests — replaced**
- `tests/Dreamlands.Combat.Tests/CmbParserTests.cs` — rewrite for new format.
- `tests/Dreamlands.Combat.Tests/ResolverTests.cs` — rewrite for slot-based resolver.
- `tests/Dreamlands.Combat.Tests/CombatRunnerTests.cs` — rewrite for Begin/Step on the new state shape.
- `tests/Dreamlands.Tactical.Tests/*` — keep for now; tactical removal is its own pass.

**Memory / docs**
- `project/design/dagger_reflex_minigame.md` — obsolete. Mark as superseded or delete.
- `project/design/combat_pivot.md` (if present) — superseded by `super_rps.md` + this plan.
- Memory entries `project_dagger_reflex_minigame.md`, `project_combat_attack_visuals.md`, `project_encounter_tuning_baseline.md` — review and update. Most claims about d20 mechanics are now stale.

**Carries through unchanged**
- `lib/Game/Rescue.cs` — chapterhouse rescue on player death. Still triggered by `playerDied` from the orchestrator.
- `lib/Game/Mechanics.cs`, `MechanicResult.cs` — `WinMechanics`/`LoseMechanics` still applied through the same pipeline on resolution.
- The `Pack`/`Haversack`/`Equipment` model — combat reads equipment to derive the player move pool but doesn't mutate inventory.
- The vignette/sprite asset paths and `BloodColor` — kept on the new `CombatEncounter`.

# Implementation phases

Order matters because the engine's data shape is the keystone. Each phase ends with a green build (`dotnet build Dreamlands.sln`) and, where applicable, green tests.

## Phase 1 — Promote the prototype to `lib/Combat`

Move the prototype's resolver and data model into `Dreamlands.Combat` and `Dreamlands.Game`, replacing the d20 implementations. Don't wire it to the orchestrator yet; just get types compiling.

- Replace `lib/Combat/Resolver.cs` with slot-resolution logic adapted from `experiments/triple-action/Combat.cs`. Keep namespace `Dreamlands.Combat`.
- Replace `lib/Combat/CombatEvent.cs` with new events:
  - `Intro(encounterId, title, text)` — keep.
  - `TurnStarted(turn, plan?)` — replaces `RoundStarted` + `IntentPreviewed`. `plan` populated only when player Read'd last turn.
  - `TellEmitted(text)` — derived from monster commitment per `super_rps.md` § Tell Logic.
  - `SlotResolved(slot, playerMove, monsterMove, playerDelta, monsterDelta, playerStunNext, monsterStunNext, conditionsApplied)` — one per slot, slot 1..3.
  - `PlayerFleeAttempted(success)` and `Outcome(...)` — keep, but drop the d20 SaveOutcome.
- Replace `lib/Combat/PlayerCombatAction.cs` with `Commit(Move slot1, Move slot2, Move slot3)` + `Flee`. `Move` is `(string Base, IReadOnlySet<string> Mutators)`; copy the prototype's record. Flee semantics: burn the entire turn — monster's three-slot commitment resolves against player slots that act as `Skipped`, then if the player is still alive the encounter ends with `PlayerFled = true` and the encounter goes back into the pool (no `+repool false` lockout).
- Delete `lib/Combat/StanceModifiers.cs`.
- Replace `lib/Game/CombatState.cs` fields:
  - Drop: `MonsterAc`, `Cooldowns` (rename — see below), `NextMoveId`, `MonsterAcBonusThisTurn`, `SkipNextMonsterTurn`, `Stance`.
  - Add: `Turn` (replaces `Round`), `PlayerCarryStun: bool[3]`, `MonsterCarryStun: bool[3]`, `PlayerLastUsedTurn: Dictionary<string,int>`, `MonsterLastUsedTurn: Dictionary<string,int>` (keyed by move encoding), `PlayerReadActiveNextTurn: bool`, `PlayerBerzerkNextTurn: bool`, `PlayerFearNextTurn: bool`, `MonsterBerzerkNextTurn: bool`, `MonsterFearNextTurn: bool`.
  - Keep: `EncounterId`, `MonsterHp`/`MonsterMaxHp`, `Profile`, terminal flags.
- Replace `lib/Game/CombatPlayerProfile.cs`:
  - Drop: `BaseAc`, `AttackBonus`, `DamageBonus`, `DamageDieCount`, `DamageDieSize`.
  - Add: `MovePool: IReadOnlyList<Move>`.
  - `From(...)` becomes "look up weapon + armor itemdefs, compose move pool per `super_rps.md` § Item Movesets, plus universal Read." No `MaxHp` field — combat reads `player.Health`/`MaxHealth` and `player.Spirits`/`MaxSpirits` directly.
  - Drop `Bushcraft` and `Cunning` from the profile — neither has a combat role anymore.
- Delete `lib/Game/SwordStance.cs`.
- Rewrite `lib/Combat/CombatRunner.cs` Begin/Step around the new state. Begin: roll initial AI commitment, emit Intro + Tell + (no Plan since Read isn't active turn 1). No surprise check — both sides commit simultaneously. Step: take three player commitments, compute three SlotResolved events, apply damage through `ApplyDamage(player, n)` (Spirits-then-Health, ported verbatim from the d20 `CombatRunner.cs`), apply forward-rider stuns, set `ReadActiveNextTurn` from this turn's player commitment, roll AI commitment for next turn, set up next-turn carry-stuns. Recover heals Spirits first up to `MaxSpirits`; overflow does not refill Health.

After this phase, the new types compile but nothing wires them — the orchestrator still references the old shape. That's expected; orchestrator gets rewritten in Phase 3.

**Note:** the prototype uses `rare`/`mythic` mutators for "once per turn / once every other turn" cooldowns. Mirror that — no separate `Timer` field. The state's `LastUsedTurn` map is the cooldown.

## Phase 2 — New `.fight` format

Design and parse the new format. Old format dies; we don't keep both.

- Update `lib/Encounter/CombatEncounter.cs`:
  - `MonsterStats { int Hp }` (single field; no AC, no to-hit, no damage dice).
  - Drop `Hitbox`, `IntentClass`, `MoveMechanic`.
  - `MonsterMove` becomes `{ Move Move, string Narration, IReadOnlyList<string> NarrationVariants? }`. Move pools allow duplicates: a monster can have two `Attack` entries with different narration to give variety.
  - Keep `Image`, `BloodColor`, `Repool`, `Intro`, `WinText`, `LoseText`, `WinMechanics`, `LoseMechanics`, `Tier`, `Category`.
- Rewrite `lib/Encounter/CmbParser.cs` for the new format. Proposed shape:
  ```
  +title Some Goblin
  +image foo/bar.webp
  +blood #7a0a0a
  +stats hp=18

  +move Big Telegraphed Rare Attack
    narration: It winds back, hauling the maul over its head.
    narration: It snarls and lifts the spike to shoulder height.

  +move Defend
    narration: It hunches behind its shield.

  +intro
    A goblin steps from the brush.

  +win
    The goblin slumps.
    > gold 8
    > tag killed_goblin

  +lose
    Everything goes black.
  ```
  - The first token of `+move` line through the last is parsed as `Move.Parse` (last token = base, rest = mutators). Reject unknowns at parse time.
  - Multiple `narration:` lines on one move = variants; resolver picks one randomly per use.
- `lib/Encounter/CombatBundle.cs` stays.
- `text/encounter-tool/EncounterCli check` should validate `.fight` files alongside `.enc`. Add a `--combat` mode if it doesn't already.

## Phase 3 — Orchestration + server API

Wire the new engine end-to-end so the server speaks RPS.

- Rewrite `lib/Orchestration/CombatOrchestrator.cs`:
  - `Begin(session, encounterId)` — same shape; internally builds the new `CombatPlayerProfile` (player move pool + max HP) and calls new `CombatRunner.Begin`.
  - `Step(session, action)` — `action` is now `Commit` or `Flee`. Flee resolves the monster's normal three-slot turn against an all-Skipped player commit, sets `PlayerFled`, and finalizes — no encounter lockout (the encounter repools).
  - `Finalize` keeps the same mechanics-application logic — just feed it the new event stream.
- `server/GameServer/GameFunctions.cs`:
  - `CombatBegin` — unchanged externally.
  - `CombatAction` — accept new request body `{ "action": "commit", "slots": ["Big Attack", "Defend", "Read"] }` or `{ "action": "flee" }`. Drop `stance`, `band`. Parse each slot string with the same `Move.Parse` the engine uses.
  - `CombatList` — return summaries with HP only.
  - `BuildCombatInfo` — populate:
    - Monster name, intro, image, blood color (unchanged).
    - Monster HP / max HP.
    - Player Health / max Health and Spirits / max Spirits — the UI shows both. Damage ablates Spirits first, then Health.
    - Tell text (current turn).
    - Plan (only when Read was active for this turn).
    - Player move pool with per-move availability flags (`onCooldown` for `rare`/`mythic`).
    - Carry-stun flags for each of the three player slots (so the UI can disable them).
    - Per-turn resolution log: list of per-slot resolutions with the move encodings on both sides and the HP deltas.
  - Drop `BuildMonsterAttackNarrative` (no more "rolled, hit/missed" fold). Each slot's narration is just the monster's chosen move's narration, surfaced verbatim; the verdict is the HP delta.
- DTOs: rewrite `CombatInfo`, `CombatLogEntry`, `CombatActionRequest`. Leave `CombatBeginRequest` and `CombatEncounterSummary` mostly intact (drop `Ac` from the summary).

After Phase 3, the server compiles and responds to combat requests with new-shape JSON. The web UI will be broken until Phase 4.

## Phase 4 — Web UI

Rewrite `ui/web/src/screens/Combat.tsx` against the new DTOs.

- Three-slot picker. Each slot: dropdown or hotkey-numbered list of player's currently-available moves (filter out on-cooldown / stunned). Match the prototype's "single keypress per slot" rhythm — let `1-9` pick from the list.
- Display panel:
  - Monster nameplate with HP bar (existing).
  - Turn counter, tell banner.
  - Plan banner — visible only when last turn the player committed Read.
  - Player vitals: both Spirits and Health bars (combat damage ablates Spirits first, then Health).
  - Active conditions on player (from `ActiveConditions` — Stunned/Berzerk/Fear are RPS-internal flags, not in `ActiveConditions`, so don't double-render).
- After commit: animate the three-slot reveal. For each slot, show both moves and the deltas. Existing `HitLens` / `HitSplat` / `MissMoon` components anchor on the monster sprite per slot — reuse them. Each player-attack slot that lands gets a splat; each that's defended gets a moon.
- Drop `SwordControls`, `DaggerControls`, `DefaultControls`. Don't branch by weapon class — the move pool *is* the weapon-class branching now.
- `CombatPicker.tsx` keeps the same shape, just stops showing AC.
- `api/types.ts` — update DTOs.

The hit-animation system designed for d20 (`project_combat_attack_visuals.md`) carries over with minor bookkeeping: instead of one swing per turn, three slot-aligned swings per turn.

## Phase 5 — CLI

Mirror the web changes minimally — the CLI is for integration testing.

- `ui/cli/Program.cs`: replace `combat attack`, `combat dagger <band>`, `combat stance <s>` with `combat commit "<m1>" "<m2>" "<m3>"`. Keep `combat begin <id>`, `combat list`, `combat flee`.
- `ui/cli/GameClient.cs`: shape the JSON body the new server expects.
- Keep the auto-server-start behaviour.

## Phase 6 — Player gear: movesets, gates, item cull

Translate `super_rps.md` § Player Weapon/Armor Philosophy and § Item Movesets into actual itemdefs.

- `lib/Rules/ItemDef.cs`:
  - Add `IReadOnlyList<Move> RpsMoves` (or `IReadOnlyList<string>` and parse at startup) on weapons and armor.
  - Add `int RequiredCombat` on weapons and armor (0 for daggers/light, 2 for axes/medium, 4 for swords/heavy).
  - Drop `SkillModifiers[Combat]` from all weapons.
  - Drop `TacticalCards` everywhere (with the tactical removal pass — or now, since they're already cosmetically dead).
  - Cull entries marked `drop item` in `super_rps.md`: jambiya, tomahawk, tulwar, hunter's gear, desert scout gear, leather, buff coat, chainmail.
  - Keep `+Cunning` modifiers on armor where present; keep `ResistModifiers`.
- Update Lucky Buckle to grant `+2 Combat`.
- Player HP: no separate combat HP field. Combat reads `player.Spirits`/`MaxSpirits` and `player.Health`/`MaxHealth` directly. Armor's identity is its move pool and resists, not a stat bump.
- `CombatPlayerProfile.From(...)` reads weapon and armor from `Equipment`, composes `MovePool = weapon.RpsMoves + armor.RpsMoves + [Read]` (Read is universally available to the player only).

## Phase 7 — Rewrite the 19 `.fight` files

Walk `tools/combat-prototype/Monsters/**/*.fight` and rewrite each to the new format. For each:

1. Identify tier from path (`tier1` / `tier2` / `tier3`).
2. Pick a monster shape from `super_rps.md` § Monster Move Philosophy (basic striker / duelist / tank, legendary striker / duelist / tank).
3. Set HP from that shape.
4. Compose move pool per the shape.
5. Carry over the existing `+intro`, `+win`, `+lose` blocks plus their `> mechanic` lines.
6. Convert each old `+move` block's `narration:` into one or more `narration:` lines on a new `+move <Move encoding>` block.

Decide whether to relocate from `tools/combat-prototype/Monsters/` into `text/encounters/combat/<biome>/tier<n>/` (mirrors the `.enc` layout). The bundle loader currently reads from `worlds/<name>/combat/` with a fallback to `tools/combat-prototype/Monsters/` (per the picker comment). Either keep the fallback or move all 19 files; either is fine, just be consistent.

## Phase 8 — Recalibrate `.enc` DCs

Effective Combat cap was previously `skill 4 + weapon mod up to +5 = +9`. New cap is `skill 4 + Lucky Buckle 2 = +6`, and the user note caps "core content" at the `+2` gate.

- Run `EncounterCli check text/encounters` to inventory all `check Combat <dc>` calls.
- Sweep each: if the prior DC assumed a high weapon bonus, drop it. Use the table in `lib/Rules/Difficulty.cs` (or wherever DCs are categorized) to keep "Easy/Medium/Hard" consistent against the new cap.
- The user's note: max gate for core content is now `+2` after the Lucky Buckle assumption.

## Phase 9 — Tests

Rewrite `tests/Dreamlands.Combat.Tests/`:
- `CmbParserTests` — parse round-trip a sample `.fight`, reject unknown mutators, reject unknown bases.
- `ResolverTests` — port the prototype's mental model into focused asserts: Attack vs Defend, Attack vs Recover (cancel + stun), Riposte vs Attack, Big damage, Wary Recover converts, Stunning attacks proc forward-stun, Shielding Defend blocks statuses, Exhausting self-stuns. Use seeded RNG for proc-based mutators.
- `CombatRunnerTests` — Begin → Step → resolves; carry-stun lockouts a slot on next turn; Read shows plan next turn; Flee outcome.

## Phase 10 — Cleanup

Once the new system is green end-to-end:

- Delete `tools/combat-prototype/` (or archive). Keep only the rewritten `.fight` files in their final home.
- Delete `experiments/triple-action/` once nothing references it.
- Delete `project/design/dagger_reflex_minigame.md`. Mark `project_dagger_reflex_minigame.md` memory as obsolete (or remove).
- Update CLAUDE.md auto-memory entries that reference d20 specifics. The big ones: `project_combat_pivot.md`, `project_combat_attack_visuals.md`, `project_encounter_tuning_baseline.md`, `project_cunning_damage_saves.md`. The Cunning-as-damage-save claim is now moot inside combat; keep it for non-combat saves.
- Remove `Bushcraft` surprise-check usage. Bushcraft survives elsewhere (foraging, terrain).
- `CombatPlayerProfile.Cunning` and `Bushcraft` are dropped in Phase 1; this is just the prose-doc cleanup pass.

# Resolved decisions

The Q&A below records decisions that shape the phases above; all are folded into the relevant phase text.

1. **Spirits and Health.** Combat keeps the Spirits-then-Health ablation that the d20 system used. PCs still have 4 expensive Health + ~20 cheap Spirits, and combat is the primary vehicle for tying combat back to the survival game. Damage applies to Spirits first, overflow to Health. The Recover action heals — for now, allowed to push Spirits above the level it started combat at, capped at `MaxSpirits`. We'll revisit if it feels too generous in playtest. (No `MaxHp = 24` field — disregard the earlier note in Phase 1/6; combat uses the existing Spirits + Health pools.)

2. **Flee.** Burn all three slots on Flee: the player commits no offensive/defensive action that turn, the monster gets a normal turn against effectively-Skipped slots, then if the player survives they're out. The encounter goes back into the pool on flee — repeated flees are a recurring beat-down cost, not a free escape.

3. **Surprise.** Gone. Both sides always commit simultaneously. No Bushcraft surprise check, no first-actor logic.

4. **`.fight` location.** Defer the move. Whatever path makes the immediate testing/tuning loop fastest is fine; we'll relocate to a canonical home (`text/encounters/combat/<biome>/tier<n>/` or `worlds/<name>/combat/`) once the encounters are stable.

5. **RPS conditions vs global conditions.** RPS conditions (`Stunned` per-slot, `Berzerk`, `Fear`) live as plain bool fields on `CombatState`, fully decoupled from `player.ActiveConditions`. The naming collision with the global `Stunned` condition is acceptable because they never operate at the same scope. The future possibility of armor resisting RPS conditions is acknowledged but out of scope here.

6. **No "Specialists".** That term in `super_rps.md`'s intro is an artifact from an older draft. There are only base moves (`Attack`/`Defend`/`Recover`/`Read`) with optional, stacked mutators — no separate specialist concept to model. Strip the language from `super_rps.md`.

7. **Combat-skill drop below equipment threshold.** Backlog. For now, players can keep equipment they no longer technically qualify for.

# Proposed sequencing and pause points

Suggested order to keep each pause point at a working state:

- Phase 1 (engine) and Phase 2 (format) can land together as "new engine compiles, nothing wired." One PR.
- Phase 3 + 4 + 5 (orchestration + server + web + CLI) must land together — there's no useful pause point in the middle. One PR.
- Phase 6 (gear cull + gate) can land before, alongside, or after Phase 3–5. Cleanest after, so we're not changing two things at once.
- Phase 7 (`.fight` rewrites) is content; can land per-tier or all-at-once. Independent of code phases as long as Phase 2's parser is in.
- Phase 8 (`.enc` DC sweep) is independent and can land any time after Phase 6 changes the Combat cap.
- Phase 9 (tests) lands with each engine-touching phase.
- Phase 10 (cleanup) is the last PR.

A clean three-PR cadence:
1. New engine + new parser + new tests against the engine.
2. Wire-through (orchestrator + server + web + CLI), rewrite all 19 `.fight` files.
3. Player gear cull + Lucky Buckle + `.enc` DC sweep + cleanup.

# Bugs

- Monsters need a "stunned" action description in their .fight file so we can narrate that. Player probably needs one as well.
- Not currently gating weapon equip on skill
- "Big" still in move descs
- Recover-based monsters remain severely unpowered
